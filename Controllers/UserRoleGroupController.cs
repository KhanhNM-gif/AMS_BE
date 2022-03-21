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

namespace WebAPI.Controllers
{
    public class UserRoleGroupController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody]JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.PQ, Role.ROLE_PQ_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            List<UserRoleGroup> lt;
            msg = DoInsertUpdate(UserToken.UserID, data, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        private string DoInsertUpdate(int UserIDCreate, [FromBody]JObject data, out List<UserRoleGroup> ltOut)
        {
            ltOut = new List<UserRoleGroup>();
            string msg = "";

            msg = data.ToString("UserIDs", out string UserIDs);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("RoleGroupID", out int RoleGroupID);
            if (msg.Length > 0) return msg;

            string[] arrUserID = UserIDs.Split(';');
            List<UserRoleGroup> ltUserRoleGroup = new List<UserRoleGroup>();
            foreach (string sUserID in arrUserID)
            {
                int UserID;
                if (!int.TryParse(sUserID, out UserID)) return (sUserID + " không phải là số nguyên").ToMessageForUser();
                ltUserRoleGroup.Add(new UserRoleGroup(UserID, RoleGroupID, UserIDCreate));
            }

            msg = DoInsertUpdate_Validate(RoleGroupID, ltUserRoleGroup);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();
            foreach (var item in ltUserRoleGroup)
            {
                UserRoleGroup uNew;
                item.AccountID = UserToken.AccountID;
                msg = item.InsertUpdate(dbm, out uNew);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }

                Log.WriteHistoryLog("Sửa phân quyền", uNew.ObjectGuid, UserIDCreate);

                ltOut.Add(uNew);
            }
            dbm.CommitTransac();

            return msg;
        }
        private string DoInsertUpdate_Validate(int RoleGroupID, List<UserRoleGroup> ltUserRoleGroup)
        {
            string msg = "";

            RoleGroup rg;
            msg = RoleGroup.GetOne(RoleGroupID, UserToken.AccountID, out rg);
            if (msg.Length > 0) return msg.ToMessageForUser();
            if (rg.IsDelete) return ("Nhóm quyền " + rg.RoleGroupName + " đang ở trạng thái Đã xóa");

            foreach (var item in ltUserRoleGroup)
            {
                msg = DataValidator.Validate(new
                {
                    item.UserID,
                    item.RoleGroupID,
                    item.UserIDCreate
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();
            }

            return msg;
        }

        [HttpGet]
        public Result GetOne(int UserID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DataValidator.Validate(new { UserID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            UserRoleGroup o;
            msg = UserRoleGroup.GetOne(UserID, out o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }
        [HttpGet]
        public Result GetSuggestSearch(string TextSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            DataTable dt;
            string msg = DoGetSuggestSearch(TextSearch, out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
        private string DoGetSuggestSearch(string TextSearch, out DataTable dt)
        {
            dt = new DataTable();

            string msg = AccountUser.SelectSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpPost]
        public Result GetListEasySearch([FromBody]JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListEasySearch(UserToken.UserID, data, out int total, out List<AccountUserSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoGetListEasySearch(int UserID, [FromBody]JObject data, out int Total, out List<AccountUserSearchResult> lt)
        {
            lt = null;
            Total = 0;

            string msg = data.ToNumber("PageSize", out int PageSize);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("CurrentPage", out int CurrentPage);
            if (msg.Length > 0) return msg;

            AccountUserSearch accountUser;
            msg = DoGetListEasySearch_GetAssetSearch(data, out accountUser);
            if (msg.Length > 0) return msg;

            accountUser.PageSize = PageSize;
            accountUser.CurrentPage = CurrentPage;

            msg = DoGetList(accountUser, out lt, out Total);
            return msg;
        }
        private string DoGetListEasySearch_GetAssetSearch([FromBody]JObject data, out AccountUserSearch ms)
        {
            ms = new AccountUserSearch();

            string msg = data.ToNumber("ObjectCategory", out int ObjectCategory);
            if (msg.Length > 0) return msg;

            msg = data.ToString("ObjectID", out string ObjectID);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("PageSize", out int PageSize);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("CurrentPage", out int CurrentPage);
            if (msg.Length > 0) return msg;

            msg = data.ToString("TextSearch", out string TextSearch);
            if (msg.Length > 0) return msg;

            ms.TextSearch = TextSearch;
            ms.CurrentPage = CurrentPage;
            ms.PageSize = PageSize;
            ms.CategorySearch = AssetSearch.DONGIAN;

            if (ObjectCategory > 0) { ms.TextSearch = ""; }

            if (ObjectCategory == 1 || ObjectCategory == 2 || ObjectCategory == 4) ms.UserID = ObjectID.ToNumber(0);
            if (ObjectCategory == 3) ms.PositionID = ObjectID.ToNumber(0);

            return "";
        }
        [HttpPost]
        public Result GetListAdvancedSearch([FromBody]JObject data)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = DoGetListAdvancedSearch(UserToken.UserID, data, out int total, out List<AccountUserSearchResult> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch(int UserID, [FromBody]JObject data, out int Total, out List<AccountUserSearchResult> lt)
        {
            lt = null;
            Total = 0;

            string msg = data.ToObject("AccountUserSearch", out AccountUserSearch AccountUserSearch);
            if (msg.Length > 0) return msg.ToMessageForUser();

            AccountUserSearch.CategorySearch = AssetSearch.NANGCAO;

            msg = DataValidator.Validate(AccountUserSearch).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoGetList(AccountUserSearch, out lt, out Total);
            return msg;
        }
        private string DoGetList(AccountUserSearch accountUser, out List<AccountUserSearchResult> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                accountUser.AccountID = UserToken.AccountID;

                string msg = AccountUser.GetListSearchTotal(accountUser, out totalSearch);
                if (msg.Length > 0) return msg;

                msg = AccountUser.GetListSearch(accountUser, out lt);
                if (msg.Length > 0) return msg;

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        [HttpGet]
        public Result GetListButtonFuction(int TabID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            RoleGroup roleGroup;
            string msg = RoleGroup.GetByUserID(UserToken.UserID, out roleGroup);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (TabID == Constants.TabID.QLTS)
            {
                bool ViewReport = false;
                //if ((roleGroup.QLTS & Role.ROLE_QLTS_VIEWREPORT) == Role.ROLE_QLTS_VIEWREPORT) ViewReport = true;
                return new { ViewReport }.ToResultOk();
            }

            //if (TabID == Constants.TabID.TSPB)
            //{
            //    bool Add = false, ViewReport = false;
            //    if ((roleGroup.TSPB & Role.ROLE_TSPB_ADD) == Role.ROLE_TSPB_ADD) Add = true;
            //    if ((roleGroup.TSPB & Role.ROLE_TSPB_VIEWREPORT) == Role.ROLE_TSPB_VIEWREPORT) ViewReport = true;
            //    return new { Add, ViewReport }.ToResultOk();
            //}

            //if (TabID == Constants.TabID.TSKPB)
            //{
            //    bool ViewReport = true;
            //    return new { ViewReport }.ToResultOk();
            //}

            if (TabID == Constants.TabID.QLTS)
            {
                bool Add = false;
                // if ((roleGroup.QLP & Role.ROLE_QLP_ADD) == Role.ROLE_QLP_ADD) Add = true;
                return new { Add }.ToResultOk();
            }

            //if (TabID == Constants.TabID.Tag)
            //{
            //    bool Add = false;
            //    if ((roleGroup.Tag & Role.ROLE_Tag_ADD) == Role.ROLE_Tag_ADD) Add = true;
            //    return new { Add }.ToResultOk();
            //}

            return "".ToResultOk();
        }
        [HttpGet]
        public Result GetListUserApprove()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<AccountUserDept> lt;
            string msg = AccountUserDept.GetListUserApprove(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListUserManager()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<AccountUserDept> lt;
            string msg = AccountUserDept.GetListUserManagerAsset(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListUserWarehouse()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<AccountUserDept> lt;
            string msg = AccountUserDept.GetListUserWarehouse(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListAllUserByAccountID()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<AccountUserDept> lt;
            string msg = AccountUserDept.GetListUserByAccountID(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpPost]
        public Result Decentralization([FromBody]JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoDecentralization(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return "".ToResultOk();
        }
        private string DoDecentralization([FromBody]JObject data)
        {
            string msg = data.ToObject("UserRoleGroup", out List<UserRoleGroup> ltUserRoleGroup);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoDecentralization_Validate(ltUserRoleGroup);
            if (msg.Length > 0) return msg;

            msg = DoDecentralization_UpdateToDB(ltUserRoleGroup);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string DoDecentralization_Validate(List<UserRoleGroup> ltUserRoleGroup)
        {
            string msg = "";

            if (ltUserRoleGroup.Count == 0) return ("Bạn chưa chọn tài khoản tương ứng với nhóm quyền").ToMessageForUser();

            foreach (var item in ltUserRoleGroup)
            {
                msg = DataValidator.Validate(new
                {
                    item.UserID,
                    item.RoleGroupID
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();
            }

            return msg;
        }
        private string DoDecentralization_UpdateToDB(List<UserRoleGroup> ltUserRoleGroup)
        {
            string msg = "";

            foreach (var item in ltUserRoleGroup)
            {
                msg = UserRoleGroup.UpdateRoleGroupIDByUserID(item.UserID, item.RoleGroupID);
                if (msg.Length > 0) return msg;
            }

            return msg;
        }
    }
}