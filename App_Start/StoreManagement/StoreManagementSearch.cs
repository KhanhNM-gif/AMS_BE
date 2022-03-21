using Newtonsoft.Json;
using System;

namespace ASM_API.App_Start.StoreManagement
{
    public class StoreManagementSearch
    {
        [JsonIgnore]
        public string TextSearch { get; set; }
        [JsonIgnore]
        public long ItemID { get; set; }
        [JsonIgnore]
        public int UserID { get; set; }
        [JsonIgnore]
        public int AccountID { get; set; }
        [JsonIgnore]
        public long BatchID { get; set; }
        public string StatusIDs { get; set; }
        public string ItemTypes { get; set; }
        public string PlaceIDs { get; set; }
        public int CreateDateCategoryID { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public virtual int PageSize { get; set; }
        public virtual int CurrentPage { get; set; }
        public StoreManagementSearch()
        {
            StatusIDs = ItemTypes = PlaceIDs = "";
            CurrentPage = 1;
            PageSize = 50;
        }
    }

    public class StoreManagementEasySearch
    {
        public int ObjectCategory { get; set; }
        public string ObjectID { get; set; }
        public virtual int PageSize { get; set; }
        public virtual int CurrentPage { get; set; }
        public string TextSearch { get; set; }
        public StoreManagementEasySearch()
        {
            ObjectCategory = 0;
            ObjectID = "";
            PageSize = 50;
            CurrentPage = 1;
            TextSearch = "";
        }
    }

    public class StoreManagementESExport : StoreManagementEasySearch
    {
        [JsonIgnore]
        public override int PageSize { get; set; }
        [JsonIgnore]
        public override int CurrentPage { get; set; }

        public StoreManagementESExport() : base()
        {
            PageSize = 10000;
            CurrentPage = 1;
        }
    }
    public class StoreManagementASExport : StoreManagementSearch
    {
        [JsonIgnore]
        public override int PageSize { get; set; }
        [JsonIgnore]
        public override int CurrentPage { get; set; }

        public StoreManagementASExport() : base()
        {
            PageSize = 10000;
            CurrentPage = 1;
        }
    }
}