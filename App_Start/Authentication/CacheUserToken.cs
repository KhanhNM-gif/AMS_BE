using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BSS;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using System.Web.Http;
using Newtonsoft.Json.Linq;

/// <summary>
/// Summary description for Cache
/// </summary>
public static class CacheUserToken
{
    private static List<UserToken> LtUser_Token = null;
    const int HOUR_TIMEOUT_TOKEN = 4;
    const int HOUR_TIMEOUT_TOKEN_REMEMBER = 24 * 7;

    private static string GetAllToken()
    {
        string msg = UserToken.GetAllExpiredDate(out LtUser_Token);
        if (msg.Length > 0) return msg;

        foreach (var item in LtUser_Token) item.TimeUpdateExpiredDateToDB = DateTime.Now;

        return msg;
    }
    public static string CreateToken(AccountUser accountUser, bool IsRememberPassword, out UserToken UserToken)
    {
        UserToken = null;
        string msg = "";

        if (LtUser_Token == null)
        {
            msg = GetAllToken();
            if (msg.Length > 0) return msg;
        }

        UserToken = new UserToken
        {
            UserID = accountUser.UserID,
            UserName = accountUser.UserName,
            AccountID = accountUser.AccountID,
            IsRememberPassword = IsRememberPassword,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpiredDate = DateTime.Now.AddHours(IsRememberPassword ? HOUR_TIMEOUT_TOKEN_REMEMBER : HOUR_TIMEOUT_TOKEN),
            CreateDate = DateTime.Now,
            DeptName = accountUser.UserDeptName,
            PositionName = accountUser.PositionName
        };

        msg = UserToken.Insert();
        if (msg.Length > 0) return msg;

        LtUser_Token.Add(UserToken);

        return msg;
    }
    public static string CreateToken(AccountUser accountUser, long exp, string sessionState, string jwt, out UserToken UserToken)
    {
        UserToken = null;
        string msg = "";

        if (LtUser_Token == null)
        {
            msg = GetAllToken();
            if (msg.Length > 0) return msg;
        }
        UserToken = new UserToken
        {
            UserID = accountUser.UserID,
            UserName = accountUser.UserName,
            AccountID = accountUser.AccountID,
            IsRememberPassword = false,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            JsonWebToken = jwt,
            SessionState = sessionState,
            ExpiredDate = CustomDateTimeConverter.LongToDateTime(exp),
            CreateDate = DateTime.Now
        };

        msg = UserToken.Insert();
        if (msg.Length > 0) return msg;

        LtUser_Token.Add(UserToken);

        return msg;
    }
    //public static string GetToken()
    //{
    //    char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

    //    byte[] data = new byte[15];
    //    using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
    //    {
    //        crypto.GetNonZeroBytes(data);
    //    }

    //    StringBuilder result = new StringBuilder(15);
    //    foreach (byte b in data) result.Append(chars[b % (chars.Length)]);

    //    return result.ToString();
    //}
    //public static Result GetResultUserToken(HttpRequestMessage request)
    //{
    //    UserToken UserToken;
    //    return GetResultUserToken(request, out  UserToken);
    //}
    //public static Result GetResultUserToken(HttpRequestMessage request, out UserToken UserToken)
    //{
    //    string msg = GetUserToken(request, out  UserToken);
    //    if (msg.Length > 0) return Log.ProcessError(msg).ToResult(-1);
    //    return Result.ResultOk;
    //}
    //public static string GetUserToken(HttpRequestMessage request, out UserToken UserToken)
    //{
    //    UserToken = null;

    //    string token = "";
    //    if (request.Headers.Contains("Authorization"))
    //        token = request.Headers.GetValues("Authorization").First();
    //    else
    //        return ("Header không chứa key Authorization (có value là Token đăng nhập)").ToMessageForUser();

    //    return GetUserToken(token, out  UserToken);
    //}
    //public static Result GetResultUserToken()
    //{
    //    UserToken UserToken;
    //    return GetResultUserToken(out  UserToken);
    //}
    public static Result GetResultUserToken(out UserToken UserToken)
    {
        string msg = GetUserToken(out UserToken);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResult(-1);
        return Result.ResultOk;
    }
    public static string GetUserToken(out UserToken UserToken)
    {
        UserToken = null;

        HttpContext context = HttpContext.Current;
        if (context == null) return "HttpContext.Current == null".ToMessageForUser();

        HttpRequest request = context.Request;
        if (request == null) return "request == null".ToMessageForUser();

        string token = "";
        if (request.Headers["Authorization"] != null)
            token = request.Headers["Authorization"];
        else
            return ("Header không chứa key Authorization (có value là Token đăng nhập)").ToMessageForUser();

        return GetUserToken(token, out UserToken);
    }
    public static string GetUserToken(string Token, out UserToken UserToken)
    {
        string msg = "";

        UserToken = null;

        if (LtUser_Token == null)
        {
            msg = GetAllToken();
            if (msg.Length > 0) return msg;
        }

        if (LtUser_Token == null) return "LtUser_Token == null";

        var vDB_Token = LtUser_Token.Where(v => v != null && v.Token == Token).ToList();
        if (vDB_Token == null || vDB_Token.Count() == 0) return "Không tồn tại token".ToMessageForUser();

        UserToken = vDB_Token.First();
        if (UserToken.ExpiredDate < DateTime.Now) return "Token đã hết hạn".ToMessageForUser();

        UserToken.ExpiredDate = DateTime.Now.AddHours(UserToken.IsRememberPassword ? HOUR_TIMEOUT_TOKEN_REMEMBER : HOUR_TIMEOUT_TOKEN);

        if ((DateTime.Now - UserToken.TimeUpdateExpiredDateToDB).TotalMinutes > 3)
        {
            UserToken.TimeUpdateExpiredDateToDB = DateTime.Now;
            msg = UserToken.UpdateExpiredDate(UserToken.ID, UserToken.ExpiredDate);
            if (msg.Length > 0) return msg;
        }

        return "";
    }
    public static string Logout([FromBody]JObject data, out string urlLogout)
    {
        string msg = "";
        urlLogout = "";
        UserToken UserToken;
        msg = GetUserToken(out UserToken);
        if (msg.Length > 0) return msg;

        bool isRemove = LtUser_Token.Remove(UserToken);
        if (!isRemove) return "Xóa token lỗi";

        msg = UserToken.Delete(UserToken.ID);
        if (msg.Length > 0) return msg;

        bool UsingSSO = bool.Parse(ConfigurationManager.AppSettings["UsingSSO"]);
        if (!UsingSSO) return "";

        string redirectUri = data["redirect_uri"].ToString();
        if (string.IsNullOrWhiteSpace(redirectUri)) return "Chưa truyền param redirect_uri vào";

        string ApiLogout = ConfigurationManager.AppSettings["ApiLogout"];
        if (string.IsNullOrWhiteSpace(ApiLogout)) return "Chưa cấu hình ApiLogout";

        urlLogout = ApiLogout + "?id_token_hint=" + UserToken.JsonWebToken + "&state=" + UserToken.SessionState + "&post_logout_redirect_uri=" + redirectUri;

        return msg;
    }
    public static string DeleteUserTokenByUserID(int UserID)
    {
        if (LtUser_Token == null) return "";

        int iCountRemove = LtUser_Token.RemoveAll(v => v.UserID == UserID);
        Log.WriteActivityLog("So luong token duoc xoa :" + iCountRemove);

        return "";
    }
}