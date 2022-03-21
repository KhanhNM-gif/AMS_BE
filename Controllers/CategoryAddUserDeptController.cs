using BSS;
using BSS.DataValidator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class CategoryAddUserDeptController : Authentication
    {

        [HttpGet]
        public Result GetListDeptDelivery()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<AccountDept> lt;
            string msg = AccountDept.GetList(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }

        [HttpGet]
        public Result GetListPositionDelivery()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<AccountPosition> lt;
            string msg = AccountPosition.GetList(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListALlUser()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = AccountUser.GetAll(UserToken.AccountID, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListUserManagementPlace()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = AccountUser.GetListUserManagementPlace(UserToken.AccountID, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpGet]
        public Result GetListUserHolding()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = RoleGroup.GetListUser(UserToken.AccountID, Constants.TabID.QLTS, Role.ROLE_QLTS_IsVisitPage, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListUserApprove()// người duyệt tài sản
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = RoleGroup.GetListUser(UserToken.AccountID, Constants.TabID.QLTS, Role.ROLE_QLTS_DUYET, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpGet]
        public Result GetListUserApproveItem()// người duyệt Vật phẩm
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = RoleGroup.GetListUser(UserToken.AccountID, Constants.TabID.QLVP, Role.ROLE_QLVP_DUYET, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpGet]
        public Result GetListUserReturn()// người tiếp nhận trả tài sản
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = RoleGroup.GetListUser(UserToken.AccountID, Constants.TabID.QLTS, Role.ROLE_QLTS_XNTTS, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListUserHandover()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = RoleGroup.GetListUser(UserToken.AccountID, Constants.TabID.QLTS, Role.ROLE_QLTS_SUDUNG, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpGet]
        public Result GetUserObjectGuid(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DataValidator.Validate(new { ObjectGuid }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            AccountUserDept o;
            msg = AccountUserDept.GetOneByObjectGUID(ObjectGuid, UserToken.AccountID, out o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }

        [HttpGet]
        public Result GetListUserManager(int DeptID, int PositionID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = GetListUserManager_Validate(DeptID, PositionID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = RoleGroup.GetListUserManager(PositionID, DeptID, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
        private string GetListUserManager_Validate(int DeptID, int PositionID)
        {
            string msg = DataValidator.Validate(new { DeptID, PositionID }).ToErrorMessage();
            if (msg.Length > 0) return msg;

            msg = AccountPosition.GetOneByPositionID(PositionID, UserToken.AccountID, out AccountPosition outAccountPosition);
            if (msg.Length > 0) return msg;
            if (outAccountPosition == null) return "Chức vụ không tồn tại hoặc không được sử dụng".ToMessageForUser();
            if (outAccountPosition.AccountID != UserToken.AccountID) return "Chức vụ không thuộc đơn vị của bạn".ToMessageForUser();


            msg = AccountDept.GetOneByDeptID(DeptID, UserToken.AccountID, out AccountDept outAccountDept);
            if (msg.Length > 0) return msg;
            if (outAccountDept == null) return "Phòng ban không tồn tại hoặc không được sử dụng".ToMessageForUser();
            if (outAccountDept.AccountID != UserToken.AccountID) return "Phòng ban không thuộc đơn vị của bạn".ToMessageForUser();

            return "";
        }
    }
}
