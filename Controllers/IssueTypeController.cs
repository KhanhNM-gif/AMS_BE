using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class IssueTypeController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.LVV, Role.ROLE_LVV_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            IssueType iNew;
            msg = DoInsertUpdate(data, out iNew);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return iNew.ToResultOk();
        }
        private string DoInsertUpdate([FromBody] JObject data, out IssueType iNew)
        {
            iNew = null;

            string msg = data.ToObject("IssueType", out IssueType issueType);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_Validate(issueType);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = issueType.InsertUpdate(new DBM(), out iNew);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog(issueType.IssueTypeID == 0 ? "Thêm mới loại vụ việc" : "Sửa loại vụ việc", iNew.ObjectGuid, UserToken.UserID);

            return msg;
        }
        private string DoInsertUpdate_Validate(IssueType issueType)
        {
            string msg = "";

            msg = DataValidator.Validate(new
            {
                issueType.AssetTypeID,
                issueType.IssueGroupID,
                issueType.IssueTypeCode,
                issueType.IssueTypeName
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            issueType.AccountID = UserToken.AccountID;

            msg = IssueType.CheckExist(issueType.AssetTypeID, issueType.IssueGroupID, issueType.IssueTypeCode, issueType.IssueTypeName, UserToken.AccountID, out IssueType issueout);
            if (msg.Length > 0) return msg.ToMessageForUser();
            if (issueout != null && issueType.IssueTypeID != issueout.IssueTypeID) return ("Dữ liệu đã tồn tại trong hệ thống").ToMessageForUser();

            if (issueType.IssueGroupID == Constants.IssueGroup.BAOTRI_BAODUONG)
            {
                if (string.IsNullOrEmpty(issueType.Cycling) || issueType.IssueDateID == 0)
                    return ("Bạn phải nhập thông tin chu kỳ hoặc chu kỳ theo Tuần, Ngày, Tháng, Năm").ToMessageForUser();
            }

            if (issueType.IssueTypeID != 0 && !issueType.IsActive)
            {
                msg = Issue.GetListIssueByIssueType(issueType.IssueTypeID, out List<Issue> outIssues);
                if (msg.Length > 0) return msg;
                if (outIssues.Any()) return $"Có Vụ việc đang gắn với Loại vụ việc {issueType.IssueTypeName}. Bạn vui lòng kiểm tra lại".ToMessageForUser();
            }

            return msg;
        }
        [HttpGet]
        public Result GetListSearch(string TextSearch, int IssueGroupID, string AssetTypeID, int PageSize, int CurrentPage)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListSearch(TextSearch, IssueGroupID, AssetTypeID, PageSize, CurrentPage, out int total, out List<IssueType> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }

        private string DoGetListSearch(string TextSearch, int IssueGroupID, string AssetTypeID, int PageSize, int CurrentPage, out int Total, out List<IssueType> lt)
        {
            lt = null;
            Total = 0;

            string msg = IssueType.SearchByIssue_Total(TextSearch, IssueGroupID, AssetTypeID, PageSize, CurrentPage, UserToken.AccountID, out Total);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = IssueType.SearchByIssue(TextSearch, IssueGroupID, AssetTypeID, PageSize, CurrentPage, UserToken.AccountID, out lt);
            if (msg.Length > 0) return msg.ToMessageForUser();

            return msg;
        }

        [HttpGet]
        public Result GetOne(int IssueTypeID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.LVV, Role.ROLE_LVV_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DataValidator.Validate(new { IssueTypeID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = IssueType.GetOne(IssueTypeID, UserToken.AccountID, out IssueType o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.LVV, Role.ROLE_LVV_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDelete(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoDelete(int UserID, [FromBody] JObject data)
        {
            string msg = data.ToNumber("IssueTypeID", out int IssueTypeID);
            if (msg.Length > 0) return msg;

            msg = IssueType.GetOne(IssueTypeID, UserToken.AccountID, out IssueType issueType);
            if (msg.Length > 0) return msg;
            if (issueType == null) return ("Không tồn loại vụ việc ID = " + IssueTypeID).ToMessageForUser();

            msg = DoDelete_Validate(issueType);
            if (msg.Length > 0) return msg;

            msg = IssueType.Delete(issueType.IssueTypeID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog("Xóa Loại vụ việc", issueType.ObjectGuid, UserID);

            return msg;
        }
        private string DoDelete_Validate(IssueType issueType)
        {
            string msg;

            msg = Issue.GetListIssueByIssueType(issueType.IssueTypeID, out List<Issue> outIssues);
            if (msg.Length > 0) return msg;
            if (outIssues.Any()) return $"Có Vụ việc đang gắn với Loại vụ việc {issueType.IssueTypeName}. Bạn vui lòng kiểm tra lại".ToMessageForUser();

            return msg;
        }
        [HttpGet]
        public Result GetListIssueGroup()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<IssueGroup> lt;
            string msg = IssueGroup.GetList(out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetListByAssetTypeIDAndIssueGroup(int AssetTypeID, int IssueGroupID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            //lấy ra danh sách tên loại vụ việc được chọn theo mã tài sản
            List<IssueType> lt;
            string msg = DoGetListByAssetTypeIDAndIssueGroup(AssetTypeID, IssueGroupID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
        public string DoGetListByAssetTypeIDAndIssueGroup(int AssetTypeID, int IssueGroupID, out List<IssueType> lt)
        {
            string msg = IssueType.GetListByAssetTypeIDAndIssueGroup(AssetTypeID, IssueGroupID, UserToken.AccountID, out lt);
            if (msg.Length > 0) return msg;
            if (lt.Count == 0) return ("Không có loại vụ việc tương ứng với tài sản bạn vừa chọn").ToMessageForUser();

            return msg;
        }
        [HttpGet]
        public Result GetListIssueDate()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<IssueDate> lt;
            string msg = IssueDate.GetList(out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
    }
}