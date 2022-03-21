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
    public class ItemApproveController : Authentication
    {
        [HttpPost]
        public Result SendApprove([FromBody] ItemSenderApprove data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoSendApprove(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoSendApprove([FromBody] ItemSenderApprove data)
        {
            string msg = DoSendApprove_Validate(data);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoSendApprove_ObjectToDB(dbm, data);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at ItemApprove DoSendApprove";
            }

            dbm.CommitTransac();

            return msg;
        }
        private string DoSendApprove_Validate([FromBody] ItemSenderApprove data)
        {
            string msg = DataValidator.Validate(new
            {
                data.Content,
                data.UserIDApprove
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (data.LtItem.Count == 0) return ("Bạn chưa chọn Vật phẩm nào").ToMessageForUser();
            if (string.IsNullOrEmpty(data.Content)) return ("Bạn cần nhập vào Nội dung").ToMessageForUser();
            if (data.Content.Length < 10) return ("Bạn cần nhập nội dung gửi duyệt tối thiểu 10 ký tự").ToMessageForUser();
            if (data.Content.Length > 255) return ("Bạn chỉ được phép nhập nội dung gửi duyệt không quá 255 ký tự").ToMessageForUser();

            for (int i = 0; i < data.LtItem.Count; i++)
            {
                var item = data.LtItem[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Item.GetOneByGuid(item.ObjectGuid, out Item outItem);
                if (msg.Length > 0) return msg;
                if (outItem == null) return "Vật phẩm không tồn tại ObjectGuid = " + item.ObjectGuid;

                data.LtItem[i] = outItem;
                if (outItem.ItemStatusID != Constants.StatusItem.MT) return "Bạn chỉ được gửi duyệt khi Vật phẩm ở trạng thái tạo mới".ToMessageForUser();
            }

            return msg;
        }
        private string DoSendApprove_ObjectToDB(DBM dbm, ItemSenderApprove itemSenderApprove)
        {
            string msg = "";
            string itemIDs = string.Join(",", itemSenderApprove.LtItem.Select(v => v.ItemID));

            msg = ItemApprove.Insert(dbm, itemIDs, itemSenderApprove.Content, itemSenderApprove.UserIDApprove);
            if (msg.Length > 0) return msg;

            msg = Item.UpdateStatusID_Approve(dbm, itemIDs, itemSenderApprove.UserIDApprove, Constants.StatusItem.CD, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in itemSenderApprove.LtItem)
            {
                msg = Log.WriteHistoryLog(dbm, "Gửi duyệt Vật phẩm", item.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }

            return msg;
        }

        [HttpPost]
        public Result CancelWaitApprove([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoCancelWaitApprove(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoCancelWaitApprove([FromBody] JObject data)
        {
            string msg = data.ToObject("ltItem", out List<Item> ltItem);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoCancelWaitApprove_Validate(ltItem);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoCancelWaitApprove_ObjectToDB(dbm, ltItem);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at ItemApprove DoCancelWaitApprove";
            }


            dbm.CommitTransac();

            return msg;
        }
        private string DoCancelWaitApprove_Validate(List<Item> ltItem)
        {
            string msg = "";

            if (ltItem.Count == 0) return ("Bạn chưa chọn Vật phẩm nào").ToMessageForUser();

            for (int i = 0; i < ltItem.Count; i++)
            {
                var item = ltItem[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Item.GetOneByGuid(item.ObjectGuid, out Item outItem);
                if (msg.Length > 0) return msg;
                ltItem[i] = outItem;

                if (outItem.ItemStatusID != Constants.StatusItem.CD) return "Bạn chỉ được hủy gửi duyệt khi Vật phẩm ở trạng thái chờ duyệt".ToMessageForUser();
            }

            return msg;
        }
        private string DoCancelWaitApprove_ObjectToDB(DBM dbm, List<Item> ltItem)
        {
            string itemIDs = string.Join(",", ltItem.Select(v => v.ItemID));

            string msg = Item.UpdateStatusID_Approve(dbm, itemIDs, 0, Constants.StatusItem.MT, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in ltItem)
            {
                msg = Log.WriteHistoryLog(dbm, "Hủy gửi duyệt Vật phẩm", item.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }

            return "";
        }

        [HttpPost]
        public Result Approve(ComfirmApproveItem data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVP, Role.ROLE_QLVP_DUYET);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoApprove(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoApprove(ComfirmApproveItem data)
        {
            string msg = DoComfirmApprove_Validate(data);
            if (msg.Length > 0) return msg.ToMessageForUser();

            int statusID = data.IsApprove ? Constants.StatusItem.ĐD_TK : Constants.StatusItem.TC;
            string contentLog = data.IsApprove ? "Đồng ý duyệt Vật phẩm" : "Từ chối duyệt Vật phẩm. Lý do: " + data.Reason;

            DBM dbm = new DBM();
            dbm.BeginTransac();

            msg = DoApprove_ObjectToDB(dbm, data, statusID, contentLog);
            if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }

            dbm.CommitTransac();

            return msg;
        }
        private string DoComfirmApprove_Validate(ComfirmApproveItem approve)
        {
            string msg = "";

            if (approve.LtItem.Count == 0) return ("Bạn chưa chọn Vật phẩm nào").ToMessageForUser();

            string itemIDs = string.Join(",", approve.LtItem.Select(v => v.ItemID));

            if (!approve.IsApprove && string.IsNullOrEmpty(approve.Reason)) return "Bạn phải nhập vào lý do từ chối duyệt Vật phẩm".ToMessageForUser();

            if (!approve.IsApprove && approve.Reason.Length < 10 || approve.Reason.Length > 250) return "Bạn phải nhập vào lý do tối thiểu 10 ký tự, tối đa 250 ký tự".ToMessageForUser();

            for (int i = 0; i < approve.LtItem.Count; i++)
            {
                var item = approve.LtItem[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Item.GetOneByGuid(item.ObjectGuid, out Item outItem);
                if (msg.Length > 0) return msg;
                approve.LtItem[i] = outItem;

                if (outItem.ItemStatusID != Constants.StatusItem.CD) return "Bạn chỉ được duyệt khi Vật phẩm ở trạng thái gửi duyệt".ToMessageForUser();
            }
            return msg;
        }
        private string DoApprove_ObjectToDB(DBM dbm, ComfirmApproveItem approve, int statusID, string contentLog)
        {
            int userID = UserToken.UserID;
            string msg = "";

            string itemIDs = string.Join(",", approve.LtItem.Select(v => v.ItemID));

            msg = ItemApprove.Update(dbm, itemIDs, approve.IsApprove, approve.Reason, userID);
            if (msg.Length > 0) return msg;

            msg = Item.UpdateStatusID_Approve(dbm, itemIDs, userID, statusID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            foreach (var item in approve.LtItem)
            {
                msg = Log.WriteHistoryLog(dbm, contentLog, item.ObjectGuid, userID);
                if (msg.Length > 0) return msg;
            }

            return "";
        }
    }
}