using BSS;
using System;
using System.Collections.Generic;
using System.Data;

public class Organization : ILogUpdate<Organization>
{
    public int OrganizationID { get; set; }
    public Guid ObjectGuid { get; set; }
    [Mapping("Loại Tổ chức", typeof(OrganizationType))]
    public int OrganizationTypeID { get; set; }
    [Mapping("Mã Tổ chức", typeof(MappingObject))]
    public string OrganizationCode { get; set; }
    [Mapping("Tên Tổ chức", typeof(MappingObject))]
    public string OrganizationName { get; set; }
    [Mapping("Địa chỉ", typeof(MappingObject))]
    public string OrganizationAddressDetail { get; set; }
    [Mapping("Số điện thoại", typeof(MappingObject))]
    public string OrganizationMobile { get; set; }
    [Mapping("Ghi chú", typeof(MappingObject))]
    public string OrganizationNote { get; set; }
    public bool IsActive { get; set; }
    public string ActiveText { get; set; }
    public int CountActive { get; set; }
    public int AccountID { get; set; }
    public string InfoLogUpdate { get; set; }

    public static string GetList(int AccountID, out List<Organization> lt)
    {
        return DBM.GetList("usp_Organization_GetAll", new { AccountID }, out lt);
    }
    public static string GetByActive(int AccountID, out List<Organization> lt)
    {
        return DBM.GetList("usp_Organization_GetByActive", new { AccountID }, out lt);
    }
    public static string GetListByProducer(int AccountID, out List<Organization> lt)
    {
        //return GetList(AccountID, Constants.OrganizationType.HSX, out lt);
        return GetList(AccountID, "Hãng sản xuất", out lt);
    }
    public static string GetListBySupplier(int AccountID, out List<Organization> lt)
    {
        //return GetList(AccountID, Constants.OrganizationType.NCC, out lt);
        return GetList(AccountID, "Nhà cung cấp", out lt);
    }
    public static string GetList(int AccountID, string OrganizationTypeName, out List<Organization> lt)
    {
        return DBM.GetList("usp_Organization_GetByOrganizationTypeID", new { OrganizationTypeName, AccountID }, out lt);
    }

    public static string GetListBySearch(string TextSearch, int OrganizationTypeID, int IsActive, int AccountID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Organization_Search", new { TextSearch, OrganizationTypeID, IsActive, AccountID }, out dt);
    }
    public static string Delete(int OrganizationID, int AccountID)
    {
        return DBM.ExecStore("usp_Organization_Delete", new { OrganizationID, AccountID });
    }
    public static string GetOne(int OrganizationID, int AccountID, out Organization o)
    {
        return DBM.GetOne("usp_Organization_GetByID", new { OrganizationID, AccountID }, out o);
    }
    public static string GetListByName(string OrganizationName, string OrganizationCode, int OrganizationTypeID, int AccountID, out List<Organization> o)
    {
        return DBM.GetList("usp_Organization_GetByName", new { OrganizationName, OrganizationCode, OrganizationTypeID, AccountID }, out o);
    }
    public string InsertUpdate(DBM dbm, out Organization o)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Organization_InsertUpdate",
                    new
                    {
                        OrganizationID,
                        OrganizationTypeID,
                        OrganizationName,
                        OrganizationCode,
                        OrganizationAddressDetail,
                        OrganizationMobile,
                        OrganizationNote,
                        IsActive,
                        AccountID
                    }
                    );
        return dbm.GetOne(out o);
    }

    public string SetInfoChangeRequest(Organization o)
    {

        string SEPARATOR = "; ";
        string msg = this.GetUpdateInfo3(o, SEPARATOR, out string logChange);
        if (msg.Length > 0) return msg;

        InfoLogUpdate = logChange;

        return string.Empty;
    }

    public string GetInfoChangeRequest() => InfoLogUpdate;
}
