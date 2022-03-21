using BSS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

public class AccountPosition
{
    public int PositionID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int PositionIDParent { get; set; }
    public string PositionCodeParent { get; set; }
    public string PositionNameParent { get; set; }
    public string PositionCode { get; set; }
    public string PositionName { get; set; }
    public int AccountID { get; set; }
    public DateTime? LastUpdate { get; set; }
    public DateTime? CreateDate { get; set; }
    public bool IsActive { get; set; }
    public string PositionIDSync { get; set; }

    public static string GetList(int AccountID, out List<AccountPosition> lt)
    {
        return DBM.GetList("usp_AccountPosition_SelectAll", new { AccountID }, out lt);
    }
    public static string GetByAccountPositionName(string PositionName,int AccountID, out List<AccountPosition> lstaccountPosition)
    {
        return DBM.GetList("usp_AccountPosition_SelectByPositionName", new { PositionName, AccountID }, out lstaccountPosition);
    }
    public static string GetOneByPositionID(int PositionID,int AccountID, out AccountPosition o)
    {
        return DBM.GetOne("usp_AccountPosition_SelectByPositionID", new { PositionID, AccountID }, out o);
    }
    public static string GetListByFilter(string PositionName, int IsActive,int AccountID, out List<AccountPosition> lt)
    {
        return DBM.GetList("usp_AccountPosition_GetByFilter", new { PositionName, IsActive, AccountID }, out lt);
    }
    public static string Delete(int PositionID,int AccountID)
    {
        return DBM.ExecStore("usp_AccountPosition_DeleteByPositionID", new { PositionID, AccountID });
    }
    public static string GetOneAccountUserDeptByPositionID(int PositionID, int AccountID, out List<AccountUser> o)
    {
        return DBM.GetList("usp_AccountUserDept_SelectByPositionID", new { PositionID, AccountID }, out o);
    }
    public static string GetListChildByPositionID(int PositionID, int AccountID, out List<AccountPosition> lstaccountPosition)
    {
        return DBM.GetList("usp_AccountPosition_SelectByDeptIDChild", new { PositionID, AccountID }, out lstaccountPosition);
    }
    public static string GetPostionListByPostionIDs(string PositionIDs, int AccountID, out List<AccountPosition> accoutPostionList)
    {
        return DBM.GetList("usp_AccountPosition_GetPositionListByPositionIDs", new { PositionIDs, AccountID }, out accoutPostionList);
    }
    public static string GetAccountPositionByUserIDs(string UserIDs, int AccountID, out List<AccountPosition> accoutPostionList)
    {
        return DBM.GetList("usp_AccountPosition_GetAccountPositionByUserIDs", new { UserIDs, AccountID }, out accoutPostionList);
    }
    public string InsertUpdate(DBM dbm, out AccountPosition o)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AccountPosition_InsertUpdate",
                    new
                    {
                        PositionID,
                        PositionIDParent,
                        PositionCode,
                        PositionName,
                        IsActive,
                        AccountID,
                        PositionIDSync
                    }
                    );
        return dbm.GetOne(out o);
    }
    public static string GetListPositionExport(int AccountID, out DataTable dt)
    {
        dt = null;
        return DBM.ExecStore("sp_AccountPosition_SelectToExportExcel", new { AccountID }, out dt);
    }
}
