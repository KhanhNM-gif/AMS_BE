using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using static Diagram;

namespace WebAPI.Controllers
{
    public class DiagramController : Authentication
    {
        const string EXTENSION_ALLOW = "jpg,jpeg,png";

        [HttpGet]
        public Result GetDiagramDetailByDiagramID(long DiagramID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DataValidator.Validate(new { DiagramID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (!ResultCheckToken.isOk) return ResultCheckToken;

            msg = DoGetDiagramDetailByDiagramID(DiagramID, out DiagramDetail diagramDetail, out List<DiagramPlace> diagramPlaceList);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            object outDiagramDetail = new { Diagram = diagramDetail, ListPlace = diagramPlaceList };

            return outDiagramDetail.ToResultOk();
        }
        private string DoGetDiagramDetailByDiagramID(long DiagramID, out DiagramDetail diagramDetail, out List<DiagramPlace> diagramPlaceList)
        {
            diagramPlaceList = new List<DiagramPlace>();

            string msg = DiagramDetail.GetOneByDiagramID(DiagramID, out diagramDetail);
            if (msg.Length > 0) return msg;
            if (diagramDetail == null) return "Không tồn tại sơ đồ";

            msg = DiagramPlace.GetListByDiagramID(DiagramID, out diagramPlaceList);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpGet]
        public Result GetListDiagram()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.SDTS, Role.ROLE_SDTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            int GET_LIST = 0;
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            msg = GetListByDiagramID(UserToken.AccountID, GET_LIST, out List<Diagram> ListDiagram);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListDiagram.ToResultOk();
        }

        [HttpGet]
        public Result GetDiagramByDiagramID(long DiagramID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DataValidator.Validate(new { DiagramID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (!ResultCheckToken.isOk) return ResultCheckToken;

            msg = GetListByDiagramID(UserToken.AccountID, DiagramID, out List<Diagram> ListDiagram);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListDiagram.ToResultOk();
        }

        [HttpGet]
        public Result GetAssetDetailByDiagramID(long DiagramID, int PlaceID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DataValidator.Validate(new { DiagramID, PlaceID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (!ResultCheckToken.isOk) return ResultCheckToken;

            msg = AssetDetail.GetAssetDetailByDiagramIDAndPlaceID(DiagramID, PlaceID, out List<AssetDetail> ListAssetDetail);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListAssetDetail.ToResultOk();
        }

        [HttpGet]
        public Result GetSuggestSearch(string TextSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetSuggestSearch(TextSearch, out List<Diagram> ListDiagram);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListDiagram.ToResultOk();
        }
        private string DoGetSuggestSearch(string TextSearch, out List<Diagram> ListDiagram)
        {
            ListDiagram = new List<Diagram>();

            string msg = Diagram.GetSuggestSearch(TextSearch, UserToken.AccountID, out ListDiagram);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpPost]
        public Result UploadFile()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoUploadFile(out List<Diagram> outDiagrams);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return outDiagrams.ToResultOk();
        }
        private string DoUploadFile(out List<Diagram> outDiagrams)
        {
            outDiagrams = null;

            string msg = DoUploadFile_UIToObject(out List<string> ltDiagramPathFile, out List<Diagram> ltDiagram);
            if (msg.Length > 0) return msg;

            msg = DoUploadFile_ValidateData(ltDiagram);
            if (msg.Length > 0) { DoRemoveFile(ltDiagramPathFile); return msg; }

            DBM dbm = new DBM();
            dbm.BeginTransac();

            msg = DoUploadFile_ObjectToDB(dbm, ltDiagram, out outDiagrams);
            if (msg.Length > 0)
            {
                dbm.RollBackTransac();
                DoRemoveFile(ltDiagramPathFile);
                return msg;
            }

            dbm.CommitTransac();

            return "";
        }
        private string DoUploadFile_UIToObject(out List<string> ltDiagramPathFile, out List<Diagram> ltDiagram)
        {
            ltDiagramPathFile = new List<string>();
            ltDiagram = new List<Diagram>();

            var httpContext = HttpContext.Current;
            if (httpContext == null) return "httpContext == null";
            if (httpContext.Request == null) return "httpContext.Request == null";
            if (httpContext.Request.Files.Count == 0) return "Không có Sơ đồ nào".ToMessageForUser();

            string msg = BSS.Common.GetSetting("FolderFileDiagram", out string FolderFileDiagram);
            if (msg.Length > 0) return msg;

            FolderFileDiagram = FolderFileDiagram + "\\" + UserToken.AccountID;
            string DiagramPathFile = HttpContext.Current.Server.MapPath(FolderFileDiagram);
            if (!Directory.Exists(DiagramPathFile)) Directory.CreateDirectory(DiagramPathFile);

            for (int i = 0; i < httpContext.Request.Files.Count; i++)
            {
                try
                {
                    HttpPostedFile httpPostedFile = httpContext.Request.Files[i];

                    string DiagramUrl = FolderFileDiagram + "/" + httpPostedFile.FileName;
                    DiagramPathFile = HttpContext.Current.Server.MapPath(DiagramUrl);
                    httpPostedFile.SaveAs(DiagramPathFile);
                    ltDiagramPathFile.Add(DiagramPathFile);

                    string[] arr = httpPostedFile.FileName.Split('.');
                    string DiagramName = arr.FirstOrDefault();

                    Diagram diagram = new Diagram
                    {
                        AccountID = UserToken.AccountID,
                        FileName = httpPostedFile.FileName,
                        DiagramUrl = DiagramUrl,
                        DiagramName = DiagramName
                    };
                    ltDiagram.Add(diagram);
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }

            }

            return "";
        }
        private void DoRemoveFile(List<string> ltDiagramPathFile)
        {
            try
            {
                foreach (string file in ltDiagramPathFile)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Log.ProcessError(ex.Message.ToString());
            }
        }
        private string DoUploadFile_ValidateData(List<Diagram> ltDiagram)
        {
            string[] arrExtemsionAllow = EXTENSION_ALLOW.Split(',');
            foreach (Diagram fa in ltDiagram)
            {
                string msg = DataValidator.Validate(new { fa.DiagramName, fa.DiagramUrl }).ToErrorMessage();
                if (msg.Length > 0) return ("Sơ đồ " + fa.DiagramName + " không hợp lệ: " + msg).ToMessageForUser();

                if (arrExtemsionAllow.Count(v => "." + v == fa.FileExttension.ToLower()) == 0) return ("Hệ thống không cho phép upload file đính kèm có đuôi " + fa.FileExttension).ToMessageForUser();
            }
            return "";
        }
        private string DoUploadFile_ObjectToDB(DBM dbm, List<Diagram> ltDiagram, out List<Diagram> outDiagrams)
        {
            outDiagrams = new List<Diagram>();
            foreach (var item in ltDiagram)
            {
                string msg = item.InsertOrUpdate(dbm, out Diagram diagram);
                if (msg.Length > 0) return msg;
                if (outDiagrams == null) return "không tồn tại sơ đồ";

                Log.WriteHistoryLog(diagram.DiagramID == 0 ? "Thêm mới sơ đồ" : "Sửa sơ đồ", diagram.ObjectGuid, UserToken.UserID);

                outDiagrams.Add(diagram);

            }

            return "";
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoDelete(data, out string DiagramIDs);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "Xóa sơ đồ thành công".ToResultOk();
        }
        private string DoDelete([FromBody] JObject data, out string DiagramIDs)
        {
            DiagramIDs = "";

            string msg = DoValidateData(data, out DiagramIDs);
            if (msg.Length > 0) return msg;

            msg = DoUpdateDiagramToBD(DiagramIDs);
            if (msg.Length > 0) return msg;

            return "";
        }
        private string DoValidateData([FromBody] JObject data, out string strDiagramID)
        {
            strDiagramID = "";

            string msg = data.ToObject("ListObjectGuid", out List<Diagram> ltDiagram);
            if (msg.Length > 0) return msg;
            if (ltDiagram.Count == 0) return "Bạn phải chọn sơ đồ để xóa";

            List<long> diagramIDs = new List<long>();
            foreach (var item in ltDiagram)
            {
                msg = CacheObject.GetDiagramIDByGUID(item.ObjectGuid, out long diagramID);
                if (msg.Length > 0) return msg;

                diagramIDs.Add(diagramID);
            }

            strDiagramID = string.Join(",", diagramIDs.ToArray());

            return "";
        }
        private string DoUpdateDiagramToBD(string diagramIDs)
        {
            string msg = DeleteByDiagramIDs(diagramIDs);
            if (msg.Length > 0) return msg;

            return "";
        }
    }
}