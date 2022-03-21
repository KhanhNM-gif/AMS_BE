using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class PlaceController : Authentication
    {
        [HttpPost]
        public Result InsertUpdateDepot(Depot data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoInsertUpdate(data, out Place place);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return place.ToResultOk();
        }
        [HttpPost]
        public Result InsertUpdatePlace(StoragePlace data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoInsertUpdate(data, out Place place);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return place.ToResultOk();
        }
        private string DoInsertUpdate(Place place, out Place mNew)
        {
            mNew = null;
            place.SetData(UserToken.AccountID);

            string msg = place.CheckRole(UserToken.UserID);
            if (msg.Length > 0) return msg;

            msg = DoInsertUpdate_Validate(place);
            if (msg.Length > 0) return msg.ToMessageForUser();


            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = place.InsertUpdate(dbm, out mNew);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }

                msg = Log.WriteHistoryLog(place.GetLogMessageInsertUpdate(), mNew.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }

                dbm.CommitTransac();
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.Message + " at Issue DoInsertUpdate";
            }

            return msg;
        }
        private string DoInsertUpdate_Validate(Place place)
        {
            string msg = "";

            string PlaceName = place.PlaceName.Trim();
            if (place.PlaceName.Length == 0) return $"Tên {place.GetDisplayName()} tài sản không được để trống";

            string PlaceCode = place.PlaceCode.Trim();
            if (PlaceCode.Length == 0) return $"Mã {place.GetDisplayName()} để tài sản không được để trống";

            msg = DataValidator.Validate(new
            {
                place.PlaceID,
                place.PlaceIDParent,
                PlaceCode,
                PlaceName,
                place.PlaceDescription
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = Place.GetListByPlaceType(place.PlaceType, UserToken.AccountID, out List<Place> ltPlace);
            if (msg.Length > 0) return msg;
            if (ltPlace.Exists(v => v.PlaceID != place.PlaceID && v.PlaceCode == place.PlaceCode && v.PlaceName == place.PlaceName))
                return ($"Thông tin {PlaceCode} - {PlaceName} đã có trong hệ thống AMS").ToMessageForUser();

            msg = place.Validate();
            if (msg.Length > 0) return msg;

            if (place.PlaceID > 0 && !place.IsActive)
            {
                msg = Place.GetOneByPlaceIDParent(place.PlaceID, UserToken.AccountID, out Place placeout);
                if (msg.Length > 0) return msg;
                if (placeout != null) return ($"Tên {place.GetDisplayName()}: {placeout.PlaceName} đang được sử dụng. Bạn cần kiểm tra lại các dữ liệu {place.GetDisplayName()} con trước khi cập nhật trạng thái thành không sử dụng").ToMessageForUser();

                msg = Asset.SelectByPlaceID(place.PlaceID, UserToken.AccountID, out List<Asset> assets);
                if (msg.Length > 0) return msg;

                string dsts = string.Join(", ", assets.Select(p => p.AssetCode).ToArray());
                if (assets != null && assets.Count > 0) return ($"Tên {place.GetDisplayName()} {PlaceName} đang được sử dụng. Bạn cần kiểm tra lại các dữ liệu Tài sản {dsts} trước khi cập nhật trạng thái thành không sử dụng").ToMessageForUser();
            }

            return msg;
        }

        [HttpPost]
        public Result CheckAssetExistInPlace([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoCheckAssetExistInPlace(data, out List<Asset> ListAsset);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListAsset.ToResultOk();
        }
        private string DoCheckAssetExistInPlace([FromBody] JObject data, out List<Asset> ListAsset)
        {
            ListAsset = new List<Asset>();
            string msg = data.ToObject("Place", out Place inputPlace);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoGetOne(inputPlace.ObjectGuid, out Place outPlace);
            if (msg.Length > 0) return msg.ToMessageForUser();
            if (outPlace == null) return "Không tồn tại nơi để với ObjectGuid=" + inputPlace.ObjectGuid;

            if (inputPlace.DiagramID != outPlace.DiagramID || inputPlace.DiagramLocation != outPlace.DiagramLocation)
            {
                msg = Asset.SelectByPlaceID(outPlace.PlaceID, UserToken.AccountID, out ListAsset);
                if (msg.Length > 0) return msg;
            }

            return "";
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoDelete(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return Result.GetResultOk();
        }
        private string DoDelete(int UserID, [FromBody] JObject data)
        {
            string msg = "";

            msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("PlaceType", out int PlaceType);
            if (msg.Length > 0) return msg;

            if (PlaceType == ConmonConstants.TYPE_IS_PLACE) msg = Role.Check(UserToken.UserID, Constants.TabID.ND, Role.ROLE_ND_CRUD);
            else msg = Role.Check(UserToken.UserID, Constants.TabID.KHO, Role.ROLE_KHO_CRUD);
            if (msg.Length > 0) return msg;

            msg = DoGetOne(ObjectGuid, out Place place);
            if (msg.Length > 0) return msg;
            if (place.PlaceID <= 0) return ($"Không tồn tại {(place.PlaceType == ConmonConstants.TYPE_IS_PLACE ? "Nơi để " : "Kho")} ID = " + place.PlaceID).ToMessageForUser();

            msg = Delete_Validate(place.PlaceID, PlaceType);
            if (msg.Length > 0) return msg.ToMessageForUser();

            /*msg = Place.GetListChild(place.PlaceID, out string IDs);
            if (msg.Length > 0) return msg;*/

            /*if (string.IsNullOrEmpty(IDs)) IDs = place.PlaceID.ToString();
            else IDs += place.PlaceID;*/

            msg = Place.Delete(place.PlaceID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog($"Xóa {(place.PlaceType == ConmonConstants.TYPE_IS_PLACE ? "Nơi để" : "Kho")}", place.ObjectGuid, UserID);

            return msg;
        }
        private string Delete_Validate(int PlaceID, int PlaceType)
        {
            string msg = Place.GetOneByPlaceIDParent(PlaceID, UserToken.AccountID, out Place place);
            if (msg.Length > 0) return msg;
            if (place != null) return $"Bạn không thể xóa được {(place.PlaceType == ConmonConstants.TYPE_IS_PLACE ? "Nơi để" : "Kho")} này, do {(place.PlaceType == ConmonConstants.TYPE_IS_PLACE ? "Nơi để" : "Kho")}: {place.PlaceName} đang được gắn với {(place.PlaceType == ConmonConstants.TYPE_IS_PLACE ? "Nơi để" : "Kho")} con.";

            msg = Asset.SelectByPlaceID(PlaceID, UserToken.AccountID, out List<Asset> assets);
            if (msg.Length > 0) return msg;
            if (assets.Count > 0) return $"Bạn không thể xóa {(PlaceType == ConmonConstants.TYPE_IS_PLACE ? "Nơi để" : "Kho") } này, vì đã gắn với thông tin tài sản";

            return msg;
        }
        /// <summary>
        /// lấy tất cả kho
        /// </summary>
        /// <param name="PlaceName"></param>
        /// <param name="PlaceType"></param>
        /// <returns></returns>
        [HttpGet]
        public Result GetList(string PlaceName, int PlaceType = ConmonConstants.TYPE_IS_DEPOT)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Place.GetListByPlaceName(PlaceName, PlaceType, UserToken.AccountID, out List<Depot> placeList);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (PlaceType == ConmonConstants.TYPE_IS_PLACE) return placeList.ToResultOk();

            foreach (var item in placeList)
            {
                msg = UserManagementPlace.GetList(item.PlaceID, out var userManagementPlaces);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                item.ltManagementUserID = userManagementPlaces;
            }

            return placeList.ToResultOk();

        }

        [HttpGet]
        public Result GetListPlace()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            //Nơi để ko phân quyền UserID=0 => GetAll
            string msg = Place.GetList(UserToken.AccountID, 0, ConmonConstants.TYPE_IS_PLACE, out List<Place> ListPlace);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListPlace.ToResultOk();
        }
        [HttpGet]
        public Result GetListDepot()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Place.GetList(UserToken.AccountID, UserToken.UserID, ConmonConstants.TYPE_IS_DEPOT, out List<Place> ListPlace);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ListPlace.ToResultOk();
        }

        [HttpGet]
        public Result ViewDetail(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoViewDetail(ObjectGuid, out PlaceDetail placeDetail, out long placeID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (placeID == ConmonConstants.TYPE_IS_PLACE)
                return placeDetail.ToResultOk();
            else
            {
                msg = UserManagementPlaceView.GetList((int)placeID, out var userManagementPlaces);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new
                {
                    placeDetail,
                    ltManagementUserID = userManagementPlaces
                }.ToResultOk();
            }
        }
        private string DoViewDetail(Guid ObjectGuid, out PlaceDetail PlaceDetail, out long placeID)
        {
            PlaceDetail = null;
            string msg = CacheObject.GetPlaceIDByGUID(ObjectGuid, out placeID);
            if (msg.Length > 0) return msg;

            msg = PlaceDetail.ViewDetailByPlaceID(Convert.ToInt32(placeID), UserToken.AccountID, out PlaceDetail);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpGet]
        public Result GetOne(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.ND, Role.ROLE_ND_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetOne(ObjectGuid, out Place place);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (place.PlaceType == ConmonConstants.TYPE_IS_PLACE)
                return place.ToResultOk();
            else
            {
                msg = UserManagementPlaceView.GetList(place.PlaceID, out var userManagementPlaces);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new
                {
                    place,
                    ltManagementUserID = userManagementPlaces
                }.ToResultOk();
            }
        }
        private string DoGetOne(Guid ObjectGuid, out Place place)
        {
            place = null;
            string msg = CacheObject.GetPlaceIDByGUID(ObjectGuid, out long placeID);
            if (msg.Length > 0) return msg;

            msg = Place.GetOneByPlaceID(Convert.ToInt32(placeID), UserToken.AccountID, out place);
            if (msg.Length > 0) return msg;

            return "";
        }
    }
}