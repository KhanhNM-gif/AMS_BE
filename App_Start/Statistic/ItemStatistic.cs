using BSS;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ASM_API.App_Start.Statistic
{
    public class StoreStatistic
    {
        public int PlaceID { get; set; }
        public string PlaceFullName { get; set; }
        public int TypeItemTotal { get; set; }

        public static string GetList(int AccountID, out List<StoreStatistic> outLtAssetStatistic)
        {
            return DBM.GetList("usp_StoreStatistic_GetList", new { AccountID }, out outLtAssetStatistic);
        }
    }

    public class ItemInStoreStatistic
    {
        public long ItemID { get; set; }
        public string ItemName { get; set; }
        public int ItemTypeID { get; set; }
        public string ItemTypeName { get; set; }
        public int Quantity { get; set; }
        public int ItemUnitID { get; set; }
        public string ItemUnitName { get; set; }
        public static string GetListPagingSearch(ItemStatisticSearch itemStatisticSearch, out List<ItemInStoreStatistic> outLtItemInStoreStatistic, out int total)
        {
            return Paging.ExecByStore("usp_ItemInStoreStatistic_GetListPagingSearch2", "ID", itemStatisticSearch, out outLtItemInStoreStatistic, out total);
        }
    }

    public class ManufactureStatistic
    {
        public int ManufactureID { get; set; }
        public string ManufactureName { get; set; }
        public int ItemTypeID { get; set; }
        public string ItemTypeName { get; set; }
        public int Quantity { get; set; }

        public static string GetListPagingSearch(ItemStatisticSearch search, out List<ManufactureStatistic> outLtAssetStatistic, out int total)
        {
            total = 0;

            string msg = DBM.GetList("usp_ManufactureStatistic_GetListPagingSearch", search, out outLtAssetStatistic);
            if (msg.Length > 0) return msg;

            return DBM.ExecStore("usp_ManufactureStatistic_GetTotal", search, out total);

        }
    }

    public class ItemStatisticSearch
    {
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int PlaceID { get; set; }
        [JsonIgnore]
        public int AccountID { get; set; }

        public string Validate()
        {
            if (PageSize <= 0 || PageSize > 10000) return $"PageSize = {PageSize} not validate";
            if (CurrentPage <= 0 || CurrentPage > 10000) return $"CurrentPage = {CurrentPage} not validate";

            string msg = Place.GetOneByPlaceID(PlaceID, AccountID, out var outPlace);
            if (msg.Length > 0) return msg;
            if (outPlace is null) return "Kho không tồn tại".ToMessageForUser();

            return "";
        }
    }
}