using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class AssetSyncController : ApiController
    {
        [HttpGet]
        public Result GetList(DateTime LastUpdate,Guid AccountGuid)
        {
            string msg = CheckAuthorization();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetList(LastUpdate, AccountGuid, out List<AssetSync> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        private string DoGetList(DateTime LastUpdate,Guid ObjectGuid, out List<AssetSync> lt)
        {
            string msg = Asset.GetListAssetSync(LastUpdate, ObjectGuid, out lt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string CheckAuthorization()
        {
            HttpContext context = HttpContext.Current;
            HttpRequest request = context.Request;
            string msg = "";
            if (request.Headers["Authorization"] != null)
            {
                string Authorization = request.Headers["Authorization"];
                msg = BSS.Common.GetSetting("KeyAuthorizationAssetSync", out string KeyAuthorization);
                if (msg.Length > 0) return msg;

                if (Authorization != KeyAuthorization) return "Authorization không hợp lệ".ToMessageForUser();
            }
            else return ("Header không chứa key Authorization (có value là Token đăng nhập)").ToMessageForUser();

            //msg = BSS.Common.GetSetting("IPStatistic", out string IPStatistic);
            //if (msg.Length > 0) return msg;

            //if (!IPStatistic.Split(',').Contains(Common.GetIPAddress())) return ("IP " + Common.GetIPAddress() + " không có quyền truy cập").ToMessageForUser();

            return "";
        }
    }
}
public class AssetSync
{
    public Guid ObjectGuid { get; set; }
    public string AssetCode { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }
    public string AssetColor { get; set; }
    public string AssetSerial { get; set; }
    public string AssetModel { get; set; }
    public int ProducerID { get; set; }
    public string ProducerName { get; set; }
    public int SupplierID { get; set; }
    public string SupplierName { get; set; }
    public DateTime AssetDateIn { get; set; }
    public DateTime AssetDateBuy { get; set; }
    public int PlaceID { get; set; }
    public string PlaceName { get; set; }
    public int AssetStatusID { get; set; }
    public string AssetStatusName { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
}
