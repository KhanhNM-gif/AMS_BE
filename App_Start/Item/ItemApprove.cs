using BSS;
using System;
using System.Collections.Generic;

public class ItemApprove
{
    public int ItemApproveID { get; set; }
    public Guid ObjectGuid { get; set; }
    public long ItemID { get; set; }
    public string Content { get; set; }
    public int UserIDApprove { get; set; }
    public bool IsApprove { get; set; }
    public string Reason { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }

    public static string Insert(DBM dbm, string ItemIDs, string Content, int UserIDApprove)
    {
        string msg = dbm.SetStoreNameAndParams("usp_ItemApprove_Insert", new { ItemIDs, Content, UserIDApprove });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string GetOne(long ItemApproveID, out ItemApprove outItemApprove)
    {
        return DBM.GetOne("usp_ItemApprove_GetOne", new { ItemApproveID }, out outItemApprove);
    }

    public static string Update(DBM dbm, string ItemIDs, bool IsApprove, string Reason, int UserIDApprove)
    {
        string msg = dbm.SetStoreNameAndParams("usp_ItemApprove_Update", new { ItemIDs, IsApprove, Reason, UserIDApprove });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
}

public class ItemSenderApprove
{
    public List<Item> LtItem { get; set; }
    public string Content { get; set; }
    public int UserIDApprove { get; set; }
}

public class ComfirmApproveItem
{
    public List<Item> LtItem { get; set; }
    public bool IsApprove { get; set; }//IsApprove = True Đồng ý duyệt, IsApprove= False Từ chối duyệt
    public string Reason { get; set; }
}