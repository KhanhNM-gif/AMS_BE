using System;
using System.Collections.Generic;
using BSS;
using System.Data;

public class AssetTypeProperty
{
    public int AssetTypePropertyID { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypePropertyName { get; set; }
    public int AssetTypePropertyDataID { get; set; }
    public string AssetTypePropertyDataName { get; set; }
    public string AssetTypePropertyValueList { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }

    public AssetTypeProperty()
    {
    }
    public string InsertUpdate(DBM dbm, out AssetTypeProperty au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_AssetTypeProperty_InsertUpdate",
                    new
                    {
                        AssetTypePropertyID,
                        AssetTypeID,
                        AssetTypePropertyName,
                        AssetTypePropertyDataID,
                        AssetTypePropertyValueList,
                        IsRequired
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public static string GetOneByAssetTypePropertyID(int AssetTypePropertyID, out AssetTypeProperty assetTypeProperty)
    {
        return DBM.GetOne("usp_AssetTypeProperty_GetByID", new { AssetTypePropertyID }, out assetTypeProperty);
    }
    public static string GetListByAssetTypeID(int AssetTypeID, out List<AssetTypeProperty> lt)
    {
        return DBM.GetList("usp_AssetTypeProperty_GetByAssetTypeID", new { AssetTypeID }, out lt);
    }
    public static string GetListByName(string AssetTypePropertyName, out List<AssetTypeProperty> lt)
    {
        return DBM.GetList("usp_AssetTypeProperty_GetByName", new { AssetTypePropertyName }, out lt);
    }
    public static string Delete(int AssetTypePropertyID)
    {
        return DBM.ExecStore("usp_AssetTypeProperty_DeleteByID", new { AssetTypePropertyID });
    }
}