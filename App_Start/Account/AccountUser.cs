using BSS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public class AccountUser : IMappingSingleField
{
    public int UserID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int AccountID { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
    public string UserDeptName { get; set; }
    public string PositionName { get; set; }
    public string FullName { get; set; }
    public string UrlAvatar { get; set; }
    public DateTime? BirthDate { get; set; }
    public int? Sex { get; set; }
    public string Email { get; set; }
    public string Mobile { get; set; }
    public bool IsActive { get; set; }
    public bool IsChangePassFirstLogin { get; set; }
    public int RoleGroupID { get; set; }
    public string UserIDSync { get; set; }
    public List<AccountUserDept> lstAccountUserDept { get; set; }

    public string InsertUpdate(DBM dbm, out AccountUser au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_AccountUser_InsertUpdate",
                    new
                    {
                        UserID,
                        UrlAvatar,
                        UserName,
                        PasswordHash,
                        PasswordSalt,
                        FullName,
                        BirthDate,
                        Sex,
                        Email,
                        Mobile,
                        IsActive,
                        IsChangePassFirstLogin,
                        UserIDSync
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    /// <summary>
    /// Lấy tất cả User
    /// </summary>
    /// <param name="lt"></param>
    /// <returns></returns>
    public static string GetAll(int AccountID, out List<AccountUser> lt)
    {
        return DBM.GetList("usp_AccountUser_SelectAll", new { AccountID }, out lt);
    }
    public static string GetListUserManagementPlace(int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_AccountUser_GetListUserManagementPlace", new { AccountID }, out lt);
    }
    public static string GetAll(int AccountID, out DataTable dt)
    {
        return DBM.ExecStore("usp_AccountUser_SelectAll", new { AccountID }, out dt);
    }

    /// <summary>
    /// Lấy thông tin User theo UserName
    /// </summary>
    /// <param name="UserName">Tài khoản đăng nhập</param>
    /// <param name="u">Đối tượng trả về</param>
    /// <returns></returns>
    /// 
    public static string GetByUserName(string UserName, out List<AccountUser> lt)
    {
        return DBM.GetList("usp_AccountUser_SelectByUserName", new { UserName }, out lt);
    }
    public static string GetOneByUserName(string UserName, out AccountUser accountUser)
    {
        accountUser = null;
        string msg = GetByUserName(UserName, out List<AccountUser> lt);
        if (lt.Count == 0) return "";

        accountUser = lt.FirstOrDefault();

        return msg;


    }
    /// <summary>
    /// Lấy thông tin User theo UserID
    /// </summary>
    /// <param name="UserID">ID Tài khoản đăng nhập</param>
    /// <param name="u">Đối tượng trả về</param>
    /// <returns></returns>
    public static string GetOneByUserID(int UserID, out AccountUser u)
    {
        return DBM.GetOne("usp_AccountUser_SelectByUserID", new { UserID }, out u);
    }
    public static string GetUserInfor(int UserID, out AccountUserInfor u)
    {
        return DBM.GetOne("usp_AccountUser_GetUserInfo", new { UserID }, out u);
    }
    public static string GetOneByObjectGUID(Guid ObjectGUID, int AccountID, out AccountUser u)
    {
        return DBM.GetOne("usp_AccountUser_SelectByObjectGUID", new { ObjectGUID, AccountID }, out u);
    }
    public static string GetSearchByDeptID(int DeptID, string TextSearch, out List<AccountUser> lt)
    {
        return DBM.GetList("usp_AccountUser_SelectSearchByDeptID", new { DeptID, TextSearch }, out lt);
    }
    public static string GetSearchByDeptIDs(string DeptIDs, string TextSearch, out List<AccountUser> lt)
    {
        return DBM.GetList("usp_AccountUser_SelectSearchByDeptIDs", new { DeptIDs, TextSearch }, out lt);
    }
    public static string SelectSuggestSearch(string TextSearch, int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_AccountUser_SelectSuggestSearch", new { TextSearch, AccountID }, out lt);
    }
    public static string Delete(long UserID)
    {
        return DBM.ExecStore("usp_AccountUser_Delete", new { UserID });
    }
    public static string GetListUserExport(out DataTable dt)
    {
        dt = null;
        return DBM.ExecStore("sp_AccountUser_SelectToExportExcel", out dt);
    }
    public static string UpdateUserInfo(int UserID, string FullName, string Email, string Mobile, out AccountUser accountUser)
    {
        return DBM.GetOne("usp_AccountUser_UpdateAccountUser", new { UserID, FullName, Email, Mobile }, out accountUser);
    }
    /// <summary>
    /// Lấy danh sách User trong 1 phòng ban
    /// </summary>
    /// <param name="DeptID">ID phòng ban</param>
    /// <param name="lt"></param>
    /// <returns></returns>
    public static string GetByDeptID(int DeptID, int AccountID, out List<AccountUser> lt)
    {
        return DBM.GetList("usp_AccountUser_SelectByDeptID", new { DeptID, AccountID }, out lt);
    }
    public static string ChangePassword(int UserID, byte[] PasswordSalt, byte[] PasswordHash)
    {
        return DBM.ExecStore("usp_AccountUser_ChangePassword", new { UserID, PasswordSalt, PasswordHash });
    }

    public static string UpdateIsChangepassFirstLogin(int UserID)
    {
        return DBM.ExecStore("usp_AccountUser_UpdateIsChangepassFirstLogin", new { UserID });
    }
    public static string GetUserNameByUserID(long UserID)
    {
        string msg = DBM.GetOne("usp_AccountUser_SelectByUserID", new { UserID }, out AccountUser accountUser);
        return accountUser == null ? "" : accountUser.UserName;
    }
    public static string GetUserManagerBySuperiorIDs(string SuperiorIDs, out List<AccountUser> accoutUserList)
    {
        return DBM.GetList("usp_AccountUser_GetUserManagerBySuperiorIDs", new { SuperiorIDs }, out accoutUserList);
    }
    public static int? GetSex(string SexName)
    {
        if (SexName == null || SexName == "") return null;
        if (SexName.ToLower() == "nam") return 1;
        if (SexName.ToLower() == "nữ") return 0;
        return null;
    }
    public static bool? GetStatusID(string StatusName)
    {
        if (StatusName == null || StatusName == "") return null;
        if (StatusName.ToLower() == "không hoạt động") return false;
        if (StatusName.ToLower() == "hoạt động") return true;
        return null;
    }
    public static string GetUrlAvatar(int? Sex)
    {
        string UrlAvatar = "";
        if (!Sex.HasValue) UrlAvatar = "/img/avatar/User.jpg";
        else if (Sex.Value == Constants.SexType.MALE) UrlAvatar = "/img/avatar/Male.jpg";
        else if (Sex.Value == Constants.SexType.FEMALE) UrlAvatar = "/img/avatar/Female.jpg";

        return UrlAvatar;
    }
    public static string GetListSearch(AccountUserSearch accountUser, out List<AccountUserSearchResult> lt)
    {
        lt = null;

        dynamic o;
        string msg = GetListSearch_Parameter(accountUser, out o);
        if (msg.Length > 0) return msg;

        return DBM.GetList("usp_AccountUser_SelectSearch", o, out lt);
    }
    public static string GetListSearchTotal(AccountUserSearch assetSearch, out int total)
    {
        total = 0;

        dynamic o;
        string msg = GetListSearch_Parameter(assetSearch, out o);
        if (msg.Length > 0) return msg;

        return DBM.ExecStore("usp_AccountUser_SelectSearch_Total", o, out total);
    }
    private static string GetListSearch_Parameter(AccountUserSearch accountUser, out dynamic o)
    {
        return GetListSearch_Parameter(false, accountUser, out o);
    }
    private static string GetListSearch_Parameter(bool IsReport, AccountUserSearch accountUser, out dynamic o)
    {
        o = null;
        string msg = "";
        o = new
        {
            accountUser.TextSearch,
            accountUser.UserID,
            accountUser.DeptID,
            accountUser.PositionID,
            accountUser.RoleGroupID,
            accountUser.IsActive,
            accountUser.AccountID,
            accountUser.PageSize,
            accountUser.CurrentPage
        };

        return msg;
    }
    public static string GetListUserManager(int AccountPositionID, int AccountDeptID, int AccountID, out List<AccountUser> accountUsers)
    {
        return DBM.GetList("usp_AccountUser_GetUserManager", new
        {
            AccountPositionID,
            AccountDeptID,
            AccountID
        }, out accountUsers);
    }
    public string GetName() => UserName;

    public string GetOne(object k, out IMappingSingleField outModel)
    {
        outModel = null;

        string msg = GetOneByUserID((int)k, out var accountUser);
        if (msg.Length > 0) return msg;
        outModel = accountUser;

        return msg;
    }

    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }

}
public class CacheAccountUser
{
    public static string GetInfoUser(int UserID)
    {
        AccountUser.GetOneByUserID(UserID, out AccountUser u);
        AccountUserDept.GetUserDeptByUserId(UserID, out List<AccountUserDept> lt);

        return u.FullName + " (" + string.Join(",", lt.Select(v => v.DeptName + " - " + v.PositionName)) + ")";
    }
}
public class AccountUserImportExcel
{
    public string UserName { get; set; }
    public int UserID { get; set; }

    public string FullName { get; set; }
    public string UrlAvatar { get; set; }

    public string SexName { get; set; }
    public int? Sex { get; set; }

    public string Email { get; set; }
    public string Mobile { get; set; }

    public string StatusName { get; set; }
    public bool IsActive { get; set; }


    public string DeptFullName { get; set; }
    public int DeptID { get; set; }
    public string PositionName { get; set; }
    public int PositionID { get; set; }

    public string RoleGroupName { get; set; }
    public int RoleGroupID { get; set; }
    public int SuperiorID { get; set; }
    public string SuperiorName { get; set; }
}
public class AccountUserSearchResult
{
    public int UserID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string PositionName { get; set; }
    public string PositionFullName { get; set; }
    public string DeptName { get; set; }
    public string DeptFullName { get; set; }
    public int RoleGroupID { get; set; }
    public string RoleGroupName { get; set; }
    public string Mobile { get; set; }
    public bool IsActive { get; set; }
}
public class AccountUserSearch
{
    public const int DONGIAN = 1, NANGCAO = 2;
    public string TextSearch { get; set; }
    public int CategorySearch { get; set; }
    public int UserID { get; set; }
    public int DeptID { get; set; }
    public int PositionID { get; set; }
    public int RoleGroupID { get; set; }
    public int IsActive { get; set; }
    public int AccountID { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public AccountUserSearch()
    {
        UserID = 0;
        DeptID = 0;
        PositionID = 0;
        RoleGroupID = 0;
        IsActive = 1;
        CurrentPage = 0;
        PageSize = 0;
    }
}
public class AccountUserInfor
{
    public int UserID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string UserName { get; set; }
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
    public string FullName { get; set; }
    public string PositionName { get; set; }
    public string UserDeptName { get; set; }
    public string Email { get; set; }
    public string Mobile { get; set; }
    public string UrlAvatar { get; set; }
    public string RoleGroupName { get; set; }
}