using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class ItemController : Authentication
    {
        /// <summary>
        /// thêm mới Vật phẩm
        /// </summary>
        /// <param name="inputItem">object item</param>
        /// <returns>object item</returns>
        [HttpPost]
        public Result InsertOrUpdate([FromBody] Item inputItem)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVP, Role.ROLE_QLVP_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertOrUpdate(inputItem, out Item itemOut);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return itemOut.ToResultOk();
        }
        private string DoInsertOrUpdate([FromBody] Item inputItem, out Item outItem)
        {
            outItem = null;

            string msg = DoValidate_Item(inputItem, out bool isUpdate, out Item outItemDB);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoInsertOrUpdate_ObjectToDB(dbm, inputItem, isUpdate, outItemDB, out outItem);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at Item DoInsertOrUpdate";
            }


            dbm.CommitTransac();

            return "";
        }
        /// <summary>
        /// kiểm tra dữ liệu đầu vào
        /// </summary>
        /// <param name="inputItem">object input item</param>
        /// <param name="outItemDB">out put item to database</param>
        /// <returns>string</returns>
        private string DoValidate_Item(Item inputItem, out bool isUpdate, out Item outItemDB)
        {
            isUpdate = false;
            outItemDB = null;

            string msg = "";
            msg = DataValidator.Validate(new
            {
                inputItem.ItemTypeID,
                inputItem.ObjectGuid,
                inputItem.ItemName,
                inputItem.ItemCode,
                inputItem.ItemStatusID,
                inputItem.ItemUnitStatusID,
                inputItem.UserIDApprove,
                inputItem.UserIDCreate,
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (inputItem.ItemTypeID == 0) return "Bạn chưa nhập Loại vật phẩm".ToMessageForUser();

            if (string.IsNullOrEmpty(inputItem.ItemName)) return "Bạn chưa nhập Tên Vật phẩm".ToMessageForUser();
            if (string.IsNullOrEmpty(inputItem.ItemCode)) return "Bạn chưa nhập Mã Vật phẩm".ToMessageForUser();

            if (inputItem.WarningThreshold.ToNumber(-1) < 0) return "Bạn chỉ được nhập kí tự số cho trường Ngưỡng cảnh báo SLT";
            if (inputItem.WarningThreshold > 999) return "Ngưỡng cảnh báo SLT không được phép vượt quá 999";

            if (inputItem.ItemName.Length < 2) return "Bạn phải nhập vào Tên Vật phẩm tối thiểu 2 ký tự".ToMessageForUser();
            if (inputItem.ItemName.Length > 250) return "Bạn chỉ được nhập vào Tên Vật phẩm tối đa 250 ký tự".ToMessageForUser();

            if (inputItem.ItemNote != null && inputItem.ItemNote.Length > 255) return "Bạn chỉ được nhập vào Ghi chú tối đa 255 ký tự".ToMessageForUser();

            if (inputItem.ItemUnitStatusID == 0) return "Bạn chưa nhập Đơn vị tính".ToMessageForUser();
            if (inputItem.WarningDate.ToNumber(-1) < 0 || inputItem.WarningDate > 30) return "Số ngày cảnh báo phải là số nguyên và không được phép vượt quá 30".ToMessageForUser();
            if (inputItem.ItemUnitStatusID == 0) return "Bạn chưa nhập Đơn vị tính".ToMessageForUser();

            if (inputItem.ObjectGuid != Guid.Empty)
            {
                msg = Item.GetOneByGuid(inputItem.ObjectGuid, out outItemDB);
                if (msg.Length > 0) return msg;

                inputItem.ItemID = outItemDB.ItemID;
                isUpdate = true;
            }

            if (inputItem.ListItemProperty == null) inputItem.ListItemProperty = new List<ItemProperty>();

            //kiểm tra Mã vật phẩm tồn tại trong hệ thống hay chưa
            msg = Item.CheckExistItem(inputItem.ItemCode, inputItem.ItemTypeID, inputItem.ItemName, inputItem.ItemUnitStatusID, UserToken.AccountID, out Item existItem);
            if (msg.Length > 0) return msg;
            if (existItem != null && inputItem.ItemID != existItem.ItemID && inputItem.ItemCode == existItem.ItemCode) return "Thông tin Vật phẩm: " + inputItem.ItemCode + " đã tồn tại".ToMessageForUser();
            //kiểm tra Loại vật phẩm, Tên vật phẩm, Đơn vị tính đã tồn tại trong hệ thống hay chưa
            if (existItem != null && inputItem.ItemID != existItem.ItemID && inputItem.ItemTypeID == existItem.ItemTypeID && inputItem.ItemName == existItem.ItemName
                && inputItem.ItemUnitStatusID == existItem.ItemUnitStatusID)
            {
                if (existItem.ItemStatusID == Constants.ItemStatus.DX) return "Vật phẩm: " + inputItem.ItemName + " này đã bị xóa. Vui lòng kiểm tra hoặc chọn khôi phục lại Vật phẩm".ToMessageForUser();
                //trường hợp sửa Vật phẩm bị trùng với Vật phẩm đã có trong hệ thống
                if (inputItem.ItemID != existItem.ItemID && inputItem.ItemID > 0) return (inputItem.ItemName + " đã tồn tại trong Hệ thống. Bạn vui lòng kiểm tra lại thông tin").ToMessageForUser();
                return "Đã tồn tại Vật phẩm: " + inputItem.ItemName + " trong hệ thống".ToMessageForUser();
            }

            //Kiểm tra trường hợp gửi approve nhưng không chọn người duyệt
            if (inputItem.IsSendApprove)
            {
                if (inputItem.UserIDApprove == 0) return "Bạn chưa chọn Người duyệt vật phẩm".ToMessageForUser();
                inputItem.ItemStatusID = Constants.StatusItem.CD;
            }
            else inputItem.ItemStatusID = Constants.StatusItem.MT;

            inputItem.AccountID = UserToken.AccountID;

            //kiểm tra input thuộc tính động của vật phẩm
            //validate kiểu dữ liệu thuộc tính vật phẩm
            if (inputItem.ListItemProperty.Count > 0)
            {
                msg = ItemProperty.ValidateProperty(inputItem.ListItemProperty);
                if (msg.Length > 0) return msg.ToMessageForUser();
            }

            if (!string.IsNullOrEmpty(inputItem.ItemImageContentBase64))
            {
                msg = DoValidateImage(inputItem.ItemImageContentBase64);
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = DoInsertUpdate_PrepareData_GetPathtImage(inputItem.ItemImageContentBase64, inputItem.ItemImageName, out string urlImage);
                if (msg.Length > 0) return msg.ToMessageForUser();

                inputItem.ItemImagePath = urlImage;
            }

            return "";
        }
        private string DoValidateImage(string itemImageContentBase64)
        {
            //validate file upload lên có phải là ảnh không
            byte[] dataAssetImage = Convert.FromBase64String(itemImageContentBase64);
            MemoryStream ms = new MemoryStream(dataAssetImage);
            string msg = ImageChecker.Check(ms, out bool isImage);
            if (msg.Length > 0) return msg;
            if (!isImage) return "File upload không phải là dạng ảnh, bạn chỉ được phép upload dưới định dạng .jpg ,jpeg, png, bpm".ToMessageForUser();

            return "";
        }
        private string DoInsertUpdate_PrepareData_GetPathtImage(string itemImageContentBase64, string itemImageName, out string urlImage)
        {
            string msg = "";
            urlImage = "";
            try
            {
                byte[] fileContent = Convert.FromBase64String(itemImageContentBase64);

                Guid guid = Guid.NewGuid();
                msg = BSS.Common.GetSetting("PathFileImageItem", out string PathFileImageAsset);
                if (msg.Length > 0) return msg;

                string folderFileImageAsset = HttpContext.Current.Server.MapPath(PathFileImageAsset);

                folderFileImageAsset = folderFileImageAsset + "/" + guid;
                if (!Directory.Exists(folderFileImageAsset)) Directory.CreateDirectory(folderFileImageAsset);

                File.WriteAllBytes(folderFileImageAsset + "/" + itemImageName, fileContent);

                urlImage = PathFileImageAsset + "/" + guid + "/" + itemImageName;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return msg;
        }
        /// <summary>
        /// thêm/sửa Vật phẩm vào DB
        /// </summary>
        /// <param name="dbm">thư viện DBM</param>
        /// <param name="inputItem">input object Vật phẩm</param>
        /// <param name="itemDB">input object Vật phẩm to DB</param>
        /// <param name="outItem"></param>
        /// <returns></returns>
        private string DoInsertOrUpdate_ObjectToDB(DBM dbm, Item inputItem, bool isUpdate, Item itemDB, out Item outItem)
        {
            //ghi lại thông tin Vật phẩm
            string msg = inputItem.InsertOrUpdate(dbm, out outItem);
            if (msg.Length > 0) return msg;

            //xóa thuộc tính cũ của Vật phẩm khi thay đổi loại Vật phẩm
            if (itemDB != null && inputItem.ItemTypeID != itemDB.ItemTypeID)
            {
                msg = ItemProperty.DeletePropertyByItemID(dbm, inputItem.ItemID);
                if (msg.Length > 0) return msg;
            }

            // ghi lại giá trị thuộc tính động của Vật phẩm
            if (inputItem.ListItemProperty.Count > 0)
            {
                msg = DoInsertUpdate_ItemProperty(dbm, outItem.ItemID, inputItem.ListItemProperty, out List<ItemProperty> outItemProperties, isUpdate);
                if (msg.Length > 0) return msg;
                outItem.ListItemProperty = outItemProperties;
            }

            msg = Log.WriteHistoryLog(dbm, inputItem.ItemID == 0 ? "Thêm Vật phẩm" : "Sửa Vật phẩm", outItem.ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            //trường hợp nhấn gửi duyệt
            if (inputItem.IsSendApprove)
            {
                string content = "Yêu cầu duyệt thông tin Vật phẩm cần quản lý";
                msg = ItemApprove.Insert(dbm, outItem.ItemID.ToString(), content, inputItem.UserIDApprove);
                if (msg.Length > 0) return msg;

                msg = Log.WriteHistoryLog(dbm, content, inputItem.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }

            return "";
        }
        /// <summary>
        /// thêm/sửa thông tin thuộc tính động
        /// </summary>
        /// <param name="dbm">Thư viện DBM</param>
        /// <param name="ItemID">Mã vật phẩm</param>
        /// <param name="ltItemProperty">Input danh sách thuộc tính động của Vật phẩm</param>
        /// <param name="outItemProperties">Output danh sách thuộc tính động của Vật phẩm</param>
        /// <returns></returns>
        private string DoInsertUpdate_ItemProperty(DBM dbm, long ItemID, List<ItemProperty> ltItemProperty, out List<ItemProperty> outItemProperties, bool isUpdate = false)
        {
            outItemProperties = new List<ItemProperty>();
            string msg = "";

            //nếu cập nhật Vật phẩm thì xóa các thuộc tính động trước đó
            if (isUpdate)
            {
                msg = ItemProperty.DeletePropertyByItemID(dbm, ItemID);
                if (msg.Length > 0) return msg;
            }

            foreach (var item in ltItemProperty)
            {
                ItemProperty itemProperty = new ItemProperty
                {
                    //nếu cập nhật Vật phẩm thì gán lại ItemPropertyID = 0
                    ItemPropertyID = isUpdate ? 0 : item.ItemPropertyID,
                    ItemTypePropertyID = item.ItemTypePropertyID,
                    ItemID = ItemID,
                    ItemPropertyName = item.ItemPropertyName,
                    Value = item.Value,
                };

                msg = itemProperty.InsertUpdate(dbm, out ItemProperty itemPropertyOut);
                if (msg.Length > 0) return msg;

                outItemProperties.Add(itemPropertyOut);
            }

            return msg;
        }
        [HttpGet]
        public Result GetOne(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetOne(ObjectGuid, out Item outItem);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return outItem.ToResultOk();
        }
        private string DoGetOne(Guid ObjectGuid, out Item outItem)
        {
            outItem = null;

            string msg = DataValidator.Validate(new { ObjectGuid }).ToErrorMessage();
            if (msg.Length > 0) return msg;

            msg = Item.GetOneByGuid(ObjectGuid, out outItem);
            if (msg.Length > 0) return msg;
            if (outItem == null) return ("Không tồn tại vật phẩm có ObjectGuid = " + ObjectGuid);

            msg = ItemProperty.GetListByIdItem(outItem.ItemID, out List<ItemProperty> outLtItemProperty);
            if (msg.Length > 0) return msg;
            outItem.ListItemProperty = outLtItemProperty;

            return msg;
        }

        [HttpGet]
        public Result ViewDetail(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoViewDetail(ObjectGuid, out ItemViewDetail outItemViewDetail);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return outItemViewDetail.ToResultOk();
        }
        private string DoViewDetail(Guid ObjectGuid, out ItemViewDetail outItemViewDetail)
        {
            outItemViewDetail = null;

            string msg = DataValidator.Validate(new { ObjectGuid }).ToErrorMessage();
            if (msg.Length > 0) return msg;

            msg = Item.GetOneViewDetailByGuid(ObjectGuid, UserToken.AccountID, out outItemViewDetail);
            if (msg.Length > 0) return msg;
            if (outItemViewDetail == null) return ("Không tồn tại vật phẩm có ObjectGuid = " + ObjectGuid);

            msg = ItemProperty.GetListByIdItem(outItemViewDetail.ItemID, out List<ItemProperty> outLtItemProperty);
            if (msg.Length > 0) return msg;
            outItemViewDetail.ListItemProperty = outLtItemProperty;

            return msg;
        }

        [HttpGet]
        public Result GetListItemByObjectGuids(string ObjectGuids)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListItemByObjectGuids(ObjectGuids, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return dt.ToResultOk();
        }
        private string DoGetListItemByObjectGuids(string ObjectGuids, out DataTable dt)
        {
            dt = null;

            string msg = Item.GetItemIDsByObjectGuids(ObjectGuids, out string itemIDs);
            if (msg.Length > 0) return msg;

            msg = Item.GetItemByItemIDs(itemIDs, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return "";
        }


        [HttpGet]
        public Result GetListItemHanding(string ObjectGuids)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListItemHanding(ObjectGuids, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return dt.ToResultOk();
        }
        private string DoGetListItemHanding(string ObjectGuids, out DataTable dt)
        {
            dt = null;

            string msg = Item.GetItemIDsByObjectGuids(ObjectGuids, out string itemIDs);
            if (msg.Length > 0) return msg;

            msg = Item.GetListItemHandling(itemIDs, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = UpdateStatusByID(data, Constants.ItemStatus.DX);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return msg.ToResultOk();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// 
        [HttpPost]
        public Result Restore([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg =
            msg = UpdateStatusByID(data, Constants.ItemStatus.MT);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return msg.ToResultOk();
        }
        private string UpdateStatusByID([FromBody] JObject data, int StatusID)
        {
            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVP, Role.ROLE_QLVP_CRUD);
            if (msg.Length > 0) return msg;

            msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;

            msg = Item.GetOneByGuid(ObjectGuid, out Item outItem);
            if (msg.Length > 0) return msg;
            if (outItem == null) return ("Không tồn tại Vật phẩm với Object Guid: " + ObjectGuid).ToMessageForUser();

            msg = UpdateStatusID(outItem, StatusID);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string UpdateStatusID(Item item, int statusIDUpdate)
        {
            string logContent = "";

            string msg = ItemStatus.GetOne(item.ItemStatusID, out ItemStatus outItemStatus);
            if (msg.Length > 0) return msg;
            if (outItemStatus == null) return ("Không tồn tại trạng thái Vật phẩm: " + outItemStatus.ItemStatusName).ToMessageForUser();

            msg = AccountUser.GetOneByUserID(UserToken.UserID, out AccountUser outAccountUser);
            if (msg.Length > 0) return msg;
            if (outAccountUser == null) return ("Không tồn tại người dùng với UserID =  " + UserToken.UserID).ToMessageForUser();

            switch (statusIDUpdate)
            {
                case Constants.ItemStatus.DX:
                    msg = DoDelete(item, outItemStatus);
                    if (msg.Length > 0) return msg;
                    logContent = outAccountUser.UserName + " xóa Vật phẩm";
                    break;
                case Constants.ItemStatus.MT:
                    msg = DoRestore(item, outItemStatus);
                    if (msg.Length > 0) return msg;

                    logContent = outAccountUser.UserName + " khôi phục Vật phẩm";
                    break;
            }

            return UpdateStatusID_SaveToDB(item, statusIDUpdate, logContent);
        }
        private string DoDelete(Item item, ItemStatus itemStatus)
        {
            if (item.UserIDCreate != UserToken.UserID) return "Bạn không phải là Người tạo vật phẩm. Không thể xóa Vật phẩm này".ToMessageForUser();

            if (item.ItemStatusID != Constants.ItemStatus.MT) return ("Bạn không được phép xóa Vật phẩm có trạng thái " + itemStatus.ItemStatusName).ToMessageForUser();

            return "";
        }
        private string DoRestore(Item item, ItemStatus itemStatus)
        {
            if (item.UserIDCreate != UserToken.UserID) return "Bạn không phải là Người tạo vật phẩm. Không thể xóa Vật phẩm này".ToMessageForUser();

            string msg = AssetType.GetOneByAssetTypeID(item.ItemTypeID, item.AccountID, out AssetType outAssetType);
            if (msg.Length > 0) return msg;
            if (outAssetType == null) return "Loại vật phẩm" + outAssetType.AssetTypeName + " có trạng thái Không sử dụng. Bạn không được khôi phục vật phẩm này".ToMessageForUser();

            if (item.ItemStatusID != Constants.ItemStatus.DX) return ("Bạn không được phép khôi phục Vật phẩm có trạng thái " + itemStatus.ItemStatusName).ToMessageForUser();

            return msg;
        }
        private string UpdateStatusID_SaveToDB(Item item, int StatusID, string logContent)
        {
            string msg = Item.UpdateStatusItem(new DBM(), item.ItemID, StatusID);
            if (msg.Length > 0) return msg;

            return Log.WriteHistoryLog(logContent, item.ObjectGuid, UserToken.AccountID, Common.GetClientIpAddress(Request));
        }

        /// <summary>
        /// Gọi ý tìm kiếm theo keyword
        /// </summary>
        /// <param name="TextSearch">input text search</param>
        /// <returns>List Item</returns>
        [HttpGet]
        public Result GetSuggestSearch(string TextSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetSuggestSearch(TextSearch, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return dt.ToResultOk();
        }
        private string DoGetSuggestSearch(string TextSearch, out DataTable dt)
        {
            dt = new DataTable();

            string msg = Item.GetSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }

        /// <summary>
        /// Tìm kiếm đơn giản
        /// </summary>
        /// <param name="itemEasySearch">input easy search</param>
        /// <returns>danh sách Vật phẩm</returns>
        [HttpPost]
        public Result GetListEasySearch([FromBody] ItemEasySearch itemEasySearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListEasySearch(itemEasySearch, out int total, out List<ItemSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoGetListEasySearch([FromBody] ItemEasySearch itemEasySearch, out int Total, out List<ItemSearchResult> lt)
        {
            lt = null;
            Total = 0;

            string msg = DoGetListEasySearch_GetItemSearch(itemEasySearch, out ItemSearch itemSearch);
            if (msg.Length > 0) return msg;

            msg = DoGetList(itemSearch, out lt, out Total);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string DoGetListEasySearch_GetItemSearch(ItemEasySearch itemEasySearch, out ItemSearch itemSearch)
        {
            itemSearch = new ItemSearch();

            itemSearch.CurrentPage = itemEasySearch.CurrentPage;
            itemSearch.PageSize = itemEasySearch.PageSize;
            itemSearch.TextSearch = itemEasySearch.TextSearch;

            return "";
        }
        [HttpPost]
        public Result GetListAdvancedSearch([FromBody] ItemSearch itemSearch)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = DoGetListAdvancedSearch(itemSearch, out int total, out List<ItemSearchResult> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch([FromBody] ItemSearch itemSearch, out int Total, out List<ItemSearchResult> lt)
        {
            lt = null;
            Total = 0;
            itemSearch.CategorySearch = ConmonConstants.ADVANCED_SEARCH;//tìm kiếm nâng cao

            string msg = DoGetList(itemSearch, out lt, out Total);
            if (msg.Length > 0) return msg;

            InsertSPVAdvancedSearch(itemSearch);

            return "";
        }
        private string DoGetList(ItemSearch itemSearch, out List<ItemSearchResult> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                if (itemSearch == null) return "Tham số không được phép null".ToMessageForUser();

                itemSearch.AccountID = UserToken.AccountID;
                itemSearch.UserID = UserToken.UserID;

                string msg = DataValidator.Validate(itemSearch).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                if (itemSearch.ItemDateTo != new DateTime(1900, 1, 1)) itemSearch.ItemDateTo = itemSearch.ItemDateTo.Date.AddDays(1);

                msg = Item.GetListPaging(itemSearch, out lt, out totalSearch);
                if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    ButtonShowItem button;

                    Item it = new Item
                    {
                        ItemID = item.ItemID,
                        ObjectGuid = item.ObjectGuid,
                        ItemStatusID = item.ItemStatusID,
                        ItemStatusName = item.ItemStatusName,
                        ItemImagePath = item.ItemImagePath,
                        UserIDCreate = item.UserIDCreate,
                        UserIDApprove = item.UserIDApprove,
                        WarningDate = item.WarningDate,
                        CreateDate = item.CreateDate
                    };
                    msg = DoGetListButtonFuction(it, UserToken.UserID, out button);
                    if (msg.Length > 0) return msg;

                    item.ItemStatusName = it.ItemStatusName;
                    item.ItemImagePath = it.ItemImagePath;
                    item.ItemStatusID = it.ItemStatusID;
                    item.ButtonShow = button;
                }

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private string DoGetListButtonFuction(Item item, int UserIDLogin, out ButtonShowItem button)
        {
            button = new ButtonShowItem();

            int status = item.ItemStatusID;
            string msg = "";

            if (UserIDLogin == item.UserIDCreate)
            {
                if (status == Constants.StatusItem.ĐX) { button.Restore = true; }
                if (status == Constants.StatusItem.CD) { button.CancelSendApprove = true; }
                if (status == Constants.StatusItem.ĐD_TK) { button.Edit = true; button.Inventory = true; }
                if (status == Constants.StatusItem.TC) { button.Edit = true; }
            }

            msg = Role.Check(UserToken.UserID, Constants.TabID.QLVP, Role.ROLE_QLVP_CRUD, out bool IsEdit);
            if (msg.Length > 0) return msg;
            if (IsEdit)
            {
                if (status == Constants.StatusItem.MT)
                {
                    button.Delete = true;
                    button.Edit = true;
                    button.SendApprove = true;
                }
                if (status == Constants.StatusItem.ĐD_TK) button.Edit = true;
            }
            msg = Role.Check(UserToken.UserID, Constants.TabID.QLVP, Role.ROLE_QLVP_DUYET, out bool IsApprove);
            if (msg.Length > 0) return msg;
            if (IsApprove && status == Constants.StatusItem.CD) { button.Reject = true; button.Approve = true; }

            button.ViewHistory = true;

            return "";
        }
        private void InsertSPVAdvancedSearch(ItemSearch itemSearch)
        {
            SPV.InsertSPVSearchItem(UserToken.UserID, Constants.PageGUID.VAT_PHAM, new
            {
                itemSearch.AccountID,
                itemSearch.UserID,
                itemSearch.TextSearch,
                itemSearch.ItemTypeIDs,
                itemSearch.ItemStatusIDs,
                itemSearch.ItemDateFrom,
                itemSearch.ItemDateTo,
                itemSearch.CurrentPage,
                itemSearch.PageSize
            });
        }

        Dictionary<string, string> MappingColumnExcel = new Dictionary<string, string>() {
                {ConmonConstants.NumericalOrder,"STT" },
                {ConmonConstants.TypeItem,"ItemTypeName" },
                {ConmonConstants.NameItem,"ItemName" },
                {ConmonConstants.CodeItem,"ItemCode" },
                {ConmonConstants.UnitItem,"ItemUnitName" },
                {ConmonConstants.WarningThreshold,"strWarningThreshold" },
                {ConmonConstants.WarningDate,"strWarningDate" },
                {ConmonConstants.Supplier,"SupplierName" },
            };
        [HttpPost]
        public Result ExportTemplateExcel([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoExportTemplateExcel(data, out string urlFile);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return urlFile.ToResultOk();
        }
        private string DoExportTemplateExcel([FromBody] JObject data, out string urlFile)
        {
            urlFile = "";

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVP, Role.ROLE_QLVP_IsVisitPage);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("AssetTypeID", out int AssetTypeID);
            if (msg.Length > 0) return msg;
            if (AssetTypeID <= 0) return ("Bạn chưa chọn Loại tài sản").ToMessageForUser();

            msg = AssetType.GetOneByAssetTypeID(AssetTypeID, UserToken.AccountID, out AssetType outAssetType);
            if (msg.Length > 0) return msg;
            if (outAssetType == null) return ("Loại tài sản bạn chọn không tồn tại").ToMessageForUser();

            msg = AssetTypeProperty.GetListByAssetTypeID(AssetTypeID, out List<AssetTypeProperty> lt);
            if (msg.Length > 0) return msg;

            DataTable dt = new DataTable();
            foreach (var item in MappingColumnExcel) dt.Columns.Add(item.Key, typeof(System.String));

            foreach (var item in lt) dt.Columns.Add(item.AssetTypePropertyName, typeof(System.String));

            DataRow dr = dt.NewRow();
            dr[ConmonConstants.NumericalOrder] = 1;
            dr[ConmonConstants.TypeItem] = outAssetType.AssetTypeName;
            dr[ConmonConstants.NameItem] = outAssetType.AssetTypeName + "1";
            dr[ConmonConstants.CodeItem] = "Hệ thống tự sinh";
            dr[ConmonConstants.UnitItem] = "Cái";
            dr[ConmonConstants.WarningThreshold] = "200";
            dr[ConmonConstants.WarningDate] = "30";
            dr[ConmonConstants.Supplier] = "FPT";
            dt.Rows.Add(dr);

            msg = ExporExcelTemplateAsset(outAssetType.AssetTypeName, dt, out urlFile);
            if (msg.Length > 0) return msg;

            return msg;
        }
        public string ExporExcelTemplateAsset(string nameSheet, DataTable dt, out string urlFile)
        {
            urlFile = "";
            try
            {
                string msg = BSS.Common.GetSetting("FolderFileExport", out string FolderFileExport);
                if (msg.Length > 0) return msg;

                urlFile = FolderFileExport + "/" + "TemplateImportItem_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

                using (ExcelPackage pack = new ExcelPackage())
                {
                    ExcelWorksheet ws = pack.Workbook.Worksheets.Add(nameSheet);
                    ws.Cells["A1"].LoadFromDataTable(dt, true);
                    ws.Column(1).Width = 5;
                    using (var range = ws.Cells[1, 1, 1, dt.Columns.Count])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Font.Size = 13;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.SteelBlue);
                        range.Style.Font.Color.SetColor(Color.White);
                    }
                    using (var range = ws.Cells[2, 1, 35, dt.Columns.Count])
                    {
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Font.Size = 12;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.White);
                        range.Style.Font.Color.SetColor(Color.DimGray);
                    }
                    for (int i = 2; i < dt.Columns.Count + 1; i++)
                        ws.Column(i).Width = 25;
                    pack.SaveAs(new FileInfo(HttpContext.Current.Server.MapPath(urlFile)));
                }
                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Api thêm mới sản phẩm bằng excel
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public Result ImportExcel()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLVP, Role.ROLE_QLVP_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoImportExcel(out List<ItemImportExcel> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
        /// <summary>
        /// Hàm xử lý thêm mới sản phẩm bằng excel
        /// </summary>
        /// <param name="lt">Danh sánh trả về các Vật được trả thêm mới được</param>
        /// <returns></returns>
        private string DoImportExcel(out List<ItemImportExcel> lt)
        {
            string msg = "";
            lt = null;
            try
            {
                msg = DoImportExcel_GetDataTable(out int ItemTypeID, out DataTable dt);//comit
                if (msg.Length > 0) return msg;

                msg = DoImportExcel_ConvertDataTableToList(ItemTypeID, dt, out lt, out List<AssetTypeProperty> outLtAssetTypeProperty);
                if (msg.Length > 0) return msg;

                msg = DoImportExcel_Validate(ItemTypeID, lt, outLtAssetTypeProperty);
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = DoImportExcel_ConvertToObject(lt, ItemTypeID, out List<Item> ltItem);
                if (msg.Length > 0) return msg;

                DBM dbm = new DBM();
                dbm.BeginTransac();

                try
                {
                    msg = DoImportExcel_ObjectToDB(dbm, ltItem);
                    if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
                }
                catch (Exception ex)
                {
                    dbm.RollBackTransac();
                    return ex.ToString() + " at Item DoImportExcel";
                }
                dbm.CommitTransac();

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        /// <summary>
        /// Hàm mapping dữ liệu được được gửi đi thông qua request
        /// </summary>
        /// <param name="ItemTypeID">Loại sản phẩm được import</param>
        /// <param name="dt">Bảng danh sách sản phẩm được trả về</param>
        /// <returns></returns>
        private string DoImportExcel_GetDataTable(out int ItemTypeID, out DataTable dt)
        {
            string msg = ""; dt = null; ItemTypeID = 0;

            var httpContext = HttpContext.Current;
            if (httpContext.Request.Files.Count == 0) return "Bạn chưa chọn File".ToMessageForUser();

            msg = WebHelper.GetStringFromRequestForm(HttpContext.Current, "ItemTypeID", out string itemtype_id);
            if (msg.Length > 0) return msg;
            ItemTypeID = itemtype_id.ToNumber(-1);
            if (ItemTypeID < 0) return ("Giá trị Loại vật phẩm bạn nhập không hợp lệ: " + itemtype_id).ToMessageForUser();

            msg = BSS.Common.GetSetting("FolderFileUpload", out string FolderFileUpload);
            if (msg.Length > 0) return msg;

            string pathFileUpload = FolderFileUpload;
            string pathFile;
            if (!Directory.Exists(pathFileUpload)) Directory.CreateDirectory(pathFileUpload);
            try
            {
                HttpPostedFile httpPostedFile = httpContext.Request.Files[0];
                pathFile = pathFileUpload + "/" + httpPostedFile.FileName;
                httpPostedFile.SaveAs(pathFile);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            msg = BSS.Common.GetDataTableFromExcelFile(pathFile, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        /// <summary>
        /// Chuyển từ DataTable danh sách Item sang List danh sách Item
        /// </summary>
        /// <param name="AssetTypeID"></param>
        /// <param name="dt"></param>
        /// <param name="lt"></param>
        /// <param name="outLtAssetTypeProperty"></param>
        /// <returns></returns>
        private string DoImportExcel_ConvertDataTableToList(int AssetTypeID, DataTable dt, out List<ItemImportExcel> lt, out List<AssetTypeProperty> outLtAssetTypeProperty)
        {
            string msg = ""; lt = null; outLtAssetTypeProperty = null;

            DataTable dt2 = new DataTable();
            foreach (var columnName in dt.Rows[0].ItemArray) dt2.Columns.Add(columnName.ToString());
            for (int i = 1; i < dt.Rows.Count; i++)
            {
                dt2.Rows.Add(dt.Rows[i].ItemArray);
            }

            msg = AssetTypeProperty.GetListByAssetTypeID(AssetTypeID, out outLtAssetTypeProperty);
            if (msg.Length > 0) return msg;

            msg = DoImportExcel_ConvertDataTableToList_Validate(outLtAssetTypeProperty, dt2);
            if (msg.Length > 0) return msg;

            msg = DoImportExcel_ConvertDataTableToList_SetValue(outLtAssetTypeProperty, dt2, out lt);
            if (msg.Length > 0) return msg;

            msg = DoImportExcel_ConvertDataTableToList_RemoveItemEmpty(lt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        /// <summary>
        /// Kiểm tra các cột dữ liệu trong DataTable
        /// </summary>
        /// <param name="ltAssetTypeProperty"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        private string DoImportExcel_ConvertDataTableToList_Validate(List<AssetTypeProperty> ltAssetTypeProperty, DataTable dt)
        {
            string columnNames = "";
            foreach (var item in MappingColumnExcel)
                if (columnNames.Length == 0) columnNames = item.Key;
                else columnNames += ";" + item.Key;

            foreach (var item in ltAssetTypeProperty) columnNames += ";" + item.AssetTypePropertyName;

            string columnNamesNoHas = "";
            foreach (var columnName in columnNames.Split(';'))
                if (!dt.Columns.Contains(columnName))
                    if (columnNamesNoHas == "") columnNamesNoHas = columnName;
                    else columnNamesNoHas += ", " + columnName;
            if (columnNamesNoHas.Length > 0) return ("File excel thiếu cột " + columnNamesNoHas).ToMessageForUser();

            return "";
        }
        /// <summary>
        /// Chuyển từ DataTable danh sách Item sang List danh sách ItemImportExcel
        /// </summary>
        /// <param name="ltAssetTypeProperty"></param>
        /// <param name="dt"></param>
        /// <param name="lt"></param>
        /// <returns></returns>
        private string DoImportExcel_ConvertDataTableToList_SetValue(List<AssetTypeProperty> ltAssetTypeProperty, DataTable dt, out List<ItemImportExcel> lt)
        {
            foreach (DataColumn column in dt.Columns)
                foreach (var item in MappingColumnExcel)
                    if (item.Key == column.ColumnName) dt.Columns[column.ColumnName].ColumnName = item.Value;

            string msg = BSS.Convertor.DataTableToList<ItemImportExcel>(dt, out lt);
            if (msg.Length > 0) return msg;

            for (int i = 0; i < lt.Count; i++)
            {
                List<ItemProperty> ltItemTypeProperty = new List<ItemProperty>();
                foreach (var item in ltAssetTypeProperty)
                {
                    string value = dt.Rows[i][item.AssetTypePropertyName].ToString();
                    ItemProperty itemProperty = new ItemProperty
                    {
                        ItemPropertyName = item.AssetTypePropertyName,
                        ItemTypePropertyID = item.AssetTypePropertyID,
                        Value = value
                    };
                    ltItemTypeProperty.Add(itemProperty);
                };
                lt[i].LtItemProperty = ltItemTypeProperty;
            }

            return msg;
        }
        /// <summary>
        /// Xóa các bản ghi ko có dữ liệu trong List ItemImportExcel 
        /// </summary>
        /// <param name="lt"></param>
        /// <returns></returns>
        private string DoImportExcel_ConvertDataTableToList_RemoveItemEmpty(List<ItemImportExcel> lt)
        {
            List<ItemImportExcel> ltEmpty = new List<ItemImportExcel>();
            foreach (var item in lt) if (string.IsNullOrWhiteSpace(item.STT) && string.IsNullOrWhiteSpace(item.ItemTypeName) && string.IsNullOrWhiteSpace(item.ItemCode) && string.IsNullOrWhiteSpace(item.strWarningThreshold) &&
                    string.IsNullOrWhiteSpace(item.SupplierName) && string.IsNullOrWhiteSpace(item.strExpiry) && string.IsNullOrWhiteSpace(item.SupplierName)) ltEmpty.Add(item);
            foreach (var item in ltEmpty) lt.Remove(item);

            return "";
        }
        /// <summary>
        /// Validate List ItemImportExcel
        /// </summary>
        /// <param name="lt"></param>
        /// <param name="LtAssetTypeProperty"></param>
        /// <param name="AssetTypeID"></param>
        /// <returns></returns>
        private string DoImportExcel_Validate(int AssetTypeID, List<ItemImportExcel> lt, List<AssetTypeProperty> LtAssetTypeProperty)
        {
            string msgError = ""; int i = 1;

            string msg = Organization.GetListBySupplier(UserToken.AccountID, out List<Organization> ltorganization);
            if (msg.Length > 0) return msg;

            msg = ItemUnit.GetList(out List<ItemUnit> outLtItemUnit);
            if (msg.Length > 0) return msg;

            foreach (var item in lt)
            {
                List<string> ltError = new List<string>();

                string ItemName = item.ItemName == null ? "" : item.ItemName.Trim().Replace("/", "\\");
                if (ItemName.Length < 2) ltError.Add("Bạn phải nhập vào Tên Vật phẩm tối thiểu 2 ký tự");
                if (ItemName.Length > 255) ltError.Add("Bạn chỉ được nhập vào Tên Vật phẩm tối đa 255 ký tự");

                string SupplierName = item.SupplierName == null ? "" : item.SupplierName.Trim().Replace("/", "\\");
                if (SupplierName.Length > 0)
                {
                    var vSupplierName = ltorganization.Where(v => v.OrganizationName != null && v.OrganizationName.ToLower() == SupplierName.ToLower());
                    if (vSupplierName.Count() == 0) ltError.Add("Không tồn tại Nhà cung cấp " + SupplierName);
                    else item.SupplierID = vSupplierName.First().OrganizationID;
                }

                string ItemUnitName = item.ItemUnitName == null ? "" : item.ItemUnitName.Trim().Replace("/", "\\");
                if (ItemUnitName.Length > 0)
                {
                    var itemUnit = outLtItemUnit.Where(v => v.ItemUnitName != null && v.ItemUnitName.ToLower() == ItemUnitName.ToLower()).FirstOrDefault();
                    if (itemUnit == null) ltError.Add("Không tồn tại Đơn vị tính đã nhập" + ItemUnitName);
                    else
                    {
                        if (!itemUnit.Active) ltError.Add("Đơn vị tính chưa được kích hoạt" + ItemUnitName);
                        else item.ItemUnitStatusID = itemUnit.ItemUnitID;
                    }
                }
                else ltError.Add("Tên Đơn vị tính không được để trống");

                if (!string.IsNullOrEmpty(item.strWarningDate))
                {
                    if (int.TryParse(item.strWarningDate, out int wexp))
                    {
                        if (wexp <= 0 && wexp > 30) ltError.Add("Cảnh báo hết hạn sử dụng lớn hơn 0 và không vượt quá 30");
                        else item.WarningDate = wexp;
                    }
                    else ltError.Add("Hạn sai định dạng Cảnh báo hết hạn sử dụng");
                }

                //kiểm tra Mã vật phẩm tồn tại trong hệ thống hay chưa
                msg = Item.CheckExistItem(item.ItemCode, AssetTypeID, item.ItemName, item.ItemUnitStatusID, UserToken.AccountID, out Item existItem);
                if (msg.Length > 0) return msg;
                //kiểm tra Loại vật phẩm, Tên vật phẩm, Đơn vị tính đã tồn tại trong hệ thống hay chưa
                if (existItem != null)
                {
                    if (existItem.ItemStatusID == Constants.ItemStatus.DX) ltError.Add("Vật phẩm: " + item.ItemName + " này đã bị xóa. Vui lòng kiểm tra hoặc chọn khôi phục lại Vật phẩm");
                    else ltError.Add("Đã tồn tại Vật phẩm: " + item.ItemName + " trong hệ thống");
                }


                if (!string.IsNullOrEmpty(item.strWarningThreshold))
                {
                    if (int.TryParse(item.strWarningThreshold, out int WarningThreshold))
                    {
                        if (WarningThreshold < 1 && item.WarningThreshold > 999) ltError.Add("Ngưỡng cảnh báo số lượng: Phải là số dương nguyên dương và không vượt quá 999");
                        else item.WarningThreshold = WarningThreshold;
                    }
                    else ltError.Add("Hạn sai định dạng Ngưỡng cảnh báo số lượng");
                }

                msg = ItemProperty.ValidateProperty(item.LtItemProperty);
                if (msg.Length > 0) ltError.Add(msg);

                //xóa các cột thuộc tính động trống
                List<ItemProperty> ltRemoveItemProperty = new List<ItemProperty>();

                foreach (var it in item.LtItemProperty)
                    if (string.IsNullOrEmpty(it.Value)) ltRemoveItemProperty.Add(it);

                foreach (var it in item.LtItemProperty) ltRemoveItemProperty.Remove(it);

                if (ltError.Count > 0) msgError += "\n " + "Vật phẩm STT " + i++ + ":\n " + string.Join("\n", ltError) + "\n ";
            }
            if (msgError.Length > 0) return "Dữ liệu file excel không hợp lệ như sau:\n" + msgError;

            return "";
        }
        /// <summary>
        /// Chuyển từ List ItemImportExcel danh sách Item sang List danh sách Item
        /// </summary>
        /// <param name="lt"></param>
        /// <param name="ItemTypeId"></param>
        /// <param name="ltItem"></param>
        /// <returns></returns>
        private string DoImportExcel_ConvertToObject(List<ItemImportExcel> lt, int ItemTypeId, out List<Item> ltItem)
        {
            string msg = "";
            ltItem = new List<Item>();
            foreach (var item in lt)
            {
                Item Item = new Item();

                msg = BSS.Common.CopyObjectPropertyData(item, Item);
                if (msg.Length > 0) return msg;

                Item.ItemTypeID = ItemTypeId;
                Item.UserIDCreate = UserToken.UserID;
                Item.UserIDManager = UserToken.UserID;
                Item.ItemStatusID = Constants.ItemStatus.MT;
                Item.AccountID = UserToken.AccountID;

                Item.ListItemProperty = item.LtItemProperty;

                ltItem.Add(Item);
            }

            return msg;
        }
        /// <summary>
        /// Thêm danh sách vật phẩm import qua excel vào DB
        /// </summary>
        /// <param name="dbm"></param>
        /// <param name="ltItem"></param>
        /// <returns></returns>
        private string DoImportExcel_ObjectToDB(DBM dbm, List<Item> ltItem)
        {
            string msg = "";

            foreach (var item in ltItem)
            {
                msg = item.InsertOrUpdateByExcel(dbm, out Item itemNew);
                if (msg.Length > 0) return msg;

                msg = DoInsertUpdate_ItemProperty(dbm, itemNew.ItemID, item.ListItemProperty, out List<ItemProperty> outItemProperties);
                if (msg.Length > 0) return msg;

                msg = Log.WriteHistoryLog(dbm, "Thêm Vật phẩm bằng file excel", itemNew.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return msg;
        }

        /// <summary>
        /// lấy danh sách trạng thái vật phẩm
        /// </summary>
        /// <returns>object</returns>
        [HttpGet]
        public Result GetListItemStatus()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = ItemStatus.GetAll(out List<ItemStatus> itemStatusList);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return itemStatusList.ToResultOk();
        }

        /// <summary>
        /// thêm mới Vật phẩm
        /// </summary>
        /// <param name="inputItem">object item</param>
        /// <returns>object item</returns>

        /// <summary>
        /// Lấy danh sách đơn vị tính của vật phẩm
        /// </summary>
        /// <returns>object</returns>
        [HttpGet]
        public Result GetListItemUnit()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = ItemUnit.GetList(out List<ItemUnit> ltItemUnit);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return ltItemUnit.ToResultOk();
        }
        /// <summary>
        /// Lấy danh sách vật phẩm theo Loại vật phẩm
        /// </summary>
        /// <param name="ItemTypeId">ID Loại vật phẩm</param>
        /// <returns></returns>
        [HttpGet]
        public Result GetListByItemType(int ItemTypeId)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListByItemType(ItemTypeId, out List<Item> outLtItem);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return outLtItem.ToResultOk();
        }
        private string DoGetListByItemType(int ItemTypeId, out List<Item> outLtItem)
        {
            outLtItem = null;

            if (ItemTypeId != 0)
            {
                string msg = AssetType.GetActiveByAssetTypeID(ItemTypeId, UserToken.AccountID, out AssetType assetType);
                if (msg.Length > 0) return msg;
                if (assetType == null || assetType.AssetTypeGroupID != Constants.AssetTypeGroup.VATPHAM) return "Loại Vật Phẩm không tồn tại".ToMessageForUser();
            }

            return Item.GetListByItemType(ItemTypeId, UserToken.AccountID, out outLtItem);
        }
    }
}
