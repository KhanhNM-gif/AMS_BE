using ASM_API.App_Start.TableModel;
using BSS;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;

namespace ASM_API.App_Start.InventoryStore
{
    public class InventoryStoreDetailBase
    {
        public long ItemID { get; set; }
        public int QuantityActual { get; set; }
        public string Reason { get; set; }
        public virtual int QuantityInStore { get; set; }
        public virtual long InventoryStoreID { get; set; }

    }
    public class InventoryStoreDetail : InventoryStoreDetailBase, IKeyCompare
    {
        [JsonIgnore]
        public override int QuantityInStore { get; set; }
        [JsonIgnore]
        public string ItemCode { get; set; }
        [JsonIgnore]
        public override long InventoryStoreID { get; set; }

        public static DataTable GetDataTable(List<InventoryStoreDetail> lt)
        {
            var dt = new DataTable();

            dt.Columns.Add("ItemID", typeof(long));
            dt.Columns.Add("InventoryStoreID", typeof(long));
            dt.Columns.Add("QuantityInStore", typeof(float));
            dt.Columns.Add("QuantityActual", typeof(float));
            dt.Columns.Add("Reason", typeof(string));

            foreach (var item in lt)
                dt.Rows.Add(
                    item.ItemID,
                    item.InventoryStoreID,
                    item.QuantityInStore,
                    item.QuantityActual,
                    item.Reason);

            return dt;
        }
        public static string InsertUpdateByDataType(DBM dbm, List<InventoryStoreDetail> lt, long InventoryStoreID, out List<InventoryStoreDetail> outInventoryStoreDetail)
        {
            outInventoryStoreDetail = null;

            string msg = dbm.SetStoreNameAndParams("usp_InventoryStoreDetail_InsertByDataType",
                        new
                        {
                            TypeData = GetDataTable(lt),
                            InventoryStoreID
                        }
                        );

            if (msg.Length > 0) return msg;

            return dbm.GetList(out outInventoryStoreDetail);
        }

        public static string GetListByInventoryStoreID(long InventoryStoreID, out List<InventoryStoreDetail> lt)
        {
            return DBM.GetList("usp_InventoryStoreDetailView_GetList", new { InventoryStoreID }, out lt);
        }

        public object GetKey() => new { ItemCode };

        public string DisplayNameKey() => "Danh sách Vật Phẩm";
    }

    public class InventoryStoreDetailSearch : InventoryStoreDetailBase
    {
        public string ItemName { get; set; }
        public string ItemUnitName { get; set; }
        public string ItemTypeName { get; set; }
        public string ItemCode { get; set; }

        public static string GetListItemInStore(int AccountID, int UserID, string ItemTypes, string BatchIDs, int PlaceID, out List<InventoryStoreDetailSearch> outListInventoryStoreDetailView)
        {
            return DBM.GetList("usp_InventoryStoreDetailSearch_GetListItemInStore", new
            {
                AccountID,
                UserID,
                ItemTypes,
                BatchIDs,
                PlaceID
            }, out outListInventoryStoreDetailView);
        }
    }

    public class InventoryStoreDetailView : InventoryStoreDetailBase
    {
        public string ItemName { get; set; }
        public string ItemUnitName { get; set; }
        public string ItemTypeName { get; set; }
        public string ItemCode { get; set; }

        public static string GetListItemByInventoryStoreID(int AccountID, long InventoryStoreID, out List<InventoryStoreDetailView> outListInventoryStoreDetailView)
        {
            return DBM.GetList("usp_InventoryStoreDetailView_GetList", new
            {
                AccountID,
                InventoryStoreID
            }, out outListInventoryStoreDetailView);
        }
    }
}