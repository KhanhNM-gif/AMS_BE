using BSS;
using System;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class SPVController : Authentication
    {
        /// <summary>
        /// Lấy SPV Tìm kiếm vật phẩm
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [HttpGet]
        public Result GetSPVItem()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            Guid page;
            string msg = Constants.PageGUID.GetPage(Constants.TabID.QLVP, out page);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetSPV(page, out object item);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return item.ToResultOk();
        }
        /// <summary>
        /// Lấy SPV Tìm kiếm Tài sản
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        [HttpGet]
        public Result GetSPVAsset()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            Guid page;
            string msg = Constants.PageGUID.GetPage(Constants.TabID.QLTS, out page);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetSPV(page, out object item);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return item.ToResultOk();
        }
        private string DoGetSPV(Guid guid, out object item)
        {
            item = null;

            string msg = SPV.GetSearchItem(UserToken.UserID, guid, out item);
            if (msg.Length > 0) return msg;

            return "";
        }
    }
}