using System;
using System.Collections.Generic;
using BSS;
using System.Data;

public class AssetProcessingFlow
{
    public int ID { get; set; }
    public Guid ObjectGuid { get; set; }
    public long AssetID { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }
    public string CommentProcess { get; set; }
    public int AssetApproveID { get; set; }
    public int ProcessType { get; set; }
    public int AssetHandOverID { get; set; }
    public int AssetReturnID { get; set; }
    public int AssetRevokeID { get; set; }
    public long ProposalFormID { get; set; }
    public string AssetCode { get; set; }
    public string AssetName { get; set; }
    public string AssetStatusName { get; set; }
    public string PlaceFullName { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public string InsertUpdate(DBM dbm, out AssetProcessingFlow au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_AssetProcessingFlow_InsertUpdate",
                    new
                    {
                        ID,
                        AssetID,
                        CommentProcess,
                        AssetApproveID,
                        ProcessType,
                        AssetHandOverID,
                        AssetReturnID,
                        AssetRevokeID,
                        ProposalFormID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public static string GetListByHandOver(int AssetHandOverID, out List<AssetProcessingFlow> lt)
    {
        return DBM.GetList("usp_AssetProcessingFlow_GetByHandOverID", new { AssetHandOverID }, out lt);
    }
    public static string GetAssetByHandOverID(int AssetHandOverID, out List<AssetProcessingFlow> lt)
    {
        return DBM.GetList("usp_AssetProcessingFlow_GetAssetByHandOverID", new { AssetHandOverID }, out lt);
    }

    public static string GetListByApprove(int AssetApproveID, out List<AssetProcessingFlow> lt)
    {
        return DBM.GetList("usp_AssetProcessingFlow_GetByApproveID", new { AssetApproveID }, out lt);
    }
    public static string GetAssetByApproveID(int AssetApproveID, out List<AssetProcessingFlow> lt)
    {
        return DBM.GetList("usp_AssetProcessingFlow_GetAssetByApproveID", new { AssetApproveID }, out lt);
    }

    public static string GetListByReturn(int AssetReturnID, out List<AssetProcessingFlow> lt)
    {
        return DBM.GetList("usp_AssetProcessingFlow_GetByReturnID", new { AssetReturnID }, out lt);
    }
    public static string GetAssetByReturnID(int AssetReturnID, out List<AssetProcessingFlow> lt)
    {
        return DBM.GetList("usp_AssetProcessingFlow_GetAssetByReturnID", new { AssetReturnID }, out lt);
    }
    public static string GetListByProposalFormID(int ProposalFormID, out List<AssetProcessingFlow> lt)
    {
        return DBM.GetList("usp_AssetProcessingFlow_GetByProposalFormID", new { ProposalFormID }, out lt);
    }
}