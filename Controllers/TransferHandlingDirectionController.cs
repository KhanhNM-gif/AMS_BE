using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using BSS;
using Newtonsoft.Json.Linq;
using BSS.DataValidator;
using System.Data;
using System;

namespace WebAPI.Controllers
{
    public class TransferHandlingDirectionController : Authentication
    {
        [HttpGet]
        public Result GetTransferDirectionPDX()
        {
            return GetTransferDirection(Constants.TransferHandling.PDX);
        }
        [HttpGet]
        public Result GetTransferDirectionPDXVP()
        {
            return GetTransferDirection(Constants.TransferHandling.PDXVP);
        }
        [HttpGet]
        public Result GetTransferDirectionPKK()
        {
            return GetTransferDirection(Constants.TransferHandling.PKK);
        }
        [HttpGet]
        public Result GetTransferDirection(int TransferTypeID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = TransferHandlingDirection.GetTransferDirection(UserToken.UserID, 1, out List<TransferHandlingDirection> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpGet]
        public Result GetUserIDHandling(string TransferDirectionID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = TransferHandlingDirection.GetUserIDHandling(UserToken.UserID, UserToken.AccountID, TransferDirectionID, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
    }
}