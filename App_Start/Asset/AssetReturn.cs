using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

public class AssetReturn
{
    public int AssetReturnID { get; set; }
    public Guid ObjectGuid { get; set; }
    public long AssetID { get; set; }
    public Guid ObjectGuidAsset { get; set; }
    public string ReturnContent { get; set; }
    public int UserIDReturn { get; set; }
    public string UserNameReturn { get; set; }
    public int UserIDReturned { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsReturn { get; set; }
    public string Reason { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public static string Insert(DBM dbm, string AssetIDs, int UserIDReturn, DateTime ReturnDate, int UserIDReturned, string ReturnContent)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AssetReturn_Insert",
                    new
                    {
                        AssetIDs,
                        UserIDReturn,
                        ReturnDate,
                        UserIDReturned,
                        ReturnContent
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string Update(DBM dbm, string AssetIDs, bool IsReturn, string Reason, int UserIDComfirmReturn, out List<AssetReturn> outLtAssetReturn)
    {
        outLtAssetReturn = null;
        string msg = dbm.SetStoreNameAndParams("usp_AssetReturn_Update",
                    new
                    {
                        AssetIDs,
                        IsReturn,
                        Reason,
                        UserIDComfirmReturn
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetList(out outLtAssetReturn);
    }
    public static string GetSearchByAssetIDs(string AssetIDs, out DataTable dt)
    {
        return DBM.ExecStore("usp_AssetReturn_GetAssetByListAssetID", new { AssetIDs }, out dt);
    }
    public static string GetOneAssetReturnByAssetID(long AssetID, out AssetReturn assetReturn)
    {
        return DBM.GetOne("usp_AssetReturn_GetAssetByAssetID", new { AssetID }, out assetReturn);
    }
}
public class AssetSenderReturn
{
    public DateTime ReturnDate { get; set; }
    public int UserIDReturned { get; set; }
    [JsonIgnore]
    public string UserNameReturned { get; set; }
    public List<Asset> ltAsset { get; set; }
    public string ReturnContent { get; set; }
}
public class AssetComfirmReturn
{
    public List<Asset> ltAsset { get; set; }
    public bool IsReturn { get; set; } //IsReturn = True Đồng ý trả, IsReturn= False Từ chối trả
    public string Reason { get; set; }
    public int PlaceID { get; set; }
}