using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

public class AssetHandOver
{
    public int AssetHandOverID { get; set; }
    public Guid ObjectGuid { get; set; }
    public long AssetID { get; set; }
    [JsonIgnore]
    public Guid ObjectGuidAsset { get; set; }
    public int UserIDHandOver { get; set; }
    public string UserHandOverName { get; set; }
    public int UserIDHandedOver { get; set; }
    public string HandOverContent { get; set; }
    public DateTime? HandOverDate { get; set; }
    public bool IsHandOver { get; set; }
    public string Reason { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }

    public static string Insert(DBM dbm, int UserIDHandOver, DateTime HandOverDate, int UserIDHandedOver, string AssetIDs, string HandOverContent)
    {
        string msg = dbm.SetStoreNameAndParams("usp_AssetHandOver_Insert",
                    new
                    {
                        UserIDHandOver,
                        HandOverDate,
                        UserIDHandedOver,
                        AssetIDs,
                        HandOverContent
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string Update(DBM dbm, string AssetIDs, bool IsHandOver, string Reason, int UserIDHandOver, out List<AssetHandOver> outAssetHandOvers)
    {
        outAssetHandOvers = null;
        string msg = dbm.SetStoreNameAndParams("usp_AssetHandOver_Update",
                    new
                    {
                        AssetIDs,
                        IsHandOver,
                        Reason,
                        UserIDHandOver
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetList(out outAssetHandOvers);
    }
    public static string GetSearchByAssetIDs(string AssetIDs, out DataTable dt)
    {
        return DBM.ExecStore("usp_AssetHandOver_GetAssetByListAssetID", new { AssetIDs }, out dt);
    }
    public static string GetListByAssetIDs(string AssetIDs, out List<AssetHandOver> lt)
    {
        return DBM.GetList("usp_AssetHandOver_GetAssetByListAssetID", new { AssetIDs }, out lt);
    }

    public static string GetDetailByAccountID(int AccountID, int AccountReceiverID, out AssetHandOverExport assetHandOverExport)
    {
        return DBM.GetOne("usp_AssetHandOver_GetDetailByAccountID", new { AccountID, AccountReceiverID }, out assetHandOverExport);
    }

    public static string GetAssetHandOverByAssetIDs(string AssetIDs, int AccountID, out List<AssetHandoverDetail> assetHandoverDetails)
    {
        return DBM.GetList("usp_AssetHandOver_GetAssetByAssetIDs", new { AssetIDs, AccountID }, out assetHandoverDetails);
    }
}
public class AssetSenderHandOver
{
    public DateTime HandOverDate { get; set; }
    public int UserIDHandedOver { get; set; }
    [JsonIgnore]
    public string UserIDHandedOverName { get; set; }
    public string HandOverContent { get; set; }
    public List<Asset> ltAsset { get; set; }
}
public class ComfirmHandOver
{
    public List<Asset> ltAsset { get; set; }
    public bool IsHandover { get; set; }//IsHandover = True Đồng ý duyệt, IsHandover= False Từ chối duyệt
    public string Reason { get; set; }
    public int PlaceID { get; set; }
}

public class AssetHandOverExport
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string DeptFullName { get; set; }
    public string UserName { get; set; }
    public string UserNameReceiver { get; set; }
}

public class ReportHandOver
{
    public List<AssetHandoverDetail> AssetHandoverDetail { get; set; }
    public string FullNameOfUserManagerHolding { get; set; }
    public string PostionNameOfUserManagerHolding { get; set; }
    public string FullNameOfUserHolding { get; set; }
    public string PostionNameOfUserHolding { get; set; }
    public string DeptNameOfUserHolding { get; set; }
    public string FullNameOfUserManagerHandover { get; set; }
    public string PostionNameOfUserManagerHandover { get; set; }
    public string FullNameOfUserHandover { get; set; }
    public string PostionNameOfUserHandover { get; set; }
    public string DeptNameOfUserHandover { get; set; }
    public string HandOverContent { get; set; }

}

public class AssetHandoverDetail
{
    public string AssetTypeName { get; set; }
    public string AssetCode { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public string PlaceFullName { get; set; }
    public int UserIDHolding { get; set; }
    public int UserIDHandover { get; set; }
}