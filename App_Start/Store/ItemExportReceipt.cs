using ASM_API.App_Start.ItemImportReceipt;
using ASM_API.App_Start.ItemProposalForm;
using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

interface ILtItemExport<T>
{
    List<T> ltItemExport { get; set; }
}
public class ItemExportReceiptBase
{
    public Guid ObjectGuid { get; set; }
    public int ItemExportReceiptTypeID { get; set; }
    public int PlaceID { get; set; }
    public DateTime? ExportDate { get; set; }
    public string Note { get; set; }
    public virtual string ItemExportReceiptCode { get; set; }
    public virtual int UserIDCreate { get; set; }
}
public class ItemExportReceipt : ItemExportReceiptBase, ILtItemExport<ItemExportReceiptDetail>
{
    public Guid ObjectGuidItemProposalForm { get; set; }
    [JsonIgnore]
    public override string ItemExportReceiptCode { get; set; }
    [JsonIgnore]
    public override int UserIDCreate { get; set; }
    [JsonIgnore]
    public long ItemExportReceiptID { get; set; }
    [JsonIgnore]
    public int AccountID { get; set; }
    [JsonIgnore]
    public long ItemProposalFormID { get; set; }
    [JsonIgnore]
    public ItemProposalForm itemProposalForm { get; set; }
    public List<ItemExportReceiptDetail> ltItemExport { get; set; }
    public string InsertOrUpdate(DBM dbm, out ItemExportReceipt itemImportReceipt)
    {
        itemImportReceipt = null;

        string msg = dbm.SetStoreNameAndParams("usp_StoreItemExport_InsertUpdate", new
        {
            ItemProposalFormID,
            ItemExportReceiptTypeID,
            PlaceID,
            ItemExportReceiptCode,
            ExportDate,
            Note,
            UserIDCreate,
            AccountID
        });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out itemImportReceipt);
    }
    public static string GetSuggestSearch(string TextSearch, int TypeStore, int AccountID, out DataTable dt)
    {
        return DBM.ExecStore("usp_StoreItem_SuggestSearch", new { TextSearch, TypeStore, AccountID }, out dt);
    }
    public static string GetListPaging(StoreSearch storeSearch, out List<AssetSearchResult> lt, out int total)
    {
        string msg = Paging.ExecByStore(@"usp_ItemImportReceipt_SelectSearch", "a.AssetID", storeSearch, out lt, out total);
        if (msg.Length > 0) return msg;

        return "";
    }


    /*public static string GetSuggestSearch(long IDUser, string TextSearch, out DataTable dt)
    {
        return DBM.ExecStore("usp_ItemImportReceipt_GetSuggestSearch", new { TextSearch, IDUser }, out dt);
    }
    public static string GetListPaging(StoreSearch storeSearch, out List<AssetSearchResult> lt, out int total)
    {
        lt = null; total = 0;

        string msg = Paging.ExecByStore(@"usp_ItemImportReceipt_SelectSearch", "a.AssetID", storeSearch, out lt, out total);
        if (msg.Length > 0) return msg;

        return "";
    }

    public static string GetOneByStoreID(long StoreID, out ItemImportReceipt outStore)
    {
        outStore = null;

        string msg = DBM.GetOne("usp_ItemImportReceipt_GetOneByStoreID", new { StoreID }, out outStore);
        if (msg.Length > 0) return msg;
        if (outStore == null) return "Không tồn tại Phiếu nhập kho có StoreID = " + StoreID;

        return msg;
    }

    public static string GetOneObjectGuid(Guid ObjectGuid, out long storeID)
    {
        storeID = 0;

        string msg = DBM.GetOne("usp_ItemImportReceipt_GetStoreIDByObjectGuid", new { ObjectGuid }, out ItemImportReceipt outStore);
        if (msg.Length > 0) return msg;
        if (outStore == null) return "Không tồn tại Phiếu nhập kho có ObjectGuid = " + ObjectGuid;

        storeID = outStore.ItemImportReceiptID;

        return msg;
    }*/
}
public class ItemExportReceiptViewDetail : ItemExportReceiptBase, ILtItemExport<ItemExportReceiptDetailView>
{
    /* [JsonIgnore]
     public override long ItemEx { get; set; }*/
    public string ItemProposalFormCode { get; set; }
    public string ItemImportReceiptTypeName { get; set; }
    public string UserProposalDetail { get; set; }
    public string PlaceName { get; set; }
    public string StatusName { get; set; }
    public string TranferHandingName { get; set; }
    public int StatusID { get; set; }
    public List<ItemExportReceiptDetailView> ltItemExport { get; set; }

    public static string ViewDetail(long ItemExportReceiptID, out ItemExportReceiptViewDetail outItemImportReceiptViewDetail)
    {
        return DBM.GetOne("usp_ItemExportReceiptViewDetail_GetOne", new { ItemExportReceiptID }, out outItemImportReceiptViewDetail);
    }
}

