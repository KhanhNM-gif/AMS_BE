using BSS;
using System;
using System.Collections.Generic;
using System.Data;

namespace ASM_API.App_Start.Store
{
    public class ImportBatchDetail
    {
        public ImportBatchDetail()
        {

        }
        public ImportBatchDetail(long ImportBatchID, ItemImportReceiptDetail itemImportReceiptDetail)
        {
            this.ImportBatchID = ImportBatchID;
            ItemID = itemImportReceiptDetail.ItemID;
            Quantity = itemImportReceiptDetail.Quantity;
            Price = itemImportReceiptDetail.PretaxPrice;
            DateManufacture = itemImportReceiptDetail.ManufacturingDate;
            ExpiryDate = itemImportReceiptDetail.ExpiryDate;
            ProducerID = itemImportReceiptDetail.ManufacturerID;
        }

        public long ImportBatchID { get; set; }
        public long ItemID { get; set; }
        public float Quantity { get; set; }
        public long Price { get; set; }
        public DateTime? DateManufacture { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public long ProducerID { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastUpdate { get; set; }

        public static DataTable GetDataTable(List<ImportBatchDetail> lt)
        {
            var dt = new DataTable();

            dt.Columns.Add("ImportBatchID", typeof(long));
            dt.Columns.Add("ItemID", typeof(long));
            dt.Columns.Add("Quantity", typeof(float));
            dt.Columns.Add("Price", typeof(long));
            dt.Columns.Add("DateManufacture", Nullable.GetUnderlyingType(typeof(DateTime)) ?? typeof(DateTime));
            dt.Columns.Add("ExpiryDate", Nullable.GetUnderlyingType(typeof(DateTime)) ?? typeof(DateTime));
            dt.Columns.Add("ProducerID", typeof(long));

            foreach (var item in lt)
                dt.Rows.Add(
                    item.ImportBatchID,
                    item.ItemID,
                    item.Quantity,
                    item.Price,
                    item.DateManufacture,
                    item.ExpiryDate,
                    item.ProducerID);

            return dt;
        }
        public static string InsertUpdateByDataType(DBM dbm, List<ImportBatchDetail> lt)
        {
            string msg = dbm.SetStoreNameAndParams("usp_ImportBatch_InsertByDataType",
                        new
                        {
                            TypeData = GetDataTable(lt)
                        }
                        );

            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }
        public static string GetList(long ItemID, long PlaceID, out List<ImportBatchDetail> outLt)
        {
            return DBM.GetList("usp_ImportBatch_GetListImportBatchByItemID", new { ItemID, PlaceID }, out outLt);
        }
        public static string GetListByIDs(string IDs, long ItemID, out List<ImportBatchDetail> itemExportReceiptDetails)
        {
            return DBM.GetList("usp_ImportBatchDetail_GetListByIDs", new { IDs, ItemID }, out itemExportReceiptDetails);
        }
    }

    public class ImportBatchDetailView : ImportBatchDetail
    {
        public string ImportBatchCode { get; set; }
        public string ProducerName { get; set; }

        public static string GetListInfoBatch(long ItemID, int PlaceID, out List<ImportBatchDetailView> lt)
        {
            return DBM.GetList("usp_ImportBatchDetailView_ListInfoBatch", new
            {
                PlaceID,
                ItemID
            }, out lt);
        }

    }
    public class ImportBatchDetailView2 : ImportBatchDetail
    {
        public string ImportBatchCode { get; set; }
        public string ProducerName { get; set; }
        public string InvoiceNumber { get; set; }
        public string VouchersNumber { get; set; }
        public string UnitItemName { get; set; }
        public long Price { get; set; }
        public DateTime ImportDate { get; set; }
        public int QuantityImport { get; set; }
        public string TypeImport { get; set; }

        public static string GetListInfoBatch(long ItemID, int PlaceID, out List<ImportBatchDetailView2> lt)
        {
            return DBM.GetList("usp_ImportBatchDetailView2_ListInfoBatch", new
            {
                PlaceID,
                ItemID
            }, out lt);
        }

    }

}