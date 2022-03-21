using ASM_API.App_Start.Issue;
using ASM_API.App_Start.TableModel;
using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public class Issue : ILogUpdate<Issue>
{
    public long IssueID { get; set; }
    public Guid ObjectGuid { get; set; }
    public long AssetID { get; set; }
    public Guid AssetObjectGuid { get; set; }
    [JsonIgnore]
    public string AssetCode { get; set; }
    public int IssueGroupID { get; set; }
    public int IssueTypeID { get; set; }
    [JsonIgnore]
    public string IssueTypeName { get; set; }
    public string IssueCode { get; set; }
    public string IssueDescription { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? IssueBeginDate { get; set; }
    public DateTime? IssueEndDate { get; set; }
    public int UserIDCreate { get; set; }
    public int UserIDProcess { get; set; }
    public bool IsUnit { get; set; }
    public int UnitID { get; set; }
    public string ProcessResult { get; set; }
    public int IssueStatusID { get; set; }
    public string IssueStatusName { get; set; }
    public string IssueCost { get; set; }
    public int AccountID { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<FileAttach> ListFileAttach { get; set; } = new List<FileAttach>();
    [JsonIgnore]
    public List<AssetProperty> ListAssetTypeProperty { get; set; }
    [JsonIgnore]
    public string InfoLogUpdate { get; set; }

    public string InsertUpdate(DBM dbm, out Issue issue)
    {
        issue = null;
        string msg = dbm.SetStoreNameAndParams("usp_Issue_InsertUpdate",
                    new
                    {
                        IssueID,
                        AssetID,
                        IssueTypeID,
                        IssueCode,
                        IssueDescription,
                        IssueDate,
                        IssueBeginDate,
                        IssueEndDate,
                        UserIDCreate,
                        IsUnit,
                        UnitID,
                        ProcessResult,
                        IssueStatusID,
                        IssueCost,
                        AccountID,
                        UserIDProcess
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out issue);
    }
    public static string GetSuggestSearch(string TextSearch, int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_Issue_SuggestSearch", new { TextSearch, AccountID }, out lt);
    }
    public static string AssetSuggestSearchIssue(string TextSearch, int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_Asset_SuggestSearchIssue", new { TextSearch, AccountID }, out lt);
    }
    public static string GetListSearch(IssueSearch issueSearch, out List<IssueSearchResult> lt, out int total)
    {
        total = 0; lt = null;

        string msg = GetListSearch_Parameter(issueSearch, out dynamic para);
        if (msg.Length > 0) return msg;

        msg = Paging.ExecByStore(@"usp_Issue_SelectSearch", "i.IssueID", para, out lt, out total);
        if (msg.Length > 0) return msg;

        return "";
    }
    public static string GetListIssueByIssueType(int IssueType, out List<Issue> ltIssue)
    {
        return DBM.GetList("usp_Issue_GetListIssueByIssueType", new { IssueType }, out ltIssue);
    }
    public static string GetListSearchTotal(IssueSearch assetSearch, out int total)
    {
        total = 0;

        dynamic o;
        string msg = GetListSearch_Parameter(assetSearch, out o);
        if (msg.Length > 0) return msg;

        return DBM.ExecStore("usp_Issue_SelectSearch_Total", o, out total);
    }
    private static string GetListSearch_Parameter(IssueSearch issueSearch, out dynamic o)
    {
        return GetListSearch_Parameter(false, issueSearch, out o);
    }
    private static string GetListSearch_Parameter(bool IsReport, IssueSearch issueSearch, out dynamic o)
    {
        o = null;
        string msg = "";
        o = new
        {
            issueSearch.TextSearch,
            issueSearch.AssetID,
            issueSearch.AssetTypeID,
            issueSearch.IssueGroupID,
            issueSearch.IssueStatusID,
            issueSearch.UnitID,
            issueSearch.IssueBeginDate,
            issueSearch.IssueEndDate,
            issueSearch.AccountID,
            issueSearch.UserIDProcess,
            issueSearch.IssueID,
            issueSearch.IssueTypeID,
            issueSearch.PageSize,
            issueSearch.CurrentPage
        };

        return msg;
    }

    public static string GetOneByGuid(Guid ObjectGuid, out long IssueID)
    {
        IssueID = 0;

        string msg = DBM.GetOne("usp_Issue_SelectByObjectGuid", new { ObjectGuid }, out Issue issue);
        if (msg.Length > 0) return msg;

        if (issue == null) return ("Không tồn tại Issue có ObjectGuid = " + ObjectGuid).ToMessageForUser();
        IssueID = issue.IssueID;

        return msg;
    }

    public static string GetOneByGuid(Guid ObjectGuid, out Issue issue)
    {
        issue = null;

        string msg = CacheObject.GetIssueIDbyGUID(ObjectGuid, out long issueID);
        if (msg.Length > 0) return msg;

        msg = GetOneByIssueID(issueID.ToNumber(0), out issue);
        if (msg.Length > 0) return msg;

        return msg;
    }
    public static string GetOneByIssueID(long IssueID, out Issue issue)
    {
        return DBM.GetOne("usp_Issue_GetByID", new { IssueID }, out issue);
    }
    public static string GetHistoryByAssetID(long AssetID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Issue_GetHistoryByAssetID", new { AssetID }, out dt);
    }
    public static string GetHistoryByItemID(long ItemID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Issue_GetHistoryByItemID", new { ItemID }, out dt);
    }
    public static string GetTotalByDateCode(string DateCode, out int Total)
    {
        return DBM.ExecStore("usp_Issue_GetByDateCode", new { DateCode }, out Total);
    }
    //public static string GetOneByGuid(Guid ObjectGuid, out long id)
    //{
    //    id = 0;

    //    string msg = DBM.GetOne("usp_Asset_GetByGuid", new { ObjectGuid }, out Asset asset);
    //    if (msg.Length > 0) return msg;

    //    if (asset == null) return ("Không tồn tại Tài sản có ObjectGuid = " + ObjectGuid).ToMessageForUser();
    //    id = asset.AssetID;
    //    return msg;
    //}

    public static string UpdateStatusIssue(DBM dbm, long IssueID, int IssueStatusID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_Issue_UpdateIssueStatus",
          new
          {
              IssueID,
              IssueStatusID
          });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }

    public string SetInfoChangeRequest(Issue IssueOld)
    {
        string SEPARATOR = "; ";
        var LtChanges = new List<string>();

        string msg = this.GetUpdateInfo2(IssueOld, SEPARATOR, out string logChange,
            new Tuple<string, string, IMappingModel>("IssueGroupID", "Nhóm vụ việc", new IssueGroup()),
            new Tuple<string, string, IMappingModel>("IssueTypeID", "Loại vụ việc", new IssueType()),
            new Tuple<string, string, IMappingModel>("IssueDescription", "Mô tả", new MappingObject()),
            new Tuple<string, string, IMappingModel>("IssueDate", "Ngày sự cố", new MappingDateTime()),
            new Tuple<string, string, IMappingModel>("IssueBeginDate", "Ngày bắt đầu", new MappingDateTime()),
            new Tuple<string, string, IMappingModel>("IssueEndDate", "Ngày kết thúc", new MappingDateTime()),
            new Tuple<string, string, IMappingModel>("ProcessResult", "Kết quả xử lý", new MappingObject()),
            new Tuple<string, string, IMappingModel>("IssueCost", "Chi phí", new MappingObject()),
            new Tuple<string, string, IMappingModel>("IssueStatusID", "Trạng thái", new IssueStatus()),
            new Tuple<string, string, IMappingModel>("UserIDCreate", "Người ghi nhận", new AccountUser()),
            new Tuple<string, string, IMappingModel>("UserIDProcess", "Người xử lý", new AccountUser())
        );
        if (msg.Length > 0) return msg;

        InfoLogUpdate = logChange;

        var ALDRRemove = IssueOld.ListFileAttach
            .Except(this.ListFileAttach, new IModelCompare<FileAttach>())
            .Select(x => $"Xóa {x.FileName}");
        LtChanges.AddRange(ALDRRemove);

        logChange += string.IsNullOrEmpty(logChange) ? "" : SEPARATOR + (LtChanges.Any() ? "Sửa Danh sách Hồ sơ đính kèm: " + string.Join(SEPARATOR, LtChanges) : "");

        return string.Empty;
    }

    public string GetInfoChangeRequest()
    {
        throw new NotImplementedException();
    }
}
public class IssueViewDetail
{
    public int IssueID { get; set; }
    public Guid ObjectGuid { get; set; }
    public long AssetID { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }
    public string AssetCode { get; set; }
    public string AssetStatusName { get; set; }
    public string AssetColor { get; set; }
    public string UserHoldingAsset { get; set; }
    public DateTime AssetDateIn { get; set; }
    public DateTime AssetDateBuy { get; set; }
    public int IssueGroupID { get; set; }
    public string IssueGroupName { get; set; }
    public int IssueTypeID { get; set; }
    public string IssueTypeName { get; set; }
    public string IssueDescription { get; set; }
    public string Cycling { get; set; }
    public string IssueDateName { get; set; }
    public long UserIDCreate { get; set; }
    public string UserCreateIssue { get; set; }
    public string UnitName { get; set; }
    public string ProcessResult { get; set; }
    public string IssueStatusName { get; set; }
    public int IssueStatusID { get; set; }
    public string IssueCode { get; set; }
    public string IssueCost { get; set; }
    public string UserProcess { get; set; }
    public long UserIDProcess { get; set; }
    public DateTime IssueBeginDate { get; set; }
    public DateTime IssueEndDate { get; set; }
    public List<FileAttach> ListFileAttach = new List<FileAttach>();
    public List<AssetProperty> ListAssetTypeProperty { get; set; }
    public static string ViewDetail(Guid ObjectGuid, int AccountID, out IssueViewDetail issue)
    {
        return DBM.GetOne("usp_Issue_ViewDetailByObjectGuid", new { ObjectGuid, AccountID }, out issue);
    }
}

public class IssueEasySearch
{
    public int ObjectCategory { get; set; }
    public string ObjectID { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public string TextSearch { get; set; }
}
public class IssueSearchResult
{
    public int IssueID { get; set; }
    public string IssueCode { get; set; }
    public Guid ObjectGuid { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }
    public DateTime IssueBeginDate { get; set; }
    public DateTime IssueEndDate { get; set; }
    public int IssueGroupID { get; set; }
    public string IssueGroupName { get; set; }
    public int IssueTypeID { get; set; }
    public string IssueTypeName { get; set; }
    public string IssueDescription { get; set; }
    public string UnitName { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public int IssueStatusID { get; set; }
    public string IssueStatusName { get; set; }
    public long UserIDCreate { get; set; }
    public long UserIDProcess { get; set; }
    public ButtonShowIssueSearch ButtonShow { get; set; }

}
public class ButtonShowIssueSearch
{
    public bool Edit { get; set; } //Sửa
    public bool Delete { get; set; } //Xóa
    public bool Restore { get; set; } //Khôi phục
    public bool Resolve { get; set; } //xử lý vụ việc
    public bool ViewHistory { get; set; }//xem lịch sử

}
public class IssueSearch
{
    public const int DONGIAN = 1, NANGCAO = 2;
    public string TextSearch { get; set; }
    public int CategorySearch { get; set; }
    public int AssetTypeID { get; set; }
    public int AssetID { get; set; }
    public int IssueID { get; set; }
    public int IssueTypeID { get; set; }
    public int UnitID { get; set; }
    public int IssueGroupID { get; set; }
    public int IssueStatusID { get; set; }
    public int AccountID { get; set; }
    public int UserIDProcess { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime IssueBeginDate { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime IssueEndDate { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public IssueSearch()
    {
        IssueStatusID = 0;
        AssetTypeID = 0;
        AssetID = 0;
        IssueTypeID = 0;
        IssueID = 0;
        UnitID = 0;
        IssueGroupID = 0;
        AccountID = 1;
        CurrentPage = 0;
        PageSize = 0;
        UserIDProcess = 0;

        DateTime dtDefault = DateTime.Parse("1900-01-01");
        IssueBeginDate = IssueEndDate = dtDefault;
    }
}

public class IssueDepartmentProcess
{
    public int ID { get; set; }
    public int ParentID { get; set; }
    public string Name { get; set; }
    public int IsUnit { get; set; }

    public static string GetDepartmentProcessList(int AccountID, out List<IssueDepartmentProcess> issueDepartmentProcesses)
    {
        return DBM.GetList("usp_Issues_GetDeptListProcess", new { AccountID }, out issueDepartmentProcesses);
    }
}