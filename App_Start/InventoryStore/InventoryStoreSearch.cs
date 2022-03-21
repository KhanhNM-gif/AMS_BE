using Newtonsoft.Json;
using System;

namespace ASM_API.App_Start.InventoryStore
{
    public class InventoryStoreSearch
    {
        [JsonIgnore]
        public string TextSearch { get; set; }
        [JsonIgnore]
        public long InventoryStoreID { get; set; }
        [JsonIgnore]
        public int UserID { get; set; }
        [JsonIgnore]
        public int AccountID { get; set; }
        [JsonIgnore]
        public bool isEasySearch { get; set; }
        [JsonIgnore]
        public long BatchID { get; set; }
        public string PlaceIDs { get; set; }
        public string StatusIDs { get; set; }
        public string ItemTypeIDs { get; set; }
        public int CreateDateCategoryID { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public virtual int PageSize { get; set; }
        public virtual int CurrentPage { get; set; }

        #region Chuẩn bị cho search nếu có CR
        [JsonIgnore]
        public DateTime? ProcessingDateFrom { get; set; }
        [JsonIgnore]
        public DateTime? ProcessingDateTo { get; set; }
        [JsonIgnore]
        public string UserIDHandings { get; set; }
        [JsonIgnore]
        public string UserCreateIDs { get; set; }
        [JsonIgnore]
        public DateTime? CreateDateFrom { get; set; }
        [JsonIgnore]
        public DateTime? CreateDateTo { get; set; }
        #endregion

        public InventoryStoreSearch()
        {
            StatusIDs = ItemTypeIDs = UserIDHandings = UserCreateIDs = "";
            CurrentPage = 1;
            PageSize = 50;
            ProcessingDateFrom = ProcessingDateTo = CreateDateFrom = CreateDateTo = null;
        }
    }

    public class InventoryStoreEasySearch
    {
        public int ObjectCategory { get; set; }
        public string ObjectID { get; set; }
        public virtual int PageSize { get; set; }
        public virtual int CurrentPage { get; set; }
        public string TextSearch { get; set; }
        public InventoryStoreEasySearch()
        {
            ObjectCategory = 0;
            ObjectID = "";
            PageSize = 50;
            CurrentPage = 1;
            TextSearch = "";
        }
    }

}