using ASM_API.App_Start.Statistic;
using BSS;
using System;
using System.Web.Http;

namespace ASM_API.Controllers
{
    public class StatisticController : Authentication
    {
        [HttpPost]
        public Result GetListAssetStatistic(AssetStatisticSearch search)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.BCTK_TS, Role.ROLE_BCTKTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (search.CreateDateCategoryID == 0) search.DateFrom = search.DateTo = null;
            else
            {
                msg = StoreItemSearchCategoryDateID.GetDateByCategoryID(search.CreateDateCategoryID, search.DateFrom, search.DateTo, out DateTime fromDate, out DateTime toDate);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError(); ;
                search.DateFrom = fromDate;
                search.DateTo = toDate;
            }

            search.AccountID = UserToken.AccountID;
            msg = AssetStatistic.GetListPagingSearch(search, out var assetStatistic);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = IssueStatistic.GetListPagingSearch(search, out var issueStatistic);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { assetStatistic, issueStatistic }.ToResultOk();
        }

        /*[HttpPost]
        public Result GetListIssueStatistic(AssetStatisticSearch search)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.BCTK_TS, Role.ROLE_BCTKTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (search.CreateDateCategoryID == 0) search.DateFrom = search.DateTo = null;
            else
            {
                msg = StoreItemSearchCategoryDateID.GetDateByCategoryID(search.CreateDateCategoryID, search.DateFrom, search.DateTo, out DateTime fromDate, out DateTime toDate);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError(); ;
                search.DateFrom = fromDate;
                search.DateTo = toDate;
            }

            search.AccountID = UserToken.AccountID;
            msg = IssueStatistic.GetListPagingSearch(search, out var o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }*/

        /*[HttpPost]
        public Result GetListPagingItemInStoreStatistic(ItemStatisticSearch search)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.BCTK_VP, Role.ROLE_BCTKVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = search.Validate();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (search.CreateDateCategoryID == 0) search.DateFrom = search.DateTo = null;
            else
            {
                msg = StoreItemSearchCategoryDateID.GetDateByCategoryID(search.CreateDateCategoryID, search.DateFrom, search.DateTo, out DateTime fromDate, out DateTime toDate);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError(); ;
                search.DateFrom = fromDate;
                search.DateTo = toDate;
            }

            search.AccountID = UserToken.AccountID;
            msg = ItemInStoreStatistic.GetListPagingSearch(search, out var lt, out int total);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }


        [HttpPost]
        public Result GetListPagingManufactureStatistic(ItemStatisticSearch search)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.BCTK_VP, Role.ROLE_BCTKVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = search.Validate();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (search.CreateDateCategoryID == 0) search.DateFrom = search.DateTo = null;
            else
            {
                msg = StoreItemSearchCategoryDateID.GetDateByCategoryID(search.CreateDateCategoryID, search.DateFrom, search.DateTo, out DateTime fromDate, out DateTime toDate);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError(); ;
                search.DateFrom = fromDate;
                search.DateTo = toDate;
            }

            search.AccountID = UserToken.AccountID;
            msg = ManufactureStatistic.GetListPagingSearch(search, out var lt, out int total);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }*/

        [HttpGet]
        public Result GetStoreStatistic()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.BCTK_VP, Role.ROLE_BCTKVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = StoreStatistic.GetList(UserToken.AccountID, out var outLtAssetStatistic);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = outLtAssetStatistic }.ToResultOk();
        }

        [HttpPost]
        public Result GetListPagingItemInStoreStatistic(ItemStatisticSearch search)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.BCTK_VP, Role.ROLE_BCTKVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            search.AccountID = UserToken.AccountID;

            msg = search.Validate();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = ItemInStoreStatistic.GetListPagingSearch(search, out var lt, out int total);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
    }
}