using BSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class AccountUserDept
{
    public int UserID { get; set; }
    public string UserName { get; set; }
    public int AccountID { get; set; }

    public int DeptID { get; set; }
    public string DeptName { get; set; }
    public int PositionID { get; set; }
    public string PositionName { get; set; }
    public int SuperiorID { get; set; }
    public bool IsActive { get; set; }
    public bool IsConcurrently { get; set; }

    public AccountUserDept()
    {
    }
    public static string GetOneByObjectGUID(Guid ObjectGUID,int AccountID, out AccountUserDept u)
    {
        return DBM.GetOne("usp_AccountUserDept_SelectByObjectGUID", new { ObjectGUID, AccountID }, out u);
    }
    public static string GetUserDeptByUserId(int UserID, out List<AccountUserDept> ou)
    {
        return DBM.GetList("usp_AccountUserDept_SelectByUserID", new { UserID }, out ou);
    }
    public string InsertUpdate(DBM dbm, out AccountUserDept aud)
    {
        aud = null;
        string msg = dbm.SetStoreNameAndParams("usp_AccountUserDept_InsertUpdate",
                    new
                    {
                        UserID,
                        DeptID,
                        PositionID,
                        SuperiorID,
                        IsActive,
                        IsConcurrently
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out aud);
    }
    public static string GetListUserApprove(int AccountID,out List<AccountUserDept> lt)
    {
        return GetListUser(AccountID, Constants.RoleGroup.KTTC, out lt);
    }
    public static string GetListUserWarehouse(int AccountID, out List<AccountUserDept> lt)
    {
        return GetListUser(AccountID, Constants.RoleGroup.QLK, out lt);
    }
    public static string GetListUserManagerAsset(int AccountID, out List<AccountUserDept> lt)
    {
        return GetListUser(AccountID, Constants.RoleGroup.NQL, out lt);
    }
    public static string GetListUser(int AccountID,int RoleGroupID, out List<AccountUserDept> lt)
    {
        return DBM.GetList("usp_AccountUserDept_GetListAccountByRoleGroup", new { AccountID , RoleGroupID }, out lt);
    }
    public static string GetListUserByAccountID(int AccountID, out List<AccountUserDept> lt)
    {
        return DBM.GetList("usp_AccountUserDept_GetListUserByAccountID", new { AccountID}, out lt);
    }
    public static string GetUserManagerByUserID(int UserID, out string UserNameManager)
    {
        return DBM.GetOne("usp_AccountUserDept_GetListUserByAccountID", new { UserID }, out UserNameManager);
    }
    public static string InsertUpdateIsActive(DBM dbm, int UserID,bool IsActive)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AccountUserDept_InsertUpdateIsActive",
                    new
                    {
                        UserID,
                        IsActive
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
}
