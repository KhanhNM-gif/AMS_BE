using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BSS;
using Newtonsoft.Json.Linq;
using BSS.DataValidator;
using System.Web;
using System.Data;
using OfficeOpenXml;
using System.Configuration;

namespace WebAPI.Controllers
{
    public class ExportExcelController : Authentication
    {
        [HttpGet]
        public Result ExporExcelAccount()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoExporExcelAccount(out string urlFile);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return urlFile.ToResultOk();
        }
        public string DoExporExcelAccount(out string urlFile)
        {
            urlFile = "";
            try
            {
                string msg = AccountUser.GetListUserExport(out DataTable dt);
                if (msg.Length > 0) return msg;

                msg = BSS.Common.GetSetting("FolderFileExport", out string FolderFileExport);
                if (msg.Length > 0) return msg;

                urlFile = FolderFileExport + "/" + "danhsachnguoidung_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

                using (ExcelPackage pack = new ExcelPackage())
                {
                    ExcelWorksheet ws = pack.Workbook.Worksheets.Add("Danh sách người dùng");
                    ws.Cells["A1"].LoadFromDataTable(dt, true);

                    pack.SaveAs(new System.IO.FileInfo(HttpContext.Current.Server.MapPath(urlFile)));
                }
                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        [HttpGet]
        public Result ExporExcelDept()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoExporExcelDept(out string urlFile);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return urlFile.ToResultOk();
        }
        public string DoExporExcelDept(out string urlFile)
        {
            urlFile = "";
            try
            {
                string msg = AccountDept.GetListDeptExport(UserToken.AccountID, out DataTable dt);
                if (msg.Length > 0) return msg;

                msg = BSS.Common.GetSetting("FolderFileExport", out string FolderFileExport);
                if (msg.Length > 0) return msg;

                urlFile = FolderFileExport + "/" + "danhsachphongban_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

                using (ExcelPackage pack = new ExcelPackage())
                {
                    ExcelWorksheet ws = pack.Workbook.Worksheets.Add("Danh sách phòng ban");
                    ws.Cells["A1"].LoadFromDataTable(dt, true);

                    pack.SaveAs(new System.IO.FileInfo(HttpContext.Current.Server.MapPath(urlFile)));
                }
                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [HttpGet]
        public Result ExporExcelPosition()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoExporExcelPosition(out string urlFile);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return urlFile.ToResultOk();
        }
        public string DoExporExcelPosition(out string urlFile)
        {
            urlFile = "";
            try
            {
                string msg = AccountPosition.GetListPositionExport(UserToken.AccountID,out DataTable dt);
                if (msg.Length > 0) return msg;

                msg = BSS.Common.GetSetting("FolderFileExport", out string FolderFileExport);
                if (msg.Length > 0) return msg;

                urlFile = FolderFileExport + "/" + "danhsachchucvu_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

                using (ExcelPackage pack = new ExcelPackage())
                {
                    ExcelWorksheet ws = pack.Workbook.Worksheets.Add("Danh sách chức vụ");
                    ws.Cells["A1"].LoadFromDataTable(dt, true);

                    pack.SaveAs(new System.IO.FileInfo(HttpContext.Current.Server.MapPath(urlFile)));
                }

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }
    }
}