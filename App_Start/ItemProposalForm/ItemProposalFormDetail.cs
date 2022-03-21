using ASM_API.App_Start.Store;
using ASM_API.App_Start.TableModel;
using BSS;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ASM_API.App_Start.ItemProposalForm
{
    public class ItemProposalFormDetailBase
    {
        public long ID { get; set; }
        public long ItemProposalFormID { get; set; }
        public int ItemTypeID { get; set; }
        public long ItemID { get; set; }
        public int ItemUnitID { get; set; }
        [Mapping("Số lượng", typeof(MappingObject))]
        public int Quantity { get; set; }
        [Mapping("Ghi chú", typeof(MappingObject))]
        public string Note { get; set; }
    }
    public class ItemProposalFormDetail : ItemProposalFormDetailBase, IKeyCompare
    {
        [JsonIgnore]
        public string ItemCode { get; set; }
        public object GetKey() => ItemCode;
        public string DisplayNameKey() => "Vật phẩm";

        /*private DataTable CreateDataTable()
        {
            DataTable db = new DataTable();
            db.Columns.Add(ItemCode, typeof(string));
            db.Columns.Add(ID, typeof(long));
            db.Columns.Add(ID, typeof(string));
        }*/

        public static string InsertUpdateByDataTable(DBM dbm, List<ItemProposalFormDetail> itemProposalFormDetails, long ItemProposalFormID, out List<ItemProposalFormDetail> au)
        {
            au = null;
            string msg = dbm.SetStoreNameAndParams("usp_ItemProposalFormDetail_InsertUpdate",
                        new
                        {
                            TypeData = itemProposalFormDetails.ToDataTable(),
                            ItemProposalFormID = ItemProposalFormID
                        }
                        );

            if (msg.Length > 0) return msg;

            return dbm.GetList(out au);
        }
        public static string GetOneByID(long ID, out ItemProposalFormDetail ItemProposalFormDetails)
        {
            return DBM.GetOne("usp_ItemProposalFormDetail_GetByID", new { ID }, out ItemProposalFormDetails);
        }
        public static string GetListByProposalFormID(long itemProposalFormID, out List<ItemProposalFormDetail> ltItemProposalFormDetails)
        {
            return DBM.GetList("usp_ItemProposalFormDetail_GetListByFormID", new { itemProposalFormID }, out ltItemProposalFormDetails);
        }
        public static string Delete(long ID)
        {
            return DBM.ExecStore("usp_ProposalFormDetail_Delete", new { ID });
        }
    }
    public class ItemProposalFormDetailModify : ItemProposalFormDetailBase
    {
        public string ItemName { get; set; }
        public string ItemUnitName { get; set; }
        public string ItemTypeName { get; set; }
        public int QuantilyInStore { get; set; }
        public string ProducerNames { get; set; }

        public static string GetListByProposalFormID(long itemProposalFormID, out List<ItemProposalFormDetailModify> ltItemProposalFormDetails)
        {
            return DBM.GetList("usp_ItemProposalFormDetailModify_GetListByFormID", new { itemProposalFormID }, out ltItemProposalFormDetails);
        }

    }
    public class ItemProposalExportReceipt : ItemProposalFormDetailModify
    {
        public List<ImportBatchDetailView> ltImportBatchDetailView { get; set; }
        public static string GetListByProposalFormID(long itemProposalFormID, int PlaceID, out List<ItemProposalExportReceipt> ltItemProposalFormDetails)
        {
            return DBM.GetList("usp_ItemProposalFormDetailModify_GetListByFormID", new { itemProposalFormID, PlaceID }, out ltItemProposalFormDetails);
        }
    }


}