using BSS;
using System;
using System.Collections.Generic;

public class TransferHandlingLog
{
    public long ID { get; set; }
    public long ObjectID { get; set; }
    public int ObjectTypeID { get; set; }
    public long UserIDHandling { get; set; }
    public string Comment { get; set; }
    public string TransferDirectionID { get; set; }
    public DateTime TransferDate { get; set; }

    public string InsertUpdate(DBM dbm, out TransferHandlingLog o)
    {
        string msg = dbm.SetStoreNameAndParams("usp_TransferHandlingLog_Insert",
                    new
                    {
                        ObjectID,
                        ObjectTypeID,
                        UserIDHandling,
                        Comment,
                        TransferDirectionID
                    }
                    );
        return dbm.GetOne(out o);
    }
    public static string GetByObjectID(long ObjectID, int TranferTypeID, out TransferHandlingLog transferHandlingLog)
    {
        return DBM.GetOne("usp_TransferHandlingLog_GetByObjectID", new { ObjectID, TranferTypeID }, out transferHandlingLog);
    }

}
public class TransferHandlingLogView : TransferHandlingLog
{
    public string UserHandlingDetail { get; set; }
    public static string GetList(long ObjectID, int ObjectTypeID, string TranferTypeID, out List<TransferHandlingLogView> transferHandlingLog)
    {
        return DBM.GetList("usp_TransferHandlingLogView_GetList", new { ObjectID, ObjectTypeID, TranferTypeID }, out transferHandlingLog);
    }
}