using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

public class UserRoleGroup
{
    public long UserID { get; set; }
    public string UserName { get; set; }
    public int AccountID { get; set; }

    [JsonIgnore]
    public Guid ObjectGuid { get; set; }
    public int RoleGroupID { get; set; }
    [JsonIgnore]
    public int UserIDCreate { get; set; }
    [JsonIgnore]
    public DateTime LastUpdate { get; set; }
    [JsonIgnore]
    public DateTime CreateDate { get; set; }

    public UserRoleGroup()
    {
    }
    public UserRoleGroup(int UserID, int RoleGroupID, int UserIDCreate)
    {
        this.UserID = UserID;
        this.RoleGroupID = RoleGroupID;
    }

    public string InsertUpdate(DBM dbm, out UserRoleGroup ur)
    {
        ur = null;

        string msg = dbm.SetStoreNameAndParams("usp_UserRoleGroup_InsertUpdate",
            new
            {
                UserID,
                RoleGroupID,
                UserIDCreate,
                AccountID
            });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out ur);
    }

    public static string GetOne(int UserID, out UserRoleGroup o)
    {
        return DBM.GetOne("usp_UserRoleGroup_SelectByUserID", new { UserID }, out o);
    }
    public static string GetListByRoleGroupID(int RoleGroupID, out List<UserRoleGroup> lt)
    {
        return DBM.GetList("usp_UserRoleGroup_SelectByRoleGroupID", new { RoleGroupID }, out lt);
    }

    public static string GetList(string TextSearch, int RoleGroupID, int DeptID, int IsActive, int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_UserRoleGroup_SelectSearch", new { TextSearch, RoleGroupID, DeptID, IsActive, AccountID }, out lt);
    }
    public static string GetSuggestSearch(string textSearch, int AccountID, out DataTable dt)
    {
        dt = null;

        return DBM.ExecStore("usp_AccountUser_SelectSuggestSearch", new { textSearch, AccountID }, out dt);
    }
    public static string UpdateRoleGroupIDByUserID(long UserID, int RoleGroupID)
    {
        return DBM.ExecStore("usp_UserRoleGroup_UpdateRoleGroupIDByUserID", new { UserID, RoleGroupID });
    }
}