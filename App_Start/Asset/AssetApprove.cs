using System;
using System.Collections.Generic;
using BSS;
using System.Data;

public class AssetApprove
{
    public int AssetApproveID { get; set; }
    public Guid ObjectGuid { get; set; }
    public long AssetID { get; set; }
    public string ApproveContent { get; set; }
    public int UserIDApprove { get; set; }
    public bool IsApprove { get; set; }
    public string Reason { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }

    public static string Insert(DBM dbm, string AssetIDs, string ApproveContent, int UserIDApprove)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AssetApprove_Insert",
                    new
                    {
                        AssetIDs,
                        ApproveContent,
                        UserIDApprove
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string Update(DBM dbm, string AssetIDs, bool IsApprove, string Reason, int UserIDApprove)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AssetApprove_Update",
                    new
                    {
                        AssetIDs,
                        IsApprove,
                        Reason,
                        UserIDApprove
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string GetSearchByAssetIDs(string AssetIDs, out DataTable dt)
    {
        return DBM.ExecStore("usp_AssetApprove_GetAssetByListAssetID", new { AssetIDs }, out dt);
    }
    public static string GetListByAssetIDs(string AssetIDs, out List<AssetApprove> lt)
    {
        return DBM.GetList("usp_AssetApprove_GetAssetByListAssetID", new { AssetIDs }, out lt);
    }
}
public class AssetSenderApprove
{
    public List<Asset> ltAsset { get; set; }
    public string ApproveContent { get; set; }
    public int UserIDApprove { get; set; }
}
public class ComfirmApprove
{
    public List<Asset> ltAsset { get; set; }
    public bool IsApprove { get; set; }//IsApprove = True Đồng ý duyệt, IsApprove= False Từ chối duyệt
    public string Reason { get; set; }
}