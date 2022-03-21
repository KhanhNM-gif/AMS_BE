using ASM_API.App_Start.InventoryStore;
using ASM_API.App_Start.Store;
using BSS;
using BSS.DataValidator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;

namespace ASM_API.Controllers
{
    public class InventoryStoreController : Authentication
    {
        [HttpPost]
        public Result InsertOrUpdate(InventoryStore InventoryStore)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertUpdate(UserToken.UserID, InventoryStore, out var o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }

        private string DoInsertUpdate(int UserIDCreate, InventoryStore inventoryStore, out InventoryStore outInventoryStore)
        {
            outInventoryStore = new InventoryStore();

            string msg = DoInsertUpdate_Validate(inventoryStore, out var InventoryStoreInDB);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = SetData(inventoryStore, InventoryStoreInDB);
            if (msg.Length > 0) return msg;

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoInsertUpdate_ObjectToDB(dbm, inventoryStore, out outInventoryStore, UserIDCreate);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at InventoryStore DoInsertUpdate";
            }

            dbm.CommitTransac();

            return msg;
        }

        private string DoInsertUpdate_Validate(InventoryStore dataInput, out InventoryStore outInventoryStore)
        {
            outInventoryStore = null;

            string msg = DataValidator.Validate(new
            {
                dataInput.InventoryStoreID,
                dataInput.InventoryStoreName,
                dataInput.PlaceID,
                dataInput.TransferDirectionID,
                dataInput.UserCreateID,
                dataInput.UserHandingID,
                dataInput.Content,
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (dataInput.UserHandingID == UserToken.UserID) return ("Bạn không được phép chuyển Phiếu kiểm kê cho chính mình").ToMessageForUser();

            if (dataInput.UserHandingID == 0) return "Bạn phải chọn người xử lý".ToMessageForUser();

            msg = Role.Check(dataInput.UserHandingID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_DUYET, out bool IsRole);
            if (msg.Length > 0) return msg;
            if (!IsRole) return "Người xử lý không có quyền duyệt phiếu".ToMessageForUser();

            if (!string.IsNullOrEmpty(dataInput.BatchIDs))
                if (dataInput.BatchIDs.Split(',').Any(x => !long.TryParse(x, out long _))) return " BatchIDs sai định dạng";

            string logmsg = "";
            List<long> duplicates = dataInput.ltInventoryStoreDetail.GroupBy(x => x.ItemID).Where(g => g.Count() > 1).Select(x => x.Key).ToList();
            msg = Item.GetListItemByItemIDs(string.Join(",", duplicates), UserToken.AccountID, out List<Item> outlt);
            if (msg.Length > 0) return msg;
            if (outlt.Any()) return $"Vậy phẩm {string.Join(",", outlt.Select(x => x.ItemName))} đang được chọn nhiều lần trong danh sách Vật phẩm kiểm kê. Vui lòng kiểm tra lại";

            msg = Place.GetOneByPlaceID(dataInput.PlaceID, UserToken.AccountID, out var place);
            if (msg.Length > 0) return msg;
            if (place is null || place.PlaceType != ConmonConstants.TYPE_IS_DEPOT) return "Kho không tồn tại";

            msg = UserManagementPlace.CheckRoleManagement(UserToken.UserID, dataInput.PlaceID, out var userManagementPlace);
            if (msg.Length > 0) return msg;
            if (userManagementPlace is null) return "Bạn không có quyền Quản lý kho này".ToMessageForUser();

            msg = NumberItemStore.GetListByPlaceID(dataInput.PlaceID, out List<NumberItemStore> ltNumberItemStore);
            if (msg.Length > 0) return msg;
            var ltExcept = dataInput.ltInventoryStoreDetail.Select(x => x.ItemID).Except(ltNumberItemStore.Select(x => x.ItemID));
            if (!(ltExcept is null) && ltExcept.Any()) return $"Vật phẩm có ID = {string.Join(",", ltExcept)} không nằm trong kho PlaceID={dataInput.PlaceID}";

            if (dataInput.ObjectGuid != Guid.Empty)
            {
                msg = CacheObject.GetInventoryStoreIDbyGUID(dataInput.ObjectGuid, out long InventoryStoreID);
                if (msg.Length > 0) return msg;

                msg = InventoryStore.GetOne(InventoryStoreID, UserToken.AccountID, out outInventoryStore);
                if (msg.Length > 0) return msg;
                if (outInventoryStore.UserCreateID != UserToken.UserID) return $"Bạn không thể sửa Phiếu kiểm kê Vật phẩm của người dùng khác tạo ra".ToMessageForUser();

                if (outInventoryStore.StatusID != Constants.StatusPKKVP.TL && outInventoryStore.StatusID != Constants.StatusPKKVP.MT) return "Bạn chỉ có thể sửa phiếu ở trạng thái Lưu nháp hoặc Trả lại".ToMessageForUser();
            }

            if (dataInput.ltInventoryStoreDetail is null || !dataInput.ltInventoryStoreDetail.Any()) return "Bạn chưa chọn Vật phẩm kiểm kê".ToMessageForUser();

            foreach (var item in dataInput.ltInventoryStoreDetail)
            {
                msg = DataValidator.Validate(new
                {
                    item.InventoryStoreID,
                    item.ItemID,
                    item.QuantityActual,
                    item.Reason
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Item.GetOneByItemID(item.ItemID, out Item outItem);
                if (msg.Length > 0) return msg;
                if (outItem is null) return ("Không có Vật phẩm có ID = " + item.ItemID).ToMessageForUser();

                item.ItemCode = outItem.ItemCode;
                item.InventoryStoreID = outInventoryStore is null ? 0 : outInventoryStore.InventoryStoreID;
                item.QuantityInStore = ltNumberItemStore.Where(x => x.ItemID == item.ItemID).First().Quality;

                if (item.QuantityActual == 0) logmsg += outItem.ItemName + ", ";
            }
            if (logmsg.Length > 0)
                return ("Bạn chưa nhập vào số lượng cho: " + logmsg).ToMessageForUser();

            return msg;
        }
        private string SetData(InventoryStore inventoryStore, InventoryStore inventoryStoreInDB)
        {
            string msg;

            inventoryStore.UserCreateID = UserToken.UserID;
            inventoryStore.AccountID = UserToken.AccountID;
            inventoryStore.StatusID = inventoryStore.IsSendApprove ? Constants.StatusPKKVP.CXL : Constants.StatusPKKVP.MT;

            if (inventoryStoreInDB == null)
            {
                msg = InventoryStore.GetTotalByDateCode(DateTime.Now.Date, out int Total);
                if (msg.Length > 0) return msg;
                inventoryStore.InventoryStoreCode = "PKKVP_" + DateTime.Now.ToString("yyMMdd") + (Total + 1);
            }
            else
            {
                inventoryStore.InventoryStoreID = inventoryStoreInDB.InventoryStoreID;
                inventoryStore.InventoryStoreCode = inventoryStoreInDB.InventoryStoreCode;

                msg = inventoryStore.SetInfoChangeRequest(inventoryStoreInDB);
                if (msg.Length > 0) return msg;
            }

            return string.Empty;
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, InventoryStore inventoryStore, out InventoryStore outInventoryStore, int UserIDCreate)
        {
            string msg = inventoryStore.InsertUpdate(dbm, out outInventoryStore);
            if (msg.Length > 0) return msg;

            if (inventoryStore.IsSendApprove)
            {
                TransferHandlingLog log = new TransferHandlingLog
                {
                    ObjectID = outInventoryStore.InventoryStoreID,
                    ObjectTypeID = Constants.TransferHandling.PKKVP,
                    UserIDHandling = outInventoryStore.UserCreateID,
                    Comment = "Chuyển xử lý Phiếu kiểm kê Vật Phẩm",
                    TransferDirectionID = inventoryStore.TransferDirectionID
                };
                msg = log.InsertUpdate(dbm, out TransferHandlingLog _);
                if (msg.Length > 0) return msg;
            }

            msg = DoInsertUpdate_InventoryStoreDetail(dbm, outInventoryStore.InventoryStoreID, inventoryStore.ltInventoryStoreDetail, out List<InventoryStoreDetail> outInventoryStoreDetails);
            if (msg.Length > 0) return msg;
            outInventoryStore.ltInventoryStoreDetail = outInventoryStoreDetails;


            if (inventoryStore.InventoryStoreID == 0)
                msg = Log.WriteHistoryLog(dbm, $"Tạo Phiếu kiểm kê Vật phẩm: <b>{UserToken.UserName}</b> tạo mới Phiếu kiểm kê Vật phẩm. Trạng thái Phiếu kiểm kê là <b>Lưu nháp</b>", outInventoryStore.ObjectGuid, UserToken.UserID);
            else if (!string.IsNullOrEmpty(inventoryStore.GetInfoChangeRequest()))
                msg = Log.WriteHistoryLog(dbm, $"Cập nhật: {inventoryStore.GetInfoChangeRequest()}", outInventoryStore.ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            if (inventoryStore.IsSendApprove)
            {
                msg = Log.WriteHistoryLog(dbm, $"Chuyển xử lý Phiếu kiểm kê Vật phẩm: <b>{UserToken.UserName}</b> chuyển xử lý Phiếu kiểm kê Vật phẩm cho { AccountUser.GetUserNameByUserID(outInventoryStore.UserHandingID)}. Trạng thái Phiếu kiểm kê là <b>Chờ xử lý</b>"
                    , outInventoryStore.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }

            return msg;
        }
        private string DoInsertUpdate_InventoryStoreDetail(DBM dbm, long ID, List<InventoryStoreDetail> inventoryStoreDetail, out List<InventoryStoreDetail> outInventoryStoreDetail)
        {
            string msg = InventoryStoreDetail.InsertUpdateByDataType(dbm, inventoryStoreDetail, ID, out outInventoryStoreDetail);
            if (msg.Length > 0) return msg;
            return msg;
        }

        [HttpGet]
        public Result GetListImportBatchs(int PlaceID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            //
            //CHECK QUYEN QLKHO
            //

            string msg = DoGetListImportBatchs(PlaceID, out var importBatches);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return importBatches.ToResultOk();
        }
        private string DoGetListImportBatchs(int PlaceID, out List<ImportBatch> importBatches)
        {
            importBatches = null;

            string msg = Place.GetOneByPlaceID(PlaceID, UserToken.AccountID, out var place);
            if (msg.Length > 0) return msg;
            if (place is null || place.PlaceType != ConmonConstants.TYPE_IS_DEPOT) return "Kho không tồn tại";

            msg = ImportBatch.GetListByPlaceID(PlaceID, out importBatches);
            if (msg.Length > 0) return msg;

            return string.Empty;
        }

        [HttpGet]
        public Result GetListTypeItemInStore(int PlaceID, string BatchIDs = "")
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListTypeItemInstore(PlaceID, BatchIDs, out var outListItemTypes);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return outListItemTypes.ToResultOk();
        }
        private string DoGetListTypeItemInstore(int PlaceID, string BatchIDs, out List<AssetType> outListItemTypes)
        {
            outListItemTypes = null;

            if (!string.IsNullOrEmpty(BatchIDs))
                if (BatchIDs.Split(',').Any(x => !long.TryParse(x, out long _))) return "BatchIDs sai định dạng";

            string msg = AssetType.GetListByPlaceID(UserToken.AccountID, BatchIDs, PlaceID, Constants.AssetTypeGroup.VATPHAM, out outListItemTypes);
            if (msg.Length > 0) return msg;

            return string.Empty;
        }

        [HttpGet]
        public Result GetListItemInStore(int PlaceID, string ItemTypes = "", string BatchIDs = "")
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListItemInStore(ItemTypes, BatchIDs, PlaceID, out var outListItemTypes);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return outListItemTypes.ToResultOk();
        }
        private string DoGetListItemInStore(string ItemTypes, string BatchIDs, int PlaceID, out List<InventoryStoreDetailSearch> outListItemTypes)
        {
            outListItemTypes = null;

            if (!string.IsNullOrEmpty(ItemTypes))
                if (ItemTypes.Split(',').Any(x => !long.TryParse(x, out long _))) return "ItemTypes sai định dạng";

            if (!string.IsNullOrEmpty(BatchIDs))
                if (BatchIDs.Split(',').Any(x => !long.TryParse(x, out long _))) return "BatchIDs sai định dạng";

            string msg = InventoryStoreDetailSearch.GetListItemInStore(UserToken.AccountID, UserToken.UserID, ItemTypes, BatchIDs, PlaceID, out outListItemTypes);
            if (msg.Length > 0) return msg;

            return string.Empty;
        }

        [HttpGet]
        public Result GetSuggestSearch(string TextSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            DataTable dt;
            string msg = DoGetSuggestSearch(TextSearch, out dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return dt.ToResultOk();
        }
        private string DoGetSuggestSearch(string TextSearch, out DataTable dt)
        {
            string msg = InventoryStore.GetSuggestSearch(TextSearch, UserToken.UserID, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        [HttpPost]
        public Result GetListEasySearch(InventoryStoreEasySearch inventoryStoreEasySearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKTS, Role.ROLE_QLKKTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDoGetListEasySearch(UserToken.UserID, inventoryStoreEasySearch, out int total, out List<InventoryStoreSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoDoGetListEasySearch(int UserID, InventoryStoreEasySearch inventoryStoreEasySearch, out int total, out List<InventoryStoreSearchResult> lt)
        {
            lt = null;
            total = 0;

            string msg = DoGetListEasySearch_GetInventoryStoreSearch(inventoryStoreEasySearch, out InventoryStoreSearch outInventoryStoreSearch);
            if (msg.Length > 0) return msg;

            outInventoryStoreSearch.PageSize = inventoryStoreEasySearch.PageSize;
            outInventoryStoreSearch.CurrentPage = inventoryStoreEasySearch.CurrentPage;
            outInventoryStoreSearch.isEasySearch = true;
            //outInventoryStoreSearch.StatusIDs = "1,2,3";// chỉ hiện trạng thái MT,CD,DD trên ds easy search

            msg = DoGetList(outInventoryStoreSearch, out lt, out total);
            return msg;

        }
        private string DoGetListEasySearch_GetInventoryStoreSearch(InventoryStoreEasySearch InventoryStoreEasySearch, out InventoryStoreSearch ms)
        {
            ms = new InventoryStoreSearch();

            ms.TextSearch = InventoryStoreEasySearch.TextSearch;
            ms.CurrentPage = InventoryStoreEasySearch.CurrentPage;
            ms.PageSize = InventoryStoreEasySearch.PageSize;

            if (InventoryStoreEasySearch.ObjectCategory == 1) ms.InventoryStoreID = InventoryStoreEasySearch.ObjectID.ToNumber(0);
            if (InventoryStoreEasySearch.ObjectCategory == 2) ms.PlaceIDs = InventoryStoreEasySearch.ObjectID.ToString();
            if (InventoryStoreEasySearch.ObjectCategory == 3) ms.BatchID = InventoryStoreEasySearch.ObjectID.ToNumber(0);
            if (InventoryStoreEasySearch.ObjectCategory == 4) ms.UserCreateIDs = InventoryStoreEasySearch.ObjectID.ToString();
            if (InventoryStoreEasySearch.ObjectCategory == 5) ms.UserIDHandings = InventoryStoreEasySearch.ObjectID.ToString();

            if (InventoryStoreEasySearch.ObjectCategory > 0) ms.TextSearch = "";

            return "";
        }
        private string DoGetList(InventoryStoreSearch formSearch, out List<InventoryStoreSearchResult> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                formSearch.AccountID = UserToken.AccountID;
                formSearch.UserID = UserToken.UserID;

                string msg = InventoryStoreSearchResult.GetListSearch(formSearch, out lt, out totalSearch);
                if (msg.Length > 0) return msg;

                msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_DUYET, out bool IsRoleApprove);
                if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    msg = DoGetListButtonFuction(item, UserToken.UserID, IsRoleApprove, out ButtonShowPKKVP b);
                    if (msg.Length > 0) return msg;

                    item.ButtonShow = b;
                }

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private string DoGetListButtonFuction(InventoryStoreSearchResult PDX, int UserIDLogin, bool IsRoleApprove, out ButtonShowPKKVP b)
        {
            b = new ButtonShowPKKVP();

            int s = PDX.StatusID;

            if (UserIDLogin == PDX.UserCreateID)
            {
                if (s == Constants.StatusPKKVP.MT)
                {
                    b.Delete = true;
                    b.Edit = true;
                }

                if (s == Constants.StatusPKKVP.DX) b.Restore = true;

                if (s == Constants.StatusPKKVP.TL) { b.Edit = true; b.Delete = true; }
            }

            if (UserIDLogin == PDX.UserHandingID && s == Constants.StatusPKKVP.CXL && IsRoleApprove) b.Approved = true;

            b.ViewHistory = true;
            return "";
        }
        [HttpPost]
        public Result GetListAdvancedSearch(InventoryStoreSearch data)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_IsVisitPage);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                msg = DoGetListAdvancedSearch(UserToken.UserID, data, out int total, out var lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch(int UserID, InventoryStoreSearch storeSearch, out int Total, out List<InventoryStoreSearchResult> lt)
        {
            string msg;

            lt = null; Total = 0;
            storeSearch.isEasySearch = false;

            if (storeSearch.CreateDateCategoryID == 0) storeSearch.DateFrom = storeSearch.DateTo = null;
            else
            {
                msg = StoreItemSearchCategoryDateID.GetDateByCategoryID(storeSearch.CreateDateCategoryID, storeSearch.DateFrom, storeSearch.DateTo, out DateTime fromDate, out DateTime toDate);
                if (msg.Length > 0) return msg;

                storeSearch.DateFrom = fromDate;
                storeSearch.DateTo = toDate;
            }

            msg = DoGetList(storeSearch, out lt, out Total);
            return msg;
        }
        [HttpGet]
        public Result GetOne(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetOne(ObjectGuid, out InventoryStoreModify outInventoryStore);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return outInventoryStore.ToResultOk();
        }
        private string DoGetOne(Guid ObjectGuid, out InventoryStoreModify outInventoryStore)
        {
            outInventoryStore = null;

            string msg = CacheObject.GetInventoryStoreIDbyGUID(ObjectGuid, out long ID);
            if (msg.Length > 0) return msg;

            msg = InventoryStoreModify.GetOne(ID, UserToken.AccountID, out outInventoryStore);
            if (msg.Length > 0) return msg;
            if (outInventoryStore is null) return $"PKK is null ID={ID},AccountID={UserToken.AccountID}";
            if (outInventoryStore.UserCreateID != UserToken.UserID && outInventoryStore.UserCreateID != UserToken.UserID)
                "Bạn không có quyền thực hiện chức năng với bản ghi".ToMessageForUser();

            msg = InventoryStoreDetailView.GetListItemByInventoryStoreID(UserToken.AccountID, ID, out List<InventoryStoreDetailView> ltform);
            if (msg.Length > 0) return msg;
            outInventoryStore.ltInventoryStoreDetailView = ltform;

            return string.Empty;
        }
        [HttpGet]
        public Result ViewDetail(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoViewDetail(ObjectGuid, out InventoryStoreViewDetail o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }
        private string DoViewDetail(Guid ObjectGuid, out InventoryStoreViewDetail ItemproposalOut)
        {
            ItemproposalOut = null;

            string msg = CacheObject.GetInventoryStoreIDbyGUID(ObjectGuid, out long ID);
            if (msg.Length > 0) return msg;

            msg = InventoryStoreViewDetail.GetListSearch(ID, UserToken.AccountID, out ItemproposalOut);
            if (msg.Length > 0) return msg;
            if (ItemproposalOut is null) return $"PKK is null ID={ID},AccountID={UserToken.AccountID}";
            if (ItemproposalOut.UserCreateID != UserToken.UserID && ItemproposalOut.UserCreateID != UserToken.UserID)
                "Bạn không có quyền thực hiện chức năng với bản ghi".ToMessageForUser();

            msg = InventoryStoreDetailView.GetListItemByInventoryStoreID(UserToken.AccountID, ID, out List<InventoryStoreDetailView> ltform);
            if (msg.Length > 0) return msg;
            ItemproposalOut.ltInventoryStoreDetailView = ltform;

            msg = TransferHandlingLogView.GetList(ID, Constants.TransferHandling.PKKVP, ItemproposalOut.TransferDirectionID, out List<TransferHandlingLogView> ltCommentInventoryStore);
            if (msg.Length > 0) return msg;
            ItemproposalOut.ltTransferHandlingLogView = ltCommentInventoryStore;

            return "";
        }

        [HttpPost]
        public Result Return(InputReturnInventoryStore input)//Trả lại phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoTransferHandle(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }

        [HttpGet]
        public Result GetListStatus()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            List<InventoryStoreStatus> lt;
            string msg = InventoryStoreStatus.GetList(out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpPost]
        public Result TransferHandle(InputTransferHandleInventoryStore input) //Chuyển xử lý
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoTransferHandle(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoTransferHandle(InputTransferHandleInventoryStore input)
        {
            string msg = DoTransferHandle_Validate(input, out InventoryStore InventoryStore);
            if (msg.Length > 0) return msg;

            DBM dbm = new DBM();
            dbm.BeginTransac();
            try
            {
                msg = TransferHandleUpdateToDB(dbm, InventoryStore, input);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }

            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return "[DoTransferHandle]" + ex.Message;
            }
            dbm.CommitTransac();


            return string.Empty;
        }

        private string DoTransferHandle_Validate(InputTransferHandleInventoryStore input, out InventoryStore InventoryStore)
        {
            string msg = ValidateHanding(input, out InventoryStore);
            if (msg.Length > 0) return msg;

            if (input is InputReturnInventoryStore) input.UserTransferHandleID = InventoryStore.UserCreateID;

            msg = AccountUser.GetOneByUserID(input.UserTransferHandleID, out var accountUser);
            if (msg.Length > 0) return msg;
            if (accountUser == null) return "Người duyệt không tồn tại".ToMessageForUser();
            input.UserTransferHandleName = accountUser.UserName;

            if (input.UserTransferHandleID == InventoryStore.UserHandingID) return "Bạn đang là người xử lý phiếu. Vui lòng kiểm tra lại".ToMessageForUser();

            msg = Role.Check(input.UserTransferHandleID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_DUYET, out bool IsRole);
            if (msg.Length > 0) return msg;
            if (!IsRole) return "Người chuyển duyệt không có quyền duyệt".ToMessageForUser();

            return string.Empty;
        }
        private string TransferHandleUpdateToDB(DBM dbm, InventoryStore InventoryStore, InputTransferHandleInventoryStore input)
        {
            string msg = InventoryStore.UpdateUserHanding(dbm, InventoryStore.InventoryStoreID, UserToken.AccountID, input.UserTransferHandleID);
            if (msg.Length > 0) return msg;

            TransferHandlingLog log = new TransferHandlingLog
            {
                ObjectID = InventoryStore.InventoryStoreID,
                ObjectTypeID = Constants.TransferHandling.PKKVP,
                UserIDHandling = UserToken.UserID,
                Comment = input.GetReason(),
                TransferDirectionID = InventoryStore.TransferDirectionID
            };
            msg = log.InsertUpdate(dbm, out TransferHandlingLog _);
            if (msg.Length > 0) return msg;

            if (input.UserTransferHandleID == InventoryStore.UserCreateID)
            {
                msg = InventoryStore.UpdateStatusID(dbm, InventoryStore.InventoryStoreID, UserToken.AccountID, input.GetStatusChange());
                if (msg.Length > 0) return msg;
            }

            msg = Log.WriteHistoryLog(dbm, input.GetContent(UserToken.UserName), InventoryStore.ObjectGuid, UserToken.UserID, Common.GetClientIpAddress(Request));
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpPost]
        public Result Delete(InputDeleteInventoryStore input)//Xóa phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoUpdateStatus(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }

        [HttpPost]
        public Result Restore(InputRestoreInventoryStore input)//Khôi phục phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoUpdateStatus(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }

        [HttpPost]
        public Result Approve(InputApproveInventoryStore input)//Duyệt phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoUpdateStatus(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoUpdateStatus(InventoryStoreHanding input)
        {
            string msg = ValidateHanding(input, out var InventoryStore);
            if (msg.Length > 0) { return msg.ToMessageForUser(); }

            DBM dbm = new DBM();
            dbm.BeginTransac();
            try
            {
                msg = UpdateStatusID_SaveToDB(dbm, InventoryStore, input.GetStatusChange(), UserToken.UserID, input.GetContent(UserToken.UserName, InventoryStore.InventoryStoreCode));
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg.ToMessageForUser(); }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return "[DoUpdateStatus] " + ex.Message;
            }
            dbm.CommitTransac();

            return msg;
        }
        private string ValidateHanding(InventoryStoreHanding input, out InventoryStore InventoryStore)
        {
            InventoryStore = null;

            string msg = CacheObject.GetInventoryStoreIDbyGUID(input.ObjectGuid, out long InventoryStoreID);
            if (msg.Length > 0) return msg;

            msg = InventoryStore.GetOne(InventoryStoreID, UserToken.AccountID, out InventoryStore);
            if (msg.Length > 0) return msg;
            if (InventoryStore == null) return ("Không tồn tại phiếu kiểm kê có ID = " + InventoryStoreID).ToMessageForUser();

            if (input is InputHandlingInventoryStore)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_DUYET);
                if (msg.Length > 0) return msg;
                if (InventoryStore.UserHandingID != UserToken.UserID) return "Bạn không phải là Tài khoản xử lý Phiếu kiểm kê vật phẩm này".ToMessageForUser();
            }
            else if (input is InputUpdateStatusInventoryStore)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKVP, Role.ROLE_QLKKVP_CRUD);
                if (msg.Length > 0) return msg;
                if (InventoryStore.UserCreateID != UserToken.UserID) return "Bạn không phải Tài khoản tạo Phiếu kiểm kê vật phẩm này".ToMessageForUser();
            }

            msg = input.ValidateInput(InventoryStore.StatusID);
            if (msg.Length > 0) return msg.ToMessageForUser();

            return string.Empty;
        }
        private string UpdateStatusID_SaveToDB(DBM dbm, InventoryStore InventoryStore, int StatusID, int UserID, string logContent)
        {
            string msg;

            msg = InventoryStore.UpdateStatusID(dbm, InventoryStore.InventoryStoreID, UserToken.AccountID, StatusID);
            if (msg.Length > 0) return msg;

            if (InventoryStore.StatusID == Constants.StatusPKKVP.DAXONG)
            {
                TransferHandlingLog log = new TransferHandlingLog
                {
                    ObjectID = InventoryStore.InventoryStoreID,
                    ObjectTypeID = Constants.TransferHandling.PKKVP,
                    UserIDHandling = UserToken.UserID,
                    Comment = "Hoàn thành phiếu Phiếu kiểm kê Vật Phẩm",
                    TransferDirectionID = InventoryStore.TransferDirectionID
                };
                msg = log.InsertUpdate(dbm, out TransferHandlingLog _);
                if (msg.Length > 0) return msg;
            }
            msg = Log.WriteHistoryLog(dbm, logContent, InventoryStore.ObjectGuid, UserID, Common.GetClientIpAddress(Request));
            return msg;
        }


    }
}