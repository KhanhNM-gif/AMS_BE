using BSS;
using System;
using System.Collections.Generic;

public class OrganizationType : IMappingSingleField
{
    public int OrganizationTypeID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string OrganizationTypeName { get; set; }
    public int OrganizationTypeOrder { get; set; }
    public bool IsActive { get; set; }

    public static string GetList(out List<OrganizationType> lt)
    {
        return DBM.GetList("usp_OrganizationType_GetAll", new { }, out lt);
    }

    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }

    public string GetName() => OrganizationTypeName;

    public string GetOne(object k, out IMappingSingleField outModel)
    {
        outModel = null;

        string msg = DBM.GetOne("usp_OrganizationType_GetOne", new { OrganizationTypeID = (int)k }, out OrganizationType outOrganizationType);
        if (msg.Length > 0) return msg;
        outModel = outOrganizationType;

        return msg;
    }
}
