using System;
using System.Collections.Generic;
using BSS;
using System.Data;

public class AssetTypeGroup
{
    public int AssetTypeGroupID { get; set; }
    public string AssetTypeGroupName { get; set; }
    public bool IsActive { get; set; }

    public static string GetList(out DataTable lt)
    {
        return DBM.ExecStore("usp_AssetTypeGroup_GetAll", out lt);
    }
}