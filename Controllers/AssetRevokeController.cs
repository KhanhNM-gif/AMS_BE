using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class AssetRevokeController : Authentication
    {
        [HttpPost]
        public Result Revoke([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLTS, Role.ROLE_QLTS_THUHOI);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertUpdate(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoInsertUpdate([FromBody] JObject data)
        {
            string msg = data.ToObject("AssetRevoke", out AssetRevoke assetRevoke);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_Validate(assetRevoke);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_ObjectToDB(new DBM(), assetRevoke);
            if (msg.Length > 0) return msg.ToMessageForUser();

            return msg;
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, AssetRevoke assetReturn)
        {
            string msg = "";
            string AssetIDs = string.Join(",", assetReturn.ltAsset.Select(v => v.AssetID));

            /*msg = Asset.UpdateStatusID_Revoke(dbm, AssetIDs, Constants.StatusAsset.ĐD_TK, UserToken.UserID, UserToken.AccountID);
            if (msg.Length > 0) return msg;*/

            foreach (var item in assetReturn.ltAsset)
            {
                msg = Asset.GetOneByAssetID(item.AssetID, out Asset asset);
                if (msg.Length > 0) return msg;

                msg = AccountUser.GetOneByUserID(asset.UserIDHolding, out AccountUser u);
                if (msg.Length > 0) return msg;

                msg = Asset.UpdateStatusID_Revoke2(dbm, item.AssetID, Constants.StatusAsset.ĐD_TK, UserToken.UserID, item.PlaceID, UserToken.AccountID);
                if (msg.Length > 0) return msg;

                if (msg.Length > 0) return msg.ToMessageForUser();
                Log.WriteHistoryLog(dbm, $"Thu hồi tài sản từ {u.FullName} ({u.UserName}). Lý do: " + assetReturn.AssetRevokeComment, item.ObjectGuid, UserToken.UserID);
            }
            return msg;
        }
        private string DoInsertUpdate_Validate(AssetRevoke assetRevoke)
        {
            string msg = DataValidator.Validate(new
            {
                assetRevoke.AssetRevokeComment
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (string.IsNullOrEmpty(assetRevoke.AssetRevokeComment)) return "Bạn chưa nhập Lý do thu hồi".ToMessageForUser();

            if (assetRevoke.ltAsset.Count == 0) return ("Bạn chưa chọn Tài sản nào").ToMessageForUser();

            for (int i = 0; i < assetRevoke.ltAsset.Count; i++)
            {
                var item = assetRevoke.ltAsset[i];

                msg = DataValidator.Validate(new { item.ObjectGuid }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByGuid(item.ObjectGuid, out Asset o);
                if (msg.Length > 0) return msg;
                assetRevoke.ltAsset[i] = o;

                if (item.PlaceID == 0) return "Bạn chưa cập nhật Nơi để mới cho Tài sản khi thu hồi".ToMessageForUser();

                if (o.AssetStatusID == Constants.StatusAsset.MT || o.AssetStatusID == Constants.StatusAsset.ĐX || o.AssetStatusID == Constants.StatusAsset.CD || o.AssetStatusID == Constants.StatusAsset.ĐD_TK || o.AssetStatusID == Constants.StatusAsset.TC)
                    return ("Bạn không được thu hồi tài sản khi tài sản đang ở trạng thái " + o.AssetStatusName).ToMessageForUser();
            }

            return msg;
        }
    }
}