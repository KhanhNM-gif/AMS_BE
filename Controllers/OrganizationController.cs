using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class OrganizationController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.TC, Role.ROLE_TC_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            Organization mNew;
            msg = DoInsertUpdate(data, out mNew);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return mNew.ToResultOk();
        }
        private string DoInsertUpdate([FromBody] JObject data, out Organization pNew)
        {
            pNew = null;

            string msg = data.ToObject("Organization", out Organization organization);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_Validate(organization);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = organization.InsertUpdate(new DBM(), out pNew);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog(organization.OrganizationID == 0 ? "Thêm mới đối tác" : $"Sửa Tổ chức: {organization.GetInfoChangeRequest()}", pNew.ObjectGuid, UserToken.UserID);

            return msg;
        }
        private string DoInsertUpdate_Validate(Organization organization)
        {
            string msg = "";

            organization.OrganizationName = organization.OrganizationName.Trim();
            if (organization.OrganizationName.Length == 0) return "Tên đối tác không được để trống";

            organization.OrganizationCode = organization.OrganizationCode.Trim();
            if (organization.OrganizationCode.Length == 0) return "Mã đối tác không được để trống";

            msg = DataValidator.Validate(new
            {
                organization.OrganizationID,
                organization.OrganizationTypeID,
                organization.OrganizationCode,
                organization.OrganizationName,
                organization.OrganizationAddressDetail,
                organization.OrganizationNote
            }, "OrganizationID|ID Tổ chức", "OrganizationTypeID|ID Loại Tổ chức", "OrganizationCode|Mã tổ chức",
            "OrganizationName|Tên tổ chức", "OrganizationAddressDetail|Địa chỉ", "OrganizationNote|Ghi chú").ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (!string.IsNullOrEmpty(organization.OrganizationMobile) && organization.OrganizationMobile.Replace(" ", "").Replace("-", "").Replace(".", "").Split(',') is var PhoneNumbers && PhoneNumbers.Any())
            {
                //string regex = @"((\(\+?([0-9]{2,3}\))([ .-]?))0?|0)([2|3|5|7|8|9])([0-9]{1})([ .-]?)([0-9]{3})([ .-]?)([0-9]{3})([ .-]?)([0-9]{1,3})";


                string regex = @"((\(\+?(\d{2,3}\)))0?|0)((2\d{1,2})|([(3|5|7|8|9]))\d{8}";
                foreach (var item in PhoneNumbers)
                    if (!Regex.IsMatch(item, regex)) return $"PhoneNumber not validate :{item}";
            }
            organization.AccountID = UserToken.AccountID;
            msg = Organization.GetListByName(organization.OrganizationName, organization.OrganizationCode, organization.OrganizationTypeID, UserToken.AccountID, out List<Organization> pa);
            if (msg.Length > 0) return msg;
            if (pa.Where(x => organization.OrganizationID != x.OrganizationID).Any())
                return ("Dữ liệu đã tồn tại trong hệ thống").ToMessageForUser();

            if (organization.OrganizationID != 0)
            {
                msg = Organization.GetOne(organization.OrganizationID, UserToken.AccountID, out Organization outOrganization);
                if (msg.Length > 0) return msg;

                msg = organization.SetInfoChangeRequest(outOrganization);
                if (msg.Length > 0) return msg;

            }
            /*if (pa.Count > 0 && (pa.Where(v => v.OrganizationID != organization.OrganizationID && v.OrganizationCode == organization.OrganizationCode && v.IsActive).Count() > 0))*/

            return msg;
        }
        private string ValidatePhonenumber(string value)
        {
            string[] vphone = value.Split(',');
            for (int i = 0; i < vphone.Length; i++)
            {
                if (vphone[i].Length == 10)
                {
                    Regex regex = new Regex(@"(\+84|84|0)+(9|3|7|8|5)+([0-9]{8})\b");
                    Match match = regex.Match(vphone[i]);

                    if (!match.Success)
                    {
                        return ("Số điện thoại không đúng định dạng").ToMessageForUser();
                    }
                }
                else return ("Số điện thoại không đúng định dạng").ToMessageForUser();
            }

            return "";
        }

        [HttpGet]
        public Result GetListBySearch(string TextSearch, int OrganizationTypeID, int IsActive, int PageSize, int CurrentPage)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            DataTable dt;
            string msg = Organization.GetListBySearch(TextSearch, OrganizationTypeID, IsActive, UserToken.AccountID, out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = Organization.GetByActive(UserToken.AccountID, out List<Organization> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            DataTable dtPaging;
            msg = UtilitiesDatatable.GetDtPaging(dt, PageSize, CurrentPage, out dtPaging);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = dtPaging, Total = dt.Rows.Count, TotalActive = lt.Count }.ToResultOk();
        }

        [HttpGet]
        public Result GetListOrganizationType()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<OrganizationType> lt;
            string msg = OrganizationType.GetList(out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        /// <summary>
        ///  lấy tất cả các tổ chức thuộc hãng sản xuất   
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Result GetListProducer()
        {
            //lấy tất cả các tổ chức thuộc hãng sản xuất
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<Organization> lt;
            string msg = Organization.GetListByProducer(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }
        /// <summary>
        /// lấy tất cả các tổ chức thuộc nhà cung cấp
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Result GetListSupplier()
        {
            //lấy tất cả các tổ chức thuộc nhà cung cấp
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<Organization> lt;
            string msg = Organization.GetListBySupplier(UserToken.AccountID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.TC, Role.ROLE_TC_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDelete(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoDelete(int UserID, [FromBody] JObject data)
        {
            string msg = data.ToNumber("OrganizationID", out int OrganizationID);
            if (msg.Length > 0) return msg;

            msg = Organization.GetOne(OrganizationID, UserToken.AccountID, out Organization outOrganization);
            if (msg.Length > 0) return msg;
            if (outOrganization == null) return ("Không tồn tại tổ chức ID = " + OrganizationID).ToMessageForUser();

            msg = Delete_Validate(OrganizationID);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = Organization.Delete(outOrganization.OrganizationID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog("Xóa tổ chức", outOrganization.ObjectGuid, UserID);

            return msg;
        }
        private string Delete_Validate(int OrganizationID)
        {
            string msg = Asset.SelectByOrganizationID(OrganizationID, UserToken.AccountID, out List<Asset> assets);
            if (msg.Length > 0) return msg;
            if (assets.Count > 0) return "Bạn không thể xóa tổ chức này, vì đã gắn với thông tin tài sản";

            return msg;
        }

        [HttpGet]
        public Result GetOne(int OrganizationID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.TC, Role.ROLE_TC_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DataValidator.Validate(new { OrganizationID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = Organization.GetOne(OrganizationID, UserToken.AccountID, out Organization o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }
    }
}