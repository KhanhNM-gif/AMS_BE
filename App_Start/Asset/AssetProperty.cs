using BSS;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class AssetProperty
{
    public long ID { get; set; }
    public long AssetID { get; set; }
    public int AssetPropertyID { get; set; }
    public string AssetPropertyName { get; set; }
    public string Value { get; set; }
    public bool IsRequired { get; set; }
    public int AssetTypePropertyID { get; set; }
    public string AssetTypePropertyName { get; set; }
    public int AssetTypePropertyDataID { get; set; }
    public string AssetTypePropertyDataName { get; set; }
    public string AssetTypePropertyValueList { get; set; }

    public static string ValidateProperty(List<AssetProperty> ListAssetProperty)
    {
        string msg = "";

        string msgError = "";
        foreach (var item in ListAssetProperty)
        {
            item.AssetPropertyID = item.AssetPropertyID == 0 ? item.AssetTypePropertyID : item.AssetPropertyID;
            string msgItem = "";
            msg = AssetTypeProperty.GetOneByAssetTypePropertyID(item.AssetPropertyID, out AssetTypeProperty assetTypeProperty);
            if (msg.Length > 0) return msg;
            if (assetTypeProperty.IsRequired)
            {
                if (string.IsNullOrEmpty(item.Value)) msgError += "Bạn phải nhật giá trị bắt buộc cho thuộc tính động: " + item.AssetPropertyName + "\n ";
            }
            else
            {
                if (string.IsNullOrEmpty(item.Value)) continue;
                ValidateTypePropertyDate(item.Value, assetTypeProperty, out msgItem);
                if (msgItem.Length > 0) msgError += assetTypeProperty.AssetTypePropertyName + ": " + msgItem + "\n ";
            }
        }

        if (msgError.Length > 0) return msgError;
        return msg;
    }

    public static void ValidateTypePropertyDate(string value, AssetTypeProperty assetTypeProperty, out string msgItem)
    {
        msgItem = "";
        switch (assetTypeProperty.AssetTypePropertyDataID)
        {
            case Constants.TypePropertyData.TEXT:
                if (value.Length > 500) msgItem += "Bạn chỉ được nhập tối đa 500 kí tự";
                break;
            case Constants.TypePropertyData.INT:
                if (value.ToNumber(-1) < 0) msgItem += "Bạn chỉ được nhập kí tự số";
                break;
            case Constants.TypePropertyData.DATE:
                if (!DateTime.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                    msgItem += "Bạn chỉ được nhập kiểu date với định dạng dd/MM/yyyy";
                break;
            case Constants.TypePropertyData.SELECT:
            case Constants.TypePropertyData.LIST:
                if (assetTypeProperty.AssetTypePropertyValueList.Split(',').Count(t => t.Trim() == value.Trim()) == 0)
                    msgItem += "Dữ liệu bạn chọn không nằm trong danh sách cấu hình: " + assetTypeProperty.AssetTypePropertyValueList;
                break;
            case Constants.TypePropertyData.CHECKBOX:
                string[] arrValue = value.Trim().Split(',');
                if (assetTypeProperty.AssetTypePropertyValueList.Split(',').Count(t => arrValue.Contains(t.Trim())) == 0)
                    msgItem += "Dữ liệu bạn chọn không nằm trong danh sách cấu hình: " + assetTypeProperty.AssetTypePropertyValueList;
                break;
            case Constants.TypePropertyData.PASSWORD:
                if (value.Length <= 6) msgItem += "Bạn phải nhận lớn hơn 6 kí tự";
                break;
            default:
                break;
        }
    }

    public AssetProperty()
    {
    }
    public string InsertUpdate(DBM dbm, out AssetProperty au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_AssetProperty_InsertUpdate",
                    new
                    {
                        ID,
                        AssetID,
                        AssetPropertyID,
                        AssetPropertyName,
                        Value
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public static string GetListByAssetID(long AssetID, out List<AssetProperty> lt)
    {
        return DBM.GetList("usp_AssetProperty_SelectByAssetID", new { AssetID }, out lt);
    }
    public static string CheckAssetPropertyIDUsed(int AssetPropertyID, string AssetTypePropertyValue, out AssetProperty property)
    {
        return DBM.GetOne("usp_AssetProperty_CheckUsed", new { AssetPropertyID, AssetTypePropertyValue }, out property);
    }
    public static string Delete(int ID)
    {
        return DBM.ExecStore("usp_AssetProperty_Delete", new { ID });
    }

    public static string SetValueForNewAssetTypeProperty(DBM dbm, int AssetTypeID, int AssetPropertyID, string AssetPropertyName)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AssetProperty_SetValueForNewAssetTypeProperty",
                    new
                    {
                        AssetTypeID,
                        AssetPropertyID,
                        AssetPropertyName
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string GetDayExpiredByAssetID(long AssetID, out string ExpriedDay)
    {
        ExpriedDay = "";
        return DBM.ExecStore("usp_Asset_DayExpired", new { AssetID }, out ExpriedDay);
    }
    public static string DeletePropertyByAssetID(DBM dbm, long AssetID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AssetProperty_DeleteByAssetID",
          new
          {
              AssetID
          });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();

    }
}