using BSS;
using BSS.DataValidator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

public class Delegacy
{
     [JsonIgnore]
    public long DelegacyID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int UserIDDelegacy { get; set; }
    public int UserIDDelegacyed { get; set; }
     [JsonIgnore]
    public int UserIDCreate { get; set; }
    public bool IsDelete { get; set; }
     [JsonIgnore]
    public DateTime LastUpdate { get; set; }
     [JsonIgnore]
    public DateTime CreateDate { get; set; }

    public string InsertUpdate(DBM dbm, out Delegacy d)
    {
        d = null;

        string msg = dbm.SetStoreNameAndParams("usp_Delegacy_InsertUpdate",
            new
            {
                DelegacyID,
                UserIDDelegacy,
                UserIDDelegacyed,
                UserIDCreate
            });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out d);
    }

    public static string GetOne(long DelegacyID, out Delegacy o)
    {
        return DBM.GetOne("usp_Delegacy_SelectByDelegacyID", new { DelegacyID }, out o);
    }

    public static string GetOneByGuid(Guid ObjectGuid, out long DelegacyID)
    {
        DelegacyID = 0;

        Delegacy d;
        string msg = DBM.GetOne("usp_Delegacy_SelectByObjectGuid", new { ObjectGuid }, out d);
        if (msg.Length > 0) return msg;

        if (d == null) return ("Không tồn tại Delegacy có ObjectGuid = " + ObjectGuid).ToMessageForUser();
        DelegacyID = d.DelegacyID;

        return msg;
    }

    public static string GetList(bool TabDelegacy, int UserID, bool IsAddAll, string UserName, int StatusID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Delegacy_SelectSearch", new { TabDelegacy, UserID, IsAddAll, UserName, StatusID }, out dt);
    }
    public static string GetList(int UserIDDelegacy, int UserIDDelegacyed, out List<Delegacy> lt)
    {
        return DBM.GetList("usp_Delegacy_SelectByUserIDDelegacyAndUserIDDelegacyed", new { UserIDDelegacy, UserIDDelegacyed }, out lt);
    }
    public static string GetList(int UserIDDelegacyed, out List<Delegacy> lt)
    {
        return DBM.GetList("sp_Delegacy_SelectByUserIDDelegacyed", new { UserIDDelegacyed }, out lt);
    }    

    public static string UpdateIsDelete(long DelegacyID, bool IsDelete)
    {
        return DBM.ExecStore("usp_Delegacy_UpdateIsDelete", new { DelegacyID, IsDelete });
    }

    public static string ValidateDelegacy(Delegacy Delegacy)
    {
        string msg = DataValidator.Validate(Delegacy).ToErrorMessage();
        if (msg.Length > 0) return msg.ToMessageForUser();
        if (Delegacy.UserIDDelegacy <= 0) return "Bạn chưa chọn Người ủy quyền xử lý";
        if (Delegacy.UserIDDelegacyed <= 0) return "Bạn chưa chọn Người được ủy quyền xử lý";
        if (Delegacy.UserIDDelegacy == Delegacy.UserIDDelegacyed) return "Không được chọn Người ủy quyền xử lý = Người được ủy quyền xử lý";

        List<Delegacy> lt;
        msg = Delegacy.GetList(Delegacy.UserIDDelegacy, Delegacy.UserIDDelegacyed, out lt);
        if (msg.Length > 0) return msg;

        if (lt.Where(v => !v.IsDelete && v.DelegacyID != Delegacy.DelegacyID).Count() > 0) return ("Đã tồn tại cặp Ủy quyền xử lý").ToMessageForUser();

        return msg;
    }
}