using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class ItemProperty
{
    public int ItemPropertyID { get; set; }
    public long ItemID { get; set; }
    public string ItemPropertyName { get; set; }
    public string Value { get; set; }
    public int ItemTypePropertyID { get; set; }
    public int ItemTypeID { get; set; }
    public bool IsRequired { get; set; }
    public string ItemTypePropertyName { get; set; }
    public int ItemTypePropertyDataID { get; set; }
    public string ItemTypePropertyDataName { get; set; }
    public string ItemTypePropertyValueList { get; set; }

    [JsonIgnore]
    public DateTime CreateDate { get; set; }

    [JsonIgnore]
    public DateTime LastUpdate { get; set; }

    public string InsertUpdate(DBM dbm, out ItemProperty outItemProperty)
    {
        outItemProperty = null;

        string msg = dbm.SetStoreNameAndParams("usp_ItemProperty_InsertOrUpdate", new { ItemPropertyID, ItemTypePropertyID, ItemID, ItemPropertyName, Value });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out outItemProperty);
    }

    public static string GetListByIdItem(long ItemID, out List<ItemProperty> outLtItemProperty)
    {
        return DBM.GetList("usp_ItemProperty_GetListByItemID", new { ItemID }, out outLtItemProperty);
    }

    public static string CheckItemPropertyIDUsed(int ItemPropertyID, string ItemTypePropertyValue, out ItemProperty property)
    {
        return DBM.GetOne("usp_ItemProperty_CheckUsed", new { ItemPropertyID, ItemTypePropertyValue }, out property);
    }

    public static string DeletePropertyByItemID(DBM dbm, long ItemID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_ItemProperty_DeleteByItemID", new { ItemID });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }

    /// <summary>
    /// kiểm tra dữ liệu các thuộc tính động của Vật phẩm
    /// </summary>
    /// <param name="ListItemProperty">danh sách thuộc tính động</param>
    /// <returns>string</returns>
    public static string ValidateProperty(List<ItemProperty> ListItemProperty)
    {
        string msg = "";

        string msgError = "";
        foreach (var item in ListItemProperty)
        {
            int AssetTypePropertyID = item.ItemTypePropertyID == 0 ? item.ItemPropertyID : item.ItemTypePropertyID;
            string msgItem = "";

            msg = AssetTypeProperty.GetOneByAssetTypePropertyID(AssetTypePropertyID, out AssetTypeProperty assetTypeProperty);

            if (assetTypeProperty.IsRequired) { if (string.IsNullOrEmpty(item.Value)) msgError += "Bạn phải nhật giá trị bắt buộc cho thuộc tính động: " + item.ItemPropertyName + "\n "; }
            else
            {
                if (string.IsNullOrEmpty(item.Value)) continue;

                
                if (msg.Length > 0) return msg;
                if (assetTypeProperty == null) return "Không tồn tại thuộc tính động với ItemTypePropertyID = " + AssetTypePropertyID;

                AssetProperty.ValidateTypePropertyDate(item.Value, assetTypeProperty,out msgItem);
                if (msgItem.Length > 0) msgError += assetTypeProperty.AssetTypePropertyName + ": " + msgItem + "\n ";
            }
        }

        if (msgError.Length > 0) return msgError;

        return msg;
    }
}
