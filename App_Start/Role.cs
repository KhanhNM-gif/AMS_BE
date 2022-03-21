using BSS;
using BSS.DataValidator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public class RoleGroup
{
    public int RoleGroupID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string RoleGroupName { get; set; }
    public long QLTS { get; set; }
    public long QLPDX { get; set; }
    public long QLPDXVP { get; set; }
    public long QLVV { get; set; }
    public long QLVP { get; set; }
    public long QLPNK { get; set; }
    public long QLPXK { get; set; }
    public long QLKKTS { get; set; }
    public long QLKKVP { get; set; }
    public long LTS { get; set; }
    public long ND { get; set; }
    public long TC { get; set; }
    public long LVV { get; set; }
    public long LVP { get; set; }
    public long KHO { get; set; }
    public long QLPB { get; set; }
    public long QLCV { get; set; }
    public long QLND { get; set; }
    public long PQ { get; set; }
    public long SDTS { get; set; }
    public long TCL { get; set; }
    public long KHOVP { get; set; }
    public long BCTK_TS { get; set; }
    public long BCTK_VP { get; set; }
    [JsonIgnore]
    public int UserIDCreate { get; set; }
    public string UserNameCreate { get; set; }
    public string PositionName { get; set; } = "   ";
    public string Note { get; set; }
    public bool IsActive { get; set; }
    public bool IsDelete { get; set; }
    public int AccountID { get; set; }
    public List<Role> ListRole = new List<Role>();
    public List<string> ListRoleDescription
    {
        get
        {
            List<string> lt = new List<string>();
            var vGroup = ListRole.Where(v => v.IsRole).GroupBy(v => new { v.TabID, v.TabName });
            foreach (var item in vGroup)
            {
                lt.Add(item.Key.TabName + ": " + string.Join("; ", item.Select(v => v.RoleName)));
            }

            return lt;
        }
    }

    public const int ADMIN = 1, USER = 2;

    static public string GetOne(int RoleGroupID, int AccountID, out RoleGroup RoleGroup)
    {
        RoleGroup = null;

        string msg = DBM.GetOne("usp_RoleGroup_GetOne", new { RoleGroupID, AccountID }, out RoleGroup);
        if (msg.Length > 0) return msg;

        if (RoleGroup == null) RoleGroup = new RoleGroup();
        return "";
    }
    public string InsertUpdate(out RoleGroup r)
    {
        return DBM.GetOne("usp_RoleGroup_InsertOrUpdate", new
        {
            RoleGroupID,
            RoleGroupName,
            QLTS,
            QLPDX,
            QLVV,
            QLPDXVP,
            QLVP,
            QLPNK,
            QLPXK,
            QLKKTS,
            QLKKVP,
            LTS,
            ND,
            TC,
            LVV,
            LVP,
            KHO,
            QLPB,
            QLCV,
            QLND,
            PQ,
            UserIDCreate,
            AccountID,
            IsActive,
            Note,
            SDTS,
            TCL,
            KHOVP,
            BCTK_TS,
            BCTK_VP
        }, out r);
    }

    public static string GetAll(int AccountID, out List<RoleGroup> lt)
    {
        return DBM.GetList("usp_RoleGroup_SelectAll", new { AccountID }, out lt);
    }
    public static string GetListByAvtive(int AccountID, out List<RoleGroup> lt)
    {
        return DBM.GetList("usp_RoleGroup_GetListByActive", new { AccountID }, out lt);
    }
    public static string GetList(string RoleGroupName, int StatusID, out List<RoleGroup> lt)
    {
        return DBM.GetList("usp_RoleGroup_SelectSearch", new { RoleGroupName, StatusID }, out lt);
    }
    public static string GetByRoleGroupName(string RoleGroupName, int AccountID, out List<RoleGroup> ltTag)
    {
        return DBM.GetList("usp_RoleGroup_SelectByRoleGroupName", new { RoleGroupName, AccountID }, out ltTag);
    }
    public static string GetListUserManager(int PositionID, int DeptID, out DataTable lt)
    {
        return DBM.ExecStore("usp_RoleGroup_GetListUserManager", new { PositionID, DeptID }, out lt);
    }
    public static string GetSuggestSearch(string TextSearch, int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_RoleGroup_SuggestSearch", new { TextSearch, AccountID }, out lt);
    }
    public static string UpdateIsDelete(int RoleGroupID, bool IsDelete)
    {
        return DBM.ExecStore("usp_RoleGroup_UpdateIsDelete", new { RoleGroupID, IsDelete });
    }
    public static string InserRoleAdmin(DBM dbm, int AccountID, int UserID, out RoleGroup roleGroup)
    {
        roleGroup = null;

        string msg = dbm.SetStoreNameAndParams("usp_RoleGroup_InserRoleAdmin",
            new
            {
                AccountID,
                UserID
            });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out roleGroup);
    }

    public static string GetAllUserByRoleGroups(string RoleGroups, out DataTable lt)
    {
        return DBM.ExecStore("usp_RoleGroup_SelectAllUserByRoleGroups", new { RoleGroups }, out lt);
    }
    public static string GetListSearch(RoleGroupSearch formSearch, out List<RoleGroup> lt)
    {
        lt = null;

        dynamic o;
        string msg = GetListSearch_Parameter(formSearch, out o);
        if (msg.Length > 0) return msg;

        return DBM.GetList("usp_RoleGroup_SelectSearch", o, out lt);
    }
    public static string GetListSearchTotal(RoleGroupSearch formSearch, out int total)
    {
        total = 0;

        dynamic o;
        string msg = GetListSearch_Parameter(formSearch, out o);
        if (msg.Length > 0) return msg;

        return DBM.ExecStore("usp_RoleGroup_SelectSearch_Total", o, out total);
    }
    private static string GetListSearch_Parameter(RoleGroupSearch formSearch, out dynamic o)
    {
        return GetListSearch_Parameter(false, formSearch, out o);
    }
    private static string GetListSearch_Parameter(bool IsReport, RoleGroupSearch formSearch, out dynamic o)
    {
        o = null;
        string msg = "";
        o = new
        {
            formSearch.TextSearch,
            formSearch.RoleGroupID,
            formSearch.UserIDCreate,
            formSearch.StatusID,
            formSearch.AccountID,
            formSearch.PageSize,
            formSearch.CurrentPage
        };

        return msg;
    }

    public static string GetListUser(int AccountID, int TabID, long RoleValue, out DataTable ltAccount)
    {
        ltAccount = null;
        string msg = GetAll(AccountID, out List<RoleGroup> lt);
        if (msg.Length > 0) return msg;

        List<int> ltRoleGroupID = new List<int>();
        foreach (var rg in lt)
        {
            msg = Role.Check(rg, TabID, RoleValue, out bool IsRole);
            if (msg.Length > 0) return msg;

            if (IsRole) ltRoleGroupID.Add(rg.RoleGroupID);
        }

        string RoleGroups = string.Join(", ", ltRoleGroupID.Select(p => p));
        msg = GetAllUserByRoleGroups(RoleGroups, out ltAccount);
        if (msg.Length > 0) return msg;

        return msg;
    }
    public static string ValidateRoleGroup(RoleGroup RoleGroup)
    {
        string msg = "";

        if (RoleGroup.RoleGroupName.Trim().Length == 0) return ("Tên Nhóm quyền không được để trống").ToMessageForUser();

        msg = DataValidator.Validate(RoleGroup).ToErrorMessage();
        if (msg.Length > 0) return msg.ToMessageForUser();

        List<RoleGroup> ltRoleGroup;
        msg = RoleGroup.GetByRoleGroupName(RoleGroup.RoleGroupName.Trim(), RoleGroup.AccountID, out ltRoleGroup);
        if (msg.Length > 0) return msg;
        if (ltRoleGroup.Where(v => !v.IsDelete && v.RoleGroupID != RoleGroup.RoleGroupID).Count() > 0) return ("Đã tồn tại Tên Nhóm quyền '" + RoleGroup.RoleGroupName.Trim() + "'").ToMessageForUser();

        return msg;
    }

    public static string GetByUserID(int UserID, out RoleGroup rg)
    {
        rg = null;

        UserRoleGroup urg;
        string msg = UserRoleGroup.GetOne(UserID, out urg);
        if (msg.Length > 0) return msg;

        int RoleGroupID;
        if (urg == null) RoleGroupID = RoleGroup.USER;
        else RoleGroupID = urg.RoleGroupID;

        msg = RoleGroup.GetOne(RoleGroupID, urg.AccountID, out rg);
        if (msg.Length > 0) return msg;

        if (rg == null || rg.IsDelete)
        {
            RoleGroupID = RoleGroup.USER;
            msg = RoleGroup.GetOne(RoleGroupID, urg.AccountID, out rg);
            if (msg.Length > 0) return msg;
        }

        return msg;
    }
}
public class Role
{
    public long RoleValue { get; set; }
    public string RoleName { get; set; }
    public int TabID { get; set; }
    public string TabName
    {
        get
        {
            return Tab.GetTabName(TabID);
        }
    }
    public int ParentID { get; set; }
    public bool IsRole { get; set; }

    public Role()
    {
        RoleValue = 0;
        RoleName = "";
        TabID = 0;
        IsRole = false;
    }

    public const long ROLE_QLTS_IsVisitPage = 1;
    public const long ROLE_QLTS_CRUD = 2;
    public const long ROLE_QLTS_DUYET = 4;
    public const long ROLE_QLTS_INBARCODE = 8;
    public const long ROLE_QLTS_XNBGTS = 16;//xác nhận bàn giao tài sản
    public const long ROLE_QLTS_XNTTS = 32;//xác nhận trả tài sản
    public const long ROLE_QLTS_THUHOI = 64;//Thu hồi tài sản
    public const long ROLE_QLTS_DCKTL = 128;//điều chuyển kho thành lý
    public const long ROLE_QLTS_VIEWALL = 256;//điều chuyển kho thành lý
    public const long ROLE_QLTS_SUDUNG = 512;//người sử dụng tài sản

    public const long ROLE_QLPDX_IsVisitPage = 1;
    public const long ROLE_QLPDX_CRUD = 2;
    public const long ROLE_QLPDX_DUYET = 4;

    public const long ROLE_QLPDXVP_IsVisitPage = 1;
    public const long ROLE_QLPDXVP_CRUD = 2;
    public const long ROLE_QLPDXVP_DUYET = 4;

    public const long ROLE_QLVV_IsVisitPage = 1;
    public const long ROLE_QLVV_SC = 2;
    public const long ROLE_QLVV_BHSC = 4;
    public const long ROLE_QLVV_BTBD = 8;

    public const long ROLE_SDTS_IsVisitPage = 1;

    public const long ROLE_QLVP_IsVisitPage = 1;
    public const long ROLE_QLVP_CRUD = 2;
    public const long ROLE_QLVP_DUYET = 4;

    public const long ROLE_QLPNK_IsVisitPage = 1;
    public const long ROLE_QLPNK_CRUD = 2;
    public const long ROLE_QLPNK_DUYET = 4;
    public const long ROLE_QLPNK_ViewAll = 8;

    public const long ROLE_QLPXK_IsVisitPage = 1;
    public const long ROLE_QLPXK_CRUD = 2;
    public const long ROLE_QLPXK_DUYET = 4;
    public const long ROLE_QLPXK_ViewAll = 8;

    public const long ROLE_QLKKTS_IsVisitPage = 1;
    public const long ROLE_QLKKTS_CRUD = 2;
    public const long ROLE_QLKKTS_DUYET = 4;

    public const long ROLE_QLKKVP_IsVisitPage = 1;
    public const long ROLE_QLKKVP_CRUD = 2;
    public const long ROLE_QLKKVP_DUYET = 4;

    public const long ROLE_BCTKTS_IsVisitPage = 1;
    public const long ROLE_BCTKVP_IsVisitPage = 1;

    public const long ROLE_LTS_IsVisitPage = 1;
    public const long ROLE_LTS_CRUD = 2;

    public const long ROLE_ND_IsVisitPage = 1;
    public const long ROLE_ND_CRUD = 2;

    public const long ROLE_TC_IsVisitPage = 1;
    public const long ROLE_TC_CRUD = 2;

    public const long ROLE_LVV_IsVisitPage = 1;
    public const long ROLE_LVV_CRUD = 2;

    public const long ROLE_LVP_IsVisitPage = 1;
    public const long ROLE_LVP_CRUD = 2;

    public const long ROLE_KHO_IsVisitPage = 1;
    public const long ROLE_KHO_CRUD = 2;

    public const long ROLE_QLPB_IsVisitPage = 1;
    public const long ROLE_QLPB_CRUD = 2;

    public const long ROLE_QLCV_IsVisitPage = 1;
    public const long ROLE_QLCV_CRUD = 2;

    public const long ROLE_QLND_IsVisitPage = 1;
    public const long ROLE_QLND_CRUD = 2;
    public const long ROLE_QLND_CHANGEPASS = 4;
    public const long ROLE_QLND_CHANGEROLE = 8;

    public const long ROLE_PQ_IsVisitPage = 1;
    public const long ROLE_PQ_CRUD = 2;

    public const long ROLE_TCL_IsVisitPage = 1;

    public const long ROLE_PKN_IsVisitPage = 1;
    public const long ROLE_PKN_CRUD = 2;
    public const long ROLE_PKN_DUYET = 4;

    public const long ROLE_KHOVP_IsVisitPage = 1;

    public static List<Role> GetListRole()
    {
        return new List<Role>
        {
             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLTS_IsVisitPage, TabID = Constants.TabID.QLTS},
             new Role { RoleName="Thêm, sửa, xóa Tài sản", RoleValue = ROLE_QLTS_CRUD, TabID = Constants.TabID.QLTS},
             new Role { RoleName="Duyệt Tài sản", RoleValue = ROLE_QLTS_DUYET, TabID = Constants.TabID.QLTS},
             new Role { RoleName="In Barcode", RoleValue = ROLE_QLTS_INBARCODE, TabID = Constants.TabID.QLTS},
             new Role { RoleName="Bàn giao Tài sản", RoleValue = ROLE_QLTS_XNBGTS, TabID = Constants.TabID.QLTS},
             new Role { RoleName="Xác nhận trả Tài sản", RoleValue = ROLE_QLTS_XNTTS, TabID = Constants.TabID.QLTS},
             new Role { RoleName="Thu hồi Tài sản", RoleValue = ROLE_QLTS_THUHOI, TabID = Constants.TabID.QLTS},
             new Role { RoleName="Điều chuyển kho thành lý", RoleValue = ROLE_QLTS_DCKTL, TabID = Constants.TabID.QLTS},
             new Role { RoleName="Sử dụng tài sản", RoleValue = ROLE_QLTS_SUDUNG, TabID = Constants.TabID.QLTS},
             new Role { RoleName="Xem tất cả Tài sản", RoleValue = ROLE_QLTS_VIEWALL, TabID = Constants.TabID.QLTS},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLPDX_IsVisitPage, TabID = Constants.TabID.QLPDX},
             new Role { RoleName="Thêm, sửa, xóa Phiếu đề xuất", RoleValue = ROLE_QLPDX_CRUD, TabID = Constants.TabID.QLPDX},
             new Role { RoleName="Duyệt Phiếu đề xuất", RoleValue = ROLE_QLPDX_DUYET, TabID = Constants.TabID.QLPDX},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLPDXVP_IsVisitPage, TabID = Constants.TabID.QLPDXVP},
             new Role { RoleName="Thêm, sửa, xóa Phiếu đề xuất vật phẩm", RoleValue = ROLE_QLPDXVP_CRUD, TabID = Constants.TabID.QLPDXVP},
             new Role { RoleName="Duyệt Phiếu đề xuất vật phẩm", RoleValue = ROLE_QLPDXVP_DUYET, TabID = Constants.TabID.QLPDXVP},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLVV_IsVisitPage, TabID = Constants.TabID.QLVV},
             new Role { RoleName="Ghi nhận Vụ việc SC", RoleValue = ROLE_QLVV_SC, TabID = Constants.TabID.QLVV},
             new Role { RoleName="Ghi nhận Vụ việc BH-SC", RoleValue = ROLE_QLVV_BHSC, TabID = Constants.TabID.QLVV},
             new Role { RoleName="Ghi nhận Vụ việc BT-BD", RoleValue = ROLE_QLVV_BTBD, TabID = Constants.TabID.QLVV},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_SDTS_IsVisitPage, TabID = Constants.TabID.SDTS},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLVP_IsVisitPage, TabID = Constants.TabID.QLVP },
             new Role { RoleName="Thêm, Sửa, Xóa Vật phẩm", RoleValue = ROLE_QLVP_CRUD, TabID = Constants.TabID.QLVP },
             new Role { RoleName="Duyệt Vật phẩm", RoleValue = ROLE_QLVP_DUYET, TabID = Constants.TabID.QLVP },

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLPNK_IsVisitPage, TabID = Constants.TabID.QLPNK},
             new Role { RoleName="Thêm, Sửa, Nhập kho", RoleValue = ROLE_QLPNK_CRUD, TabID = Constants.TabID.QLPNK},
             new Role { RoleName="Duyệt Nhập kho", RoleValue = ROLE_QLPNK_DUYET, TabID = Constants.TabID.QLPNK},
             new Role { RoleName="Xem Tất cả", RoleValue = ROLE_QLPNK_ViewAll, TabID = Constants.TabID.QLPNK},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLPXK_IsVisitPage, TabID = Constants.TabID.QLPXK},
             new Role { RoleName="Thêm, Sửa, Xóa Xuất kho", RoleValue = ROLE_QLPXK_CRUD, TabID = Constants.TabID.QLPXK},
             new Role { RoleName="Duyệt Xuất kho", RoleValue = ROLE_QLPXK_DUYET, TabID = Constants.TabID.QLPXK},
             new Role { RoleName="Xem, Tất cả", RoleValue = ROLE_QLPXK_ViewAll, TabID = Constants.TabID.QLPXK},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLKKTS_IsVisitPage, TabID = Constants.TabID.QLKKTS},
             new Role { RoleName="Thêm, Sửa, Xóa Phiếu kiểm kê", RoleValue =ROLE_QLKKTS_CRUD, TabID = Constants.TabID.QLKKTS},
             new Role { RoleName="Duyệt Phiếu kiểm kê", RoleValue = ROLE_QLKKTS_DUYET, TabID = Constants.TabID.QLKKTS},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_KHOVP_IsVisitPage, TabID = Constants.TabID.KHOVP},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_BCTKTS_IsVisitPage, TabID = Constants.TabID.BCTK_TS},
             new Role { RoleName="Truy cập trang", RoleValue = ROLE_BCTKVP_IsVisitPage, TabID = Constants.TabID.BCTK_VP},


             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLKKVP_IsVisitPage, TabID = Constants.TabID.QLKKVP},
             new Role { RoleName="Thêm, Sửa, Xóa Phiếu kiểm kê", RoleValue =ROLE_QLKKVP_CRUD, TabID = Constants.TabID.QLKKVP},
             new Role { RoleName="Duyệt Phiếu kiểm kê", RoleValue = ROLE_QLKKVP_DUYET, TabID = Constants.TabID.QLKKVP},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_LTS_IsVisitPage, TabID = Constants.TabID.LTS},
             new Role { RoleName="Thêm, Sửa, Xóa Loại tài sản", RoleValue = ROLE_LTS_CRUD, TabID = Constants.TabID.LTS},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_ND_IsVisitPage, TabID = Constants.TabID.ND},
             new Role { RoleName="Thêm, Sửa, Xóa Nơi để", RoleValue = ROLE_ND_CRUD, TabID = Constants.TabID.ND},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_TC_IsVisitPage, TabID = Constants.TabID.TC},
             new Role { RoleName="Thêm, Sửa, Xóa Tổ chức", RoleValue = ROLE_TC_CRUD, TabID = Constants.TabID.TC},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_LVV_IsVisitPage, TabID = Constants.TabID.LVV},
             new Role { RoleName="Thêm, Sửa, Xóa Loại vụ việc", RoleValue = ROLE_LVV_CRUD, TabID = Constants.TabID.LVV},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_LVP_IsVisitPage, TabID = Constants.TabID.LVP},
             new Role { RoleName="Thêm, Sửa, Xóa Loại Vật phẩm", RoleValue = ROLE_LVP_CRUD, TabID = Constants.TabID.LVP},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_KHO_IsVisitPage, TabID = Constants.TabID.KHO},
             new Role { RoleName="Thêm, Sửa, Xóa Kho", RoleValue = ROLE_KHO_CRUD, TabID = Constants.TabID.KHO},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLPB_IsVisitPage, TabID = Constants.TabID.QLPB},
             new Role { RoleName="Thêm, Sửa, Xóa Phòng ban", RoleValue = ROLE_QLPB_CRUD, TabID = Constants.TabID.QLPB},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLCV_IsVisitPage, TabID = Constants.TabID.QLCV},
             new Role { RoleName="Thêm, Sửa, Xóa Chức vụ", RoleValue = ROLE_QLCV_CRUD, TabID = Constants.TabID.QLCV},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_QLND_IsVisitPage, TabID = Constants.TabID.QLND},
             new Role { RoleName="Thêm, Sửa, Xóa Người dùng", RoleValue = ROLE_QLND_CRUD, TabID = Constants.TabID.QLND},
             new Role { RoleName="Đổi mật khẩu Người dùng", RoleValue = ROLE_QLND_CHANGEPASS, TabID = Constants.TabID.QLND},
             new Role { RoleName="Gán quyền Người dùng", RoleValue = ROLE_QLND_CHANGEROLE, TabID = Constants.TabID.QLND},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_PQ_IsVisitPage, TabID = Constants.TabID.PQ},
             new Role { RoleName="Thêm, Sửa, Xóa Nhóm quyền", RoleValue = ROLE_PQ_CRUD, TabID = Constants.TabID.PQ},

             new Role { RoleName="Truy cập trang", RoleValue = ROLE_TCL_IsVisitPage, TabID = Constants.TabID.TCL},
        };
    }
    public static string CheckUQXLAllUser(int UserID, out bool IsUQXLAllUser)
    {
        return Role.Check(UserID, Constants.TabID.QLTS, Role.ROLE_QLTS_IsVisitPage, out IsUQXLAllUser);
    }
    public static string CheckVisitPage(int UserID, int TabID)
    {
        long RoleValueVisitPage;
        Role.GetRoleValueVisitPage(TabID, out RoleValueVisitPage);

        return Check(UserID, TabID, RoleValueVisitPage);
    }
    public static string CheckVisitPage(RoleGroup rg, int TabID, out bool IsRole)
    {
        long RoleValueVisitPage;
        Role.GetRoleValueVisitPage(TabID, out RoleValueVisitPage);

        Role.Check(rg, TabID, RoleValueVisitPage, out IsRole);
        return "";
    }
    public static string Check(int UserID, int TabID)
    {
        bool IsRole;
        string msg = Check(UserID, TabID, -1, out IsRole);
        if (msg.Length > 0) return msg;

        if (!IsRole) return "Bạn không có quyền thực hiện chức năng này".ToMessageForUser();
        else return "";
    }
    public static string Check(int UserID, int TabID, long RoleValue)
    {
        bool IsRole;
        string msg = Check(UserID, TabID, RoleValue, out IsRole);
        if (msg.Length > 0) return msg;

        if (!IsRole) return "Bạn không có quyền thực hiện chức năng này".ToMessageForUser();
        else return "";
    }
    public static string Check(int UserID, int TabID, long RoleValue, out bool IsRole)
    {
        IsRole = false;

        RoleGroup rg;
        string msg = RoleGroup.GetByUserID(UserID, out rg);
        if (msg.Length > 0) return msg;

        return Check(rg, TabID, RoleValue, out IsRole);
    }
    public static string Check(RoleGroup rg, int TabID, long RoleValue, out bool IsRole)
    {
        IsRole = false;

        //if (TabID == Constants.TabID.PQ || TabID == Constants.TabID.QLPB || TabID == Constants.TabID.QLCV)
        //{
        //    if (rg.RoleGroupID == RoleGroup.ADMIN) IsRole = true;
        //    else IsRole = false;
        //}
        //else
        //{
        long RoleGroupCol = 0;
        switch (TabID)
        {
            case Constants.TabID.QLTS:
                RoleGroupCol = rg.QLTS;
                break;
            case Constants.TabID.QLPDX:
                RoleGroupCol = rg.QLPDX;
                break;
            case Constants.TabID.QLPDXVP:
                RoleGroupCol = rg.QLPDXVP;
                break;
            case Constants.TabID.KHOVP:
                RoleGroupCol = rg.KHOVP;
                break;
            case Constants.TabID.QLVV:
                RoleGroupCol = rg.QLVV;
                break;
            case Constants.TabID.SDTS:
                RoleGroupCol = rg.SDTS;
                break;
            case Constants.TabID.QLVP:
                RoleGroupCol = rg.QLVP;
                break;
            case Constants.TabID.QLPNK:
                RoleGroupCol = rg.QLPNK;
                break;
            case Constants.TabID.QLPXK:
                RoleGroupCol = rg.QLPXK;
                break;
            case Constants.TabID.QLKKTS:
                RoleGroupCol = rg.QLKKTS;
                break;
            case Constants.TabID.QLKKVP:
                RoleGroupCol = rg.QLKKVP;
                break;
            case Constants.TabID.BCTK_TS:
                RoleGroupCol = rg.BCTK_TS;
                break;
            case Constants.TabID.BCTK_VP:
                RoleGroupCol = rg.BCTK_VP;
                break;
            case Constants.TabID.LTS:
                RoleGroupCol = rg.LTS;
                break;
            case Constants.TabID.ND:
                RoleGroupCol = rg.ND;
                break;
            case Constants.TabID.TC:
                RoleGroupCol = rg.TC;
                break;
            case Constants.TabID.LVV:
                RoleGroupCol = rg.LVV;
                break;
            case Constants.TabID.LVP:
                RoleGroupCol = rg.LVP;
                break;
            case Constants.TabID.KHO:
                RoleGroupCol = rg.KHO;
                break;
            case Constants.TabID.QLPB:
                RoleGroupCol = rg.QLPB;
                break;
            case Constants.TabID.QLCV:
                RoleGroupCol = rg.QLCV;
                break;
            case Constants.TabID.QLND:
                RoleGroupCol = rg.QLND;
                break;
            case Constants.TabID.PQ:
                RoleGroupCol = rg.PQ;
                break;
            case Constants.TabID.TCL:
                RoleGroupCol = rg.TCL;
                break;
            case Constants.TabID.PNK:
                RoleGroupCol = rg.QLPNK;
                break;
            default:
                break;
        }

        if (RoleGroupCol > 0) IsRole = ((RoleGroupCol & RoleValue) == RoleValue);
        else IsRole = false;
        //}

        return "";
    }
    public static string GetRoleValueVisitPage(int tabID, out long RoleValue)
    {
        RoleValue = 0;
        switch (tabID)
        {
            case Constants.TabID.QLTS:
                RoleValue = Role.ROLE_QLTS_IsVisitPage;
                break;
            case Constants.TabID.QLPDX:
                RoleValue = Role.ROLE_QLPDX_IsVisitPage;
                break;
            case Constants.TabID.QLPDXVP:
                RoleValue = Role.ROLE_QLPDXVP_IsVisitPage;
                break;
            case Constants.TabID.QLVV:
                RoleValue = Role.ROLE_QLVV_IsVisitPage;
                break;
            case Constants.TabID.SDTS:
                RoleValue = Role.ROLE_SDTS_IsVisitPage;
                break;
            case Constants.TabID.QLVP:
                RoleValue = Role.ROLE_QLVP_IsVisitPage;
                break;
            case Constants.TabID.QLPNK:
                RoleValue = Role.ROLE_QLPNK_IsVisitPage;
                break;
            case Constants.TabID.QLPXK:
                RoleValue = Role.ROLE_QLPXK_IsVisitPage;
                break;
            case Constants.TabID.BCTK_TS:
                RoleValue = Role.ROLE_BCTKTS_IsVisitPage;
                break;
            case Constants.TabID.BCTK_VP:
                RoleValue = Role.ROLE_BCTKVP_IsVisitPage;
                break;
            case Constants.TabID.KHOVP:
                RoleValue = Role.ROLE_KHOVP_IsVisitPage;
                break;
            case Constants.TabID.QLKKTS:
                RoleValue = Role.ROLE_QLKKTS_IsVisitPage;
                break;
            case Constants.TabID.QLKKVP:
                RoleValue = Role.ROLE_QLKKVP_IsVisitPage;
                break;
            case Constants.TabID.LTS:
                RoleValue = Role.ROLE_LTS_IsVisitPage;
                break;
            case Constants.TabID.ND:
                RoleValue = Role.ROLE_ND_IsVisitPage;
                break;
            case Constants.TabID.TC:
                RoleValue = Role.ROLE_TC_IsVisitPage;
                break;
            case Constants.TabID.LVV:
                RoleValue = Role.ROLE_LVV_IsVisitPage;
                break;
            case Constants.TabID.LVP:
                RoleValue = Role.ROLE_LVP_IsVisitPage;
                break;
            case Constants.TabID.KHO:
                RoleValue = Role.ROLE_KHO_IsVisitPage;
                break;
            case Constants.TabID.QLPB:
                RoleValue = Role.ROLE_QLPB_IsVisitPage;
                break;
            case Constants.TabID.QLCV:
                RoleValue = Role.ROLE_QLCV_IsVisitPage;
                break;
            case Constants.TabID.QLND:
                RoleValue = Role.ROLE_QLND_IsVisitPage;
                break;
            case Constants.TabID.PQ:
                RoleValue = Role.ROLE_PQ_IsVisitPage;
                break;
            case Constants.TabID.TCL:
                RoleValue = Role.ROLE_TCL_IsVisitPage;
                break;
            default:
                break;
        }
        return "";
    }
    public static string GetRoleGroupCol(int tabID, RoleGroup rg, out long RoleGroupCol)
    {
        RoleGroupCol = 0;
        switch (tabID)
        {
            case Constants.TabID.QLTS:
                RoleGroupCol = rg.QLTS;
                break;
            case Constants.TabID.QLPDX:
                RoleGroupCol = rg.QLPDX;
                break;
            case Constants.TabID.QLPDXVP:
                RoleGroupCol = rg.QLPDXVP;
                break;
            case Constants.TabID.QLVV:
                RoleGroupCol = rg.QLVV;
                break;
            case Constants.TabID.SDTS:
                RoleGroupCol = rg.SDTS;
                break;
            case Constants.TabID.QLVP:
                RoleGroupCol = rg.QLVP;
                break;
            case Constants.TabID.QLPNK:
                RoleGroupCol = rg.QLPNK;
                break;
            case Constants.TabID.QLPXK:
                RoleGroupCol = rg.QLPXK;
                break;
            case Constants.TabID.KHOVP:
                RoleGroupCol = rg.KHOVP;
                break;
            case Constants.TabID.QLKKTS:
                RoleGroupCol = rg.QLKKTS;
                break;
            case Constants.TabID.QLKKVP:
                RoleGroupCol = rg.QLKKVP;
                break;
            case Constants.TabID.BCTK_TS:
                RoleGroupCol = rg.BCTK_TS;
                break;
            case Constants.TabID.BCTK_VP:
                RoleGroupCol = rg.BCTK_VP;
                break;
            case Constants.TabID.LTS:
                RoleGroupCol = rg.LTS;
                break;
            case Constants.TabID.ND:
                RoleGroupCol = rg.ND;
                break;
            case Constants.TabID.TC:
                RoleGroupCol = rg.TC;
                break;
            case Constants.TabID.LVV:
                RoleGroupCol = rg.LVV;
                break;
            case Constants.TabID.LVP:
                RoleGroupCol = rg.LVP;
                break;
            case Constants.TabID.KHO:
                RoleGroupCol = rg.KHO;
                break;
            case Constants.TabID.QLPB:
                RoleGroupCol = rg.QLPB;
                break;
            case Constants.TabID.QLCV:
                RoleGroupCol = rg.QLCV;
                break;
            case Constants.TabID.QLND:
                RoleGroupCol = rg.QLND;
                break;
            case Constants.TabID.PQ:
                RoleGroupCol = rg.PQ;
                break;
            case Constants.TabID.TCL:
                RoleGroupCol = rg.TCL;
                break;
            default:
                break;
        }
        return "";
    }
}
