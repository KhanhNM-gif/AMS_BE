using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BSS;
using Newtonsoft.Json.Linq;
using BSS.DataValidator;
using System.Web;
using System.Data;

namespace WebAPI.Controllers
{
    public class AccountController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody]JObject data)
        {
            string msg = CheckAuthorization();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            Account nAccount;
            msg = DoInsertUpdate(data, out nAccount);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return nAccount.ToResultOk();
        }
        private string DoInsertUpdate([FromBody]JObject data, out Account nAccount)
        {
            nAccount = null;

            string msg = data.ToObject("Account", out Account account);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_Validate(account);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            msg = DoInsertUpdate_ObjectToDB(dbm, account, out nAccount);
            if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }

            dbm.CommitTransac();

            return msg;
        }
        private string DoInsertUpdate_Validate(Account account)
        {
            string msg = "";

            if (string.IsNullOrEmpty(account.Name)) return ("Tên Tài khoản không được để trống").ToMessageForUser();
            if (string.IsNullOrEmpty(account.Code)) return ("Mã Tài khoản không được để trống").ToMessageForUser();

            msg = DataValidator.Validate(new
            {
                account.AccountID,
                account.TaxCode
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = Account.GetOneByTaxCode(account.TaxCode, out Account accountout);
            if (msg.Length > 0) return msg;
            if (accountout != null && account.AccountID != accountout.AccountID)
                return ("Mã số thuế đã tồn tại trong hệ thống").ToMessageForUser();

            return msg;
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, Account accountInput, out Account accountOut)
        {
            string msg = accountInput.InsertUpdate(dbm, out accountOut);
            if (msg.Length > 0) return msg;

            if (accountInput.AccountID == 0)
            {
                byte[] PasswordSalt = Common.GenerateRandomBytes(16);
                byte[] PasswordHash = Common.GetInputPasswordHash(accountInput.Password, PasswordSalt);

                AccountUser account = new AccountUser { UserID = 0, UserName = accountOut.TaxCode, PasswordHash = PasswordHash, PasswordSalt = PasswordSalt, FullName = accountOut.Name, Email = accountOut.Email, Mobile = accountOut.Phone, IsActive = true };
                msg = account.InsertUpdate(dbm, out AccountUser accountUser);
                if (msg.Length > 0) return msg;

                //tạo quyền Admin cho tài khoản vừa tạo
                msg = RoleGroup.InserRoleAdmin(dbm, accountOut.AccountID, accountUser.UserID, out RoleGroup roleGroup);
                if (msg.Length > 0) return msg;

                //gắn quyền với người dùng vừa thêm
                UserRoleGroup userRoleGroup = new UserRoleGroup { UserID = accountUser.UserID, RoleGroupID = roleGroup.RoleGroupID, UserIDCreate = accountUser.UserID, AccountID = accountOut.AccountID };
                msg = userRoleGroup.InsertUpdate(dbm, out UserRoleGroup uNew);
                if (msg.Length > 0) return msg;
            }
            return msg;
        }
        [HttpGet]
        public Result GetUserInfor()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Account.GetOneByAccountID(UserToken.AccountID, out Account o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (o != null)
            {
                o.ltAccountUser = new List<AccountUser>();
                msg = AccountUser.GetUserInfor(UserToken.UserID, out AccountUserInfor accountUser);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                o.AccountUser = accountUser;
            }

            return o.ToResultOk();
        }
        [HttpPost]
        public Result UpdateAccount([FromBody]JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoUpdateAccount(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoUpdateAccount(int UserID, [FromBody]JObject data)
        {
            string msg = data.ToObject("Account", out Account account);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (account.Name.Length == 0) return ("Tên tài khoản không được để trống").ToMessageForUser();

            msg = Account.UpdateAccount(UserToken.AccountID, account.Name, account.Email, account.Phone, account.Address);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog("Cập nhật Tài khoản", account.ObjectGuid, UserID);

            return msg;
        }
        private string CheckAuthorization()
        {
            HttpContext context = HttpContext.Current;
            HttpRequest request = context.Request;

            if (request.Headers["Authorization"] != null)
            {
                string Authorization = request.Headers["Authorization"];
                if (Authorization != "8997eae17d04338f388e35ec34a3ebd9") return "Authorization không hợp lệ".ToMessageForUser();
            }
            else return ("Header không chứa key Authorization (có value là Token đăng nhập)").ToMessageForUser();

            string msg = BSS.Common.GetSetting("IPStatistic", out string IPStatistic);
            if (msg.Length > 0) return msg;

            if (!IPStatistic.Split(',').Contains(Common.GetIPAddress())) return ("IP " + Common.GetIPAddress() + " không có quyền truy cập").ToMessageForUser();

            return "";
        }
    }
}