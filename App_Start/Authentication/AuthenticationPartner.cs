using BSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

public class AuthenticationPartner : ApiController
{
    public Result ResultCheckPartner;
    public Partner Partner;

    public AuthenticationPartner()
    {
        string msg = CheckPartner(out Partner);
        if (msg.Length > 0) ResultCheckPartner = Log.ProcessError(msg).ToResultError();
        else ResultCheckPartner = Result.ResultOk;
    }
    public static string CheckPartner(out Partner Partner)
    {
        Partner = null;

        HttpContext context = HttpContext.Current;
        if (context == null) return "HttpContext.Current == null".ToMessageForUser();

        HttpRequest request = context.Request;
        if (request == null) return "request == null".ToMessageForUser();

        string sPartnerGUID = "";
        if (request.Headers["PartnerGUID"] != null) sPartnerGUID = request.Headers["PartnerGUID"];
        else return ("Header không chứa key PartnerGUID").ToMessageForUser();

        string PartnerToken = "";
        if (request.Headers["PartnerToken"] != null) PartnerToken = request.Headers["PartnerToken"];
        else return ("Header không chứa key PartnerToken").ToMessageForUser();

        Guid PartnerGUID;
        if(!Guid.TryParse(sPartnerGUID, out PartnerGUID)) return "PartnerGUID không phải là GUID".ToMessageForUser();
        
        string msg = Partner.GetOne(PartnerGUID, out Partner);
        if (msg.Length > 0) return msg;

        if (Partner == null) return ("PartnerGUID = " + PartnerGUID + " không tồn tại").ToMessageForUser();
        if (Partner.PartnerToken != PartnerToken) return ("PartnerToken = " + PartnerToken + " không khớp").ToMessageForUser();
        if (Partner.PartnerIP == "" || Partner.PartnerIP == null || Partner.PartnerIP == "*") return "";
        else
        {
            string[] arrPartnerIP = Partner.PartnerIP.Split(',');
            string ipAddress = Common.GetIPAddress(request);
            if (arrPartnerIP.Contains(ipAddress)) return "";
            else return ("Địa chỉ IP = " + ipAddress + " không hợp lệ").ToMessageForUser();
        }
    }
}