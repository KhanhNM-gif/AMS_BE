using System;
using System.Collections.Generic;
using BSS;
using System.Data;

public class AssetTypePropertyData
{
    public int AssetTypePropertyDataID { get; set; }
    public string AssetTypePropertyDataName { get; set; }
    public bool IsActive { get; set; }

    public static string GetList(out DataTable lt)
    {
        return DBM.ExecStore("usp_AssetTypePropertyData_GetAll", out lt);
    }
    public static string GetOne(int AssetTypePropertyDataID, out AssetTypePropertyData o)
    {
        return DBM.GetOne("usp_AssetTypePropertyData_GetOne", new { AssetTypePropertyDataID }, out o);
    }
    public static string GetAssetTypePropertyDataName(int AssetTypePropertyDataID)
    {
        string msg = GetOne(AssetTypePropertyDataID, out AssetTypePropertyData o);
        if (msg.Length > 0) return msg;

        if (o == null) return "";
        else return o.AssetTypePropertyDataName;
    }
}