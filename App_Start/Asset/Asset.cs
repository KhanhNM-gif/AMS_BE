using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

public class Asset : IMappingSingleField
{
    //[JsonIgnore]
    public long AssetID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetCode { get; set; }
    public string AssetImagePath { get; set; }
    public string AssetImageName { get; set; }
    public string AssetImageContentBase64 { get; set; }
    public string AssetColor { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public int ProducerID { get; set; }
    public int SupplierID { get; set; }
    public DateTime? AssetDateIn { get; set; }
    public DateTime? AssetDateBuy { get; set; }
    public int PlaceID { get; set; }
    public string PlaceFullName { get; set; }
    public string AssetDescription { get; set; }
    [JsonIgnore]
    public int AccountID { get; set; }
    public int UserIDApprove { get; set; }
    public int UserIDCreate { get; set; } = 0;
    public int UserIDHolding { get; set; } = 0;
    public int UserIDHandover { get; set; } = 0;
    public int UserIDReturn { get; set; } = 0;
    public int UserIDInventory { get; set; } = 0;
    public int AssetStatusID { get; set; }
    public string AssetStatusName { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public bool IsSendApprove { get; set; }
    [JsonIgnore]
    public int? ExpiryDate { get; set; }
    public List<AssetProperty> ListAssetProperty { get; set; }

    public string InsertUpdate(DBM dbm, out Asset au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_Asset_InsertUpdate",
                    new
                    {
                        AssetID,
                        AssetTypeID,
                        AssetCode,
                        AssetImagePath,
                        AssetColor,
                        AssetSerial,
                        AssetModel,
                        ProducerID,
                        SupplierID,
                        AssetDateIn,
                        AssetDateBuy,
                        PlaceID,
                        AssetDescription,
                        AccountID,
                        UserIDApprove,
                        UserIDCreate,
                        UserIDHolding,
                        AssetStatusID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public string UpdateHandOver(DBM dbm, out Asset au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateHandOver",
                    new
                    {
                        AssetID,
                        PlaceID,
                        UserIDHolding,
                        AssetStatusID,
                        AccountID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public string UpdateApprove(DBM dbm, out Asset au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateApprove",
                    new
                    {
                        AssetID,
                        UserIDApprove,
                        AssetStatusID,
                        AccountID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public string UpdateReturn(DBM dbm, out Asset au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateReturn",
                    new
                    {
                        AssetID,
                        AssetStatusID,
                        UserIDHolding,
                        AccountID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }

    public string UpdateMoveAssetToPlace(DBM dbm, out Asset asset)
    {
        asset = null;
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateMoveToPlace",
                    new
                    {
                        AssetID,
                        AssetStatusID,
                        UserIDHolding,
                        PlaceID,
                        AccountID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out asset);
    }
    public string UpdateRevoke(DBM dbm, out Asset au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateRevoke",
                    new
                    {
                        AssetID,
                        AssetStatusID,
                        UserIDHolding,
                        AccountID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public static string GetOneByAssetID(long AssetID, out Asset asset)
    {
        return DBM.GetOne("usp_Asset_GetByID", new { AssetID }, out asset);
    }

    public static string GetListAssetByUserID(long UserID, out List<Asset> lt)
    {
        return DBM.GetList("usp_Asset_GetByUserID", new { UserID }, out lt);
    }

    public static string GetOneByGuid(Guid ObjectGuid, out Asset asset)
    {
        asset = null;

        string msg = CacheObject.GetAssetIDbyGUID(ObjectGuid, out long assetID);
        if (msg.Length > 0) return msg;

        msg = GetOneByAssetID(assetID, out asset);
        if (msg.Length > 0) return msg;

        return msg;
    }
    public static string GetOneByGuid(Guid ObjectGuid, out long id)
    {
        id = 0;

        string msg = DBM.GetOne("usp_Asset_GetByGuid", new { ObjectGuid }, out Asset asset);
        if (msg.Length > 0) return msg;

        if (asset == null) return ("Không tồn tại Tài sản có ObjectGuid = " + ObjectGuid).ToMessageForUser();
        id = asset.AssetID;
        return msg;
    }
    public static string GetListAssetSync(DateTime LastUpdate, Guid ObjectGuid, out List<AssetSync> lt)
    {
        return DBM.GetList("usp_Asset_GetListAssetSync", new { LastUpdate, ObjectGuid }, out lt);
    }
    public static string GetAssetIDsByObjectGuids(string ObjectGuids, out string AssetIDs)
    {
        AssetIDs = "";
        string msg = "";
        string[] lstObjectGuids = ObjectGuids.Split(',');
        List<long> ltAssetID = new List<long>();
        foreach (var strObjectGuid in lstObjectGuids)
        {
            Guid ObjectGuid = strObjectGuid.ToGuid(Guid.Empty);
            if (ObjectGuid == Guid.Empty) return "ObjectGuid = " + strObjectGuid + " không hợp lệ";

            msg = CacheObject.GetAssetIDbyGUID(ObjectGuid, out long assetID);
            if (msg.Length > 0) return msg;

            ltAssetID.Add(assetID);
        }

        AssetIDs = string.Join(",", ltAssetID);
        return msg;
    }

    public static string GetSearchByAssetIDs(string AssetIDs, int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_Asset_GetAssetByListAssetID", new { AssetIDs, AccountID }, out lt);
    }
    public static string GetSuggestSearch(string textSearch, int AccountID, out DataTable dt)
    {
        dt = null;

        return DBM.ExecStore("usp_Asset_SuggestSearch", new { textSearch, AccountID }, out dt);
    }
    public static string AssetViewDetailByGuid(long AssetID, int AccountID, out AssetViewDetail asset)
    {
        return DBM.GetOne("usp_Asset_ViewDetail", new { AssetID, AccountID }, out asset);
    }
    public static string AssetViewDetailByListAssetID(string AssetIDs, int AccountID, out List<AssetViewDetail> assetList)
    {
        return DBM.GetList("usp_Asset_ViewDetailByListAsset", new { AssetIDs, AccountID }, out assetList);
    }
    public static string CheckExistAsset(string AssetModel, string AssetSerial, int AccountID, out Asset asset)
    {
        return DBM.GetOne("usp_Asset_CheckExistAsset", new { AssetModel, AssetSerial, AccountID }, out asset);
    }
    public static string SelectByAssetTypeID(int AssetTypeID, int AccountID, out List<Asset> lt)
    {
        return DBM.GetList("usp_Asset_GetByAssetTypeID", new { AssetTypeID, AccountID }, out lt);
    }
    public static string SelectByPlaceID(int PlaceID, int AccountID, out List<Asset> lt)
    {
        return DBM.GetList("usp_Asset_GetByPlaceID", new { PlaceID, AccountID }, out lt);
    }
    public static string SelectByOrganizationID(int OrganizationID, int AccountID, out List<Asset> lt)
    {
        return DBM.GetList("usp_Asset_GetByOrganizationID", new { OrganizationID, AccountID }, out lt);
    }
    public static string GetAssetList(out List<Asset> assetList)
    {
        return DBM.GetList("usp_Asset_GetAssetList", new { }, out assetList);
    }

    public static string GetAssetCodeList(out DataTable dt)
    {
        return DBM.ExecStore("usp_Asset_GetAssetCodeList", out dt);
    }

    public static string UpdateStatusID_Approve(DBM dbm, string AssetIDs, int UserIDApprove, int AssetStatusID, int AccountID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateStatusID_Approve",
           new
           {
               AssetIDs,
               UserIDApprove,
               AssetStatusID,
               AccountID
           });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string UpdateStatusID_Handover(DBM dbm, string AssetIDs, int UserIDHandover, int PlaceID, int AssetStatusID, int AccountID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateStatusID_Handover",
           new
           {
               AssetIDs,
               UserIDHandover,
               PlaceID,
               AssetStatusID,
               AccountID
           });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string UpdateStatusID_Return(DBM dbm, string AssetIDs, int UserIDReturn, int AssetStatusID, int PlaceID, int AccountID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateStatusID_Return",
           new
           {
               AssetIDs,
               UserIDReturn,
               AssetStatusID,
               PlaceID,
               AccountID
           });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string UpdateStatusID_Revoke(DBM dbm, string AssetIDs, int AssetStatusID, int UserIDHolding, int AccountID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateRevoke",
           new
           {
               AssetIDs,
               AssetStatusID,
               UserIDHolding,
               AccountID
           });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string UpdateStatusID_Revoke2(DBM dbm, long AssetID, int AssetStatusID, int UserIDHolding, int PlaceID, int AccountID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateRevoke2",
           new
           {
               AssetID,
               AssetStatusID,
               UserIDHolding,
               PlaceID,
               AccountID
           });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string UpdateStatusID(DBM dbm, long AssetID, int AssetStatusID, int AccountID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Asset_UpdateStatusID",
           new
           {
               AssetID,
               AssetStatusID,
               AccountID
           });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string GetByAssetCode(string AssetCode, int AccountID, out Asset a)
    {
        return DBM.GetOne("usp_Asset_GetByAssetCode", new { AssetCode, AccountID }, out a);
    }
    public static string ViewDetailByAssetCode(string AssetCode, int AccountID, out AssetViewDetail a)
    {
        return DBM.GetOne("usp_Asset_ViewDetailByAssetCode", new { AssetCode, AccountID }, out a);
    }
    public static string GetListSearchTotal(AssetSearch assetSearch, out int total)
    {
        total = 0;

        dynamic o;
        string msg = GetListSearch_Parameter(assetSearch, out o);
        if (msg.Length > 0) return msg;

        return DBM.ExecStore("usp_Asset_SelectSearch_Total", o, out total);
    }
    public static string GetListPaging(AssetSearch assetSearch, out List<AssetSearchResult> lt, out int total)
    {
        lt = null; total = 0;
        string msg = GetListSearch_Parameter(assetSearch, out dynamic para);
        if (msg.Length > 0) return msg;

        msg = Paging.ExecByStore(@"usp_Asset_SelectSearch", "a.AssetID", para, out lt, out total);
        if (msg.Length > 0) return msg;

        return "";
    }

    private static string GetListSearch_Parameter(AssetSearch assetSearch, out dynamic o)
    {
        return GetListSearch_Parameter(false, assetSearch, out o);
    }
    private static string GetListSearch_Parameter(bool IsReport, AssetSearch assetSearch, out dynamic o)
    {
        o = null;
        string msg = "";
        o = new
        {
            assetSearch.TextSearch,
            assetSearch.AssetDateFrom,
            assetSearch.AssetDateTo,
            assetSearch.CurrentPage,
            assetSearch.PageSize,
            assetSearch.AccountID,
            assetSearch.UserID,
            assetSearch.AssetID,
            assetSearch.AssetCodes,
            assetSearch.UserIDHoldings,
            assetSearch.PlaceIDs,
            assetSearch.SupplierIDs,
            assetSearch.AssetTypeIDs,
            assetSearch.AssetStatusIDs,
            assetSearch.ViewAll
        };

        return msg;
    }
    public string GetOne(object k, out IMappingSingleField outModel)
    {
        outModel = null;

        string msg = DBM.GetOne("usp_Asset_GetByID", new { AssetID = (long)k }, out Asset outAsset);
        if (msg.Length > 0) return msg;
        outModel = outAsset;

        return msg;
    }

    public string GetName() => AssetCode;

    public void SetKey(object k) => AssetID = (long)k;

    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }
}
public class AssetViewDetail
{
    public long AssetID { get; set; }
    public string AccountCode { get; set; }
    public Guid ObjectGuid { get; set; }
    public string AssetTypeName { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetCode { get; set; }
    public string AssetImagePath { get; set; }
    public string AssetColor { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public string ProducerName { get; set; }
    public string SupplierName { get; set; }
    public int AssetStatusID { get; set; }
    public string AssetStatusName { get; set; }
    public string PlaceObjectGuid { get; set; }
    public string PlaceName { get; set; }
    public string PlaceFullName { get; set; }
    public string UserManagerName { get; set; }
    public string AssetDescription { get; set; }
    public string DeptCode { get; set; }
    public DateTime? AssetDateIn { get; set; }
    public DateTime? AssetDateBuy { get; set; }
    public List<AssetProperty> ListAssetProperty { get; set; }
    public string UsePerformance { get; set; } = "30%";
}
public class AssetImportExcel
{
    public string STT { get; set; }

    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }

    public string AssetCode { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public string AssetColor { get; set; }

    public int ProducerID { get; set; }
    public string ProducerName { get; set; }

    public int SupplierID { get; set; }
    public string SupplierName { get; set; }

    public DateTime? AssetDateIn { get; set; }
    public string StrAssetDateIn { get; set; }

    public DateTime? AssetDateBuy { get; set; }
    public string StrAssetDateBuy { get; set; }

    public int PlaceID { get; set; }
    public string PlaceName { get; set; }

    public List<AssetProperty> ListAssetProperty { get; set; }
}
public class AssetEasySearch
{
    public int ObjectCategory { get; set; }
    public string ObjectID { get; set; }
    public virtual int PageSize { get; set; }
    public virtual int CurrentPage { get; set; }
    public string TextSearch { get; set; }


}

public class AssetEasySearchExport : AssetEasySearch
{
    [JsonIgnore]
    public override int CurrentPage { get; set; }
    [JsonIgnore]
    public override int PageSize { get; set; }

    public AssetEasySearchExport() : base()
    {
        CurrentPage = 1;
        PageSize = 10000;
    }

}

public class AssetSearch
{
    public const int DONGIAN = 1, NANGCAO = 2;
    public virtual long AssetID { get; set; }
    public string PlaceIDs { get; set; }
    public virtual int AccountID { get; set; }
    public virtual int UserID { get; set; }
    public virtual string TextSearch { get; set; }
    public string AssetCodes { get; set; }
    public virtual int CategorySearch { get; set; }
    public string AssetStatusIDs { get; set; }
    public string AssetTypeIDs { get; set; }
    public string UserIDHoldings { get; set; }
    public string SupplierIDs { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime? AssetDateFrom { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime? AssetDateTo { get; set; }
    public int InputDate { get; set; }
    public virtual int CurrentPage { get; set; }
    public virtual int PageSize { get; set; }
    public virtual bool ViewAll { get; set; }
    public AssetSearch()
    {
        UserIDHoldings = PlaceIDs = SupplierIDs = AssetTypeIDs = AssetCodes = AssetStatusIDs = "";

        UserID = 0;
        AssetID = 0;
        ViewAll = false;
        AssetDateFrom = AssetDateTo = null;
    }
}

public class AssetSearchExport : AssetSearch
{
    [JsonIgnore]
    public override int CurrentPage { get; set; }
    [JsonIgnore]
    public override int PageSize { get; set; }
    [JsonIgnore]
    public override long AssetID { get; set; }
    [JsonIgnore]
    public override int AccountID { get; set; }
    [JsonIgnore]
    public override int UserID { get; set; }
    [JsonIgnore]
    public override string TextSearch { get; set; }
    [JsonIgnore]
    public override int CategorySearch { get; set; }
    [JsonIgnore]
    public override bool ViewAll { get; set; }

    public AssetSearchExport() : base()
    {
        CurrentPage = 1;
        PageSize = 10000;
    }

}

public class AssetSearchResult
{
    [JsonIgnore]
    public long AssetID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }
    public string AssetCode { get; set; }
    public string AssetImagePath { get; set; }
    public string AssetImageName { get; set; }
    public string AssetColor { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public int ExpriedDay { get; set; }
    public int AssetStatusID { get; set; }
    public int UserIDCreate { get; set; }
    public int UserIDApprove { get; set; }
    public int UserIDHolding { get; set; }
    public string UserNameHolding { get; set; }
    public int UserIDHandover { get; set; }
    public int UserIDReturn { get; set; }
    public string AssetStatusName { get; set; }
    public string PlaceFullName { get; set; }
    public string PlaceName { get; set; }
    public string ProducerName { get; set; }
    public string SupplierName { get; set; }
    public string AssetDateIn { get; set; }
    public string AssetDateBuy { get; set; }
    public string AssetDescription { get; set; }
    public string UsePerformance { get; set; }
    [JsonIgnore]
    public int? ExpiryDate { get; set; }
    public ButtonShowAsset ButtonShow { get; set; }
}
public class AssetStatus
{
    public int AssetStatusID { get; set; }
    public string AssetStatusName { get; set; }
    public static string GetListStatus(out List<AssetStatus> lt)
    {
        return DBM.GetList("usp_AssetStatus_SelectAll", new { }, out lt);
    }
}
public class AssetExport
{
    public Guid ObjectGuid { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }
    public string AssetCode { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public string PlaceFullName { get; set; }
    public string PlaceName { get; set; }
    public string ProducerName { get; set; }
    public string SupplierName { get; set; }
    public string AssetDateIn { get; set; }
    public string AssetDateBuy { get; set; }
    public string AssetColor { get; set; }
    public string UserNameHolding { get; set; }
    public string AssetDescription { get; set; }
    public string UsePerformance { get; set; }
    public string AssetStatusName { get; set; }
}