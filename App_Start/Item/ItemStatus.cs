using BSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class ItemStatus
{
    public long ItemStatusID { get; set; }
    public string ItemStatusName { get; set; }
    public string ItemStatusNameShort { get; set; }
    public bool Active { get; set; }
    public static string GetOne(int ItemStatusID,out ItemStatus outItemStatus)
    {
        return DBM.GetOne("usp_ItemStatus_GetOneById", new { ItemStatusID }, out outItemStatus);
    }
    public static string GetAll(out List<ItemStatus> lt)
    {
        return DBM.GetList("usp_ItemStatus_GetAll", new { }, out lt);
    }
}
