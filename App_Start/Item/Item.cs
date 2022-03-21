using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

public class Item
{
    public long ItemID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int ItemTypeID { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public int ItemUnitStatusID { get; set; }
    public string ItemImageName { get; set; }
    public string ItemImageContentBase64 { get; set; }
    public string ItemImagePath { get; set; }
    public int WarningThreshold { get; set; }
    public string ItemNote { get; set; }
    public int ItemStatusID { get; set; }

    [JsonIgnore]
    public string ItemStatusName { get; set; }
    public int SupplierID { get; set; }
    public int AccountID { get; set; }
    public int UserIDCreate { get; set; }
    public int UserIDManager { get; set; }
    public int UserIDApprove { get; set; }
    public int WarningDate { get; set; }
    public bool IsSendApprove { get; set; }
    [JsonIgnore]
    public DateTime CreateDate { get; set; }
    [JsonIgnore]
    public DateTime LastUpdate { get; set; }
    public List<ItemProperty> ListItemProperty { get; set; }

    public static string GetOneByGuid(Guid ObjectGuid, out Item item)
    {
        item = null;

        string msg = CacheObject.GetItemIDbyGUID(ObjectGuid, out long itemID);
        if (msg.Length > 0) return msg;

        msg = GetOneByItemID(itemID, out item);
        if (msg.Length > 0) return msg;

        return msg;
    }

    public static string GetOneByItemID(long itemID, out Item item)
    {
        return DBM.GetOne("usp_Item_GetByID", new { itemID }, out item);
    }

    public static string GetByItemCode(string ItemCode, int AccountID, out Item existItemCode)
    {
        return DBM.GetOne("usp_Item_GetByItemCode", new { ItemCode, AccountID }, out existItemCode);
    }

    public static string CheckExistItem(string ItemCode, int ItemTypeID, string ItemName, int ItemUnitStatusID, int AccountID, out Item existItem)
    {
        return DBM.GetOne("usp_Item_CheckExistItem", new { ItemCode, ItemTypeID, ItemName, ItemUnitStatusID, AccountID }, out existItem);
    }

    public static string GetOneObjectGuid(Guid ObjectGuid, out long itemID)
    {
        itemID = 0;

        string msg = DBM.GetOne("usp_Item_GetByObjectGuid", new { ObjectGuid }, out Item outItem);
        if (msg.Length > 0) return msg;

        if (outItem == null) return ("Không tồn tại vật phẩm có ObjectGuid = " + ObjectGuid);

        itemID = outItem.ItemID;

        return msg;
    }

    public static string GetItemIDsByObjectGuids(string objectGuids, out string itemIDs)
    {
        itemIDs = "";
        string msg = "";
        string[] lstObjectGuids = objectGuids.Split(',');
        List<long> ltItemID = new List<long>();
        foreach (var strObjectGuid in lstObjectGuids)
        {
            Guid ObjectGuid = strObjectGuid.ToGuid(Guid.Empty);
            if (ObjectGuid == Guid.Empty) return "ObjectGuid = " + strObjectGuid + " không hợp lệ";

            msg = CacheObject.GetItemIDbyGUID(ObjectGuid, out long itemID);
            if (msg.Length > 0) return msg;

            ltItemID.Add(itemID);
        }
        itemIDs = string.Join(",", ltItemID);

        return "";
    }
    public static string GetListItemByItemIDs(string ItemIDs, int AccountID, out List<Item> lt)
    {
        return DBM.GetList("usp_Item_GetItemByItemIDs", new { ItemIDs, AccountID }, out lt);
    }
    public static string GetItemByItemIDs(string ItemIDs, int AccountID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Item_GetItemByItemIDs", new { ItemIDs, AccountID }, out dt);
    }

    public static string GetListItemHandling(string ItemIDs, int AccountID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Item_GetListItemHandling", new { ItemIDs, AccountID }, out dt);
    }

    public static string GetListStatusItemByItemIDs(string ItemIDs, int AccountID, out List<Item> outLtItem)
    {
        return DBM.GetList("usp_Item_GetListStatusItemByItemIDs", new { ItemIDs, AccountID }, out outLtItem);
    }

    public string InsertOrUpdate(DBM dbm, out Item item)
    {
        item = null;

        string msg = dbm.SetStoreNameAndParams("usp_Item_InsertOrUpdate",
                new
                {
                    ItemID,
                    ItemTypeID,
                    ItemCode,
                    ItemName,
                    ItemUnitStatusID,
                    ItemImagePath,
                    WarningThreshold,
                    ItemNote,
                    ItemStatusID,
                    SupplierID,
                    WarningDate,
                    AccountID,
                    UserIDCreate,
                    UserIDManager,
                    UserIDApprove
                });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out item);
    }

    public string InsertOrUpdateByExcel(DBM dbm, out Item item)
    {
        item = null;

        string msg = dbm.SetStoreNameAndParams("usp_Item_InsertByExcel",
                new
                {
                    ItemID,
                    ItemTypeID,
                    ItemName,
                    ItemUnitStatusID,
                    WarningThreshold,
                    ItemStatusID,
                    SupplierID,
                    WarningDate,
                    AccountID,
                    UserIDCreate,
                    UserIDManager,
                    UserIDApprove
                });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out item);
    }

    public static string GetOneViewDetailByGuid(Guid ObjectGuid, int AccountID, out ItemViewDetail outItemViewDetail)
    {
        outItemViewDetail = null;

        string msg = CacheObject.GetItemIDbyGUID(ObjectGuid, out long ItemID);
        if (msg.Length > 0) return msg;

        return DBM.GetOne("usp_Item_ViewDetailByID", new { ItemID, AccountID }, out outItemViewDetail);
    }

    public static string UpdateStatusItem(DBM dbm, long ItemID, int StatusID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Item_UpdateItemStatus", new { ItemID, StatusID });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }

    public static string UpdateStatusID_Approve(DBM dbm, string Items, int UserIDApprove, int ItemStatusID, int AccountID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Item_UpdateStatusID_Approve", new { Items, UserIDApprove, ItemStatusID, AccountID });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }

    public static string GetSuggestSearch(string textSearch, int AccountID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Item_SuggestSearch", new { textSearch, AccountID }, out dt);
    }

    public static string GetListPaging(ItemSearch itemSearch, out List<ItemSearchResult> lt, out int total)
    {
        lt = null; total = 0;

        string msg = GetListPaging_Parameter(itemSearch, out dynamic para);
        if (msg.Length > 0) return msg;

        msg = Paging.ExecByStore(@"usp_Item_SelectSearch", "item.ItemID", para, out lt, out total);
        if (msg.Length > 0) return msg;

        return "";
    }

    private static string GetListPaging_Parameter(ItemSearch itemSearch, out dynamic o)
    {
        o = new
        {
            itemSearch.TextSearch,
            itemSearch.CategorySearch,
            itemSearch.ItemTypeIDs,
            itemSearch.ItemStatusIDs,
            itemSearch.ItemDateFrom,
            itemSearch.ItemDateTo,
            itemSearch.CurrentPage,
            itemSearch.PageSize,
            itemSearch.AccountID,
            itemSearch.UserID
        };

        return "";
    }

    public static string GetListByItemType(int ItemTypeID, int AccountID, out List<Item> outLtItem)
    {
        return DBM.GetList("usp_Item_GetListByItemTypeId", new { ItemTypeID, AccountID }, out outLtItem);
    }
}

public class ItemViewDetail
{
    public long ItemID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int ItemTypeID { get; set; }
    public string ItemTypeName { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public int ItemUnitStatusID { get; set; }
    public string ItemUnitName { get; set; }
    public string ItemImagePath { get; set; }
    public int WarningThreshold { get; set; }
    public int ItemStatusID { get; set; }
    public string ItemStatusName { get; set; }
    public int SupplierID { get; set; }
    public string SupplierName { get; set; }
    public int WarningDate { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public string UserNameManager { get; set; }
    public string FullNameManager { get; set; }
    public string PositonNameManager { get; set; }
    public string UserNameApprove { get; set; }
    public string FullNameApprove { get; set; }
    public string PositonNameApprove { get; set; }
    public string ItemNote { get; set; }
    public List<ItemProperty> ListItemProperty { get; set; }
}

public class ItemImportExcel
{
    public string STT { get; set; }
    public int ItemTypeID { get; set; }
    public string ItemTypeName { get; set; }
    public string ItemName { get; set; }
    public string ItemCode { get; set; }
    public int ItemUnitStatusID { get; set; }
    public string ItemUnitName { get; set; }
    public int WarningThreshold { get; set; }
    public string strWarningThreshold { get; set; }
    public string strExpiry { get; set; }
    public int WarningDate { get; set; }
    public string strWarningDate { get; set; }
    public int SupplierID { get; set; }
    public string SupplierName { get; set; }

    public List<ItemProperty> LtItemProperty { get; set; }
}
public class ItemSearch
{
    [JsonIgnore]
    public int AccountID { get; set; }

    [JsonIgnore]
    public long UserID { get; set; }
    public string TextSearch { get; set; }
    public int CategorySearch { get; set; } = 1;
    public string ItemTypeIDs { get; set; }
    public string ItemStatusIDs { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime ItemDateFrom { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime ItemDateTo { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public ItemSearch()
    {
        TextSearch = "";
        CurrentPage = 1;
        PageSize = 20;
        ItemTypeIDs = ItemStatusIDs = "";

        DateTime dtDefault = DateTime.Parse("1900-01-01");
        ItemDateFrom = ItemDateTo = dtDefault;
    }
}
public class ItemEasySearch
{
    public string TextSearch { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public ItemEasySearch()
    {
        TextSearch = "";
        CurrentPage = 1;
        PageSize = 20;
    }
}

public class ItemSearchResult
{
    public long ItemID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int ItemTypeID { get; set; }
    public string ItemTypeName { get; set; }
    public string ItemImagePath { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public int ItemUnitStatusID { get; set; }
    public string ItemUnitName { get; set; }
    public int WarningThreshold { get; set; }
    public int ItemStatusID { get; set; }
    public string ItemStatusName { get; set; }
    public int UserIDCreate { get; set; }
    public int UserIDApprove { get; set; }
    public int WarningDate { get; set; }
    public DateTime CreateDate { get; set; }
    public ButtonShowItem ButtonShow { get; set; }
}
