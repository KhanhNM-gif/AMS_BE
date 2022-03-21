using BSS;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class UserController : ApiController
    {
        [HttpPost]
        public Result LoginJWT([FromBody]JObject data)
        {
            string msg = DoLoginJWT(data, out object o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResult(-2);
            return o.ToResultOk();
        }
        private string DoLoginJWT([FromBody]JObject data, out object o)
        {
            o = null; string msg = "";

            string idToken = BSS.Convertor.ToString(data["id_token"], "");
            if (string.IsNullOrWhiteSpace(idToken)) return "Chưa truyền id_token vào";

            string sessionState = BSS.Convertor.ToString(data["session_state"], "");
            if (string.IsNullOrWhiteSpace(sessionState)) return "Chưa truyền param session_state vào";

            msg = DoLoginJWT_VerifySignature(idToken, out string UserName, out long exp);
            if (msg.Length > 0) return "Hệ thống không tự đăng nhập được! Mời bạn chọn đăng nhập bằng cách khác.".ToMessageForUser();

            msg = DoLoginSSO_ExecLogin(UserName, exp, sessionState, idToken, out o);
            if (msg.Length > 0) return msg;

            return msg;
        }

        private string DoLoginJWT_VerifySignature(string accessToken, out string UserName, out long exp)
        {
            string msg = ""; UserName = ""; exp = 0;
            string[] tokenParts = accessToken.Split('.');
            try
            {
                string JWTModulus = BSS.Common.GetSettingWithDefault("JWTModulus", "");
                if (string.IsNullOrWhiteSpace(JWTModulus)) return "Chưa cấu hình JWTModulus";

                string JWTExponent = BSS.Common.GetSettingWithDefault("JWTExponent", "");
                if (string.IsNullOrWhiteSpace(JWTExponent)) return "Chưa cấu hình JWTExponent";

                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                rsa.ImportParameters(
                  new RSAParameters()
                  {
                      Modulus = UtilitiesString.FromBase64Url(JWTModulus),
                      Exponent = UtilitiesString.FromBase64Url(JWTExponent)
                  });
                SHA256 sha256 = SHA256.Create();
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(tokenParts[0] + '.' + tokenParts[1]));

                RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm("SHA256");
                if (!rsaDeformatter.VerifySignature(hash, UtilitiesString.FromBase64Url(tokenParts[2]))) return "Không Verify được id_token";

                msg = DecodeJWT(accessToken, out UserName, out exp);
                if (msg.Length > 0) return msg;
            }
            catch
            {
                return "Không Verify được id_token";
            }
            return "";
        }

        [HttpPost]
        public Result GetUrlLoginSSO([FromBody]JObject data)
        {
            string msg = DoGetUrlLoginSSO(data, out string o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }
        private string DoGetUrlLoginSSO([FromBody]JObject data, out string o)
        {
            o = ""; string msg = "";

            bool UsingSSO = BSS.Common.GetSettingWithDefault("UsingSSO", "").ToBoolean(false);
            if (!UsingSSO) return "";

            string redirectUri = BSS.Convertor.ToString(data["redirect_uri"], "");
            if (string.IsNullOrWhiteSpace(redirectUri)) return "Chưa truyền param redirect_uri vào";

            string UrlLoginSSO = BSS.Common.GetSettingWithDefault("UrlLoginSSO", "");
            if (string.IsNullOrWhiteSpace(UrlLoginSSO)) return "Chưa cấu hình UrlLoginSSO";

            string ClientID = BSS.Common.GetSettingWithDefault("ClientID", "");
            if (string.IsNullOrWhiteSpace(ClientID)) return "Chưa cấu hình ClientID";

            o = UrlLoginSSO + "?response_type=code&scope=openid&client_id=" + ClientID + "&redirect_uri=" + redirectUri;

            return msg;
        }

        [HttpPost]
        public Result LoginSSO([FromBody]JObject data)
        {
            string msg = DoLoginSSO(data, out object o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }
        private string DoLoginSSO([FromBody]JObject data, out object o)
        {
            o = null;
            string msg = "";

            msg = GetTokenSSO(data, out string UserName, out long exp, out string sessionState, out string jwt);
            if (msg.Length > 0) return msg;

            msg = DoLoginSSO_ExecLogin(UserName, exp, sessionState, jwt, out o);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string DoLoginSSO_ExecLogin(string Username, long exp, string sessionState, string jwt, out object o)
        {
            string msg = "";
            o = null;

            msg = AccountUser.GetByUserName(Username, out List<AccountUser> lt);
            if (msg.Length > 0) return msg;
            if (lt.Count == 0) return ("Hệ thống AMS không tồn tại tên đăng nhập " + Username).ToMessageForUser();

            AccountUser user = lt.First();

            msg = CacheUserToken.CreateToken(user, exp, sessionState, jwt, out UserToken UserToken);
            if (msg.Length > 0) return msg;

            user.IsChangePassFirstLogin = false;

            o = new { User = user, UserToken = UserToken };

            return msg;
        }
        private string GetTokenSSO([FromBody]JObject data, out string UserName, out long exp, out string sessionState, out string jwt)
        {
            UserName = ""; exp = 0; jwt = "";

            sessionState = BSS.Convertor.ToString(data["session_state"], "");
            if (string.IsNullOrWhiteSpace(sessionState)) return "Chưa truyền param session_state vào";

            string code = BSS.Convertor.ToString(data["code"], "");
            if (string.IsNullOrWhiteSpace(code)) return "Chưa truyền param code vào";

            string redirectUri = BSS.Convertor.ToString(data["redirect_uri"], "");
            if (string.IsNullOrWhiteSpace(redirectUri)) return "Chưa truyền param redirect_uri vào";

            string ApiGetToken = BSS.Common.GetSettingWithDefault("ApiGetToken", "");
            if (string.IsNullOrWhiteSpace(ApiGetToken)) return "Chưa cấu hình ApiGetToken";

            string ApiClientID = BSS.Common.GetSettingWithDefault("ClientID", "");
            if (string.IsNullOrWhiteSpace(ApiGetToken)) return "Chưa cấu hình ClientID";

            string ApiSecretID = BSS.Common.GetSettingWithDefault("SecretID", "");
            if (string.IsNullOrWhiteSpace(ApiGetToken)) return "Chưa cấu hình SecretID";

            //string msg = GetTokenSSO_Request(ApiGetToken, "code=" + code + "&redirect_uri=" + redirectUri + "&grant_type=authorization_code&client_id="+ ApiClientID + "&client_secret="+ ApiSecretID, out JObject jsonResponse);
            string msg = GetTokenSSO_Request(ApiGetToken, "code=" + code + "&redirect_uri=" + redirectUri + "&grant_type=authorization_code", out JObject jsonResponse);
            if (msg.Length > 0) return msg;

            jwt = (string)jsonResponse.SelectToken("id_token");
            msg = DecodeJWT(jwt, out UserName, out exp);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string GetTokenSSO_Request(string url, string postParameters, out JObject jsonResponse)
        {
            string msg = "";
            jsonResponse = null;
            try
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                   ((sender, certificate, chain, sslPolicyErrors) => true);

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                WebRequest myWebRequest = WebRequest.Create(url);
                myWebRequest.Method = "POST";
                myWebRequest.ContentType = "application/x-www-form-urlencoded";
                myWebRequest.PreAuthenticate = true;

                string ClientID = BSS.Common.GetSettingWithDefault("ClientID", "");
                if (string.IsNullOrWhiteSpace(ClientID)) return "Chưa cấu hình ClientID";

                string SecretID = BSS.Common.GetSettingWithDefault("SecretID", "");
                if (string.IsNullOrWhiteSpace(SecretID)) return "Chưa cấu hình SecretID";

                myWebRequest.Headers.Add("Authorization", "Basic " + UtilitiesString.EncodeStringToBase64(ClientID + ":" + SecretID));

                Stream reqStream = myWebRequest.GetRequestStream();
                byte[] data = Encoding.ASCII.GetBytes(postParameters);
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();

                StreamReader myStreamReader = new StreamReader(myWebRequest.GetResponse().GetResponseStream());
                jsonResponse = JObject.Parse(myStreamReader.ReadToEnd());
            }
            catch (Exception ex)
            {
                msg = ("Lỗi khi lấy Token: " + ex.ToString()).ToMessageForUser();
            }

            return msg;
        }
        private string DecodeJWT(string idToken, out string UserName, out long exp)
        {
            UserName = ""; exp = 0;
            try
            {
                string[] token = idToken.Split('.');
                string dummyData = token[1].Trim().Replace(" ", "+");
                if (dummyData.Length % 4 > 0) dummyData = dummyData.PadRight(dummyData.Length + 4 - dummyData.Length % 4, '=');

                JObject jsonDecoded = JObject.Parse(UtilitiesString.DecodeStringFromBase64(dummyData));

                UserName = jsonDecoded.GetValue("sub").ToString();
                exp = jsonDecoded.GetValue("exp").ToNumber(-1);
                if (exp == -1) return "exp = jsonDecoded.GetValue(\"exp\").ToNumber(-1);";
            }
            catch (Exception e)
            {
                return e.ToString();
            }

            return "";
        }

        [HttpPost]
        public Result Login([FromBody]JObject data)
        {
            string msg = data.ToString("Username", out string Username);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = data.ToString("Password", out string Password);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = data.ToBool("IsRememberPassword", out bool IsRememberPassword);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = data.ToString("RecaptchaResponse", out string RecaptchaResponse);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            object o;
            msg = DoExecLogin(Username, Password, IsRememberPassword, RecaptchaResponse, out o);
            if (msg.Length > 0)
            {
                string RequireCaptchaLogin = ConfigurationManager.AppSettings["RequireCaptchaLogin"];
                //if (RequireCaptchaLogin == "true")
                //{
                //    object oCaptcha;
                //    string msgCaptcha = CheckCaptcha(RecaptchaResponse, out oCaptcha);
                //    if (msgCaptcha != null && msgCaptcha.Length > 0) return Log.ProcessError(msgCaptcha).ToResultError();
                //    if (oCaptcha != null) return oCaptcha.ToResult(-2);
                //}

                return Log.ProcessError(msg).ToResultError();
            }

            return o.ToResultOk();
        }

        const string RECAPTCHA_SITEKEY = "6LefEkAUAAAAAHr8xrCP2WPFpGzWtEcQo189W4DL", RECAPTCHA_SECRETKEY = "6LefEkAUAAAAALDI9Mq_dYgPRsArsSoJhY91CQtZ";
        private string CheckCaptcha(string RecaptchaResponse, out object o)
        {
            string msg = ""; o = null;

            BruteForceGuard.SetParameters(15, 3, false, RECAPTCHA_SECRETKEY);
            string ip = System.Web.HttpContext.Current.Request.UserHostAddress;
            if (BruteForceGuard.IsBlocked(ip, out msg))
            {
                if (string.IsNullOrWhiteSpace(RecaptchaResponse))
                {
                    o = new { RecaptchaSitekey = RECAPTCHA_SITEKEY, Message = "Bạn hãy tick vào ô captcha để tiếp tục" };
                    return "";
                }

                try
                {
                    var client = new WebClient();
                    var result = client.DownloadString(string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteip={2}", RECAPTCHA_SECRETKEY, RecaptchaResponse, ip));
                    var obj = JObject.Parse(result);
                    var ok = (bool)obj.SelectToken("success");
                    if (ok) BruteForceGuard.RemoveBlockedIP(ip);
                    else
                    {
                        o = new { RecaptchaSitekey = RECAPTCHA_SITEKEY, Message = "Bạn hãy tick vào ô captcha để tiếp tục" };
                        return "";
                    }
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }

            }
            return msg;
        }
        private string DoExecLogin(string Username, string Password, bool IsRememberPassword, string gRecaptchaResponse, out object o)
        {
            string msg = "";
            o = null;

            msg = DoExecLogin_Validate(Username, Password);
            if (msg.Length > 0) return msg;

            msg = AccountUser.GetByUserName(Username, out List<AccountUser> lt);
            if (msg.Length > 0) return msg;
            if (lt.Count == 0) return AddFailureAndGetMessage();
            AccountUser accountUser = lt.First();

            byte[] PasswordHash = Common.GetInputPasswordHash(Password, accountUser.PasswordSalt);
            if (!accountUser.PasswordHash.SequenceEqual(PasswordHash)) return AddFailureAndGetMessage();

            if (!accountUser.IsActive) return ("Tài khoản chưa hoạt động").ToMessageForUser();

            msg = CacheUserToken.CreateToken(accountUser, IsRememberPassword, out UserToken UserToken);
            if (msg.Length > 0) return msg;

            o = new { User = accountUser, UserToken = UserToken };

            return msg;
        }
        private string AddFailureAndGetMessage()
        {
            string ip = System.Web.HttpContext.Current.Request.UserHostAddress;
            BruteForceGuard.AddFailure(ip);
            return "Bạn nhập sai tên tài khoản hoặc mật khẩu".ToMessageForUser();
        }
        private string DoExecLogin_Validate(string Username, string Password)
        {
            if (Username == "") return "Tên tài khoản không được để trống".ToMessageForUser();
            if (Password == "") return "Mật khẩu không được để trống".ToMessageForUser();
            return "";
        }

        [HttpGet]
        public Result GetListTab()
        {
            try
            {
                UserToken ut;
                Result r = CacheUserToken.GetResultUserToken(out ut);
                if (!r.isOk) return r;

                RoleGroup rg; List<Tab> ListTab;
                string msg = DoGetListTab(ut.UserID, out rg, out ListTab);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { RoleGroup = new { RoleGroupID = (rg == null ? 0 : rg.RoleGroupID), RoleGroupName = (rg == null ? "" : rg.RoleGroupName) }, ListTab }.ToResultOk();
            }
            catch (Exception ex)
            {
                return ex.ToString().ToResultError();
            }
        }
        private string DoGetListTab(int UserID, out RoleGroup rg, out List<Tab> ListTab)
        {
            rg = null; ListTab = Tab.GetListTab();

            try
            {
                string msg = RoleGroup.GetByUserID(UserID, out rg);
                if (msg.Length > 0) return msg;

                int TabIDFocus = Constants.TabID.QLTS;
                //msg = SPV.GetPageMain(UserID, out TabIDFocus);
                //if (msg.Length > 0) return msg;

                foreach (var item in ListTab)
                {
                    int tabID = item.TabID;

                    bool IsRoleVisitPage;
                    if (tabID == Constants.TabID.QLTS) IsRoleVisitPage = true;
                    else Role.CheckVisitPage(rg, tabID, out IsRoleVisitPage);
                    item.IsVisitTab = IsRoleVisitPage;

                    if (tabID == TabIDFocus) item.IsFocusTab = true;
                }

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [HttpPost]
        public Result Logout([FromBody]JObject data)
        {
            string msg = CacheUserToken.Logout(data, out string urlLogout);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return urlLogout.ToResultOk();
        }
        [HttpPost]
        public Result LogoutSSO([FromBody]JObject data)
        {
            string msg = DoLogoutSSO(data, out string UserName, out long exp);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = AccountUser.GetOneByUserName(UserName, out AccountUser accountUser);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            Log.WriteActivityLog("3 Param :" + accountUser.UserName);

            msg = UserToken.DeleteByUserID(accountUser.UserID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            Log.WriteActivityLog("4 Param :" + accountUser.UserID);

            msg = CacheUserToken.DeleteUserTokenByUserID(accountUser.UserID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            Log.WriteActivityLog("5 Param :" + accountUser.UserID);

            return "".ToResultOk();
        }
        private string DoLogoutSSO([FromBody]JObject data, out string UserName, out long exp)
        {
            string msg = ""; UserName = ""; exp = 0;

            msg = data.ToString("logout_token", out string logout_token);
            if (msg.Length > 0) return msg;

            Log.WriteActivityLog("1 Param :" + logout_token);

            msg = DoLoginJWT_VerifySignature(logout_token, out UserName, out exp);
            if (msg.Length > 0) return ("Đăng nhập tự động bằng SSO không thành công. <br> " + "Có thể xảy ra 1 trong 2 nguyên nhân sau đây:<br>" + "- Giá trị tham số trên url: id_token = <b>" + logout_token + "</b> không hợp lệ.<br>" + "- Giá trị tham số cấu hình trong webconfig: <b>JWTModulus</b> không hợp lệ.").ToMessageForUser();

            Log.WriteActivityLog("2 Param :" + UserName);

            return msg;
        }
    }
}