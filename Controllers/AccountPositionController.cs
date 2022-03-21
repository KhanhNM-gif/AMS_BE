using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class AccountPositionController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLCV, Role.ROLE_QLCV_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            AccountPosition mNew;
            msg = DoInsertUpdate(data, out mNew);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return mNew.ToResultOk();
        }
        private string DoInsertUpdate([FromBody] JObject data, out AccountPosition mNew)
        {
            mNew = null;
            string msg = "";

            msg = data.ToObject("Position", out AccountPosition accountPosition);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_Validate(accountPosition);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = accountPosition.InsertUpdate(new DBM(), out mNew);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog(accountPosition.PositionID == 0 ? "Thêm chức vụ" : "Sửa chức vụ", mNew.ObjectGuid, UserToken.UserID);

            return msg;
        }
        private string DoInsertUpdate_Validate(AccountPosition accountPosition)
        {
            string msg = "";

            string PositionName = accountPosition.PositionName.Trim();
            if (PositionName.Length == 0) return "Tên chức vụ không được để trống";

            msg = DataValidator.Validate(new
            {
                accountPosition.PositionID,
                accountPosition.PositionIDParent,
                accountPosition.PositionName,
                accountPosition.PositionCode,
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            accountPosition.AccountID = UserToken.AccountID;

            List<AccountPosition> ltaccountPosition;
            //msg = AccountPosition.GetByAccountPositionName(PositionName, out ltaccountPosition);
            msg = AccountPosition.GetList(UserToken.AccountID, out ltaccountPosition);
            if (msg.Length > 0) return msg;
            if (ltaccountPosition.Where(v => v.PositionID != accountPosition.PositionID && v.PositionCode == accountPosition.PositionCode || v.PositionName == accountPosition.PositionName && !v.IsActive).Count() > 0)
                return ("Thông tin Tên phòng ban, Mã phòng ban đã  tồn tại trong hệ thống").ToMessageForUser();

            if (accountPosition.PositionID > 0 && !accountPosition.IsActive)
            {
                msg = AccountPosition.GetOneAccountUserDeptByPositionID(accountPosition.PositionID, UserToken.AccountID, out List<AccountUser> ltUser);
                if (msg.Length > 0) return msg;

                if (ltUser.Count > 0)
                {
                    string strUsername = string.Join(", ", ltUser.Select(v => v.UserName));
                    return ($"Chức vụ {accountPosition.PositionName} đang được sử dụng. Bạn cần kiểm tra lại các dữ liệu Người dùng {strUsername} trước khi cập nhật trạng thái thành không sử dụng").ToMessageForUser();
                }

                msg = AccountPosition.GetListChildByPositionID(accountPosition.PositionID, UserToken.AccountID, out List<AccountPosition> ltPo);
                if (msg.Length > 0) return msg;
                if (ltPo.Count > 0) return ($"Chức vụ {accountPosition.PositionName} có các chức vụ con đang hoạt động. Bạn cần chuyển tất các các chức vụ con sang trạng thái không sử dụng").ToMessageForUser();
            }

            return msg;
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLCV, Role.ROLE_QLCV_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDelete(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoDelete(int UserID, [FromBody] JObject data)
        {
            string msg = "";

            msg = data.ToNumber("PositionID", out int PositionID);
            if (msg.Length > 0) return msg;

            AccountPosition accountPosition;
            msg = AccountPosition.GetOneByPositionID(PositionID, UserToken.AccountID, out accountPosition);
            if (msg.Length > 0) return msg;
            if (accountPosition == null) return ("Không tồn tại chức vụ ID = " + PositionID).ToMessageForUser();

            msg = DoDelete_Validate(accountPosition);
            if (msg.Length > 0) return msg;

            msg = AccountPosition.Delete(accountPosition.PositionID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog("Xóa chức vụ", accountPosition.ObjectGuid, UserID);

            return msg;
        }
        private string DoDelete_Validate(AccountPosition accountPosition)
        {
            string msg = "";

            List<AccountUser> lstaccount;
            msg = AccountPosition.GetOneAccountUserDeptByPositionID(accountPosition.PositionID, UserToken.AccountID, out lstaccount);
            if (msg.Length > 0) return msg;
            if (lstaccount.Count > 0)
            {
                string finalName = string.Join(", ", lstaccount.Select(p => p.UserName).ToArray());
                return ("Chức vụ đang được gắn với tài khoản người dùng " + finalName + ", bạn không thể xóa chức vụ này").ToMessageForUser();
            }

            msg = AccountPosition.GetListChildByPositionID(accountPosition.PositionID, UserToken.AccountID, out List<AccountPosition> ltPo);
            if (msg.Length > 0) return msg;
            if (ltPo.Count > 0) return ($"Bạn không thể xóa Chức vụ này, do Chức vụ: {accountPosition.PositionName} đang được gắn với các Chức vụ con").ToMessageForUser();

            return msg;
        }

        [HttpGet]
        public Result GetOne(int PositionID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLCV, Role.ROLE_QLCV_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DataValidator.Validate(new { PositionID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            AccountPosition o;
            msg = AccountPosition.GetOneByPositionID(PositionID, UserToken.AccountID, out o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }
        [HttpGet]
        public Result GetListByFilter(string PositionName, int IsActive)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLCV, Role.ROLE_QLCV_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            List<AccountPosition> lt;
            msg = AccountPosition.GetListByFilter(PositionName, IsActive, UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetList()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLCV, Role.ROLE_QLCV_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            List<AccountPosition> lt;
            msg = AccountPosition.GetList(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
    }
}