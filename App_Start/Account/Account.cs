using BSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Account
{
    public int AccountID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string TaxCode { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string Password { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<AccountUser> ltAccountUser { get; set; }
    public AccountUserInfor AccountUser { get; set; }
    public static string GetOneByAccountID(int AccountID, out Account a)
    {
        return DBM.GetOne("usp_Account_GetOneByAccount", new { AccountID }, out a);
    }
    public static string GetListUserByAccount(int AccountID, out List<AccountUser> ltAccountUser)
    {
        return DBM.GetList("usp_Account_GetListUserByAccount", new { AccountID }, out ltAccountUser);
    }
    public static string GetOneByTaxCode(string TaxCode, out Account a)
    {
        return DBM.GetOne("usp_Account_GetOneByTaxCode", new { TaxCode }, out a);
    }
    public static string UpdateAccount(int AccountID, string Name, string Email, string Phone, string Address)
    {
        return DBM.ExecStore("usp_Account_UpdateAccount", new { AccountID, Name, Email, Phone, Address });
    }
    public string InsertUpdate(DBM dbm, out Account aud)
    {
        aud = null;
        string msg = dbm.SetStoreNameAndParams("usp_Account_InsertUpdate",
                    new
                    {
                        AccountID,
                        Name,
                        Code,
                        TaxCode,
                        Email,
                        Phone,
                        Address
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out aud);
    }
}
