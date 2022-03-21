using ASM_API.App_Start.Issue;
using BSS;
using BSS.DataValidator;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class IssueController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoInsertUpdate(out Issue issueNew);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return issueNew.ToResultOk();
        }

        private string DoInsertUpdate(out Issue outIssue)
        {
            outIssue = null;

            string msg = InsertUpdate_GetData(out Issue issue);
            if (msg.Length > 0) return msg;

            msg = DoInsertUpdate_Validate(issue, out var issueTypeDB, out var issueDB, out var assetDB);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (issue.IssueGroupID == Constants.IssueGroup.SU_CO)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_SC);
                if (msg.Length > 0) return msg;
            }
            else if (issue.IssueGroupID == Constants.IssueGroup.BAOHANH_SUACHUA)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_BHSC);
                if (msg.Length > 0) return msg;
            }
            else if (issue.IssueGroupID == Constants.IssueGroup.BAOTRI_BAODUONG)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_BTBD);
                if (msg.Length > 0) return msg;
            }
            else return "ID Nhóm vụ việc không hợp lệ".ToMessageForUser();

            msg = SetData(issue, issueDB, issueTypeDB, assetDB);
            if (msg.Length > 0) return msg;

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = issue.InsertUpdate(dbm, out outIssue);
                if (msg.Length > 0) return msg;

                if (issue.ListFileAttach != null)
                    foreach (var fa in issue.ListFileAttach)
                    {
                        msg = FileAttachObject.InsertUpdate(dbm, fa.FileAttachGUID, outIssue.ObjectGuid, fa.IsDelete);
                        if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
                    }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at Issue DoInsertUpdate";
            }

            dbm.CommitTransac();

            if (!(outIssue is null))
            {
                msg = FileAttach.GetByObjectGUID(outIssue.ObjectGuid, out List<FileAttach> outListFileAttach);
                if (msg.Length > 0) return msg;
                outIssue.ListFileAttach = outListFileAttach;
            }

            string logProsess = "";
            if (issue.IssueStatusID == Constants.IssueStatus.DX) logProsess = "Đã xử lý xong";
            if (issue.IssueStatusID == Constants.IssueStatus.KXL) logProsess = "Không xử lý";

            Log.WriteHistoryLog(issue.IssueID == 0 ? $"Ghi nhận vụ việc: {issue.IssueTypeName} - {issue.AssetCode}" : $"Cập nhật: {issue.InfoLogUpdate}", outIssue.ObjectGuid, UserToken.UserID);

            if (!string.IsNullOrEmpty(logProsess)) Log.WriteHistoryLog($"{logProsess} vụ việc {issue.IssueTypeName} - {issue.AssetCode}", outIssue.ObjectGuid, UserToken.UserID);

            return msg;
        }
        private string InsertUpdate_GetData(out Issue issue)
        {
            string msg; issue = null;

            msg = WebHelper.GetStringFromRequestForm(HttpContext.Current, "Issue", out string outIssueJson);
            if (msg.Length > 0) return msg;

            issue = JsonConvert.DeserializeObject<Issue>(outIssueJson);
            if (issue == null) return $"InsertUpdate_GetData -> Không thể convert Json to Issue; outIssueJson= {outIssueJson} ";

            return msg;
        }
        private string DoUploadFile(Guid ObjectGuid, out List<FileAttach> ltFileAttach)
        {
            ltFileAttach = new List<FileAttach>();
            string msg;

            int totalFile = HttpContext.Current.Request.Files.Count;
            if (totalFile == 0) return string.Empty;

            if (ObjectGuid != Guid.Empty)
            {
                msg = FileAttach.GetByObjectGUID(ObjectGuid, out List<FileAttach> outListFileAttach);
                if (msg.Length > 0) return msg;
                totalFile += outListFileAttach.Count;
            }
            if (totalFile > 5) return "Số lượng file đính kèm đã vượt quá số lượng tối đa".ToMessageForUser();

            try
            {
                return FileAttachUpload.Upload(UserToken.UserID, "Issue", Guid.Empty, out ltFileAttach);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private string SetData(Issue issueInput, Issue issueDB, IssueType issueType, Asset asset)
        {
            string msg;

            issueInput.AccountID = UserToken.AccountID;
            issueInput.IssueTypeName = issueType.IssueTypeName;
            issueInput.AssetCode = asset.AssetCode;

            if (issueDB == null)
            {
                if (issueInput.IssueGroupID == Constants.IssueGroup.SU_CO) issueInput.IssueCode = GetIssueCode("SC_");
                if (issueInput.IssueGroupID == Constants.IssueGroup.BAOHANH_SUACHUA) issueInput.IssueCode = GetIssueCode("BHSC_");
                if (issueInput.IssueGroupID == Constants.IssueGroup.BAOTRI_BAODUONG) issueInput.IssueCode = GetIssueCode("BHBD_");

            }
            else
            {
                msg = Issue.GetOneByIssueID(issueInput.IssueID, out Issue issueout);
                if (msg.Length > 0) return msg;
                issueInput.IssueCode = issueout.IssueCode;
                issueInput.ObjectGuid = issueDB.ObjectGuid;

                msg = issueInput.SetInfoChangeRequest(issueDB);
                if (msg.Length > 0) return msg;
            }

            msg = DoUploadFile(issueInput.ObjectGuid, out List<FileAttach> ltFileAttach);
            if (msg.Length > 0) return msg;
            issueInput.ListFileAttach = ltFileAttach;

            return string.Empty;
        }
        private string DoInsertUpdate_Validate(Issue issue, out IssueType issueTypeDB, out Issue issueDB, out Asset AssetDB)
        {
            string msg;

            issueDB = null;
            issueTypeDB = null;
            AssetDB = null;

            msg = DataValidator.Validate(new
            {
                issue.IssueID,
                issue.AssetID
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();
            if (issue.IssueID != 0)
            {
                msg = Issue.GetOneByIssueID(issue.IssueID, out issueDB);
                if (msg.Length > 0) return msg;
            }
            if (issue.IssueStatusID != Constants.IssueStatus.CXL && string.IsNullOrEmpty(issue.ProcessResult)) return ("Bạn bắt buộc nhập kết quả xử lý").ToMessageForUser();

            IssueType.GetOne(issue.IssueTypeID, UserToken.AccountID, out issueTypeDB);
            if (msg.Length > 0) return msg;
            if (issueTypeDB == null) return "Loại vụ việc không tồn tại".ToMessageForUser();

            Asset.GetOneByAssetID(issue.AssetID, out AssetDB);
            if (msg.Length > 0) return msg;
            if (AssetDB == null) return "Tài sản không tồn tại".ToMessageForUser();

            msg = Asset.GetOneByAssetID(issue.AssetID, out Asset asset);
            if (asset == null) return "ID tài sản không tồn tại".ToMessageForUser();

            if (issue.AssetID == 0) return ("Bạn chưa chọn Tài sản").ToMessageForUser();

            if (issue.UnitID == 0) return ("Bạn chưa chọn giá trị cho đơn vị xử lý").ToMessageForUser();

            if (issue.ProcessResult.Length < 20 && issue.ProcessResult.Length > 500) return ("Kết quả xử lý chỉ cho phép nhập lớn hơn 20 ký tự và nhỏ hơn 500 ký tự").ToMessageForUser();

            return msg;
        }
        private string GetIssueCode(string GroupNameCode)
        {
            string msg = "";
            msg = Issue.GetTotalByDateCode(GroupNameCode + DateTime.Now.ToString("yyMMdd"), out int Total);
            if (msg.Length > 0) return msg;
            return GroupNameCode + DateTime.Now.ToString("yyMMdd") + "_" + (Total + 1);
        }
        [HttpGet]
        public Result GetSuggestSearch(string TextSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            DataTable dt;
            string msg = DoGetSuggestSearch(TextSearch, out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
        private string DoGetSuggestSearch(string TextSearch, out DataTable dt)
        {
            dt = new DataTable();

            string msg = Issue.GetSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        [HttpGet]
        public Result GetAssetSuggestSearchIssue(string TextSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            DataTable dt;
            string msg = DoGetAssetSuggestSearchIssue(TextSearch, out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
        private string DoGetAssetSuggestSearchIssue(string TextSearch, out DataTable dt)
        {
            dt = new DataTable();

            string msg = Issue.AssetSuggestSearchIssue(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        [HttpPost]
        public Result GetListEasySearch([FromBody] IssueEasySearch data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetListEasySearch(data, out int total, out List<IssueSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoGetListEasySearch([FromBody] IssueEasySearch data, out int Total, out List<IssueSearchResult> lt)
        {
            lt = null;
            Total = 0;

            string msg = DoGetListEasySearch_GetAssetSearch(data, out IssueSearch issueSearch);
            if (msg.Length > 0) return msg;

            return DoGetList(issueSearch, out lt, out Total);
        }
        private string DoGetListEasySearch_GetAssetSearch([FromBody] IssueEasySearch data, out IssueSearch ms)
        {
            ms = new IssueSearch();
            ms.TextSearch = data.TextSearch;
            ms.CurrentPage = data.CurrentPage;
            ms.PageSize = data.PageSize;
            ms.CategorySearch = IssueSearch.DONGIAN;

            if (data.ObjectCategory == 1) ms.AssetTypeID = data.ObjectID.ToNumber(0);
            if (data.ObjectCategory == 2) ms.AssetID = data.ObjectID.ToNumber(0);
            if (data.ObjectCategory == 3) ms.IssueGroupID = data.ObjectID.ToNumber(0);
            if (data.ObjectCategory == 4) ms.IssueTypeID = data.ObjectID.ToNumber(0);
            if (data.ObjectCategory == 5 || data.ObjectCategory == 7) ms.IssueID = data.ObjectID.ToNumber(0);
            if (data.ObjectCategory == 6) ms.UserIDProcess = data.ObjectID.ToNumber(0);

            if (data.ObjectCategory != 0) ms.TextSearch = "";

            return "";
        }
        [HttpPost]
        public Result GetListAdvancedSearch([FromBody] IssueSearch issueSearch)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_IsVisitPage);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                msg = DoGetListAdvancedSearch(issueSearch, out int total, out List<IssueSearchResult> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch([FromBody] IssueSearch issueSearch, out int Total, out List<IssueSearchResult> lt)
        {
            lt = null;
            Total = 0;

            issueSearch.CategorySearch = AssetSearch.NANGCAO;

            return DoGetList(issueSearch, out lt, out Total);
        }
        private string DoGetList(IssueSearch IssueSearch, out List<IssueSearchResult> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                IssueSearch.AccountID = UserToken.AccountID;

                string msg = DataValidator.Validate(IssueSearch).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Issue.GetListSearch(IssueSearch, out lt, out totalSearch);
                if (msg.Length > 0) return msg;

                //msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_BHSC, out bool IsBHSC);
                //if (msg.Length > 0) return msg;

                //msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_BTBD, out bool IsBTBD);
                //if (msg.Length > 0) return msg;

                //msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_SC, out bool IsSC);
                //if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    msg = DoGetListButtonFuction(item, UserToken.UserID, out ButtonShowIssueSearch b);
                    if (msg.Length > 0) return msg;

                    item.ButtonShow = b;
                }

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private string DoGetListButtonFuction(IssueSearchResult issueSearchResult, int UserIDLogin,
            //bool IsBHSC, bool IsBTBD, bool IsSC,
            out ButtonShowIssueSearch b)
        {
            b = new ButtonShowIssueSearch();

            int s = issueSearchResult.IssueStatusID;

            if (UserIDLogin == issueSearchResult.UserIDCreate)
            {
                if (s == Constants.IssueStatus.CXL)
                {
                    b.Delete = true;
                    b.Edit = true;
                }
                if (s == Constants.IssueStatus.X)
                {
                    b.Restore = true;
                }
            }

            if (UserIDLogin == issueSearchResult.UserIDProcess)
            {
                if (s != Constants.IssueStatus.DX && s != Constants.IssueStatus.KXL && s != Constants.IssueStatus.X)
                {
                    b.Resolve = true;
                }
            }

            b.ViewHistory = true;
            return "";
        }
        [HttpGet]
        public Result GetOne(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = Issue.GetOneByGuid(ObjectGuid, out Issue issueDB);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            if (issueDB == null) return Log.ProcessError(("Không tồn tại vụ việc có  ObjectGuid =" + ObjectGuid).ToMessageForUser()).ToResultError();

            msg = Issue.GetOneByIssueID(issueDB.IssueID, out Issue issue);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (UserToken.UserID == issue.UserIDProcess && issue.IssueStatusID == Constants.IssueStatus.CXL)
            {
                msg = Issue.UpdateStatusIssue(new DBM(), issue.IssueID, Constants.IssueStatus.DXL);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                msg = AccountUser.GetOneByUserID(issue.UserIDProcess.ToNumber(0), out AccountUser accountUser);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                Log.WriteHistoryLog($"{accountUser.FullName}({accountUser.UserName}) đã xem và Cập nhật sang trạng thái Đang xử lý", issue.ObjectGuid, UserToken.UserID);

                issue.IssueStatusID = Constants.IssueStatus.DXL;
                issue.IssueStatusName = "Đang xử lý";

            }

            msg = AssetProperty.GetListByAssetID(issue.AssetID, out List<AssetProperty> typeProperties);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            issue.ListAssetTypeProperty = typeProperties;

            msg = FileAttach.GetByObjectGUID(ObjectGuid, out List<FileAttach> ListFileAttach);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            issue.ListFileAttach = ListFileAttach;

            return issue.ToResultOk();
        }
        [HttpGet]
        public Result ViewDetail(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = IssueViewDetail.ViewDetail(ObjectGuid, UserToken.AccountID, out IssueViewDetail issueViewDetail);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            if (issueViewDetail == null) return Log.ProcessError(("Không tồn tại vụ việc có  ObjectGuid =" + ObjectGuid).ToMessageForUser()).ToResultError();

            if (UserToken.UserID == issueViewDetail.UserIDProcess && issueViewDetail.IssueStatusID == Constants.IssueStatus.CXL)
            {
                msg = Issue.UpdateStatusIssue(new DBM(), issueViewDetail.IssueID, Constants.IssueStatus.DXL);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                msg = AccountUser.GetOneByUserID(issueViewDetail.UserIDProcess.ToNumber(0), out AccountUser accountUser);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                Log.WriteHistoryLog($"{accountUser.FullName}({accountUser.UserName}) đã xem và Cập nhật sang trạng thái Đang xử lý", issueViewDetail.ObjectGuid, UserToken.UserID);

                issueViewDetail.IssueStatusID = Constants.IssueStatus.DXL;
                issueViewDetail.IssueStatusName = "Đang xử lý";
            }

            msg = AssetProperty.GetListByAssetID(issueViewDetail.AssetID, out List<AssetProperty> typeProperties);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            issueViewDetail.ListAssetTypeProperty = typeProperties;

            msg = FileAttach.GetByObjectGUID(ObjectGuid, out List<FileAttach> ListFileAttach);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            issueViewDetail.ListFileAttach = ListFileAttach;

            return issueViewDetail.ToResultOk();
        }

        [HttpGet]
        public Result GetIssueStatusList()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetIssueStatusList(out List<IssueStatus> issueStatuses);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return issueStatuses.ToResultOk();
        }
        private string DoGetIssueStatusList(out List<IssueStatus> issueStatuses)
        {
            string msg = IssueStatus.GetStatusList(out issueStatuses);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpGet]
        public Result GetDepartmentListProcess()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetDepartmentListProcess(out List<IssueDepartmentProcess> issueDepartmentProcesses);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return issueDepartmentProcesses.ToResultOk();
        }
        private string DoGetDepartmentListProcess(out List<IssueDepartmentProcess> issueDepartmentProcesses)
        {
            string msg = IssueDepartmentProcess.GetDepartmentProcessList(UserToken.AccountID, out issueDepartmentProcesses);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)//Xóa vụ việc
        {
            return UpdateStatusByID(data, Constants.IssueStatus.X);
        }
        [HttpPost]
        public Result Restore([FromBody] JObject data)//Phục hồi vụ việc
        {
            return UpdateStatusByID(data, Constants.IssueStatus.CXL);
        }
        private Result UpdateStatusByID([FromBody] JObject data, int StatusID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVV, Role.ROLE_QLVV_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = UpdateStatusID(data, StatusID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string UpdateStatusID([FromBody] JObject data, int StatusID)
        {
            string logContent = "";
            string msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;

            msg = Issue.GetOneByGuid(ObjectGuid, out Issue issue);
            if (msg.Length > 0) return msg;

            if (issue == null) return ("Không tồn tại vụ việc: " + issue.IssueID).ToMessageForUser();

            if (StatusID == Constants.IssueStatus.X)
            {
                if (issue.IssueStatusID != Constants.IssueStatus.CXL) return "Bạn chỉ được xóa Vụ việc ở trạng thái Không xử lý".ToMessageForUser();
                logContent = "Xóa Vụ việc";
            }
            else if (StatusID == Constants.IssueStatus.CXL)
            {
                if (issue.IssueStatusID != Constants.IssueStatus.X) return "Bạn chỉ được khôi phục Vụ việc ở trạng thái Đã xóa".ToMessageForUser();
                logContent = "Khôi phục Vụ việc";
            }
            msg = UpdateStatusID_SaveToDB(issue, StatusID, logContent);
            if (msg.Length > 0) { return msg; }

            return msg;
        }
        private string UpdateStatusID_SaveToDB(Issue issue, int StatusID, string logContent)
        {
            DBM dbm = new DBM();
            dbm.BeginTransac();

            string msg = "";
            try
            {
                msg = Issue.UpdateStatusIssue(dbm, issue.IssueID, StatusID);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at Issue UpdateStatusID_SaveToDB";
            }

            dbm.CommitTransac();

            msg = Log.WriteHistoryLog(logContent, issue.ObjectGuid, UserToken.AccountID, Common.GetClientIpAddress(Request));
            return msg;
        }
        [HttpGet]
        public Result GetListHistoryAsset(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = CacheObject.GetAssetIDbyGUID(ObjectGuid, out long assetID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            DataTable dt;
            msg = Issue.GetHistoryByAssetID(assetID, out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }

        [HttpGet]
        public Result GetListHistoryItem(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListHistoryItem(ObjectGuid, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return dt.ToResultOk();

        }
        private string DoGetListHistoryItem(Guid ObjectGuid, out DataTable dt)
        {
            dt = null;

            string msg = Item.GetOneObjectGuid(ObjectGuid, out long outItemID);
            if (msg.Length > 0) return msg;

            msg = Issue.GetHistoryByItemID(outItemID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }
    }
}