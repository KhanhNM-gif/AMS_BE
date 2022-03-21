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
    public class RoleGroupController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.PQ, Role.ROLE_PQ_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            RoleGroup mNew;
            msg = DoInsertUpdate(UserToken.UserID, data, out mNew);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return mNew.ToResultOk();
        }
        private string DoInsertUpdate(int UserID, [FromBody] JObject data, out RoleGroup mNew)
        {
            mNew = null;
            string msg = "";

            msg = data.ToObject("RoleGroup", out RoleGroup RoleGroup);
            if (msg.Length > 0) return msg;

            RoleGroup.AccountID = UserToken.AccountID;
            msg = RoleGroup.ValidateRoleGroup(RoleGroup);
            if (msg.Length > 0) return msg;

            RoleGroup.UserIDCreate = UserID;

            RoleGroup.QLTS = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLTS);
            RoleGroup.QLPDX = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLPDX);
            RoleGroup.QLPDXVP = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLPDXVP);
            RoleGroup.QLVV = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLVV);
            RoleGroup.QLVP = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLVP);
            RoleGroup.QLPNK = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLPNK);
            RoleGroup.QLPXK = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLPXK);
            RoleGroup.QLKKTS = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLKKTS);
            RoleGroup.QLKKVP = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLKKVP);
            RoleGroup.LTS = GetRoleValue(RoleGroup.ListRole, Constants.TabID.LTS);
            RoleGroup.ND = GetRoleValue(RoleGroup.ListRole, Constants.TabID.ND);
            RoleGroup.TC = GetRoleValue(RoleGroup.ListRole, Constants.TabID.TC);
            RoleGroup.LVV = GetRoleValue(RoleGroup.ListRole, Constants.TabID.LVV);
            RoleGroup.LVP = GetRoleValue(RoleGroup.ListRole, Constants.TabID.LVP);
            RoleGroup.KHO = GetRoleValue(RoleGroup.ListRole, Constants.TabID.KHO);
            RoleGroup.QLPB = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLPB);
            RoleGroup.QLCV = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLCV);
            RoleGroup.QLND = GetRoleValue(RoleGroup.ListRole, Constants.TabID.QLND);
            RoleGroup.PQ = GetRoleValue(RoleGroup.ListRole, Constants.TabID.PQ);
            RoleGroup.SDTS = GetRoleValue(RoleGroup.ListRole, Constants.TabID.SDTS);
            RoleGroup.TCL = GetRoleValue(RoleGroup.ListRole, Constants.TabID.TCL);
            RoleGroup.KHOVP = GetRoleValue(RoleGroup.ListRole, Constants.TabID.KHOVP);
            RoleGroup.BCTK_TS = GetRoleValue(RoleGroup.ListRole, Constants.TabID.BCTK_TS);
            RoleGroup.BCTK_VP = GetRoleValue(RoleGroup.ListRole, Constants.TabID.BCTK_VP);

            msg = RoleGroup.InsertUpdate(out mNew);
            if (msg.Length > 0) return msg;
            mNew.ListRole = RoleGroup.ListRole;

            Log.WriteHistoryLog(RoleGroup.RoleGroupID == 0 ? "Thêm Nhóm quyền" : "Sửa Nhóm quyền", mNew.ObjectGuid, UserID);

            return msg;
        }
        private long GetRoleValue(List<Role> ListRole, int tabID)
        {
            return ListRole.Where(v => v.TabID == tabID && v.IsRole).Sum(v => v.RoleValue);
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.PQ, Role.ROLE_PQ_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            RoleGroup roleGroup;
            msg = UpdateDelete(UserToken.UserID, true, data, out roleGroup);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return roleGroup.ToResultOk();
        }
        [HttpPost]
        public Result Restore([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.PQ, Role.ROLE_PQ_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            RoleGroup roleGroup;
            msg = UpdateDelete(UserToken.UserID, false, data, out roleGroup);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return roleGroup.ToResultOk();
        }
        private string UpdateDelete(int UserID, bool IsDelete, [FromBody] JObject data, out RoleGroup roleGroup)
        {
            roleGroup = null;

            string msg = "";

            msg = data.ToNumber("RoleGroupID", out int RoleGroupID);
            if (msg.Length > 0) return msg;

            msg = RoleGroup.GetOne(RoleGroupID, UserToken.AccountID, out roleGroup);
            if (msg.Length > 0) return msg;
            if (roleGroup == null) return ("Không tồn tại Nhóm quyền có ID = " + RoleGroupID).ToMessageForUser();

            if (IsDelete)
            {
                if (roleGroup.RoleGroupID == RoleGroup.ADMIN) return "Không được xóa Nhóm quyền Admin".ToMessageForUser();
                if (roleGroup.RoleGroupID == RoleGroup.USER) return "Không được xóa Nhóm quyền User".ToMessageForUser();

                msg = UserRoleGroup.GetListByRoleGroupID(roleGroup.RoleGroupID, out List<UserRoleGroup> lt);
                if (msg.Length > 0) return msg;
                if (lt.Count > 0) return ($"Bạn không thể xóa nhóm quyền {roleGroup.RoleGroupName}. Vì nhóm quyền đang được gắn với thông tin người dùng {string.Join(", ", lt.Select(v => v.UserName))}").ToMessageForUser();


                msg = RoleGroup.UpdateIsDelete(roleGroup.RoleGroupID, true);
                if (msg.Length > 0) return msg;

                Log.WriteHistoryLog("Xóa Nhóm quyền", roleGroup.ObjectGuid, UserID);
            }
            else
            {
                msg = RoleGroup.ValidateRoleGroup(roleGroup);
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = RoleGroup.UpdateIsDelete(roleGroup.RoleGroupID, false);
                if (msg.Length > 0) return msg;

                Log.WriteHistoryLog("Khôi phục Nhóm quyền", roleGroup.ObjectGuid, UserID);
            }
            roleGroup.IsDelete = IsDelete;

            return msg;
        }

        [HttpGet]
        public Result GetOne(int RoleGroupID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.PQ, Role.ROLE_PQ_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DataValidator.Validate(new { RoleGroupID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            RoleGroup o;
            msg = RoleGroup.GetOne(RoleGroupID, UserToken.AccountID, out o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            o.ListRole = GetListRole(o);
            return o.ToResultOk();
        }
        private List<Role> GetListRole(RoleGroup o)
        {
            List<Role> ListRole = Role.GetListRole();
            foreach (Role role in ListRole)
            {
                int tabID = role.TabID;
                long roleValue = role.RoleValue;
                role.IsRole = (tabID == Constants.TabID.QLTS && (o.QLTS & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLPDX && (o.QLPDX & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLPDXVP && (o.QLPDXVP & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLVV && (o.QLVV & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.SDTS && (o.SDTS & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLVP && (o.QLVP & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLPNK && (o.QLPNK & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLPXK && (o.QLPXK & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLKKTS && (o.QLKKTS & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLKKVP && (o.QLKKVP & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.LTS && (o.LTS & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.ND && (o.ND & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.TC && (o.TC & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.LVV && (o.LVV & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.LVP && (o.LVP & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.KHO && (o.KHO & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLPB && (o.QLPB & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLCV && (o.QLCV & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.QLND && (o.QLND & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.TCL && (o.TCL & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.PQ && (o.PQ & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.KHOVP && (o.KHOVP & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.BCTK_TS && (o.BCTK_TS & roleValue) == roleValue) ||
                              (tabID == Constants.TabID.BCTK_VP && (o.BCTK_VP & roleValue) == roleValue);
                if (tabID == Constants.TabID.QLTS || tabID == Constants.TabID.QLPDX || tabID == Constants.TabID.QLVV || tabID == Constants.TabID.QLKKTS || tabID == Constants.TabID.SDTS) role.ParentID = 1;
                if (tabID == Constants.TabID.QLVP || tabID == Constants.TabID.QLPDXVP || tabID == Constants.TabID.QLPNK || tabID == Constants.TabID.QLPXK || tabID == Constants.TabID.QLKKVP || tabID == Constants.TabID.KHOVP) role.ParentID = 2;
                if (tabID == Constants.TabID.LTS || tabID == Constants.TabID.ND || tabID == Constants.TabID.TC || tabID == Constants.TabID.LVV || tabID == Constants.TabID.LVP || tabID == Constants.TabID.KHO) role.ParentID = 3;
                if (tabID == Constants.TabID.QLPB || tabID == Constants.TabID.QLCV || tabID == Constants.TabID.QLND || tabID == Constants.TabID.PQ || tabID == Constants.TabID.TCL) role.ParentID = 4;
                if (tabID == Constants.TabID.BCTK_TS || tabID == Constants.TabID.BCTK_VP) role.ParentID = 5;
            }
            return ListRole;
        }

        [HttpGet]
        public Result GetList(string RoleGroupName, int StatusID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<RoleGroup> lt;
            string msg = RoleGroup.GetList(RoleGroupName, StatusID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            foreach (var item in lt) item.ListRole = GetListRole(item);

            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListByActive()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<RoleGroup> lt;
            string msg = RoleGroup.GetListByAvtive(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            //foreach (var item in lt) item.ListRole = GetListRole(item);

            //SPV.InsertTab(UserToken.UserID, Constants.TabID.NHOMQUYEN);

            return lt.ToResultOk();
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

            string msg = RoleGroup.GetSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpPost]
        public Result GetListEasySearch([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.PQ, Role.ROLE_PQ_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDoGetListEasySearch(UserToken.UserID, data, out int total, out List<RoleGroup> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoDoGetListEasySearch(int UserID, [FromBody] JObject data, out int total, out List<RoleGroup> lt)
        {
            lt = null;
            total = 0;

            string msg = data.ToNumber("PageSize", out int PageSize);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("CurrentPage", out int CurrentPage);
            if (msg.Length > 0) return msg;

            RoleGroupSearch roleGroupSearch;
            msg = DoGetListEasySearch_GetRoleGroupSearch(data, out roleGroupSearch);
            if (msg.Length > 0) return msg;

            roleGroupSearch.PageSize = PageSize;
            roleGroupSearch.CurrentPage = CurrentPage;

            msg = DoGetList(roleGroupSearch, out lt, out total);
            return msg;

        }
        private string DoGetListEasySearch_GetRoleGroupSearch([FromBody] JObject data, out RoleGroupSearch ms)
        {
            ms = new RoleGroupSearch();

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
            ms.AccountID = UserToken.AccountID;

            if (ObjectCategory == 3) ms.UserIDCreate = ObjectID.ToNumber(0);
            if (ObjectCategory == 1 || ObjectCategory == 2) ms.RoleGroupID = ObjectID.ToNumber(0);

            return "";
        }
        private string DoGetList(RoleGroupSearch formSearch, out List<RoleGroup> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                formSearch.AccountID = UserToken.AccountID;

                string msg = RoleGroup.GetListSearchTotal(formSearch, out totalSearch);
                if (msg.Length > 0) return msg;

                msg = RoleGroup.GetListSearch(formSearch, out lt);
                if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    item.ListRole = GetListRole(item);
                }

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        [HttpPost]
        public Result GetListAdvancedSearch([FromBody] JObject data)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = Role.Check(UserToken.UserID, Constants.TabID.PQ, Role.ROLE_PQ_IsVisitPage);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                msg = DoGetListAdvancedSearch(UserToken.UserID, data, out int total, out List<RoleGroup> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch(int UserID, [FromBody] JObject data, out int Total, out List<RoleGroup> lt)
        {
            lt = null;
            Total = 0;

            string msg = data.ToObject("RoleGroupSearch", out RoleGroupSearch formSearch);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoGetList(formSearch, out lt, out Total);
            return msg;
        }

    }
}