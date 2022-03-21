using ASM_API.App_Start.Store;
using ASM_API.App_Start.TableModel;
using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ASM_API.App_Start.ItemImportReceipt
{
    public class ItemExportReceiptDetailBase
    {
        [JsonIgnore]
        public string ItemCode { get; set; }
        public long ItemID { get; set; }
        public virtual DateTime? ExpiryDate { get; set; }
        public virtual long ManufacturerID { get; set; }
        public virtual float Quantity { get; set; }
    }

    public class ItemExportReceiptDetail : ItemExportReceiptDetailBase, IKeyCompare
    {
        public string ImportBatchIDs { get; set; }
        [JsonIgnore]
        public long ID { get; set; }
        [JsonIgnore]
        public long ItemExportReceiptID { get; set; }
        [JsonIgnore]
        public List<ImportBatchDetail> ltImportBatch { get; set; }
        [JsonIgnore]
        public override DateTime? ExpiryDate { get; set; }
        [JsonIgnore]
        public override long ManufacturerID { get; set; }
        [JsonIgnore]
        public override float Quantity { get; set; }

        public string DisplayNameKey() => "Vật phẩm";
        public object GetKey() => ItemCode;
        private static DataTable GetDataTable(List<ItemExportReceiptDetail> lt)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("ItemExportReceiptID", typeof(long));
            dt.Columns.Add("ItemID", typeof(long));
            dt.Columns.Add("ExpiryDate", Nullable.GetUnderlyingType(typeof(DateTime)) ?? typeof(DateTime));
            dt.Columns.Add("Quantity", typeof(int));
            dt.Columns.Add("ManufacturerID", typeof(int));

            foreach (var item in lt)
                dt.Rows.Add(item.ItemExportReceiptID, item.ItemID, item.ExpiryDate, item.Quantity, item.ManufacturerID);

            return dt;
        }
        public string SetListImportBatch(int PlaceID)
        {
            string msg = ImportBatchDetail.GetList(ItemID, PlaceID, out var importBatchDetails);
            if (msg.Length > 0) return msg;

            if (string.IsNullOrEmpty(ImportBatchIDs))
                if (importBatchDetails.Any()) return "Bạn chưa chọn Lô xuất VP".ToMessageForUser();
                else return string.Empty;

            if (ImportBatchIDs.Split(',').Any(x => !long.TryParse(x, out long _))) return " ImportBatchIDs sai định dạng";

            msg = ImportBatchDetail.GetListByIDs(ImportBatchIDs, ItemID, out var outlt);
            if (msg.Length > 0) return msg;

            ltImportBatch = new List<ImportBatchDetail>();
            float quanlity = Quantity;
            foreach (var batch in outlt)
            {
                ltImportBatch.Add(batch);
                if (batch.Quantity >= quanlity) { batch.Quantity -= quanlity; quanlity = 0; break; }
                else { batch.Quantity = 0; quanlity -= batch.Quantity; }
            }

            if (quanlity > 0 && importBatchDetails.Count > outlt.Count) return "Số lượng VP trong lô đã chọn trong lô không đủ";

            var item = ltImportBatch.FirstOrDefault();

            if (item is null) return string.Empty;

            ExpiryDate = item.ExpiryDate;
            ManufacturerID = item.ProducerID;

            return string.Empty;
        }

        public static string InsertByDataTable(DBM dbm, long ItemExportReceiptID, List<ItemExportReceiptDetail> lt, out List<ItemExportReceiptDetail> outLt)
        {
            outLt = null;
            string msg = dbm.SetStoreNameAndParams("usp_ItemExportReceiptDetail_InsertByDataTable",
                        new
                        {
                            TypeData = GetDataTable(lt),
                            ItemExportReceiptID = ItemExportReceiptID
                        });

            if (msg.Length > 0) return msg;

            return dbm.GetList(out outLt);
        }
    }

    public class ItemExportReceiptDetailView : ItemExportReceiptDetailBase
    {
        public float ProposalQuantity { get; set; }
        public float InStoreQuantity { get; set; }
        public string ItemName { get; set; }
        public string ItemTypeName { get; set; }
        public string ManufacturerName { get; set; }
        public string ItemUnitName { get; set; }

        public static string GetList(long ItemExportReceiptID, out List<ItemExportReceiptDetailView> outItemImportReceiptDetailView)
        {
            return DBM.GetList("usp_ItemExportReceiptDetailView_GetOne", new { ItemExportReceiptID }, out outItemImportReceiptDetailView);
        }
    }
}