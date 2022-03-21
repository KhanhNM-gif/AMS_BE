using BSS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

public class AccountDept : AccountUserDept
{
    public Guid ObjectGuid { get; set; }
    public int DeptIDParent { get; set; }
    public string DeptCodeParent { get; set; }
    public string DeptNameParent { get; set; }
    public string DeptCode { get; set; }
    public string DeptShortName { get; set; }
    public string DeptFullName { get; set; }
    public string DeptFullNameParent { get; set; }
    public string DeptIDSync { get; set; }
    public static string GetList(int AccountID,out List<AccountDept> lt)
    {
        return DBM.GetList("usp_AccountDept_SelectAll", new { AccountID }, out lt);
    }
    public static string GetListByFilter(string DeptName, int IsActive,int AccountID, out List<AccountDept> lt)
    {
        return DBM.GetList("usp_AccountDept_GetByFilter", new { DeptName, IsActive, AccountID }, out lt);
    }
    public static string GetListSelectByUserID(int UserID,int AccountID, out List<AccountDept> lt)
    {
        return DBM.GetList("usp_AccountDept_SelectByUserID", new { UserID, AccountID }, out lt);
    }
    public static string Delete(int DeptID,int AccountID)
    {
        return DBM.ExecStore("usp_AccountDept_DeleteByDeptID", new { DeptID, AccountID });
    }
    public static string GetOneByDeptID(int DeptID,int AccountID, out AccountDept o)
    {
        return DBM.GetOne("usp_AccountDept_SelectByDeptID", new { DeptID, AccountID }, out o);
    }
    public static string GetOneAccountUserDeptByDeptID(int DeptID, out List<AccountUser> o)
    {
        return DBM.GetList("usp_AccountUserDept_SelectByDeptID", new { DeptID }, out o);
    }
    public static string GetByAccountDeptName(string DeptName,int AccountID ,out List<AccountDept> lstaccountDepts)
    {
        return DBM.GetList("usp_AccountDept_SelectByDeptName", new { DeptName , AccountID }, out lstaccountDepts);
    }
    public static string GetListChildByDeptID(int DeptID, int AccountID, out List<AccountDept> lstaccountDepts)
    {
        return DBM.GetList("usp_AccountDept_SelectByDeptIDChild", new { DeptID, AccountID }, out lstaccountDepts);
    }
    public static string GetAccountDeptByUserID(int UserID, int AccountID, out AccountDept accountDept)
    {
        return DBM.GetOne("usp_AccountDept_GetAccountDeptByUserID", new { UserID, AccountID }, out accountDept);
    }
    public string InsertUpdate(DBM dbm, out AccountDept o)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AccountDept_InsertUpdate",
                    new
                    {
                        DeptID,
                        DeptIDParent,
                        DeptCode,
                        DeptName = DeptName.Trim(),
                        IsActive,
                        AccountID,
                        DeptIDSync
                    }
                    );
        return dbm.GetOne(out o);
    }

    public static string GetListDeptExport(int AccountID, out DataTable dt)
    {
        dt = null;
        return DBM.ExecStore("sp_AccountDept_SelectToExportExcel", new { AccountID }, out dt);
    }
}
