using ASM_API.App_Start.Store;
using BSS;
using System;
using System.Collections.Generic;
using System.Data;

namespace ASM_API.App_Start.StoreManagement
{
    public class StoreManagement
    {
        public string ItemTypeName { get; set; }
        public int ItemTypeID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string PlaceName { get; set; }
        public int PlaceID { get; set; }
        public int QuantityInStore { get; set; }
        public int WarningThreshold { get; set; }
        public string StatusName { get; set; }
        public int StatusID { get; set; }
        public int BatchTotal { get; set; }
        public ButtonShowQLK ButtonShow { get; set; }

        public static string GetSuggestSearch(string TextSearch, int AccountID, out DataTable dt)
        {
            return DBM.ExecStore("usp_StoreManagement_SuggestSearch", new { TextSearch, AccountID }, out dt);
        }

        public static string GetListPaging(StoreManagementSearch storeManagementSearch, out List<StoreManagement> storeManagement, out int total)
        {
            return Paging.ExecByStore("usp_StoreManagement_GetListPaging", "NumberItemStore.ID", storeManagementSearch, out storeManagement, out total);
        }
    }

    public class StoreManagementExcel
    {
        public StoreManagementExcel(int stt, StoreManagement storeManagement, ImportBatchDetailView importBatchDetailView)
        {
            STT = stt;
            ItemTypeName = storeManagement.ItemTypeName;
            ItemCode = storeManagement.ItemCode + " / " + storeManagement.ItemName;
            //ItemName = storeManagement.ItemName;
            PlaceName = storeManagement.PlaceName;
            QuantityInStore = storeManagement.QuantityInStore;
            WarningThreshold = storeManagement.WarningThreshold;
            StatusName = storeManagement.StatusName;

            BatchCode = importBatchDetailView.ImportBatchCode;
            ManufactureExpiryDate = $"{(importBatchDetailView.DateManufacture != null ? ((DateTime)(importBatchDetailView.DateManufacture)).ToString("dd/MM/yyyy") : "")} - {(importBatchDetailView.ExpiryDate != null ? ((DateTime)(importBatchDetailView.ExpiryDate)).ToString("dd/MM/yyyy") : "")}";
        }

        public int STT { get; set; }
        public string ItemTypeName { get; set; }
        public string ItemCode { get; set; }
        //public string ItemName { get; set; }
        public string PlaceName { get; set; }
        public string BatchCode { get; set; }
        public string ManufactureExpiryDate { get; set; }
        public int QuantityInStore { get; set; }
        public int WarningThreshold { get; set; }
        public string StatusName { get; set; }


    }


}