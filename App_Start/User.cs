using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BSS;
using System.Data;

namespace WebAPI
{
    public class User
    {
        //public int UserID { get; set; }
        //public Guid ObjectGuid { get; set; }
        //public string UrlAvatar { get; set; }
        //public string UserName { get; set; }
        //public string UserDeptName { get; set; }
        //public string FullName { get; set; }
        //public DateTime BirthDate { get; set; }
        //public int Sex { get; set; }
        //public string Email { get; set; }
        //public string Mobile { get; set; }

        //public static string GetAll(out DataTable dt)
        //{
        //    return DBM.ExecStore("usp_User_SelectAll", new { }, out dt);
        //}

        public static string GetAllUserInDept(int UserID, out DataTable dt)
        {
            return DBM.ExecStore("usp_User_SelectAllInDept", new { UserID }, out dt);
        }

        public static string GetAllUserDelegacyInDept(int UserID, out DataTable dt)
        {
            return DBM.ExecStore("usp_User_SelectUserDelegacyInDept", new { UserID }, out dt);
        }
        public static string GetForEgovCreateMission(int UserID, out DataTable dt)
        {
            return DBM.ExecStore("usp_User_SelectForEgovCreateMission", new { UserID }, out dt);
        }

        //public static string GetByUserName(string UserName, out User u)
        //{
        //    return DBM.GetOne("usp_User_SelectByUserName", new { UserName }, out u);
        //}

        //public static string GetOneByUserID(int UserID, out User u)
        //{
        //    return DBM.GetOne("usp_User_SelectByUserID", new { UserID }, out u);
        //}

        public static string GetByDeptID(int DeptID, out List<AccountUser> lt)
        {
            return DBM.GetList("usp_User_SelectByDeptID", new { DeptID }, out lt);
        }

        public static string GetOneByGuid(Guid ObjectGuid, out long UserID)
        {
            UserID = 0;

            AccountUser u;
            string msg = DBM.GetOne("usp_User_SelectByObjectGuid", new { ObjectGuid }, out u);
            if (msg.Length > 0) return msg;

            if (u == null) return ("Không tồn tại User có ObjectGuid = " + ObjectGuid).ToMessageForUser();
            UserID = u.UserID;

            return msg;
        }
    }
}