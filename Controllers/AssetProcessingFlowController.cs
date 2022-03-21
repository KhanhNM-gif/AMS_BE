using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BSS;
using Newtonsoft.Json.Linq;
using BSS.DataValidator;
using System.Web;
using System.Data;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WebAPI.Controllers
{
    public class AssetProcessingFlowController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody]JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.LTS);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertUpdate(UserToken.UserID, data, out AssetProcessingFlow outprocessingFlow);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return outprocessingFlow.ToResultOk();
        }
        private string DoInsertUpdate(int UserIDCreate, [FromBody]JObject data, out AssetProcessingFlow outass)
        {
            outass = new AssetProcessingFlow();
            string msg = "";

            AssetProcessingFlow processingFlow = new AssetProcessingFlow();
            msg = BSS.Common.CopyObjectPropertyData(data, processingFlow);
            if (msg.Length > 0) return msg;

            msg = DoInsertUpdate_Validate(processingFlow);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_ObjectToDB(new DBM(), processingFlow, out outass, UserIDCreate);
            if (msg.Length > 0) { return msg; }

            return msg;
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, AssetProcessingFlow processingFlow, out AssetProcessingFlow outprocessingFlow, int UserIDCreate)
        {
            string msg = processingFlow.InsertUpdate(dbm, out outprocessingFlow);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog(dbm, processingFlow.ProcessType == 3 ? "Gửi duyệt Tài sản" : "Hủy gửi duyệt tài sản", outprocessingFlow.ObjectGuid, UserToken.UserID);

            return msg;
        }
        private string DoInsertUpdate_Validate([FromBody]AssetProcessingFlow data)
        {
            string msg = "";

            msg = DataValidator.Validate(new
            {
                data.ID,
                data.AssetID,
                data.CommentProcess,
                data.ProcessType,
                data.AssetApproveID
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            return msg;
        }
        

    }
}