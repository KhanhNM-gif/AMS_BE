using ASM_API.App_Start.FileReport;
using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class AssetHandOverController : Authentication
    {
        [HttpPost]
        public Result SendHandover([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoSendHandover(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoSendHandover(int UserIDCreate, [FromBody] JObject data)
        {
            string msg = data.ToObject("AssetSenderHandOver", out AssetSenderHandOver assetHandOver);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoSendHandover_Validate(assetHandOver);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoSendHandover_ObjectToDB(dbm, assetHandOver, UserIDCreate);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetHandOver DoSendHandover";
            }


            dbm.CommitTransac();

            return msg;
        }
        private string DoSendHandover_ObjectToDB(DBM dbm, AssetSenderHandOver assetHandOver, int UserIDCreate)
        {
            string msg = "";
            string AssetIDs = string.Join(",", assetHandOver.ltAsset.Select(v => v.AssetID));

            msg = AssetHandOver.Insert(dbm, UserToken.UserID, assetHandOver.HandOverDate, assetHandOver.UserIDHandedOver, AssetIDs, assetHandOver.HandOverContent);
            if (msg.Length > 0) return msg;

            msg = Asset.UpdateStatusID_Handover(dbm, AssetIDs, assetHandOver.UserIDHandedOver, 0, Constants.StatusAsset.CXN_BG, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in assetHandOver.ltAsset)
            {
                msg = Log.WriteHistoryLog(dbm, $"Bàn giao tài sản cho {assetHandOver.UserIDHandedOverName}", item.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;

            }
            return msg;
        }
        private string DoSendHandover_Validate(AssetSenderHandOver assetHandOver)
        {
            string msg = DataValidator.Validate(new
            {
                assetHandOver.HandOverContent,
                assetHandOver.UserIDHandedOver,
                assetHandOver.HandOverDate
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (assetHandOver.ltAsset.Count == 0) return ("Bạn chưa chọn Tài sản nào").ToMessageForUser();

            if (string.IsNullOrEmpty(assetHandOver.HandOverContent)) return ("Bạn chưa nhập Nội dung bàn giao").ToMessageForUser();

            msg = AccountUser.GetOneByUserID(assetHandOver.UserIDHandedOver, out AccountUser accountUserHandedOver);
            if (msg.Length > 0) return msg.ToMessageForUser();
            if (accountUserHandedOver == null) return "User ID không tồn tại";
            if (!accountUserHandedOver.IsActive) return "Bàn khoản bàn giao đã bị khóa";
            assetHandOver.UserIDHandedOverName = $"{accountUserHandedOver.FullName} ({accountUserHandedOver.UserName})";

            for (int i = 0; i < assetHandOver.ltAsset.Count; i++)
            {
                var item = assetHandOver.ltAsset[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByGuid(item.ObjectGuid, out Asset o);
                if (msg.Length > 0) return msg;
                assetHandOver.ltAsset[i] = o;

                if (o.AssetStatusID != Constants.StatusAsset.ĐD_TK)
                    return "Bạn chỉ được bàn giao khi tài sản đang ở trạng thái đã duyệt trong kho".ToMessageForUser();

                msg = AssetApprove.GetListByAssetIDs(o.AssetID.ToString(), out List<AssetApprove> ltAssetApprove);
                if (msg.Length > 0) return msg;

                if (ltAssetApprove.Count(v => v.CreateDate > assetHandOver.HandOverDate) > 0) return "Ngày bàn giao tài sản phải lớn hơn ngày duyệt tài sản".ToMessageForUser();
            }

            return msg;
        }

        [HttpPost]
        public Result ComfirmHandOver([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLTS, Role.ROLE_QLTS_SUDUNG);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoConfirmHandOver(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoConfirmHandOver(int UserID, [FromBody] JObject data)
        {
            string msg = data.ToObject("ComfirmHandOver", out ComfirmHandOver assetHand);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoComfirmHandOver_Validate(assetHand);
            if (msg.Length > 0) return msg.ToMessageForUser();

            int StatusID = assetHand.IsHandover ? Constants.StatusAsset.ĐSD : Constants.StatusAsset.ĐD_TK;
            string contentLog = assetHand.IsHandover ? "Xác nhận {0} đã bàn giao tài sản" : "Từ chối {0} bàn giao tài sản. Lý do: " + assetHand.Reason;

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoComfirmHandOver_ObjectToDB(dbm, assetHand, StatusID, contentLog);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetHandOver DoConfirmHandOver";
            }



            dbm.CommitTransac();

            return msg;
        }
        private string DoComfirmHandOver_ObjectToDB(DBM dbm, ComfirmHandOver comfirmHandOver, int StatusID, string contentLog)
        {
            string msg = "";

            string AssetIDs = string.Join(",", comfirmHandOver.ltAsset.Select(v => v.AssetID));

            msg = AssetHandOver.Update(dbm, AssetIDs, comfirmHandOver.IsHandover, comfirmHandOver.Reason, UserToken.UserID, out List<AssetHandOver> outLtAssetHandOver);
            if (msg.Length > 0) return msg;

            msg = Asset.UpdateStatusID_Handover(dbm, AssetIDs, UserToken.UserID, comfirmHandOver.PlaceID, StatusID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in outLtAssetHandOver)
            {
                msg = Log.WriteHistoryLog(dbm, string.Format(contentLog, item.UserHandOverName), item.ObjectGuidAsset, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return "";
        }
        private string DoComfirmHandOver_Validate(ComfirmHandOver assetHandOver)
        {
            string msg = "";

            msg = DataValidator.Validate(new { assetHandOver.Reason, assetHandOver.PlaceID }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (!assetHandOver.IsHandover && string.IsNullOrEmpty(assetHandOver.Reason)) return "Bạn chưa nhập Lý do từ chối".ToMessageForUser();

            if (assetHandOver.IsHandover && assetHandOver.PlaceID == 0) return "Bạn chưa chọn Nơi để Tài sản".ToMessageForUser();

            if (assetHandOver.ltAsset.Count == 0) return ("Bạn chưa chọn Tài sản nào").ToMessageForUser();

            for (int i = 0; i < assetHandOver.ltAsset.Count; i++)
            {
                var item = assetHandOver.ltAsset[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByGuid(item.ObjectGuid, out Asset o);
                if (msg.Length > 0) return msg;
                assetHandOver.ltAsset[i] = o;

                if (o.AssetStatusID != Constants.StatusAsset.CXN_BG)
                    return "Bạn chỉ được Xác nhận bàn giao khi Tài sản ở trạng thái Chờ xác nhận bàn giao".ToMessageForUser();
            }
            return msg;
        }

        [HttpGet]
        public Result ViewDetail(string ObjectGuids)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLTS, Role.ROLE_QLTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = Asset.GetAssetIDsByObjectGuids(ObjectGuids, out string AssetIDs);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = AssetHandOver.GetSearchByAssetIDs(AssetIDs, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }

        [HttpPost]
        public Result ExportAssetHandOver([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoExportAssetHandOver(data, out string url);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return url.ToResultOk();
        }
        private string DoExportAssetHandOver([FromBody] JObject data, out string filePath)
        {
            filePath = "";
            string msg = data.ToObject("AssetSearchResult", out List<AssetSearchResult> assetSearchResultList);
            if (msg.Length > 0) return msg;
            if (assetSearchResultList == null || assetSearchResultList.Count == 0) return "Bạn phải chọn Tài sản để xem biên bản bàn giao";

            List<long> assetIDList = new List<long>();
            foreach (var item in assetSearchResultList)
            {
                msg = CacheObject.GetAssetIDbyGUID(item.ObjectGuid, out long assetID);
                if (msg.Length > 0) return msg;

                assetIDList.Add(assetID);
            }

            string assetIDs = string.Join(",", assetIDList.ToArray());

            msg = AssetHandOver.GetAssetHandOverByAssetIDs(assetIDs, UserToken.AccountID, out List<AssetHandoverDetail> AssetHandoverDetails);
            if (msg.Length > 0) return msg;
            if (AssetHandoverDetails == null || AssetHandoverDetails.Count == 0) return "Không tồn tại tài sản để bản giao".ToMessageForUser(); ;

            var checkUserIDHandover = AssetHandoverDetails.GroupBy(x => x.UserIDHandover).ToList();
            if (checkUserIDHandover.Count > 1) return "Hệ thống chỉ cho phép xem Biên bản bàn giao cho một người trực tiếp nhận".ToMessageForUser();

            var checkUserIDHolding = AssetHandoverDetails.GroupBy(x => x.UserIDHolding).ToList();
            if (checkUserIDHolding.Count > 1) return "Hệ thống chỉ cho phép xem Biên bản bàn giao cho một người bàn giao".ToMessageForUser();

            ReportHandOver ReportHandOver = new ReportHandOver();
            ReportHandOver.AssetHandoverDetail = AssetHandoverDetails;

            msg = AssetHandOver.GetListByAssetIDs(assetIDList.FirstOrDefault().ToString(), out var ltAssetHandOver);
            if (msg.Length > 0) return msg;
            ReportHandOver.HandOverContent = ltAssetHandOver.FirstOrDefault().HandOverContent;

            msg = GetUserDetail(AssetHandoverDetails.FirstOrDefault().UserIDHolding, ReportHandOver);
            if (msg.Length > 0) return msg;

            msg = GetUserDetail(AssetHandoverDetails.FirstOrDefault().UserIDHandover, ReportHandOver, true);
            if (msg.Length > 0) return msg;

            msg = DoCreateFile(ReportHandOver, out filePath);
            if (msg.Length > 0) return msg;

            return msg;
        }

        private string GetUserDetail(int UserID, ReportHandOver reportHandOver, bool isUserHandover = false)
        {
            string msg = AccountUser.GetOneByUserID(UserID, out AccountUser accountUser);
            if (msg.Length > 0) return msg;

            if (isUserHandover) reportHandOver.FullNameOfUserHandover = accountUser != null ? accountUser.FullName : "";
            else reportHandOver.FullNameOfUserHolding = accountUser != null ? accountUser.FullName : "";

            msg = AccountDept.GetListSelectByUserID(UserID, UserToken.AccountID, out List<AccountDept> accountDeptList);
            if (msg.Length > 0) return msg;

            if (accountDeptList == null || accountDeptList.Count == 0) return "";

            string PostionIDs = string.Join(", ", accountDeptList.Select(x => x.PositionID).ToArray());
            msg = AccountPosition.GetPostionListByPostionIDs(PostionIDs, UserToken.AccountID, out List<AccountPosition> accountPositionList);
            if (msg.Length > 0) return msg;

            string SuperiorIDs = string.Join(", ", accountDeptList.Select(x => x.SuperiorID).ToArray());
            msg = AccountUser.GetUserManagerBySuperiorIDs(SuperiorIDs, out List<AccountUser> accountUserManagerList);
            if (msg.Length > 0) return msg;

            msg = AccountPosition.GetAccountPositionByUserIDs(SuperiorIDs, UserToken.AccountID, out List<AccountPosition> accountPositionUserManagerList);
            if (msg.Length > 0) return msg;

            if (!isUserHandover)
            {
                reportHandOver.DeptNameOfUserHolding = (accountPositionList != null && accountPositionList.Count > 0) ? string.Join(", ", accountDeptList.Select(x => x.DeptFullName).ToArray()) : "";
                reportHandOver.PostionNameOfUserHolding = (accountPositionList != null && accountPositionList.Count > 0) ? string.Join(", ", accountPositionList.Select(x => x.PositionName).ToArray()) : "";
                reportHandOver.FullNameOfUserManagerHolding = (accountUserManagerList != null && accountUserManagerList.Count > 0) ? string.Join(", ", accountUserManagerList.Select(x => x.FullName).ToArray()) : "";
                reportHandOver.PostionNameOfUserManagerHolding = (accountPositionUserManagerList != null && accountPositionUserManagerList.Count > 0) ? string.Join(", ", accountPositionUserManagerList.Select(x => x.PositionName).ToArray()) : "";
            }
            else
            {
                reportHandOver.DeptNameOfUserHandover = (accountPositionList != null && accountPositionList.Count > 0) ? string.Join(", ", accountDeptList.Select(x => x.DeptFullName).ToArray()) : "";
                reportHandOver.PostionNameOfUserHandover = (accountPositionList != null && accountPositionList.Count > 0) ? string.Join(", ", accountPositionList.Select(x => x.PositionName).ToArray()) : "";
                reportHandOver.FullNameOfUserManagerHandover = (accountUserManagerList != null && accountUserManagerList.Count > 0) ? string.Join(", ", accountUserManagerList.Select(x => x.FullName).ToArray()) : "";
                reportHandOver.PostionNameOfUserManagerHandover = (accountPositionUserManagerList != null && accountPositionUserManagerList.Count > 0) ? string.Join(", ", accountPositionUserManagerList.Select(x => x.PositionName).ToArray()) : "";
            }


            return "";
        }

        private string DoCreateFile(ReportHandOver assetHandover, out string filePath)
        {
            string fileName = "asset_handover_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
            UtilitiesFile file = UtilitiesFile.GetInfoFile(DateTime.Now, fileName, ConfigurationManager.AppSettings["FolderFileReport"].ToString(), false);

            filePath = UtilitiesFile.GetUrlPage() + "/" + file.FilePathVirtual;
            string msg = FileReportPDF.CreateAssetHandoverFile(assetHandover, file.FilePathPhysical);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpPost]
        public Result ExportHandoverAsset([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoExportHandoverAsset(data, out AssetSenderHandOver assetHandOver, out AssetHandOverExport assetHandOverExport);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoExportFileHandoverAssetPDF(assetHandOverExport, assetHandOver, out string filePath);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return filePath.ToResultOk();
        }

        private string DoExportHandoverAsset([FromBody] JObject data, out AssetSenderHandOver assetHandOver, out AssetHandOverExport assetHandOverExport)
        {
            assetHandOverExport = null;

            string msg = data.ToObject("AssetSenderHandOver", out assetHandOver);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = AssetHandOver.GetDetailByAccountID(UserToken.UserID, assetHandOver.UserIDHandedOver, out assetHandOverExport);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (assetHandOverExport == null) return "Không tồn tại thông tin tài sản".ToMessageForUser();

            return "";
        }

        private string DoExportFileHandoverAssetPDF(AssetHandOverExport assetHandOverExport, AssetSenderHandOver assetHandOver, out string filePath)
        {
            filePath = "";
            string fileName = "BienBanBanGiaoTaiSan_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
            UtilitiesFile file = UtilitiesFile.GetInfoFile(DateTime.Now, fileName, ConfigurationManager.AppSettings["FolderFileExport"].ToString(), false);

            filePath = GetUrlPage() + "/" + file.FilePathVirtual;

            string msg = FileExportHandoverAssetPDF.CreateFile(file.FilePathPhysical, assetHandOverExport, assetHandOver);
            if (msg.Length > 0) return msg;

            return "";
        }

        private string GetUrlPage()
        {
            if (HttpContext.Current == null) return "HttpContext.Current = null";
            if (HttpContext.Current.Request == null) return "HttpContext.Current.Request = null";
            if (HttpContext.Current.Request.Url == null) return "HttpContext.Current.Request.Url = null";
            if (HttpContext.Current.Request.Url.AbsoluteUri == null) return "HttpContext.Current.Request.Url.AbsoluteUri = null";

            String strPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
            String strUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "");
            return strUrl;
        }
    }
}