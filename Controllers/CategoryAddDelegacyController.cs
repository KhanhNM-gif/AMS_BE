using BSS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class CategoryAddDelegacyController : Authentication
    {
        [HttpGet]
        public Result GetListUserDelegacy()
        {

            if (!ResultCheckToken.isOk) return ResultCheckToken;

            DataTable dt;
            string msg = GetListUser(out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
        [HttpGet]
        public Result GetListUserDelegacyed()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            DataTable dt;
            string msg = GetListUser(out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
        private string GetListUser(out DataTable dt)
        {
            dt = null;

            bool IsUQXLAllUser;
            string msg = Role.CheckUQXLAllUser(UserToken.UserID, out IsUQXLAllUser);
            if (msg.Length > 0) return msg;

            if (IsUQXLAllUser)
            {
                msg = AccountUser.GetAll(UserToken.AccountID,out dt);
                if (msg.Length > 0) return msg;
            }
            else
            {
                msg = WebAPI.User.GetAllUserDelegacyInDept(UserToken.UserID, out dt);
                if (msg.Length > 0) return msg;
            }

            return msg;
        }
    }
}