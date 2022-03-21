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
    public class AssetTypeController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoInsertUpdate(UserToken.UserID, data, out AssetType assettypeOut);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return assettypeOut.ToResultOk();
        }
        private string DoInsertUpdate(int UserIDCreate, [FromBody] JObject data, out AssetType assetTypeOut)
        {
            assetTypeOut = new AssetType();

            string msg = data.ToObject("AssetType", out AssetType assetType);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (assetType.AssetTypeGroupID == Constants.AssetTypeGroup.TAISAN)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.LTS, Role.ROLE_LTS_CRUD);
                if (msg.Length > 0) return msg;
            }

            if (assetType.AssetTypeGroupID == Constants.AssetTypeGroup.VATPHAM)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.LVP, Role.ROLE_LVP_CRUD);
                if (msg.Length > 0) return msg;
            }

            msg = DoInsertUpdate_Validate(assetType);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoInsertUpdate_ObjectToDB(dbm, assetType, out assetTypeOut, UserIDCreate);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetType DoInsertUpdate";
            }

            dbm.CommitTransac();

            return msg;
        }
        private string DoInsertUpdate_Validate(AssetType data)
        {
            string msg = "";

            msg = DataValidator.Validate(new
            {
                data.AssetTypeID,
                data.AssetTypeGroupID,
                data.AssetTypeName,
                data.AssetTypeCode,
                data.AssetTypeDescription
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            foreach (var item in data.ListAssetTypeProperty)
            {
                msg = DataValidator.Validate(new
                {
                    item.AssetTypePropertyName,
                    item.AssetTypePropertyDataID
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();
            }

            data.AccountID = UserToken.AccountID;

            msg = DoInsertUpdate_Validate_AssetTypePropertyName(data);
            if (msg.Length > 0) return msg;

            msg = DoInsertUpdate_Validate_AssetTypePropertyTypeData(data);
            if (msg.Length > 0) return msg;

            msg = DoInsertUpdate_Validate_AssetTypePropertyValue(data);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string DoInsertUpdate_Validate_AssetTypePropertyName(AssetType data)
        {
            string msg = AssetType.CheckExitsAssetTypeByCodeAndName(data.AssetTypeCode, data.AssetTypeName, data.AssetTypeGroupID, UserToken.AccountID, out AssetType assetTypeNameExist);
            if (msg.Length > 0) return msg;
            if (assetTypeNameExist != null && assetTypeNameExist.AssetTypeID != data.AssetTypeID) return ("Đã tồn tại dữ liệu: có Tên " + data.AssetTypeName + " hoặc Mã " + data.AssetTypeCode + "  trong hệ thống").ToMessageForUser();

            if (data.ListAssetTypeProperty.Count(v => string.IsNullOrEmpty(v.AssetTypePropertyName)) > 0) return "Không được để trống tên thuộc tính".ToMessageForUser();

            var vAssetTypePropertyName = data.ListAssetTypeProperty.GroupBy(v => v.AssetTypePropertyName);
            string TypePropertyNameDuplicate = "";
            foreach (var itemTypePropertyName in vAssetTypePropertyName)
                if (itemTypePropertyName.Count() >= 2)
                {
                    if (TypePropertyNameDuplicate != "") TypePropertyNameDuplicate += ", ";
                    TypePropertyNameDuplicate += itemTypePropertyName.Key;
                }
            if (TypePropertyNameDuplicate.Length > 0) return ("Tên thuộc tính " + TypePropertyNameDuplicate + " không được trùng nhau").ToMessageForUser();

            if (data.AssetTypeGroupID == Constants.AssetTypeGroup.TAISAN)
            {
                string[] arrTypePropertyNameMain = { "Serial", "Model", "Màu sắc", "Nhà cung cấp", "Hãng sản xuất" };
                string TypePropertyNameMainDuplicate = "";
                foreach (var TypePropertyNameMain in arrTypePropertyNameMain)
                    if (data.ListAssetTypeProperty.Count(v => v.AssetTypePropertyName.ToLower() == TypePropertyNameMain.ToLower()) >= 1)
                    {
                        if (TypePropertyNameMainDuplicate != "") TypePropertyNameMainDuplicate += ", ";
                        TypePropertyNameMainDuplicate += TypePropertyNameMain;
                    }
                if (TypePropertyNameMainDuplicate.Length > 0) return ("Các thuộc tính " + TypePropertyNameMainDuplicate + " là Thuộc tính chính của Loại tài sản. Bạn vui lòng xóa các Thuộc tính này").ToMessageForUser();
            }
            if (data.AssetTypeGroupID == Constants.AssetTypeGroup.VATPHAM)
            {
                string[] arrTypePropertyNameMain = { "Đơn vị tính", "Ngưỡng cảnh báo SLT", "Hạn sử dụng", "Nhà cung cấp" };
                string TypePropertyNameMainDuplicate = "";
                foreach (var TypePropertyNameMain in arrTypePropertyNameMain)
                    if (data.ListAssetTypeProperty.Count(v => v.AssetTypePropertyName.ToLower() == TypePropertyNameMain.ToLower()) >= 1)
                    {
                        if (TypePropertyNameMainDuplicate != "") TypePropertyNameMainDuplicate += ", ";
                        TypePropertyNameMainDuplicate += TypePropertyNameMain;
                    }
                if (TypePropertyNameMainDuplicate.Length > 0) return ("Các thuộc tính " + TypePropertyNameMainDuplicate + " là Thuộc tính chính của Loại vật phẩm. Bạn vui lòng xóa các Thuộc tính này").ToMessageForUser();
            }

            foreach (var item in data.ListAssetTypeProperty)
                if (item.AssetTypePropertyID > 0)
                {
                    msg = AssetProperty.CheckAssetPropertyIDUsed(item.AssetTypePropertyID, "", out AssetProperty assetProperty);
                    if (msg.Length > 0) return msg;

                    msg = AssetTypeProperty.GetOneByAssetTypePropertyID(item.AssetTypePropertyID, out AssetTypeProperty assetPropertyDB);
                    if (msg.Length > 0) return msg;

                    if (assetProperty != null && item.AssetTypePropertyName != assetPropertyDB.AssetTypePropertyName)
                        return ("Thuộc tính " + assetPropertyDB.AssetTypePropertyName + " đã được sử dụng cho Tài sản. Bạn không thể sửa Tên thuộc tính thành " + item.AssetTypePropertyName).ToMessageForUser();
                }

            return msg;
        }
        private string DoInsertUpdate_Validate_AssetTypePropertyTypeData(AssetType data)
        {
            int[] arrValidateText = new int[] { Constants.TypePropertyData.INT, Constants.TypePropertyData.DATE, Constants.TypePropertyData.CHECKBOX, Constants.TypePropertyData.LIST, Constants.TypePropertyData.SELECT };
            int[] arrValidateCategory = new int[] { Constants.TypePropertyData.DATE, Constants.TypePropertyData.INT, Constants.TypePropertyData.PASSWORD };

            Dictionary<int, int[]> dValidate = new Dictionary<int, int[]> {
                { Constants.TypePropertyData.TEXT, arrValidateText},
                {Constants.TypePropertyData.PASSWORD, arrValidateText },
                {Constants.TypePropertyData.INT, new int[] { Constants.TypePropertyData.DATE, Constants.TypePropertyData.CHECKBOX, Constants.TypePropertyData.LIST, Constants.TypePropertyData.SELECT } },
                {Constants.TypePropertyData.DATE, new int[] { Constants.TypePropertyData.INT,Constants.TypePropertyData.CHECKBOX, Constants.TypePropertyData.LIST, Constants.TypePropertyData.SELECT, Constants.TypePropertyData.PASSWORD } },
                {Constants.TypePropertyData.LIST, arrValidateCategory},
                {Constants.TypePropertyData.SELECT, arrValidateCategory},
                {Constants.TypePropertyData.CHECKBOX, arrValidateCategory},
            };

            foreach (var assetTypePropertyDBInput in data.ListAssetTypeProperty)
                if (assetTypePropertyDBInput.AssetTypePropertyID > 0)
                {
                    string msg = AssetTypeProperty.GetOneByAssetTypePropertyID(assetTypePropertyDBInput.AssetTypePropertyID, out AssetTypeProperty assetTypePropertyDB);
                    if (msg.Length > 0) return msg;

                    if (assetTypePropertyDB.AssetTypePropertyDataID != assetTypePropertyDBInput.AssetTypePropertyDataID)
                    {
                        msg = AssetProperty.CheckAssetPropertyIDUsed(assetTypePropertyDBInput.AssetTypePropertyID, "", out AssetProperty assetProperty);
                        if (msg.Length > 0) return msg;

                        if (assetProperty != null)
                            foreach (var item in dValidate)
                                if (assetTypePropertyDB.AssetTypePropertyDataID == item.Key && item.Value.Contains(assetTypePropertyDBInput.AssetTypePropertyDataID))
                                    return ("Thuộc tính: " + assetTypePropertyDBInput.AssetTypePropertyName + ". Bạn không thể sửa kiểu dữ liệu " + AssetTypePropertyData.GetAssetTypePropertyDataName(assetTypePropertyDB.AssetTypePropertyDataID) + " sang kiểu dữ liệu " + AssetTypePropertyData.GetAssetTypePropertyDataName(assetTypePropertyDBInput.AssetTypePropertyDataID)).ToMessageForUser();
                    }
                }

            return "";
        }
        private string DoInsertUpdate_Validate_AssetTypePropertyValue(AssetType data)
        {
            foreach (var assetTypePropertyInput in data.ListAssetTypeProperty)
                if ((assetTypePropertyInput.AssetTypePropertyDataID == Constants.TypePropertyData.LIST || assetTypePropertyInput.AssetTypePropertyDataID == Constants.TypePropertyData.SELECT || assetTypePropertyInput.AssetTypePropertyDataID == Constants.TypePropertyData.CHECKBOX))
                    if (string.IsNullOrEmpty(assetTypePropertyInput.AssetTypePropertyValueList)) return ("Bạn phải nhập Giá trị cho thuộc tính: " + assetTypePropertyInput.AssetTypePropertyName).ToMessageForUser();
                    else
                        if (assetTypePropertyInput.AssetTypePropertyID > 0)
                    {
                        string msg = AssetTypeProperty.GetOneByAssetTypePropertyID(assetTypePropertyInput.AssetTypePropertyID, out AssetTypeProperty assetTypePropertyDB);
                        if (msg.Length > 0) return msg;

                        if (assetTypePropertyDB.AssetTypePropertyValueList != null)
                        {
                            string[] arrAssetTypePropertyValue_DB = assetTypePropertyDB.AssetTypePropertyValueList.Split(',');
                            string[] arrAssetTypePropertyValue_Input = assetTypePropertyInput.AssetTypePropertyValueList.Split(',');
                            string AssetTypePropertyValue_NoDelete = "";
                            foreach (var AssetTypePropertyValue_DB in arrAssetTypePropertyValue_DB)
                                if (arrAssetTypePropertyValue_Input.Count(v => v.Trim() == AssetTypePropertyValue_DB.Trim()) == 0)
                                {
                                    msg = AssetProperty.CheckAssetPropertyIDUsed(assetTypePropertyInput.AssetTypePropertyID, AssetTypePropertyValue_DB.Trim(), out AssetProperty assetProperty);
                                    if (msg.Length > 0) return msg;

                                    if (assetProperty != null)
                                    {
                                        if (AssetTypePropertyValue_NoDelete.Length > 0) AssetTypePropertyValue_NoDelete += ", ";
                                        AssetTypePropertyValue_NoDelete += AssetTypePropertyValue_DB.Trim();
                                    }
                                }

                            if (AssetTypePropertyValue_NoDelete.Length > 0) return ("Thuộc tính " + assetTypePropertyInput.AssetTypePropertyName + ": Giá trị " + AssetTypePropertyValue_NoDelete + " đã được sử dụng cho Tài sản. Bản không được phép xóa.").ToMessageForUser();
                        }
                    }

            return "";
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, AssetType assetType, out AssetType assetTypeOut, int UserIDCreate)
        {
            string msg = assetType.InsertUpdate(dbm, out assetTypeOut);
            if (msg.Length > 0) return msg;

            if (assetType.ListAssetTypeProperty.Count > 0)
            {
                msg = DoInsertUpdate_AssetProperty(dbm, assetType.AssetTypeID != 0, assetTypeOut.AssetTypeID, assetType.ListAssetTypeProperty, out List<AssetTypeProperty> outassetProperties);
                if (msg.Length > 0) return msg;
                assetTypeOut.ListAssetTypeProperty = outassetProperties;
            }
            Log.WriteHistoryLog(dbm, assetType.AssetTypeID == 0 ? (assetType.AssetTypeGroupID == Constants.AssetTypeGroup.TAISAN) ? "Thêm loại tài sản" : "Thêm loại vật phẩm" : (assetType.AssetTypeGroupID == Constants.AssetTypeGroup.TAISAN) ? "Sửa loại tài sản" : "Sửa loại vật phẩm", assetTypeOut.ObjectGuid, UserToken.UserID);

            return msg;
        }
        private string DoInsertUpdate_AssetProperty(DBM dbm, bool isUpdate, int AssetTypeID, List<AssetTypeProperty> ltassetProperty, out List<AssetTypeProperty> outassetProperties)
        {
            string msg = "";
            outassetProperties = new List<AssetTypeProperty>();
            foreach (var item in ltassetProperty)
            {
                AssetTypeProperty asstypeproperty = new AssetTypeProperty
                {
                    AssetTypeID = AssetTypeID,
                    AssetTypePropertyID = item.AssetTypePropertyID,
                    AssetTypePropertyName = item.AssetTypePropertyName,
                    AssetTypePropertyDataID = item.AssetTypePropertyDataID,
                    AssetTypePropertyValueList = item.AssetTypePropertyValueList,
                    IsRequired = item.IsRequired
                };

                msg = asstypeproperty.InsertUpdate(dbm, out AssetTypeProperty asstypepropertyOut);
                if (msg.Length > 0) return msg;

                if (isUpdate && item.AssetTypePropertyID == 0) AssetProperty.SetValueForNewAssetTypeProperty(dbm, AssetTypeID, asstypepropertyOut.AssetTypePropertyID, asstypepropertyOut.AssetTypePropertyName);

                outassetProperties.Add(asstypepropertyOut);
            }

            return msg;
        }

        [HttpGet]
        public Result GetOne(int AssetTypeID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetOne(AssetTypeID, out AssetType assetType);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return assetType.ToResultOk();
        }
        public string DoGetOne(int AssetTypeID, out AssetType assetType)
        {
            assetType = null;
            string msg = DataValidator.Validate(new { AssetTypeID }).ToErrorMessage();
            if (msg.Length > 0) return msg;

            msg = AssetType.GetOneByAssetTypeID(AssetTypeID, UserToken.AccountID, out assetType);
            if (msg.Length > 0) return msg;
            if (assetType == null) return ("Loại tài sản không tồn tại AssetTypeID " + AssetTypeID).ToMessageForUser();

            msg = AssetTypeProperty.GetListByAssetTypeID(assetType.AssetTypeID, out List<AssetTypeProperty> typeProperties);
            if (msg.Length > 0) return msg;

            assetType.ListAssetTypeProperty = typeProperties;

            return "";
        }

        [HttpGet]
        public Result GetListPropertyByAssetTypeID(int AssetTypeID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            string msg = DoGetListProperty(AssetTypeID, out List<AssetTypeProperty> assetTypeProperties);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return assetTypeProperties.ToResultOk();
        }
        public string DoGetListProperty(int AssetTypeID, out List<AssetTypeProperty> assetTypeProperties)
        {
            assetTypeProperties = null;
            string msg = DataValidator.Validate(new { AssetTypeID }).ToErrorMessage();
            if (msg.Length > 0) return msg;

            msg = AssetTypeProperty.GetListByAssetTypeID(AssetTypeID, out assetTypeProperties);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpGet]
        public Result GetListByAssetTypeID(int AssetTypeID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.LTS, Role.ROLE_LTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DataValidator.Validate(new { AssetTypeID }).ToErrorMessage();
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = AssetTypeProperty.GetListByAssetTypeID(AssetTypeID, out List<AssetTypeProperty> typeProperties);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return typeProperties.ToResultOk();
        }

        [HttpGet]
        public Result GetListByActive(int AssetTypeGroupID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = AssetType.GetAllByActive(UserToken.AccountID, AssetTypeGroupID, out List<AssetType> outAssetType);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            if (outAssetType.Count > 0)
                foreach (var item in outAssetType)
                {
                    msg = AssetTypeProperty.GetListByAssetTypeID(item.AssetTypeID, out List<AssetTypeProperty> ltAssetTypeProperty);
                    if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
                    item.ListAssetTypeProperty = ltAssetTypeProperty;
                }
            return outAssetType.ToResultOk();
        }

        [HttpGet]
        public Result GetListAssetType(string TextSearch, int IsActive, int PageSize, int CurrentPage)
        {
            return GetList(TextSearch, Constants.AssetTypeGroup.TAISAN, IsActive, PageSize, CurrentPage);
        }
        [HttpGet]
        public Result GetListItemType(string TextSearch, int IsActive)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListItem(TextSearch, IsActive, Constants.AssetTypeGroup.VATPHAM, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = dt, Total = dt.Rows.Count }.ToResultOk();
        }
        private string DoGetListItem(string TextSearch, int IsActive, int AssetTypeGroupID, out DataTable dt)
        {
            return SearchAllByFilter(TextSearch, IsActive, AssetTypeGroupID, out dt);
        }

        private Result GetList(string TextSearch, int AssetTypeGroupID, int IsActive, int PageSize, int CurrentPage)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.LTS, Role.ROLE_LTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            DataTable dt;
            msg = SearchAllByFilter(TextSearch, IsActive, AssetTypeGroupID, out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            DataTable dtPaging;
            msg = UtilitiesDatatable.GetDtPaging(dt, PageSize, CurrentPage, out dtPaging);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            SPV.InsertTab(UserToken.UserID, Constants.TabID.LTS);

            return new { Data = dtPaging, Total = dt.Rows.Count }.ToResultOk();
        }

        private string SearchAllByFilter(string TextSearch, int IsActive, int AssetTypeGroupID, out DataTable dt)
        {
            string msg = AssetType.SearchAllByFilter(TextSearch, IsActive, AssetTypeGroupID, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.LTS, Role.ROLE_LTS_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDelete(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoDelete(int UserID, [FromBody] JObject data)
        {
            string msg = "";

            msg = data.ToNumber("AssetTypeID", out int AssetTypeID);
            if (msg.Length > 0) return msg.ToMessageForUser();

            AssetType assetType;
            msg = AssetType.GetActiveByAssetTypeID(AssetTypeID, UserToken.AccountID, out assetType);
            if (msg.Length > 0) return msg;
            if (assetType == null) return ("Không tồn tại loại tài sản ID = " + AssetTypeID).ToMessageForUser();

            if (assetType.AssetTypeGroupID == Constants.AssetTypeGroup.TAISAN)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.LTS, Role.ROLE_LTS_CRUD);
                if (msg.Length > 0) return msg;
            }
            if (assetType.AssetTypeGroupID == Constants.AssetTypeGroup.VATPHAM)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.LVP, Role.ROLE_LVP_CRUD);
                if (msg.Length > 0) return msg;
            }

            msg = Delete_Validate(assetType.AssetTypeGroupID, assetType.AssetTypeID);
            if (msg.Length > 0) return msg.ToMessageForUser();

            //thực hiện xóa cứng nếu loại tài sản không được gắn với tài sản nào
            msg = AssetType.Delete(assetType.AssetTypeID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            Log.WriteHistoryLog("Xóa loại tài sản", assetType.ObjectGuid, UserID);

            return msg;
        }
        private string Delete_Validate(int AssetTypeGroupID, int AssetTypeID)
        {
            string msg;

            List<Asset> outAssets = new List<Asset>();
            List<Item> outItems = new List<Item>();

            if (AssetTypeGroupID == Constants.AssetTypeGroup.TAISAN)
                msg = Asset.SelectByAssetTypeID(AssetTypeID, UserToken.AccountID, out outAssets);
            else if (AssetTypeGroupID == Constants.AssetTypeGroup.VATPHAM)
                msg = Item.GetListByItemType(AssetTypeID, UserToken.AccountID, out outItems);
            else return "AssetTypeGroupID is not validate";
            if (msg.Length > 0) return msg;
            if (outAssets.Any() || outItems.Any()) return $"Bạn không thể xóa Loại {(AssetTypeGroupID == Constants.AssetTypeGroup.TAISAN ? "Tài sản " : "Vật Phẩm")} này, vì đã gắn với thông tin {(AssetTypeGroupID == Constants.AssetTypeGroup.TAISAN ? "Tài sản " : "Vật Phẩm")}";

            return msg;
        }

        [HttpPost]
        public Result DeleteAssetTypeProperty([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoDeleteAssetTypeProperty(UserToken.UserID, data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoDeleteAssetTypeProperty(int UserID, [FromBody] JObject data)
        {
            string msg = data.ToNumber("AssetTypePropertyID", out int AssetTypePropertyID);
            if (msg.Length > 0) return msg;

            AssetTypeProperty assetTypeProperty;
            msg = AssetTypeProperty.GetOneByAssetTypePropertyID(AssetTypePropertyID, out assetTypeProperty);
            if (msg.Length > 0) return msg;
            if (assetTypeProperty == null) return ("Không thuộc tính loại tài sản ID = " + AssetTypePropertyID).ToMessageForUser();

            msg = AssetProperty.CheckAssetPropertyIDUsed(assetTypeProperty.AssetTypePropertyID, "", out AssetProperty property);
            if (msg.Length > 0) return msg;
            if (property != null) return ("Thuộc tính " + assetTypeProperty.AssetTypePropertyName + " đã được sử dụng cho Tài sản. Bạn không được xóa Thuộc tính").ToMessageForUser();

            msg = AssetTypeProperty.Delete(assetTypeProperty.AssetTypePropertyID);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpGet]
        public Result GetListAssetTypePropertyData()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.CheckVisitPage(UserToken.UserID, Constants.TabID.LTS);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = AssetTypePropertyData.GetList(out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
        [HttpGet]
        public Result GetMenuAssetType()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = AssetTypeMenu.GetAll(UserToken.AccountID, out List<AssetTypeMenu> dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
    }
}