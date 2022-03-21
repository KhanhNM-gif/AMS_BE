using BSS;
using System.Collections.Generic;
using System.Data;

public class TransferHandlingDirection
{
    public int TransferTypeID { get; set; }
    public string TransferDirectionID { get; set; }
    public long UserIDHandling { get; set; }
    public string TransferDirectionName { get; set; }
    public long UserID { get; set; }

    public static string GetTransferDirection(long UserID, int TransferTypeID, out List<TransferHandlingDirection> lt)
    {
        return DBM.GetList("usp_TransferHandlingDirection_GetTransferDirection", new { UserID, TransferTypeID }, out lt);
    }
    public static string GetUserIDHandling(long UserID, int AccountID, string TransferDirectionID, out DataTable lt)
    {
        return DBM.ExecStore("usp_TransferHandlingDirection_GetUserIDHandling", new { UserID, AccountID, TransferDirectionID }, out lt);
    }
    public static string GetUserHandling(long UserID, int AccountID, string TransferDirectionID, out List<AccountUser> lt)
    {
        return DBM.GetList("usp_TransferHandlingDirection_GetUserIDHandling", new { UserID, AccountID, TransferDirectionID }, out lt);
    }
}
