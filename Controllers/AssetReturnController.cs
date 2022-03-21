using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class AssetReturnController : Authentication
    {
        [HttpPost]
        public Result SendReturn([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoSendReturn(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoSendReturn([FromBody] JObject data)
        {
            string msg = data.ToObject("AssetSenderReturn", out AssetSenderReturn assetReturn);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoSendReturn_Validate(assetReturn);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoSendReturn_ObjectToDB(dbm, assetReturn);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetReturn DoSendReturn";
            }


            dbm.CommitTransac();

            return msg;
        }
        private string DoSendReturn_ObjectToDB(DBM dbm, AssetSenderReturn assetReturn)
        {
            string msg = "";
            string AssetIDs = string.Join(",", assetReturn.ltAsset.Select(v => v.AssetID));

            msg = AssetReturn.Insert(dbm, AssetIDs, UserToken.UserID, assetReturn.ReturnDate, assetReturn.UserIDReturned, assetReturn.ReturnContent);
            if (msg.Length > 0) return msg;

            msg = Asset.UpdateStatusID_Return(dbm, AssetIDs, assetReturn.UserIDReturned, Constants.StatusAsset.CXN_T, 0, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in assetReturn.ltAsset)
            {
                msg = Log.WriteHistoryLog(dbm, $"Trả tài sản cho {assetReturn.UserNameReturned} lý do: " + assetReturn.ReturnContent, item.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return msg;
        }
        private string DoSendReturn_Validate(AssetSenderReturn assetReturn)
        {
            string msg = DataValidator.Validate(new
            {
                assetReturn.ReturnContent,
                assetReturn.UserIDReturned,
                assetReturn.ReturnDate
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (assetReturn.ltAsset.Count == 0) return ("Bạn chưa chọn Tài sản nào").ToMessageForUser();

            msg = AccountUser.GetOneByUserID(assetReturn.UserIDReturned, out AccountUser outAccountUserReturned);
            if (msg.Length > 0) return msg.ToMessageForUser();
            if (outAccountUserReturned == null) return "User ID không tồn tại";
            if (!outAccountUserReturned.IsActive) return "Bàn khoản bàn giao đã bị khóa";
            assetReturn.UserNameReturned = $"{outAccountUserReturned.FullName} ({outAccountUserReturned.UserName})";

            for (int i = 0; i < assetReturn.ltAsset.Count; i++)
            {
                var item = assetReturn.ltAsset[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByGuid(item.ObjectGuid, out Asset o);
                if (msg.Length > 0) return msg;
                assetReturn.ltAsset[i] = o;

                if (o.AssetStatusID != Constants.StatusAsset.ĐSD) return "Bạn chỉ được trả tài sản khi Tài sản ở trạng thái đang sử dụng".ToMessageForUser();

                msg = AssetHandOver.GetListByAssetIDs(o.AssetID.ToString(), out List<AssetHandOver> ltAssetHandOver);
                if (msg.Length > 0) return msg;

                if (ltAssetHandOver.Count(v => v.HandOverDate > assetReturn.ReturnDate) > 0) return "Ngày trả tài sản phải lớn hơn ngày bàn giao".ToMessageForUser();
            }

            return msg;
        }

        [HttpPost]
        public Result CancelWaitReturn([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoCancelWaitReturn(UserToken.UserID, data);

            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return Result.GetResultOk();
        }
        private string DoCancelWaitReturn(int UserID, [FromBody] JObject data)
        {
            string msg = data.ToObject("ltAsset", out List<Asset> ltAsset);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoCancelWaitReturn_Validate(ltAsset);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoCancelWaitReturn_ObjectToDB(dbm, ltAsset);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetReturn DoCancelWaitReturn";
            }

            dbm.CommitTransac();

            return msg;
        }
        private string DoCancelWaitReturn_ObjectToDB(DBM dbm, List<Asset> ltAsset)
        {
            string AssetIDs = string.Join(",", ltAsset.Select(v => v.AssetID));

            string msg = Asset.UpdateStatusID_Return(dbm, AssetIDs, 0, Constants.StatusAsset.ĐSD, 0, UserToken.AccountID);

            if (msg.Length > 0) return msg;

            foreach (var item in ltAsset)
            {
                msg = Log.WriteHistoryLog(dbm, "Hủy chờ xác nhận trả tài sản", item.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return "";
        }
        private string DoCancelWaitReturn_Validate(List<Asset> ltAsset)
        {
            string msg = "";

            if (ltAsset.Count == 0) return ("Bạn chưa chọn tài sản nào").ToMessageForUser();

            for (int i = 0; i < ltAsset.Count; i++)
            {
                var item = ltAsset[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByGuid(item.ObjectGuid, out Asset o);
                if (msg.Length > 0) return msg;
                ltAsset[i] = o;

                msg = AssetReturn.GetOneAssetReturnByAssetID(o.AssetID, out AssetReturn assetReturn);
                if (msg.Length > 0) return msg;

                if (o.AssetStatusID != Constants.StatusAsset.CXN_T)
                    return "Bạn chỉ được hủy chờ trả tài sản khi Tài sản ở trạng thái chờ xác nhận trả".ToMessageForUser();

                double TotalMinutes = DateTime.Now.Subtract(assetReturn.CreateDate).TotalMinutes;
                if (TotalMinutes > 10) return ("Bạn chỉ được trả tài sản sau 10 phút").ToMessageForUser();

            }
            return msg;
        }

        [HttpPost]
        public Result ComfirmReturn([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLTS, Role.ROLE_QLTS_XNTTS);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoComfirmReturn(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return Result.GetResultOk();
        }
        private string DoComfirmReturn([FromBody] JObject data)
        {
            string msg = data.ToObject("AssetComfirmReturn", out AssetComfirmReturn assetReturn);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoComfirmReturn_Validate(assetReturn);
            if (msg.Length > 0) return msg.ToMessageForUser();

            int StatusID = assetReturn.IsReturn ? Constants.StatusAsset.ĐD_TK : Constants.StatusAsset.ĐSD;
            string contentLog = assetReturn.IsReturn ? "Xác nhận {0} đã trả Tài sản" : "Từ chối {0} trả Tài sản. Lý do: " + assetReturn.Reason;


            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoComfirmReturn_ObjectToDB(dbm, assetReturn, StatusID, contentLog);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetReturn DoComfirmReturn";
            }

            dbm.CommitTransac();

            return msg;
        }
        private string DoComfirmReturn_Validate(AssetComfirmReturn assetComfirm)
        {
            string msg = "";

            if (assetComfirm.ltAsset.Count == 0) return ("Bạn chưa chọn Tài sản nào").ToMessageForUser();

            if (!assetComfirm.IsReturn && string.IsNullOrEmpty(assetComfirm.Reason)) return "Bạn chưa nhập Lý do từ chối".ToMessageForUser();


            if (assetComfirm.IsReturn && assetComfirm.PlaceID == 0) return "Bạn chưa chọn kho".ToMessageForUser();

            for (int i = 0; i < assetComfirm.ltAsset.Count; i++)
            {
                var item = assetComfirm.ltAsset[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByGuid(item.ObjectGuid, out Asset o);
                if (msg.Length > 0) return msg;
                assetComfirm.ltAsset[i] = o;

                if (o.AssetStatusID != Constants.StatusAsset.CXN_T) return "Bạn chỉ được xác nhận trả tài sản khi Tài sản ở trạng thái chờ xác nhận trả".ToMessageForUser();
            }
            return msg;
        }
        private string DoComfirmReturn_ObjectToDB(DBM dbm, AssetComfirmReturn assetComfirm, int StatusID, string contentLog)
        {
            string msg = "";

            string AssetIDs = string.Join(",", assetComfirm.ltAsset.Select(v => v.AssetID));

            msg = AssetReturn.Update(dbm, AssetIDs, assetComfirm.IsReturn, assetComfirm.Reason, UserToken.UserID, out List<AssetReturn> outLtAssetReturn);
            if (msg.Length > 0) return msg;

            msg = Asset.UpdateStatusID_Return(dbm, AssetIDs, UserToken.UserID, StatusID, assetComfirm.PlaceID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in outLtAssetReturn)
            {
                msg = Log.WriteHistoryLog(dbm, string.Format(contentLog, item.UserNameReturn), item.ObjectGuidAsset, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return "";
        }
        [HttpGet]
        public Result ViewDetail(string ObjectGuids)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLTS, Role.ROLE_QLTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = Asset.GetAssetIDsByObjectGuids(ObjectGuids, out string AssetIDs);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = AssetReturn.GetSearchByAssetIDs(AssetIDs, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
    }
}