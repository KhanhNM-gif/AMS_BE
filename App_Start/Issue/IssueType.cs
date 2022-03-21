using BSS;
using System;
using System.Collections.Generic;

public class IssueType : IMappingSingleField
{
    public int IssueTypeID { get; set; }
    public Guid ObjectGuid { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }
    public int IssueGroupID { get; set; }
    public string IssueGroupName { get; set; }
    public string IssueTypeCode { get; set; }
    public string IssueTypeName { get; set; }
    public string Cycling { get; set; }
    public int? IssueDateID { get; set; }
    public string IssueDateName { get; set; }
    public bool IsActive { get; set; }
    public int AccountID { get; set; }

    public string InsertUpdate(DBM dbm, out IssueType o)
    {
        string msg = dbm.SetStoreNameAndParams("usp_IssueType_InsertUpdate",
                    new
                    {
                        IssueTypeID,
                        AssetTypeID,
                        IssueGroupID,
                        IssueTypeCode,
                        IssueTypeName,
                        Cycling,
                        IssueDateID,
                        IsActive,
                        AccountID
                    }
                    );
        return dbm.GetOne(out o);
    }
    public static string SearchByIssue(string TextSearch, int IssueGroupID, string AssetTypeID, int PageSize, int CurrentPage, int AccountID, out List<IssueType> lt)
    {
        return DBM.GetList("usp_IssueType_GetIssueBySearch", new { TextSearch, IssueGroupID, AssetTypeID, PageSize, CurrentPage, AccountID }, out lt);
    }
    public static string SearchByIssue_Total(string TextSearch, int IssueGroupID, string AssetTypeID, int PageSize, int CurrentPage, int AccountID, out int Total)
    {
        Total = 0;
        return DBM.ExecStore("usp_IssueType_GetIssueBySearch_Total", new { TextSearch, IssueGroupID, AssetTypeID, PageSize, CurrentPage, AccountID }, out Total);
    }
    public static string GetOne(int IssueTypeID, int AccountID, out IssueType o)
    {
        return DBM.GetOne("usp_IssueType_GetByID", new { IssueTypeID, AccountID }, out o);
    }
    public static string CheckExist(int AssetTypeID, int IssueGroupID, string IssueTypeCode, string IssueTypeName, int AccountID, out IssueType o)
    {
        return DBM.GetOne("usp_IssueType_CheckExist", new { AssetTypeID, IssueGroupID, IssueTypeCode, IssueTypeName, AccountID }, out o);
    }
    public static string Delete(int IssueTypeID, int AccountID)
    {
        return DBM.ExecStore("usp_IssueType_Delete", new { IssueTypeID, AccountID });
    }
    public static string GetListByAssetTypeIDAndIssueGroup(int AssetTypeID, int IssueGroupID, int AccountID, out List<IssueType> lt)
    {
        return DBM.GetList("usp_IssueType_GetListByAssetTypeIDAndIssueGroup", new { AssetTypeID, IssueGroupID, AccountID }, out lt);
    }

    public string GetName() => IssueTypeName;

    public string GetOne(object k, out IMappingSingleField outModel)
    {
        outModel = null;

        string msg = DBM.GetOne("usp_IssueType_GetOne", new { IssueTypeID = (int)k }, out IssueType outIssueType);
        if (msg.Length > 0) return msg;
        outModel = outIssueType;

        return msg;
    }

    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }
}
public class IssueGroup : IMappingSingleField
{
    public int IssueGroupID { get; set; }
    public string IssueGroupName { get; set; }
    public static string GetList(out List<IssueGroup> lt)
    {
        return DBM.GetList("usp_IssueGroup_GetAll", new { }, out lt);
    }
    public string GetDifferences(object obj_new, object obj_old, out string strChange)
    {
        string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }

    public string GetName() => IssueGroupName;

    public string GetOne(object k, out IMappingSingleField outModel)
    {
        outModel = null;

        string msg = DBM.GetOne("usp_IssueGroup_GetOne", new { IssueGroupID = (int)k }, out IssueGroup issueGroup);
        if (msg.Length > 0) return msg;
        outModel = issueGroup;

        return msg;
    }
}
public class IssueDate
{
    public int IssueDateID { get; set; }
    public string IssueDateName { get; set; }
    public static string GetList(out List<IssueDate> lt)
    {
        return DBM.GetList("usp_IssueTypeDate_GetAll", new { }, out lt);
    }
}