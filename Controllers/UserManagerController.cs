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
    public class UserManagerController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] AccountUser data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLND, Role.ROLE_QLND_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertUpdate(UserToken.UserID, data, out AccountUser AccountUserOut);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return AccountUserOut.ToResultOk();
        }
        private string DoInsertUpdate(int UserIDCreate, [FromBody] AccountUser data, out AccountUser AccountUserOut)
        {
            AccountUserOut = new AccountUser();
            string msg = "";

            if (data.ObjectGuid != Guid.Empty)
            {
                long UserID;
                msg = CacheObject.GetUserIDbyGUID(data.ObjectGuid, out UserID);
                if (msg.Length > 0) return msg;
                data.UserID = (int)UserID;
            }

            msg = DoInsertUpdate_Validate(data);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (string.IsNullOrEmpty(data.UrlAvatar)) data.UrlAvatar = AccountUser.GetUrlAvatar(data.Sex);

            AccountUser user = new AccountUser();
            msg = BSS.Common.CopyObjectPropertyData(data, user);
            if (msg.Length > 0) return msg;

            if (user.UserID == 0)
            {
                user.PasswordSalt = Common.GenerateRandomBytes(16);
                user.PasswordHash = Common.GetInputPasswordHash(user.Password, user.PasswordSalt);
            }
            else
            {
                msg = AccountUser.GetOneByUserID(user.UserID, out AccountUser accountUser);
                if (msg.Length > 0) return msg;

                user.PasswordSalt = accountUser.PasswordHash;
                user.PasswordHash = accountUser.PasswordHash;
            }

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoInsertUpdate_ObjectToDB(dbm, UserIDCreate, user, data.RoleGroupID, out AccountUserOut);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at UserManager DoInsertUpdate";
            }



            dbm.CommitTransac();

            return msg;
        }

        private string DoInsertUpdate_ObjectToDB(DBM dbm, int UserIDCreate, AccountUser user, int RoleGroupID, out AccountUser AccountUserOut)
        {
            string msg = user.InsertUpdate(dbm, out AccountUserOut);
            if (msg.Length > 0) return msg;

            //gắn quyền với người dùng vừa thêm
            UserRoleGroup userRoleGroup = new UserRoleGroup { UserID = AccountUserOut.UserID, RoleGroupID = RoleGroupID, UserIDCreate = UserIDCreate, AccountID = UserToken.AccountID };
            msg = userRoleGroup.InsertUpdate(dbm, out UserRoleGroup uNew);
            if (msg.Length > 0) return msg;

            List<AccountUserDept> lstUserDept = new List<AccountUserDept>();

            msg = AccountUserDept.InsertUpdateIsActive(dbm, user.UserID, false);
            if (msg.Length > 0) return msg;

            foreach (var item in user.lstAccountUserDept)
            {
                AccountUserDept accountUserDept = new AccountUserDept { UserID = AccountUserOut.UserID, DeptID = item.DeptID, PositionID = item.PositionID, SuperiorID = item.SuperiorID, IsActive = item.IsActive, IsConcurrently = item.IsConcurrently };
                msg = accountUserDept.InsertUpdate(dbm, out AccountUserDept accUserDept);
                if (msg.Length > 0) return msg;
                lstUserDept.Add(accUserDept);
            }
            AccountUserOut.lstAccountUserDept = lstUserDept;

            Log.WriteHistoryLog(user.UserID == 0 ? "Thêm người dùng" : "Sửa người dùng", AccountUserOut.ObjectGuid, UserIDCreate);
            return msg;
        }
        private string DoInsertUpdate_Validate([FromBody] AccountUser data)
        {
            string msg = "";

            //kiểm tra username đã tồn tại
            msg = AccountUser.GetByUserName(data.UserName, out List<AccountUser> userExist);
            if (userExist != null && userExist.Count(t => t.AccountID == UserToken.AccountID && data.UserID != t.UserID) > 0) return ("Tên đăng nhập " + data.UserName + " đã tồn tại").ToMessageForUser();

            if (data.UserID == 0)
            {
                msg = DataValidator.Validate(new
                {
                    data.UserID,
                    data.UrlAvatar,
                    data.BirthDate,
                    data.Sex
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = DataValidator.Validate(new
                {
                    data.Password
                }).ToErrorMessage();
                if (msg.Length > 0) return ("Mật khẩu của bạn phải có ít nhất 8 ký tự, bao gồm một số, một chữ in và một chữ thường").ToMessageForUser();
            }
            else
            {
                msg = DataValidator.Validate(new
                {
                    data.UserID,
                    data.UrlAvatar,
                    data.BirthDate,
                    data.Sex
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();
            }

            if (data.FullName.Length < 5 || data.FullName.Length > 50) return "Tên đầy đủ bạn cần nhập tối thiểu 5 ký tự - tối đa 50 ký tự";
            if (data.UserName.Length < 5 || data.UserName.Length > 30) return "Tên đăng nhập bạn cần nhập tối thiểu 5 ký tự - tối đa 30 ký tự";

            AllABC09CharactersValidationRule vr = new AllABC09CharactersValidationRule();
            Result vResult = vr.Validate(data.UserName);
            if (!vResult.isOk) return ("Tên đăng nhập không hợp lệ: " + data.UserName).ToMessageForUser();

            if (data.Password.Length <= 6) ("Bạn phải nhập vào Mật khẩu tối thiểu 6 ký tự và không vượt quá 20 ký tự").ToMessageForUser();

            if (data.lstAccountUserDept.Count == 0) return ("Bạn phải chọn phòng ban, chức vụ cho User:" + data.UserName).ToMessageForUser();

            foreach (var item in data.lstAccountUserDept)
            {
                if (item.PositionID == 0 || item.DeptID == 0) return ("Bạn phải chọn đầy đủ phòng ban, chức vụ User:" + data.UserName).ToMessageForUser();
            }

            return msg;
        }
        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLND, Role.ROLE_QLND_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDelete(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoDelete([FromBody] JObject data)
        {
            string msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;

            msg = AccountUser.GetOneByObjectGUID(ObjectGuid, UserToken.AccountID, out AccountUser acc);
            if (msg.Length > 0) return msg;
            if (acc == null) return ("Không tồn tại tài sản Guid = " + ObjectGuid).ToMessageForUser();

            msg = Asset.GetListAssetByUserID(acc.UserID, out List<Asset> lt);
            if (msg.Length > 0) return msg;

            if (lt != null && lt.Count > 0)
            {
                string AssetCode = string.Join(", ", lt.Select(p => p.AssetCode).ToArray());
                return ($"Người dùng {acc.UserName} đang được gắn với thông tin Tài sản {AssetCode}. Vui lòng rà soát lại thông tin trước khi xóa!").ToMessageForUser();
            }

            msg = ProposalForm.GetUserIDHandlingByStatus(UserToken.UserID, Constants.StatusPDX.CD, out List<ProposalForm> ltproposalform);
            if (msg.Length > 0) return msg;

            if (ltproposalform != null && ltproposalform.Count > 0)
            {
                string ProposalFormCode = string.Join(", ", ltproposalform.Select(p => p.ProposalFormCode).ToArray());
                return ($"Người dùng {acc.UserName} đang được gắn với thông tin phiếu đề xuất {ProposalFormCode}. Vui lòng rà soát lại thông tin trước khi xóa!").ToMessageForUser();
            }

            msg = AccountUser.Delete(acc.UserID);
            if (msg.Length > 0) return msg;

            msg = AccountUser.GetOneByUserID(UserToken.UserID, out AccountUser acc1);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog($"{acc1.UserName} xóa tài khoản người dùng {acc.UserName}", acc.ObjectGuid, UserToken.UserID);

            return msg;
        }

        public Result GetListUserManager(int AccountPositionID, int AccountDeptID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLND, Role.ROLE_QLND_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = AccountUser.GetListUserManager(AccountPositionID, AccountDeptID, UserToken.AccountID, out var ltAccountUserManager);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ltAccountUserManager.ToResultOk();
        }

        [HttpPost]
        public Result ChangePass([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            //string msg = Role.Check(UserToken.UserID, Constants.TabID.QLND, Role.ROLE_QLND_CRUD);
            //if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            string msg = DoChangePass(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = CacheUserToken.Logout(data, out string urlLogout);
            return urlLogout.ToResultOk();

        }

        private string DoChangePass(int UserID, [FromBody] JObject data)
        {
            string msg = "";

            msg = data.ToString("PasswordOld", out string PasswordOld);
            if (msg.Length > 0) return msg;

            msg = data.ToString("PasswordNew", out string Password);
            if (msg.Length > 0) return msg;

            msg = DoChangePass_Validate(PasswordOld, Password, UserID, out AccountUser o);
            if (msg.Length > 0) return msg;

            byte[] PasswordSalt = Common.GenerateRandomBytes(16);
            byte[] PasswordHash = Common.GetInputPasswordHash(Password, PasswordSalt);

            msg = AccountUser.ChangePassword(UserID, PasswordSalt, PasswordHash);
            if (msg.Length > 0) return msg;

            if (o.IsChangePassFirstLogin)
            {
                msg = AccountUser.UpdateIsChangepassFirstLogin(UserToken.UserID);
                if (msg.Length > 0) return msg;
            }

            msg = Log.WriteHistoryLog("Đổi mật khẩu", o.ObjectGuid, UserID);

            return msg;
        }
        private string DoChangePass_Validate(string PasswordOld, string Password, int UserID, out AccountUser o)
        {
            string msg = AccountUser.GetOneByUserID(UserID, out o);
            if (msg.Length > 0) return msg;

            msg = DataValidator.Validate(new
            {
                Password
            }).ToErrorMessage();
            if (msg.Length > 0) return ("Mật khẩu của bạn phải có ít nhất 8 ký tự, bao gồm một số, một chữ in và một chữ thường").ToMessageForUser();

            byte[] PasswordHash = Common.GetInputPasswordHash(PasswordOld, o.PasswordSalt);
            if (!o.PasswordHash.SequenceEqual(PasswordHash)) return ("Mật khẩu cũ không chính xác").ToMessageForUser();

            if (Password.Length < 6 && Password.Length > 50) return ("Bạn chỉ được phép nhập Mật khẩu tối thiểu 6 ký tự, tối đa 50 ký tự cho phép").ToMessageForUser();

            return msg;

        }

        [HttpPost]
        public Result ChangePassForOtherUser([FromBody] JObject data)
        {
            //if (!ResultCheckToken.isOk) return ResultCheckToken;

            //string msg = Role.Check(UserToken.UserID, Constants.TabID.QLND, Role.ROLE_QLND_CHANGEROLE);
            //if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            string msg = "";
            int userID = Convertor.ToNumber(data["UserID"], -1);
            string password = Convertor.ToString(data["PasswordNew"], "");

            //msg = DoCheckValidatePasswordForOtherUser(UserToken.UserID, password);
            //if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            byte[] PasswordSalt = Common.GenerateRandomBytes(16);
            byte[] PasswordHash = Common.GetInputPasswordHash(password, PasswordSalt);

            msg = AccountUser.ChangePassword(userID, PasswordSalt, PasswordHash);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = AccountUser.GetOneByUserID(userID, out AccountUser o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            //msg = Log.WriteHistoryLog($"Đổi mật khẩu cho {o.FullName}", o.ObjectGuid, UserToken.UserID);
            //if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }

        private string DoCheckValidatePasswordForOtherUser(int UserID, string Password)
        {
            int USER_ROLE_IS_ADMIN = 1;
            if (UserID < 1) return "Người dùng không tồn tại".ToMessageForUser();

            if (string.IsNullOrEmpty(Password)) return "Mật khẩu không được để trống.";
            if (Password.Length < 6 || Password.Length > 50) return "Bạn chỉ được phép nhập Mật khẩu tối thiểu 6 ký tự, tối đa 50 ký tự cho phép".ToMessageForUser();

            string msg = DataValidator.Validate(new
            {
                Password
            }).ToErrorMessage();
            if (msg.Length > 0) return ("Mật khẩu của bạn phải có ít nhất 8 ký tự, bao gồm một số, một chữ in và một chữ thường").ToMessageForUser();

            msg = UserRoleGroup.GetOne(UserID, out UserRoleGroup userRoleGroup);
            if (msg.Length > 0) return msg;
            if (userRoleGroup == null || userRoleGroup.RoleGroupID != USER_ROLE_IS_ADMIN) return "Bạn không có quyền thay đổi mật khẩu".ToMessageForUser();

            return "";
        }

        [HttpPost]
        public Result UpdateUserInfo([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLND, Role.ROLE_QLND_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoUpdateUserInfo(UserToken.UserID, data, out AccountUser accountUser);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return accountUser.ToResultOk();
        }
        private string DoUpdateUserInfo(int UserID, [FromBody] JObject data, out AccountUser accountUser)
        {
            accountUser = null;
            string msg = data.ToObject("AccountUser", out AccountUser AccountUser);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (string.IsNullOrEmpty(AccountUser.FullName)) return ("Bạn chưa nhập họ và tên").ToMessageForUser();

            msg = AccountUser.UpdateUserInfo(UserToken.UserID, AccountUser.FullName, AccountUser.Email, AccountUser.Mobile, out accountUser);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog("Sửa thông tin tài khoản", accountUser.ObjectGuid, UserID);

            return msg;
        }
        [HttpPost]
        public Result ImportExcel()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLND, Role.ROLE_QLND_CRUD);
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
                msg = DoImportExcel_GetData(out List<AccountUserImportExcel> lt);
                if (msg.Length > 0) return msg;

                msg = DoImportExcel_Validate(lt);
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = DoImportExcel_ConvertToObject(lt, out List<AccountUser> ltAccountUser, out List<AccountUserDept> ltAccountUserDept, out List<UserRoleGroup> ltUserRoleGroup);
                if (msg.Length > 0) return msg;

                DBM dbm = new DBM();
                dbm.BeginTransac();

                try
                {
                    msg = DoImportExcel_ObjectToDB(dbm, ltAccountUser, ltAccountUserDept, ltUserRoleGroup);
                    if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
                }
                catch (Exception ex)
                {
                    dbm.RollBackTransac();
                    return ex.ToString() + " at UserManager DoImportExcel";
                }


                dbm.CommitTransac();

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        Dictionary<string, string> MappingColumnExcel = new Dictionary<string, string>() {
                {"STT","STT" },
                {"Tài khoản đăng nhập","UserName" },
                {"Họ và Tên","FullName" },
                {"Giới tính","SexName" },
                {"Email","Email" },
                {"Số điện thoại","Mobile" },
                {"Tên đầy đủ phòng ban","DeptFullName" },
                {"Tên chức vụ","PositionName" },
                {"Tên nhóm quyền","RoleGroupName" },
                {"Phụ trách","SuperiorName" },
            };
        private string DoImportExcel_GetData(out List<AccountUserImportExcel> lt)
        {
            string msg = ""; lt = null;

            var httpContext = HttpContext.Current;
            if (httpContext.Request.Files.Count == 0) return "Bạn chưa chọn File".ToMessageForUser();

            msg = BSS.Common.GetSetting("FolderFileUpload", out string FolderFileUpload);
            if (msg.Length > 0) return msg;

            string pathFileUpload = FolderFileUpload + "/" + Guid.NewGuid();
            if (!Directory.Exists(pathFileUpload)) Directory.CreateDirectory(pathFileUpload);

            HttpPostedFile httpPostedFile = httpContext.Request.Files[0];
            string pathFile = pathFileUpload + "/" + httpPostedFile.FileName;
            httpPostedFile.SaveAs(pathFile);

            msg = BSS.Common.GetDataTableFromExcelFile(pathFile, out DataTable dt);
            if (msg.Length > 0) return msg;

            DataTable dt2 = new DataTable();
            foreach (var columnName in dt.Rows[0].ItemArray) dt2.Columns.Add(columnName.ToString());

            foreach (DataColumn column in dt2.Columns)
                foreach (var item in MappingColumnExcel)
                    if (item.Key == column.ColumnName) dt2.Columns[column.ColumnName].ColumnName = item.Value;

            string columnNames = "UserName,FullName,SexName,Email,Mobile,DeptFullName,PositionName,RoleGroupName,SuperiorName";
            foreach (var columnName in columnNames.Split(',')) if (!dt2.Columns.Contains(columnName)) return ("File excel thiếu cột " + columnName).ToMessageForUser();

            for (int i = 1; i < dt.Rows.Count; i++) if (!DoCheckDataRowEmpty(dt.Rows[i])) dt2.Rows.Add(dt.Rows[i].ItemArray);

            msg = BSS.Convertor.DataTableToList<AccountUserImportExcel>(dt2, out lt);
            if (msg.Length > 0) return msg;

            return msg;
        }

        private bool DoCheckDataRowEmpty(DataRow dr)
        {
            return dr == null || dr.ItemArray.All(i => i is DBNull);
        }

        private string DoImportExcel_Validate(List<AccountUserImportExcel> lt)
        {
            string msg = "";

            msg = AccountUser.GetAll(UserToken.AccountID, out List<AccountUser> ltAccountUser);
            if (msg.Length > 0) return msg;
            msg = AccountDept.GetList(UserToken.AccountID, out List<AccountDept> ltAccountDept);
            if (msg.Length > 0) return msg;
            msg = AccountPosition.GetList(UserToken.AccountID, out List<AccountPosition> ltAccountPosition);
            if (msg.Length > 0) return msg;
            msg = RoleGroup.GetAll(UserToken.AccountID, out List<RoleGroup> ltRoleGroup);
            if (msg.Length > 0) return msg;

            foreach (var item in lt)
            {
                List<string> ltError = new List<string>();
                //string msgErr = BSS.DataValidator.DataValidator.Validate(new { item.UserName, item.SexName, item.Email, MobileUser = item.Mobile, item.DeptFullName, item.PositionName, item.RoleGroupName }).ToErrorMessage();
                //if (msgErr.Length > 0) ltError.Add(msgErr);

                string UserName = item.UserName == null ? "" : item.UserName.Trim();
                var vUserName = ltAccountUser.Where(v => v.UserName == UserName);

                if (vUserName.Count() != 0) ltError.Add("Người dùng đã tồn tại");

                string SexName = item.SexName == null ? "" : item.SexName.Trim();
                if (SexName != "")
                {
                    int? Sex = AccountUser.GetSex(SexName);
                    if (!Sex.HasValue) ltError.Add("Giới tính phải có giá trị Nam hoặc Nữ");
                    else item.Sex = Sex;
                }

                item.IsActive = true;

                string DeptFullName = item.DeptFullName == null ? "" : item.DeptFullName.Trim().Replace("/", "\\");
                if (DeptFullName.Length == 0) ltError.Add("Tên phòng ban đầy đủ không được để trống");
                else
                {
                    var vDeptFullName = ltAccountDept.Where(v => v.DeptFullName != null && v.DeptFullName.ToLower() == DeptFullName.ToLower());
                    if (vDeptFullName.Count() == 0)
                        ltError.Add("Không tồn tại Tên phòng ban đầy đủ = " + DeptFullName);
                    else item.DeptID = vDeptFullName.First().DeptID;
                }

                string PositionName = item.PositionName == null ? "" : item.PositionName.Trim();
                if (PositionName.Length == 0) ltError.Add("Tên chức vụ không được để trống");
                else
                {
                    var vPositionName = ltAccountPosition.Where(v => v.PositionName != null && v.PositionName.ToLower() == PositionName.ToLower());
                    if (vPositionName.Count() == 0) ltError.Add("Không tồn tại Tên chức vụ = " + PositionName);
                    else item.PositionID = vPositionName.First().PositionID;
                }

                string RoleGroupName = item.RoleGroupName == null ? "" : item.RoleGroupName.Trim();
                if (RoleGroupName.Length == 0) ltError.Add("Tên nhóm quyền không được để trống");
                else
                {
                    var vRoleGroupName = ltRoleGroup.Where(v => v.RoleGroupName != null && v.RoleGroupName.ToLower() == RoleGroupName.ToLower());
                    if (vRoleGroupName.Count() == 0) ltError.Add("Không tồn tại Tên nhóm quyền = " + RoleGroupName);
                    else item.RoleGroupID = vRoleGroupName.First().RoleGroupID;
                }

                string SuperiorName = item.SuperiorName == null ? "" : item.SuperiorName.Trim();
                if (!string.IsNullOrEmpty(SuperiorName))
                {
                    var vSuperiorName = ltAccountUser.Where(v => v.FullName == SuperiorName);
                    if (vSuperiorName.Count() == 0) ltError.Add("Không tồn tại Tên phụ trách = " + SuperiorName);
                    else item.SuperiorID = vSuperiorName.First().UserID;
                }

                if (ltError.Count > 0) msg += " \n" + item.UserName + "\n " + string.Join("\n", ltError) + "\n";
            }
            if (msg.Length > 0) return "Dữ liệu file excel không hợp lệ như sau:\n" + msg;

            return "";
        }
        private string DoImportExcel_ConvertToObject(List<AccountUserImportExcel> lt, out List<AccountUser> ltAccountUser,
                                                    out List<AccountUserDept> ltAccountUserDept, out List<UserRoleGroup> ltUserRoleGroup)
        {
            string msg = "";
            ltAccountUser = new List<AccountUser>(); ltAccountUserDept = new List<AccountUserDept>(); ltUserRoleGroup = new List<UserRoleGroup>();
            foreach (var item in lt)
            {
                AccountUser AccountUser = new AccountUser();
                msg = BSS.Common.CopyObjectPropertyData(item, AccountUser);
                if (msg.Length > 0) return msg;

                AccountUser.UrlAvatar = AccountUser.GetUrlAvatar(AccountUser.Sex);
                ltAccountUser.Add(AccountUser);

                AccountUserDept AccountUserDept = new AccountUserDept();
                msg = BSS.Common.CopyObjectPropertyData(item, AccountUserDept);
                if (msg.Length > 0) return msg;
                ltAccountUserDept.Add(AccountUserDept);

                UserRoleGroup UserRoleGroup = new UserRoleGroup();
                msg = BSS.Common.CopyObjectPropertyData(item, UserRoleGroup);
                if (msg.Length > 0) return msg;
                ltUserRoleGroup.Add(UserRoleGroup);
            }

            return msg;
        }
        private string DoImportExcel_ObjectToDB(DBM dbm, List<AccountUser> ltAccountUser, List<AccountUserDept> ltAccountUserDept, List<UserRoleGroup> ltUserRoleGroup)
        {
            string msg = "";
            string defaultPass = "12345678";
            foreach (var item in ltAccountUser)
            {
                item.PasswordSalt = Common.GenerateRandomBytes(16);
                item.PasswordHash = Common.GetInputPasswordHash(defaultPass, item.PasswordSalt);

                msg = item.InsertUpdate(dbm, out AccountUser AccountUserNew);
                if (msg.Length > 0) return msg;
                item.UserID = AccountUserNew.UserID;

                Log.WriteHistoryLog((item.UserID == 0 ? "Thêm người dùng" : "Sửa người dùng") + " (Nhập bằng file excel)", AccountUserNew.ObjectGuid, UserToken.UserID);

                msg = AccountUserDept.InsertUpdateIsActive(dbm, AccountUserNew.UserID, false);
                if (msg.Length > 0) return msg;
            }

            foreach (var item in ltAccountUserDept)
            {
                var vAccountUser = ltAccountUser.Where(v => v.UserName == item.UserName);
                if (vAccountUser.Count() == 0) return "vAccountUser.Count() == 0";
                item.UserID = vAccountUser.First().UserID;

                item.IsActive = true;

                msg = item.InsertUpdate(dbm, out AccountUserDept aud);
                if (msg.Length > 0) return msg;
            }

            foreach (var item in ltUserRoleGroup)
            {
                var vAccountUser = ltAccountUser.Where(v => v.UserName == item.UserName);
                if (vAccountUser.Count() == 0) return "vAccountUser.Count() == 0";
                item.UserID = vAccountUser.First().UserID;
                item.AccountID = UserToken.AccountID;

                msg = item.InsertUpdate(dbm, out UserRoleGroup urg);
                if (msg.Length > 0) return msg;
            }
            return msg;
        }

        [HttpGet]
        public Result GetUserObjectGuid(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLND, Role.ROLE_QLND_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DataValidator.Validate(new { ObjectGuid }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            AccountUser o;
            msg = AccountUser.GetOneByObjectGUID(ObjectGuid, UserToken.AccountID, out o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            if (o == null) return ("Không có tài khoản tương ứng với ObjectGuid = " + ObjectGuid).ToResultError();

            List<AccountUserDept> userDepts;
            msg = AccountUserDept.GetUserDeptByUserId(o.UserID, out userDepts);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            o.lstAccountUserDept = userDepts;

            return o.ToResultOk();
        }
    }
}