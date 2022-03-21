using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ASM_API.App_Start.Statistic
{
    /// <summary>
    /// Thống kê trạng thái Tài sản
    /// </summary>
    public class AssetStatistic
    {
        public int StatusID { get; set; }
        public string StatusName { get; set; }
        public int Quantity { get; set; }

        public static string GetListPagingSearch(AssetStatisticSearch search, out List<AssetStatistic> outLtAssetStatistic)
        {
            return DBM.GetList("usp_AssetStatistic_GetListPagingSearch", search, out outLtAssetStatistic);
        }
    }

    /// <summary>
    /// Thống kê Vụ việc
    /// </summary>
    public class IssueStatistic
    {
        /// <summary>
        /// SL TS gặp sự cố
        /// </summary>
        public int NumberCrashItem { get; set; }

        /// <summary>
        /// SL TS bảo hành
        /// </summary>
        public int NumberInsuranceItem { get; set; }

        /// <summary>
        /// SL TS bảo trì
        /// </summary>
        public int NumberMaintenanceItem { get; set; }

        public static string GetListPagingSearch(AssetStatisticSearch search, out List<IssueStatistic> outLtAssetStatistic)
        {
            return DBM.GetList("usp_IssueStatistic_GetListPagingSearch", search, out outLtAssetStatistic);
        }
    }

    public class AssetStatisticSearch
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int CreateDateCategoryID { get; set; }
        [JsonIgnore]
        public int AccountID { get; set; }
    }
}