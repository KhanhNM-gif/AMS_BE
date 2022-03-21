using BSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class ItemUnit
{
    public int ItemUnitID { get; set; }
    public string ItemUnitName { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool Active { get; set; }

    public static string GetOne(int IdItemUnit,out ItemUnit outItemUnit)
    {
        return DBM.GetOne("usp_ItemUnit_GetOne", new { IdItemUnit }, out outItemUnit);
    }
    public static string GetOneByItemUnitName(string ItemUnitName, out ItemUnit outItemUnit)
    {
        return DBM.GetOne("usp_ItemUnit_GetOneByNameItemUnit", new { ItemUnitName }, out outItemUnit);
    }
    public static string GetList(out List<ItemUnit> ltItemUnit)
    {
        return DBM.GetList("usp_ItemUnit_GetList", out ltItemUnit);
    }
}
