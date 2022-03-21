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
    public class AssetApproveController : Authentication
    {
        [HttpPost]
        public Result SendApprove([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoSendApprove(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoSendApprove(int UserIDCreate, [FromBody] JObject data)
        {
            string msg = data.ToObject("AssetSenderApprove", out AssetSenderApprove AssetSenderApprove);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoSendApprove_Validate(AssetSenderApprove);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoSendApprove_ObjectToDB(dbm, AssetSenderApprove, UserIDCreate);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetApprove DoSendApprove";
            }



            dbm.CommitTransac();

            return msg;
        }
        private string DoSendApprove_ObjectToDB(DBM dbm, AssetSenderApprove AssetSenderApprove, int UserIDCreate)
        {
            string msg = "";
            string AssetIDs = string.Join(",", AssetSenderApprove.ltAsset.Select(v => v.AssetID));

            msg = AssetApprove.Insert(dbm, AssetIDs, AssetSenderApprove.ApproveContent, AssetSenderApprove.UserIDApprove);
            if (msg.Length > 0) return msg;

            msg = Asset.UpdateStatusID_Approve(dbm, AssetIDs, AssetSenderApprove.UserIDApprove, Constants.StatusAsset.CD, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in AssetSenderApprove.ltAsset)
            {
                msg = Log.WriteHistoryLog(dbm, "Gửi duyệt tài sản", item.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return msg;
        }
        private string DoSendApprove_Validate([FromBody] AssetSenderApprove data)
        {
            string msg = DataValidator.Validate(new
            {
                data.ApproveContent,
                data.UserIDApprove
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (data.ltAsset.Count == 0) return ("Bạn chưa chọn Tài sản nào").ToMessageForUser();
            if (string.IsNullOrEmpty(data.ApproveContent)) return ("Bạn chưa nhập nội dung").ToMessageForUser();
            for (int i = 0; i < data.ltAsset.Count; i++)
            {
                var item = data.ltAsset[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByGuid(item.ObjectGuid, out Asset o);
                if (msg.Length > 0) return msg;
                data.ltAsset[i] = o;

                if (o.AssetStatusID != Constants.StatusAsset.MT) return "Bạn chỉ được gửi duyệt khi Tài sản ở trạng thái tạo mới".ToMessageForUser();
            }

            return msg;
        }

        [HttpPost]
        public Result CancelWaitApprove([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoCancelWaitApprove(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return Result.GetResultOk();
        }
        private string DoCancelWaitApprove(int UserID, [FromBody] JObject data)
        {
            string msg = data.ToObject("ltAsset", out List<Asset> ltAsset);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoCancelWaitApprove_Validate(ltAsset);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();
            try
            {
                msg = DoCancelWaitApprove_ObjectToDB(dbm, ltAsset);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetApprove DoCancelWaitApprove";
            }

            dbm.CommitTransac();

            return msg;
        }
        private string DoCancelWaitApprove_ObjectToDB(DBM dbm, List<Asset> ltAsset)
        {
            string AssetIDs = string.Join(",", ltAsset.Select(v => v.AssetID));

            string msg = Asset.UpdateStatusID_Approve(dbm, AssetIDs, 0, Constants.StatusAsset.MT, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in ltAsset)
            {
                msg = Log.WriteHistoryLog(dbm, "Hủy gửi duyệt tài sản", item.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return "";
        }
        private string DoCancelWaitApprove_Validate(List<Asset> ltAsset)
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

                if (o.AssetStatusID != Constants.StatusAsset.CD) return "Bạn chỉ được hủy gửi duyệt khi Tài sản ở trạng thái chờ duyệt".ToMessageForUser();
            }
            return msg;
        }

        [HttpPost]
        public Result Approve([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLTS, Role.ROLE_QLTS_DUYET);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoApprove(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoApprove([FromBody] JObject data)
        {
            string msg = data.ToObject("ComfirmApprove", out ComfirmApprove approve);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoComfirmApprove_Validate(approve);
            if (msg.Length > 0) return msg.ToMessageForUser();

            int StatusID = approve.IsApprove ? Constants.StatusAsset.ĐD_TK : Constants.StatusAsset.TC;
            string contentLog = approve.IsApprove ? "Đồng ý duyệt Tài sản" : "Từ chối duyệt Tài sản. Lý do: " + approve.Reason;

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoApprove_ObjectToDB(dbm, approve, StatusID, contentLog);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetApprove DoApprove";
            }




            dbm.CommitTransac();

            return msg;
        }
        private string DoApprove_ObjectToDB(DBM dbm, ComfirmApprove approve, int StatusID, string contentLog)
        {
            string msg = "";

            string AssetIDs = string.Join(",", approve.ltAsset.Select(v => v.AssetID));

            msg = AssetApprove.Update(dbm, AssetIDs, approve.IsApprove, approve.Reason, UserToken.UserID);
            if (msg.Length > 0) return msg;

            msg = Asset.UpdateStatusID_Approve(dbm, AssetIDs, UserToken.UserID, StatusID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in approve.ltAsset)
            {
                msg = Log.WriteHistoryLog(dbm, contentLog, item.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return "";
        }
        private string DoComfirmApprove_Validate(ComfirmApprove approve)
        {
            string msg = "";

            if (approve.ltAsset.Count == 0) return ("Bạn chưa chọn Tài sản nào").ToMessageForUser();

            if (!approve.IsApprove && string.IsNullOrEmpty(approve.Reason)) return "Bạn chưa nhập Lý do từ chối".ToMessageForUser();

            for (int i = 0; i < approve.ltAsset.Count; i++)
            {
                var item = approve.ltAsset[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByGuid(item.ObjectGuid, out Asset o);
                if (msg.Length > 0) return msg;
                approve.ltAsset[i] = o;

                if (o.AssetStatusID != Constants.StatusAsset.CD) return "Bạn chỉ được duyệt khi Tài sản ở trạng thái gửi duyệt".ToMessageForUser();
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

            msg = AssetApprove.GetSearchByAssetIDs(AssetIDs, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
    }
}