using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BSS;

public class UserToken
{
    public long ID { get; set; }
    public int AccountID { get; set; }
    public int UserID { get; set; }
    public string UserName { get; set; }
    public string DeptName { get; set; }
    public string PositionName { get; set; }
    public bool IsRememberPassword { get; set; }
    public string Token { get; set; }
    public string JsonWebToken { get; set; }
    public string SessionState { get; set; }
    public DateTime ExpiredDate { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime TimeUpdateExpiredDateToDB { get; set; }

    public string Insert()
    {
        long newID;
        string msg = DBM.ExecStore("usp_User_Token_Insert", new { AccountID, UserID, UserName, IsRememberPassword, Token, JsonWebToken, SessionState, ExpiredDate, CreateDate }, out newID);
        if (msg.Length > 0) return msg;

        ID = newID;
        TimeUpdateExpiredDateToDB = DateTime.Now;

        return msg;
    }

    static public string GetAllExpiredDate(out List<UserToken> ltDB_Token)
    {
        return DBM.GetList("usp_User_Token_SelectAllExpiredDate", new { }, out ltDB_Token);
    }

    static public string UpdateExpiredDate(long ID, DateTime ExpiredDate)
    {
        return DBM.ExecStore("usp_User_Token_UpdateExpiredDate", new { ID, ExpiredDate });
    }

    static public string Delete(long ID)
    {
        return DBM.ExecStore("usp_User_Token_DeleteByID", new { ID });
    }
    static public string DeleteByUserID(long UserID)
    {
        return DBM.ExecStore("usp_User_Token_DeleteByUserID", new { UserID });
    }
    static public string GetAllExpiredDateByUserID(int UserID, out List<UserToken> ltDB_Token)
    {
        return DBM.GetList("usp_User_Token_SelectAllExpiredDateByUserID", new { UserID }, out ltDB_Token);
    }
}