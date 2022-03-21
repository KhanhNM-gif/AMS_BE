using BSS;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class FileAttachController : Authentication
    {
        [HttpPost]
        public Result UploadFile()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<FileAttach> ltFileAttach;
            string msg = DoUploadFile(out ltFileAttach);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ltFileAttach.ToResultOk();
        }
        private string DoUploadFile(out List<FileAttach> ltFileAttach)
        {
            ltFileAttach = null;
            string msg = "";

            try
            {
                Guid ObjectGUID = Guid.Empty;

                msg = WebHelper.GetStringFromRequestForm(HttpContext.Current, "FunctionID", out string FunctionID);
                if (msg.Length > 0) return msg;

                msg = FileAttachUpload.Upload(UserToken.UserID, FunctionID, ObjectGUID, out ltFileAttach);
                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


        [HttpPost]
        public Result DeleteFile([FromBody]JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoDeleteFile(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoDeleteFile([FromBody]JObject data)
        {
            string msg = data.ToString("FileAttachGUID", out string sFileAttachGUID);
            if (msg.Length > 0) return msg;

            Guid FileAttachGUID;
            msg = Convertor.ObjectToGuid(sFileAttachGUID, out FileAttachGUID);
            if (msg.Length > 0) return msg;

            msg = FileAttach.UpdateIsDelete(new DBM(), FileAttachGUID, true);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpGet]
        public Result GetByObjectGUID(Guid ObjectGUID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<FileAttach> lt;
            string msg = FileAttach.GetByObjectGUID(ObjectGUID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }

        [HttpGet]
        public Result GetByUserIDCreate(string FunctionID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<FileAttach> lt;
            string msg = FileAttach.GetByUserIDCreate(Guid.Empty, UserToken.UserID, FunctionID, out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.Where(v => !v.IsDelete).ToResultOk();
        }
    }
}