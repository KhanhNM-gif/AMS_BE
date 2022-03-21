using ASM_API.App_Start.TableModel;
using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

public class ItemImportReceiptDetailBase
{
    public long ItemID { get; set; }
    public long ManufacturerID { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public long PretaxPrice { get; set; }
    public virtual float Quantity { get; set; }
}

public class ItemImportReceiptDetail : ItemImportReceiptDetailBase, IKeyCompare
{
    [JsonIgnore]
    public string ItemCode { get; set; }
    [JsonIgnore]
    public long ID { get; set; }
    [JsonIgnore]
    public override float Quantity { get; set; }
    [JsonIgnore]
    public int VAT { get; set; }
    [JsonIgnore]
    public long ItemImportReceiptID { get; set; }
    public bool isBatchManagement { get; set; }


    private static DataTable GetDataTable(List<ItemImportReceiptDetail> lt)
    {
        DataTable dt = new DataTable();

        dt.Columns.Add("ItemImportReceiptID", typeof(long));
        dt.Columns.Add("ItemID", typeof(long));
        dt.Columns.Add("ManufacturerID", typeof(int));
        dt.Columns.Add("ManufacturingDate", Nullable.GetUnderlyingType(typeof(DateTime)) ?? typeof(DateTime));
        dt.Columns.Add("ExpiryDate", Nullable.GetUnderlyingType(typeof(DateTime)) ?? typeof(DateTime));
        dt.Columns.Add("PretaxPrice", typeof(long));
        dt.Columns.Add("VAT", typeof(int));
        dt.Columns.Add("Quantity", typeof(int));

        foreach (var item in lt)
            dt.Rows.Add(
                item.ItemImportReceiptID,
                item.ItemID,
                item.ManufacturerID,
                item.ManufacturingDate,
                item.ExpiryDate,
                item.PretaxPrice,
                item.VAT,
                item.Quantity);

        return dt;
    }


    public static string InsertByDataTable(DBM dbm, long ItemImportReceiptID, List<ItemImportReceiptDetail> lt, out List<ItemImportReceiptDetail> outLt)
    {
        outLt = null;
        string msg = dbm.SetStoreNameAndParams("usp_ItemImportReceiptDetail_InsertByDataTable",
                    new
                    {
                        TypeData = GetDataTable(lt),
                        ItemImportReceiptID = ItemImportReceiptID
                    }
                    );

        if (msg.Length > 0) return msg;

        return dbm.GetList(out outLt);
    }

    public string DisplayNameKey() => "Vật phẩm";

    public object GetKey() => ItemCode;
}
public class ItemImportReceiptDetailView : ItemImportReceiptDetailBase
{
    public override float Quantity { get; set; }
    public string ItemName { get; set; }
    public string ManufacturerName { get; set; }
    public string ItemUnitName { get; set; }
    public long Amount { get { return (long)(Quantity * PretaxPrice); } }

    public static string GetOne(long ItemImportReceiptID, out List<ItemImportReceiptDetailView> outItemImportReceiptDetailView)
    {
        return DBM.GetList("usp_ItemImportReceiptDetailView_GetOne", new { ItemImportReceiptID }, out outItemImportReceiptDetailView);
    }
}