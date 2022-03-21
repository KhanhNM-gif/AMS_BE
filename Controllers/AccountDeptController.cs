using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class AccountDeptController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPB, Role.ROLE_QLPB_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            AccountDept mNew;
            msg = DoInsertUpdate(data, out mNew);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return mNew.ToResultOk();
        }
        private string DoInsertUpdate([FromBody] JObject data, out AccountDept mNew)
        {
            mNew = null;
            string msg = "";

            msg = data.ToObject("Department", out AccountDept AccountDept);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_Validate(AccountDept);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = AccountDept.InsertUpdate(new DBM(), out mNew);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog(AccountDept.DeptID == 0 ? "Thêm phòng ban" : "Sửa phòng ban", mNew.ObjectGuid, UserToken.UserID);

            return msg;
        }
        private string DoInsertUpdate_Validate(AccountDept AccountDept)
        {
            string msg = "";

            string DeptName = AccountDept.DeptName.Trim();
            if (DeptName.Length == 0) return "Tên phòng ban không được để trống";

            if (DeptName.Length < 5 || DeptName.Length > 250) return "Bạn cần nhập tối thiểu 5 ký tự - tối đa 250 kýtự";

            msg = DataValidator.Validate(new
            {
                AccountDept.DeptID,
                AccountDept.DeptIDParent,
                AccountDept.DeptName,
                AccountDept.DeptCode
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            //AllABC09CharactersValidationRule vr = new AllABC09CharactersValidationRule();
            //Result vResult = vr.Validate(AccountDept.DeptCode);
            //if (!vResult.isOk) return vResult.Object.ToString().ToMessageForUser();

            AccountDept.AccountID = UserToken.AccountID;
            msg = AccountDept.GetList(UserToken.AccountID, out List<AccountDept> ltAccountDept);
            if (msg.Length > 0) return msg;
            if (ltAccountDept.Where(v => v.DeptID != AccountDept.DeptID && v.DeptCode == AccountDept.DeptCode && v.DeptName == AccountDept.DeptName && v.IsActive).Count() > 0)
                return "Thông tin " + AccountDept.DeptName + ", " + AccountDept.DeptCode + " đã có trong hệ thống AMS".ToMessageForUser();

            if (AccountDept.DeptID > 0 && !AccountDept.IsActive)
            {
                msg = AccountDept.GetOneAccountUserDeptByDeptID(AccountDept.DeptID, out List<AccountUser> ltUser);
                if (msg.Length > 0) return msg;

                if (ltUser.Count > 0)
                {
                    string strUsername = string.Join(", ", ltUser.Select(v => v.UserName));
                    return ($"Phòng Ban {AccountDept.DeptName} đang được sử dụng. Bạn cần kiểm tra lại các dữ liệu Người dùng {strUsername} trước khi cập nhật trạng thái thành không sử dụng").ToMessageForUser();
                }

                msg = AccountDept.GetListChildByDeptID(AccountDept.DeptID, UserToken.AccountID, out List<AccountDept> ltDept);
                if (msg.Length > 0) return msg;
                if (ltDept.Count > 0) return ($"Phòng ban {AccountDept.DeptName} có các phòng ban con đang hoạt động. Bạn cần chuyển tất các các phòng ban con sang trạng thái không sử dụng").ToMessageForUser();
            }


            return msg;
        }
        [HttpGet]
        public Result GetListByFilter(string DeptName, int IsActive)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPB, Role.ROLE_QLPB_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            List<AccountDept> lt;
            msg = AccountDept.GetListByFilter(DeptName, IsActive, UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpGet]
        public Result GetList()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPB, Role.ROLE_QLPB_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            List<AccountDept> lt;
            msg = AccountDept.GetList(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPB, Role.ROLE_QLPB_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDelete(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoDelete(int UserID, [FromBody] JObject data)
        {
            string msg = "";

            msg = data.ToNumber("DeptID", out int DeptID);
            if (msg.Length > 0) return msg;

            AccountDept accountDept;
            msg = AccountDept.GetOneByDeptID(DeptID, UserToken.AccountID, out accountDept);
            if (msg.Length > 0) return msg;
            if (accountDept == null) return ("Không tồn tại phòng ban ID = " + DeptID).ToMessageForUser();

            msg = DoDelete_Validate(accountDept);
            if (msg.Length > 0) return msg;

            msg = AccountDept.Delete(accountDept.DeptID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog("Xóa phòng ban", accountDept.ObjectGuid, UserID);

            return msg;
        }
        private string DoDelete_Validate(AccountDept AccountDept)
        {
            string msg = "";

            List<AccountUser> lstaccount;
            msg = AccountDept.GetOneAccountUserDeptByDeptID(AccountDept.DeptID, out lstaccount);
            if (msg.Length > 0) return msg;
            if (lstaccount != null && lstaccount.Count > 0)
            {
                string finalName = string.Join(", ", lstaccount.Select(p => p.UserName).ToArray());
                return ("Phòng ban đang được gắn với tài khoản người dùng " + finalName + ", bạn không thể xóa phòng ban này").ToMessageForUser();
            }

            msg = AccountDept.GetListChildByDeptID(AccountDept.DeptID, UserToken.AccountID, out List<AccountDept> ltDept);
            if (msg.Length > 0) return msg;
            if (ltDept.Count > 0) return ($"Bạn không thể xóa Phòng ban này, do Phòng ban: {AccountDept.DeptName} đang được gắn với các Chức vụ con").ToMessageForUser();

            return msg;
        }

        [HttpGet]
        public Result GetOne(int DeptID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPB, Role.ROLE_QLPB_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DataValidator.Validate(new { DeptID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            AccountDept o;
            msg = AccountDept.GetOneByDeptID(DeptID, UserToken.AccountID, out o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }

        [HttpPost]
        public Result ImportExcel()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPB, Role.ROLE_QLPB_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoImportExcel();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoImportExcel()
        {
            string msg = "";

            try
            {
                msg = DoImportExcel_GetData(out List<AccountDept> ltAccountDept);
                if (msg.Length > 0) return msg;

                msg = DoImportExcel_Validate(ltAccountDept);
                if (msg.Length > 0) return msg.ToMessageForUser();

                DBM dbm = new DBM();
                dbm.BeginTransac();
                msg = DoImportExcel_ObjectToDB(dbm, ltAccountDept);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
                dbm.CommitTransac();

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        Dictionary<string, string> MappingColumnExcel = new Dictionary<string, string>() {
                {"Mã phòng ban","DeptCode" },
                {"Tên đầy đủ phòng ban","DeptFullName" }
            };
        private string DoImportExcel_GetData(out List<AccountDept> lt)
        {
            string msg = ""; lt = null;

            var httpContext = HttpContext.Current;
            if (httpContext.Request.Files.Count == 0) return "Bạn chưa chọn File".ToMessageForUser();

            msg = BSS.Common.GetSetting("FolderFileUpload", out string FolderFileUpload);
            if (msg.Length > 0) return msg;

            Guid FileAttachGUID = Guid.NewGuid();
            string folderFileUpload = FolderFileUpload + "/" + FileAttachGUID;
            if (!Directory.Exists(folderFileUpload)) Directory.CreateDirectory(folderFileUpload);

            HttpPostedFile httpPostedFile = httpContext.Request.Files[0];
            string pathFile = folderFileUpload + "/" + httpPostedFile.FileName;
            httpPostedFile.SaveAs(pathFile);

            msg = BSS.Common.GetDataTableFromExcelFile(pathFile, out DataTable dt);
            if (msg.Length > 0) return msg;

            DataTable dt2 = new DataTable();
            foreach (var columnName in dt.Rows[0].ItemArray) dt2.Columns.Add(columnName.ToString());

            foreach (DataColumn column in dt2.Columns)
                foreach (var item in MappingColumnExcel)
                    if (item.Key == column.ColumnName) dt2.Columns[column.ColumnName].ColumnName = item.Value;

            string columnNames = "DeptCode,DeptFullName";
            foreach (var columnName in columnNames.Split(','))
                if (!dt2.Columns.Contains(columnName))
                {
                    foreach (var item in MappingColumnExcel)
                        if (item.Value == columnName)
                            return ("File excel thiếu cột " + item.Key).ToMessageForUser();
                }

            for (int i = 1; i < dt.Rows.Count; i++) if (!DoCheckDataRowEmpty(dt.Rows[i])) dt2.Rows.Add(dt.Rows[i].ItemArray);

            msg = BSS.Convertor.DataTableToList<AccountDept>(dt2, out lt);
            if (msg.Length > 0) return msg;

            return msg;
        }

        private bool DoCheckDataRowEmpty(DataRow dr)
        {
            return dr == null || dr.ItemArray.All(i => i is DBNull);
        }

        private string DoImportExcel_Validate(List<AccountDept> ltAccountDeptExcel)
        {
            string msg = "";

            msg = AccountDept.GetList(UserToken.AccountID, out List<AccountDept> ltAccountDeptDB);
            if (msg.Length > 0) return msg;

            var vGroup = ltAccountDeptExcel.GroupBy(v => v.DeptFullName);
            foreach (var item in vGroup) if (item.Count() > 1) return ("Không được thêm nhiều hơn 1 dòng " + item.Key).ToMessageForUser();

            foreach (var item in ltAccountDeptExcel)
            {
                List<string> ltError = new List<string>();
                string DeptFullName = item.DeptFullName == null ? "" : item.DeptFullName.Trim().Replace("/", "\\");
                if (DeptFullName.Length == 0) ltError.Add("Tên đầy đủ phòng ban không được để trống");
                else
                {
                    var vDeptFullNameDB = ltAccountDeptDB.Where(v => v.DeptFullName != null && v.DeptFullName.ToLower() == DeptFullName.ToLower());
                    if (vDeptFullNameDB.Count() > 0) ltError.Add("Phòng ban đã tồn tại trong hệ thống");

                    int LastIndexOf = item.DeptFullName.LastIndexOf("\\");
                    if (LastIndexOf == -1)
                    {
                        item.DeptName = item.DeptFullName;
                        if (item.DeptName.Length < 5 || item.DeptName.Length > 250) ltError.Add("Tên phòng ban, bạn cần nhập tối thiểu 5 ký tự - tối đa 250 ký tự ");
                    }
                    else if (LastIndexOf + 1 == item.DeptFullName.Length) ltError.Add("Tên phòng ban không được để trống");
                    else
                    {
                        item.DeptName = item.DeptFullName.Substring(LastIndexOf + 1);

                        if (item.DeptName.Length < 5 || item.DeptName.Length > 250) ltError.Add("Tên phòng ban, bạn cần nhập tối thiểu 5 ký tự - tối đa 250 ký tự ");

                        string DeptFullNameParent = item.DeptFullName.Substring(0, LastIndexOf);
                        var vDeptFullNameParentDB = ltAccountDeptDB.Where(v => v.DeptFullName == DeptFullNameParent);
                        if (vDeptFullNameParentDB.Count() > 0) item.DeptIDParent = vDeptFullNameParentDB.First().DeptID;
                        else
                        {
                            var vAccountDeptParentExcel = ltAccountDeptExcel.Where(v => v.DeptFullName == DeptFullNameParent);
                            if (vAccountDeptParentExcel.Count() == 0) ltError.Add("Không tồn tại phòng ban cha <b>" + DeptFullNameParent + "</b>. Bạn vui lòng thêm vào file excel");
                            else item.DeptFullNameParent = vAccountDeptParentExcel.First().DeptFullName;
                        }
                    }
                }
                string DeptCode = item.DeptCode == null ? "" : item.DeptCode.Trim().Replace("/", "\\");
                if (DeptCode.Length == 0) ltError.Add("Mã phòng ban không được để trống");

                AllABC09CharactersValidationRule vr = new AllABC09CharactersValidationRule();
                Result vResult = vr.Validate(item.DeptCode);
                if (!vResult.isOk) ltError.Add(vResult.Object.ToString());

                item.AccountID = UserToken.AccountID;
                item.IsActive = true;
                if (ltError.Count > 0) msg += " \n" + item.DeptFullName + "\n " + string.Join("\n", ltError) + "\n";
            }
            if (msg.Length > 0) return "Dữ liệu file excel không hợp lệ như sau: \n" + msg;

            return "";
        }
        private string DoImportExcel_ObjectToDB(DBM dbm, List<AccountDept> ltAccountDeptExcel)
        {
            string msg = "";

            var vAccountDeptExcel = ltAccountDeptExcel.OrderBy(v => v.DeptFullName);
            foreach (var item in vAccountDeptExcel)
            {
                msg = item.InsertUpdate(dbm, out AccountDept AccountDeptNew);
                if (msg.Length > 0) return msg;

                Log.WriteHistoryLog((item.DeptID == 0 ? "Thêm phòng ban " : "Sửa phòng ban") + " (Nhập bằng file excel)", AccountDeptNew.ObjectGuid, UserToken.UserID);

                foreach (var AccountDeptExcel in ltAccountDeptExcel)
                    if (AccountDeptExcel.DeptFullNameParent == item.DeptFullName) AccountDeptExcel.DeptIDParent = AccountDeptNew.DeptID;
            }

            return msg;
        }
    }
}