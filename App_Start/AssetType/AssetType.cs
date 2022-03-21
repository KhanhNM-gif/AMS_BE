using BSS;
using System;
using System.Collections.Generic;
using System.Data;

public class AssetType
{
    public int AssetTypeID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int AssetTypeGroupID { get; set; }
    public string AssetTypeGroupName { get; set; }
    public string AssetTypeName { get; set; }
    public string AssetTypeCode { get; set; }
    public string AssetTypeDescription { get; set; }
    public int AccountID { get; set; }
    public bool IsActive { get; set; }
    public string ActiveText { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<AssetTypeProperty> ListAssetTypeProperty { get; set; }
    public string InsertUpdate(DBM dbm, out AssetType au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_AssetType_InsertUpdate",
                    new
                    {
                        AssetTypeID,
                        AssetTypeGroupID,
                        AssetTypeName = AssetTypeName.Trim(),
                        AssetTypeCode = AssetTypeCode.Trim(),
                        AssetTypeDescription,
                        IsActive,
                        AccountID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public static string GetOneByAssetTypeID(int AssetTypeID, int AccountID, out AssetType assetType)
    {
        return DBM.GetOne("usp_AssetType_GetByAssetTypeID", new { AssetTypeID, AccountID }, out assetType);
    }
    public static string CheckExitsAssetTypeByCodeAndName(string AssetTypeCode, string AssetTypeName, int AssetTypeGroupID, int AccountID, out AssetType assetType)
    {
        return DBM.GetOne("usp_AssetType_CheckExitsAssetTypeByCodeAndName", new { AssetTypeCode, AssetTypeName, AssetTypeGroupID, AccountID }, out assetType);
    }
    public static string GetActiveByAssetTypeID(int AssetTypeID, int AccountID, out AssetType assetType)
    {
        return DBM.GetOne("usp_AssetType_GetActiveByAssetTypeID", new { AssetTypeID, AccountID }, out assetType);
    }
    public static string SearchAllByFilter(string TextSearch, int IsActive, int AssetTypeGroupID, int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_AssetType_GetAllByFilter", new { TextSearch, IsActive, AssetTypeGroupID, AccountID }, out lt);
    }
    public static string Delete(int AssetTypeID, int AccountID)
    {
        return DBM.ExecStore("usp_AssetType_DeleteByAssetTypeID", new { AssetTypeID, AccountID });
    }
    public static string GetAll(int AssetTypeGroupID, int AccountID, out List<AssetType> u)
    {
        return DBM.GetList("usp_AssetType_GetAll", new { AccountID, AssetTypeGroupID }, out u);
    }
    public static string GetAllByActive(int AccountID, int AssetTypeGroupID, out List<AssetType> u)
    {
        return DBM.GetList("usp_AssetType_GetAllByActive", new { AccountID, AssetTypeGroupID }, out u);
    }
    public static string GetListByPlaceID(int AccountID,string BatchIDs, int PlaceID, int AssetTypeGroupID, out List<AssetType> u)
    {
        return DBM.GetList("usp_AssetType_GetListByPlaceID", new { PlaceID, BatchIDs, AccountID, AssetTypeGroupID }, out u);
    }
}
public class AssetTypeMenu
{
    public int MenuID { get; set; }
    public string MenuName { get; set; }
    public int AssetTypeID { get; set; }
    public int AccountID { get; set; }
    public static string GetAll(int AccountID, out List<AssetTypeMenu> u)
    {
        return DBM.GetList("usp_AssetTypeMenu_GetAll", new { AccountID }, out u);
    }
}