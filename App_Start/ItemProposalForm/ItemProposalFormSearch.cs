using Newtonsoft.Json;
using System;

namespace ASM_API.App_Start.ItemProposalForm
{
    public class ItemProposalFormSearch
    {
        [JsonIgnore]
        public string TextSearch { get; set; }
        [JsonIgnore]
        public long ID { get; set; }
        [JsonIgnore]
        public int UserID { get; set; }
        [JsonIgnore]
        public int AccountID { get; set; }
        [JsonIgnore]
        public bool isEasySearch { get; set; }
        public int ItemProposalFormTypeID { get; set; }
        public string StatusIDs { get; set; }
        public string ItemTypeIDs { get; set; }
        public string UserIDHandings { get; set; }
        public string UserCreateIDs { get; set; }
        public DateTime? CreateDateFrom { get; set; }
        public DateTime? CreateDateTo { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public DateTime? ProcessingDateFrom { get; set; }
        public DateTime? ProcessingDateTo { get; set; }
        public ItemProposalFormSearch()
        {
            StatusIDs = ItemTypeIDs = UserIDHandings = UserCreateIDs = "";
            CurrentPage = 1;
            PageSize = 50;
            ProcessingDateFrom = ProcessingDateTo = CreateDateFrom = CreateDateTo = null;
        }
    }

    public class ItemProposalFormEasySearch
    {
        public int ObjectCategory { get; set; }
        public string ObjectID { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public string TextSearch { get; set; }
        public ItemProposalFormEasySearch()
        {
            ObjectCategory = 0;
            ObjectID = "";
            PageSize = 50;
            CurrentPage = 1;
            TextSearch = "";
        }
    }
}