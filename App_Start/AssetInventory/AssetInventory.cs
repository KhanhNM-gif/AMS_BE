using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

namespace ASM_API.App_Start.AssetInventory
{
    public class AssetInventory
    {
        public long InventoryID { get; set; }
        public string InventoryCode { get; set; }
        public Guid ObjectGuid { get; set; }
        public string InventoryName { get; set; }
        public int UserIDPerform { get; set; }
        public string AccountName { get; set; }
        public int DeptID { get; set; }
        public string DeptCode { get; set; }
        public int UserIDApprover { get; set; }
        public string UserNameApprover { get; set; }
        public int AccountID { get; set; }
        public string Note { get; set; }
        public string ReasonRefuse { get; set; }
        public int StatusID { get; set; }
        public bool IsSendApprove { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastUpdate { get; set; }
        public string TransferDirectionID { get; set; }
        public string Commenthandling { get; set; }
        [JsonIgnore]
        public string InfoUpdate { get; set; }
        public List<AssetInventoryDetail> AssetInventoryDetails { get; set; }
        public string InsertUpdate(DBM dbm, out AssetInventory au)
        {
            au = null;
            string msg = dbm.SetStoreNameAndParams("usp_AssetInventory_InsertUpdate",
                        new
                        {
                            InventoryID,
                            InventoryCode,
                            InventoryName,
                            Note,
                            BeginDate,
                            EndDate,
                            ReasonRefuse,
                            UserIDPerform,
                            UserIDApprover,
                            AccountID,
                            StatusID
                        }
                        );
            if (msg.Length > 0) return msg;

            return dbm.GetOne(out au);
        }
        public static string GetAll(out List<AssetInventory> inventories)
        {
            return DBM.GetList("usp_Inventory_GetAll", new { }, out inventories);
        }
        public static string GetOne(long InventoryID, out AssetInventory inventory)
        {
            return DBM.GetOne("usp_AssetInventory_GetByID", new { InventoryID }, out inventory);
        }
        public static string GetTotalByDateCode(string InventoryCode_Prefix, out int Total)
        {
            return DBM.ExecStore("usp_AssetInventory_GetByInventoryCode_Prefix", new { InventoryCode_Prefix }, out Total);
        }
        public static string GetSuggestSearch(string TextSearch, int AccountID, out DataTable lt)
        {
            return DBM.ExecStore("usp_AssetInventory_SuggestSearch", new { TextSearch, AccountID }, out lt);
        }
        public static string GetOneObjectGuid(Guid ObjectGuid, out long inventoryID)
        {
            inventoryID = 0;

            string msg = DBM.GetOne("usp_AssetInventory_GetByObjectGuid", new { ObjectGuid }, out AssetInventory inventory);
            if (msg.Length > 0) return msg;

            if (inventory == null) return ("Không tồn tại User có ObjectGuid = " + ObjectGuid).ToMessageForUser();
            inventoryID = inventory.InventoryID;

            return msg;
        }

        public static string GetOneObjectGuid(Guid ObjectGuid, out AssetInventory inventory)
        {
            inventory = null;

            string msg = CacheObject.GetAssetInventoryByGUID(ObjectGuid, out long inventoryID);
            if (msg.Length > 0) return msg;

            msg = GetOne(inventoryID, out inventory);
            if (msg.Length > 0) return msg;

            return msg;
        }

        public static string GetListSearch(InventorySearch inventorySearch, out List<InventorySearchResult> lt)
        {
            lt = null;

            dynamic o;
            string msg = GetListSearch_Parameter(inventorySearch, out o);
            if (msg.Length > 0) return msg;

            return DBM.GetList("usp_AssetInventory_SelectSearch", o, out lt);
        }
        public static string GetListSearchTotal(InventorySearch inventorySearch, out int total)
        {
            total = 0;

            dynamic o;
            string msg = GetListSearch_Parameter(inventorySearch, out o);
            if (msg.Length > 0) return msg;

            return DBM.ExecStore("usp_AssetInventory_SelectSearch_Total", o, out total);
        }

        public static string GetListPaging(InventorySearch inventorySearch, out List<InventorySearchResult> lt, out int total)
        {
            total = 0; lt = null;

            string msg = GetListSearch_Parameter(inventorySearch, out dynamic para);
            if (msg.Length > 0) return msg;

            msg = Paging.ExecByStore(@"usp_AssetInventory_SelectSearch2", "ai.InventoryID", para, out lt, out total);
            if (msg.Length > 0) return msg;

            return "";
        }

        private static string GetListSearch_Parameter(InventorySearch inventorySearch, out dynamic o)
        {
            o = null;
            string msg = "";
            o = new
            {
                inventorySearch.TextSearch,
                inventorySearch.InventoryID,
                inventorySearch.UserIDPerform,
                inventorySearch.UserIDApprover,
                inventorySearch.StatusID,
                inventorySearch.DateFrom,
                inventorySearch.DateTo,
                inventorySearch.AccountID,
                inventorySearch.UserID,
                inventorySearch.PageSize,
                inventorySearch.CurrentPage
            };

            return msg;
        }

        public static string UpdateStatusID(DBM dbm, long InventoryID, int InventoryStatusID)
        {
            string msg = dbm.SetStoreNameAndParams("usp_AssetInventory_UpdateStatus",
              new
              {
                  InventoryID,
                  InventoryStatusID
              });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }
        public static string UpdateUserIDApprover(DBM dbm, long InventoryID, long UserIDApprover)
        {
            string msg = dbm.SetStoreNameAndParams("usp_AssetInventory_UpdateUserIDApprover", new { InventoryID, UserIDApprover });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }
    }

    public class AssetInventoryDetail
    {
        public long ID { get; set; }
        public long InventoryID { get; set; }
        public long AssetID { get; set; }
        public int AssetTypeID { get; set; }
        public string AssetTypeName { get; set; }
        [JsonIgnore]
        public int AssetTypeTotal { get; set; }
        public string AssetCode { get; set; }
        public string AssetSerial { get; set; }
        public string AssetModel { get; set; }
        public string PlaceFullName { get; set; }
        public string Note { get; set; }
        public int StateID { get; set; }
        public string StateName { get; set; }
        public string InsertUpdate(DBM dbm, out AssetInventoryDetail au)
        {
            au = null;
            string msg = dbm.SetStoreNameAndParams("usp_AssetInventoryDetail_InsertUpdate",
                        new
                        {
                            ID,
                            InventoryID,
                            AssetID,
                            Note,
                            StateID
                        }
                        );
            if (msg.Length > 0) return msg;

            return dbm.GetOne(out au);
        }
        public static string GetListByInventoryID(long InventoryID, out List<AssetInventoryDetail> assetInventoryDetails)
        {
            return DBM.GetList("usp_AssetInventoryDetail_GetListByInventoryID", new { InventoryID }, out assetInventoryDetails);
        }
        public static string GetAssetInventoryDetailByAssetTypeIDsOrPlaceIDs(string AssetTypeIDs, string PlaceIDs, out List<AssetInventoryDetail> assetInventoryDetails)
        {
            return DBM.GetList("usp_AssetInventoryDetail_GetAssetDetailListByAssetIDsOrPlaceIDs", new { AssetTypeIDs, PlaceIDs }, out assetInventoryDetails);
        }
        public static string Delete(DBM dbm, string AssetIDs, long InventoryID)
        {
            string msg = dbm.SetStoreNameAndParams("usp_AssetInventoryDetail_Delete", new { AssetIDs, InventoryID });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }

    }

    public class AssetInventoryDetailCompare : IEqualityComparer<AssetInventoryDetail>
    {
        public bool Equals(AssetInventoryDetail x, AssetInventoryDetail y)
        {
            return x.ID == y.ID;
        }

        public int GetHashCode(AssetInventoryDetail obj)
        {
            return obj.ID.GetHashCode();
        }
    }
    public class AssetInventoryViewDetail
    {
        public long InventoryID { get; set; }
        public Guid ObjectGuid { get; set; }
        public string InventoryCode { get; set; }
        public string InventoryName { get; set; }
        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UsernamePerformDetail { get; set; }
        public string UsernameApproverDetail { get; set; }
        public string Note { get; set; }
        public string StatusName { get; set; }
        public List<AssetInventoryDetail> assetInventoryDetails { get; set; }
        public string TransferDirectionID { get; set; }
        public static string ViewDetail(long InventoryID, out AssetInventoryViewDetail assetInventoryViewDetail)
        {
            return DBM.GetOne("usp_AssetInventory_ViewDetail", new { InventoryID }, out assetInventoryViewDetail);
        }
    }

    public class InventorySearch
    {
        public int InventoryID { get; set; }
        public string TextSearch { get; set; }
        public int UserIDPerform { get; set; }
        public int UserIDApprover { get; set; }
        public int StatusID { get; set; }
        public long UserID { get; set; }
        public int AccountID { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public InventorySearch()
        {
            TextSearch = "";
            InventoryID = UserIDPerform = UserIDApprover = StatusID = CurrentPage = PageSize = 0;

            DateTime dtDefault = DateTime.Parse("1900-01-01");
            DateFrom = DateTo = dtDefault;
        }
    }
    public class InventorySearchResult
    {
        [JsonIgnore]
        public long InventoryID { get; set; }
        public Guid ObjectGuid { get; set; }
        public string InventoryCode { get; set; }
        public string InventoryName { get; set; }
        public int UserIDPerform { get; set; }
        public string UserNamePerform { get; set; }
        public string FullNameNamePerform { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int UserIDApprover { get; set; }
        public string UserNameApprover { get; set; }
        public string FullNameApprover { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; }
        public ButtonShowPKK ButtonShow { get; set; }
    }
}