using ASM_API.App_Start.Store;
using ASM_API.App_Start.TableModel;
using BSS;
using System;
using System.Collections.Generic;

public class ImportBatch : IKeyCompare
{
    public long ImportBatchID { get; set; }
    public long? StoreItemID { get; set; }
    [Mapping("Lô", typeof(MappingObject))]
    public string ImportBatchCode { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? LastUpdate { get; set; }

    public string DisplayNameKey() => "Lô";

    public object GetKey()
    {
        string msg = DBM.ExecStore("usp_ImportBatch_GetCode", new { ImportBatchID }, out object obj);
        if (msg.Length > 0) return msg;

        return obj;
    }
    public string Insert(DBM dbm, out ImportBatch importBatch)
    {
        importBatch = null;

        string msg = dbm.SetStoreNameAndParams("usp_ImportBatch_Insert", new
        {
            ImportBatchID,
            StoreItemID,
            ImportBatchCode
        });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out importBatch);
    }

    public static string GetTotal(DateTime dateTime, int AccountID, out int total)
    {

        return DBM.ExecStore("usp_ImportBatch_GetTotalInDate", new { dateTime, AccountID }, out total);
    }
    public static string GetListByPlaceID(int PlaceID, out List<ImportBatch> importBatch)
    {
        return DBM.GetList("usp_ImportBatch_GetListByPlaceID", new { PlaceID }, out importBatch);
    }
}

public class ImportBatchViewDetail : ImportBatch
{
    public string ItemName { get; set; }
    public string StatusName { get; set; }
    public int StatusID { get; set; }
    public string ItemCode { get; set; }
    public string PlaceFullName { get; set; }
    public int PlaceID { get; set; }
    public string UserCreateDetail { get; set; }
    public List<ImportBatchDetailView2> ltImportBatchDetailView { get; set; }

    public static string GetOne(long ImportBatchID, int AccountID, long ItemID, out ImportBatchViewDetail outImportBatchViewDetail)
    {
        return DBM.GetOne("usp_ImportBatchViewDetail_GetOne", new { ImportBatchID, ItemID, AccountID }, out outImportBatchViewDetail);
    }
}
