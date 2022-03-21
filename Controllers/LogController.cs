using BSS;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class LogController : Authentication
    {
        [HttpGet]
        public Result GetListHistory(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = LogHistory.GetListHistory(ObjectGuid, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return dt.ToResultOk();
        }

        [HttpGet]
        public Result GetListHistoryUse(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListHistoryUse(ObjectGuid, out object o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }
        private string DoGetListHistoryUse(Guid ObjectGuid, out object o)
        {
            o = null;

            string msg = CacheObject.GetAssetIDbyGUID(ObjectGuid, out long AssetID);
            if (msg.Length > 0) return msg;
            if (AssetID == 0) return ("Không tồn tại tài sản với ObjectGuid=" + ObjectGuid).ToMessageForUser();

            msg = AssetUse.GetListHistoryUse(AssetID, out List<AssetUse> LtAssetUse);
            if (msg.Length > 0) return msg;

            List<AssetUse> LtAssetUse_Order = LtAssetUse.OrderBy(v => v.ExecutionDate).ToList();
            msg = AssetUse.GetSumTimeUse(LtAssetUse_Order, out double TotalTimeUse);
            if (msg.Length > 0) return msg;

            o = new
            {
                LtAssetUse = LtAssetUse_Order,
                CountTimesUse = LtAssetUse.Count(v => v.CategoryHistory == AssetUse.CategoryHistory_HandOver),
                TotalTimeUse = (int)Math.Round(TotalTimeUse)
            };
            return "";
        }

        [HttpPost]
        public Result GetListEasySearch([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListEasySearch(data, out List<LogSearch> ListLogSearch);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListLogSearch.ToResultOk();
        }
        private string DoGetListEasySearch([FromBody] JObject data, out List<LogSearch> ListLogSearch)
        {
            ListLogSearch = new List<LogSearch>();

            string msg = Role.Check(UserToken.UserID, Constants.TabID.TCL, Role.ROLE_TCL_IsVisitPage);
            if (msg.Length > 0) return msg;

            msg = data.ToString("TextSearch", out string TextSearch);
            if (msg.Length > 0) return msg;

            LogSearchInput logSearchInput = new LogSearchInput() { TextSearch = TextSearch };

            msg = DoGetList(logSearchInput, out ListLogSearch);
            if (msg.Length > 0) return msg;

            return "";
        }
        private string DoGetList(LogSearchInput logSearchInput, out List<LogSearch> ListLogSearch)
        {
            ListLogSearch = new List<LogSearch>();

            string msg = LogSearch.GetListLogSearch(logSearchInput, out ListLogSearch);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpPost]
        public Result GetListAdvancedSearch([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListAdvancedSearch(data, out List<LogSearch> ListLogSearch);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListLogSearch.ToResultOk();
        }
        private string DoGetListAdvancedSearch([FromBody] JObject data, out List<LogSearch> ListLogSearch)
        {
            ListLogSearch = new List<LogSearch>();

            string msg = Role.Check(UserToken.UserID, Constants.TabID.TCL, Role.ROLE_TCL_IsVisitPage);
            if (msg.Length > 0) return msg;

            msg = data.ToObject("LogSearchInput", out LogSearchInput logSearchInput);
            if (msg.Length > 0) return msg;

            msg = DoValidateInputParams(logSearchInput);
            if (msg.Length > 0) return msg;

            msg = DoGetList(logSearchInput, out ListLogSearch);
            if (msg.Length > 0) return msg;

            return "";
        }
        private string DoValidateInputParams(LogSearchInput logSearchInput)
        {
            if (logSearchInput == null) return "Tham số truyền vào không hợp lệ".ToMessageForUser();
            if (logSearchInput.LogFrom == null || logSearchInput.LogTo == null) return "Từ ngày hoặc đến ngày không được để trống".ToMessageForUser();
            if (logSearchInput.LogFrom > logSearchInput.LogTo) return "Từ ngày không được lớn hơn đến ngày".ToMessageForUser();

            if (logSearchInput.LogTypeID != ConmonConstants.LOG_ALL &&
               logSearchInput.LogTypeID != ConmonConstants.LOG_ACTIVITY_TYPE &&
               logSearchInput.LogTypeID != ConmonConstants.LOG_ERROR_TYPE &&
               logSearchInput.LogTypeID != ConmonConstants.LOG_HISTORY_TYPE) return "Không xác định loại log".ToMessageForUser();

            return "";
        }
    }
}