using ASM_API.App_Start.AssetInventory;
using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web.Http;

namespace WebAPI.Controllers
{
    public class AssetInventoryController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKTS, Role.ROLE_QLKKTS_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertUpdate(data, out AssetInventory inventoryOut);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return inventoryOut.ToResultOk();
        }
        private string DoInsertUpdate([FromBody] JObject data, out AssetInventory inventoryOut)
        {
            inventoryOut = new AssetInventory();

            string msg = data.ToObject("AssetInventory", out AssetInventory inventoryInput);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (inventoryInput == null) return "Không tồn tại phiếu kiểm kể";

            msg = DoInsertUpdate_Validate(inventoryInput);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_SetValue(inventoryInput);
            if (msg.Length > 0) return msg;

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoInsertUpdate_ObjectToDB(dbm, inventoryInput, out inventoryOut);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetInventory DoInsertUpdate";
            }

            dbm.CommitTransac();

            return msg;
        }

        private string DoInsertUpdate_Validate(AssetInventory inventory)
        {
            string msg = DataValidator.Validate(new
            {
                inventory.InventoryID,
                inventory.InventoryName,
                inventory.UserIDApprover,
                inventory.StatusID
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (string.IsNullOrEmpty(inventory.InventoryName)) return "Tên Phiếu kiểm kê không được để trống".ToMessageForUser(); ;

            if (inventory.AssetInventoryDetails.Count == 0) return "Bạn cần chọn Tài sản kiểm kê".ToMessageForUser();

            if (!string.IsNullOrEmpty(inventory.Note) && inventory.Note.Length > 200) return "Ghi chú không được vượt quá 200 ký tự".ToMessageForUser();

            if (inventory.BeginDate == DateTime.MinValue) return "Bạn cần nhập Ngày bắt đầu".ToMessageForUser();
            if (inventory.EndDate == DateTime.MinValue) return "Bạn cần nhập Ngày kết thúc".ToMessageForUser();

            if (inventory.BeginDate > inventory.EndDate) return "Không cho phép nhập Ngày bắt đầu lớn hơn Ngày kết thúc".ToMessageForUser();

            inventory.UserIDPerform = UserToken.UserID;
            inventory.AccountID = UserToken.AccountID;

            inventory.StatusID = (inventory.IsSendApprove == true) ? inventory.StatusID = Constants.StatusPKK.CD : inventory.StatusID = Constants.StatusPKK.MT;

            if (inventory.UserIDApprover == UserToken.UserID) return ("Bạn không được phép chuyển Phiếu kiểm kê cho chính mình").ToMessageForUser();

            foreach (var item in inventory.AssetInventoryDetails)
            {
                msg = DataValidator.Validate(new
                {
                    item.ID,
                    item.InventoryID,
                    item.StateID
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Asset.GetOneByAssetID(item.AssetID, out Asset asset);
                if (msg.Length > 0) return msg;

                if (asset == null) return ("Không có Tài sản có ID=" + item.AssetID).ToMessageForUser();
            }

            return msg;
        }

        private string DoInsertUpdate_SetValue(AssetInventory inventoryInput)
        {
            string msg = "";
            if (inventoryInput.ObjectGuid == Guid.Empty)
            {
                string formatDate = DateTime.Now.ToString("yyMMdd");
                string InventoryCode_Prefix = "PKK" + formatDate;
                msg = AssetInventory.GetTotalByDateCode(InventoryCode_Prefix, out int Total);
                if (msg.Length > 0) return msg;

                inventoryInput.InventoryCode = InventoryCode_Prefix + (Total + 1);
            }
            else
            {
                msg = AssetInventory.GetOneObjectGuid(inventoryInput.ObjectGuid, out AssetInventory outInventory);
                if (msg.Length > 0) return msg;
                inventoryInput.InventoryID = outInventory.InventoryID;

                msg = setInfoUpdate(outInventory, inventoryInput, out var logChange);
                if (msg.Length > 0) return msg;
                inventoryInput.InfoUpdate = logChange;
            }

            return "";
        }
        private string setInfoUpdate(AssetInventory inventoryOld, AssetInventory inventoryNew, out string logChange)
        {
            logChange = null;
            List<string> LtChanges = new List<string>();

            string msg = TransferHandlingLog.GetByObjectID(inventoryOld.InventoryID, Constants.TransferHandling.PKK, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;
            inventoryOld.TransferDirectionID = transferHandlingLog != null ? transferHandlingLog.TransferDirectionID : "";

            msg = TransferHandlingDirection.GetTransferDirection(UserToken.UserID, Constants.TransferHandling.PKK, out List<TransferHandlingDirection> outltTransferDicrection);
            if (msg.Length > 0) return msg;

            msg = TransferHandlingDirection.GetUserHandling(UserToken.UserID, UserToken.AccountID, inventoryNew.TransferDirectionID, out List<AccountUser> outLtAccoutUser);
            if (msg.Length > 0) return msg;

            msg = AssetInventoryDetail.GetListByInventoryID(inventoryOld.InventoryID, out var outLtAssetInventoryDetails);
            if (msg.Length > 0) return msg;

            msg = AssetInventoryState.GetAllStateName(out var assetInventoriesState);
            if (msg.Length > 0) return msg;

            string SEPARATOR = "; ";
            logChange = inventoryNew.GetUpdateInfo(inventoryOld, SEPARATOR,
                new Tuple<string, string, Dictionary<object, string>>("InventoryName", "Tên Phiếu kiểm kê", null),
                new Tuple<string, string, Dictionary<object, string>>("BeginDate", "Ngày bắt đầu", null),
                new Tuple<string, string, Dictionary<object, string>>("EndDate", "Ngày kết thúc", null),
                new Tuple<string, string, Dictionary<object, string>>("Commenthandling", "Thông tin chuyển xử lý", null),
                new Tuple<string, string, Dictionary<object, string>>("ReasonRefuse", "Ghi chú", null),
                new Tuple<string, string, Dictionary<object, string>>("TransferDirectionID", "Hướng chuyển", outltTransferDicrection.ToDictionary(x => (object)x.TransferDirectionID, x => x.TransferDirectionName)),
                new Tuple<string, string, Dictionary<object, string>>("UserIDApprover", "Người xử lý", outLtAccoutUser.ToDictionary(x => (object)x.UserID, x => x.UserName))
                );

            var ALDRChange = inventoryNew.AssetInventoryDetails.Except(outLtAssetInventoryDetails, new AssetInventoryDetailCompare()).Select(x => $"Thêm {x.AssetCode}");
            LtChanges.AddRange(ALDRChange);

            ALDRChange = outLtAssetInventoryDetails.Except(inventoryNew.AssetInventoryDetails, new AssetInventoryDetailCompare()).Select(x => $"Xóa {x.AssetCode}");
            LtChanges.AddRange(ALDRChange);

            var dicLtAssetInventoryDetails = outLtAssetInventoryDetails.ToDictionary(x => x.ID, y => y);
            foreach (var item in inventoryNew.AssetInventoryDetails)
            {
                if (!dicLtAssetInventoryDetails.TryGetValue(item.ID, out var outItem)) continue;

                string s = $"Sửa {item.AssetCode}: " + item.GetUpdateInfo(outItem, ", ", new Tuple<string, string, Dictionary<object, string>>("StateID", "Tình trạng",
                    assetInventoriesState.ToDictionary(x => (object)x.StateID, y => y.StateName)));

                LtChanges.Add(s);
            }

            logChange += string.IsNullOrEmpty(logChange) ? "" : SEPARATOR + (LtChanges.Any() ? "Sửa Danh sách tài sản: " + string.Join(SEPARATOR, LtChanges) : "");

            return string.Empty;
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, AssetInventory inventoryInput, out AssetInventory inventoryOut)
        {
            string msg = inventoryInput.InsertUpdate(dbm, out inventoryOut);
            if (msg.Length > 0) return msg;

            TransferHandlingLog log = new TransferHandlingLog
            {
                ObjectID = inventoryOut.InventoryID,
                ObjectTypeID = Constants.TransferHandling.PKK,
                UserIDHandling = inventoryOut.UserIDApprover,
                Comment = inventoryOut.ReasonRefuse,
                TransferDirectionID = inventoryInput.TransferDirectionID
            };
            msg = log.InsertUpdate(dbm, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;

            if (inventoryInput.AssetInventoryDetails.Count > 0)
            {
                string AssetIDs = String.Join(",", inventoryInput.AssetInventoryDetails.Select(x => x.AssetID).ToArray());
                msg = AssetInventoryDetail.Delete(dbm, AssetIDs, inventoryInput.InventoryID);
                if (msg.Length > 0) return msg;

                msg = DoInsertUpdate_InventoryDetail(dbm, inventoryOut.InventoryID, inventoryInput.AssetInventoryDetails, out List<AssetInventoryDetail> assetInventoryDetailOut);
                if (msg.Length > 0) return msg;

                inventoryOut.AssetInventoryDetails = assetInventoryDetailOut;
            }
            msg = Log.WriteHistoryLog(dbm, inventoryInput.InventoryID == 0 ? $"Thêm mới Phiếu kiểm kê thành công. Mã phiếu {inventoryOut.InventoryCode}" : $"Sửa {inventoryOut.InventoryCode}: {inventoryInput.InfoUpdate}", inventoryOut.ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            if (inventoryOut.StatusID == Constants.StatusPKK.CD)
            {
                msg = Log.WriteHistoryLog(dbm, $"Chuyển xử lý Phiếu kiểm kê cho người xử lý: {AccountUser.GetUserNameByUserID(inventoryOut.UserIDApprover)} thành công. Nội dung ý kiến xử lý - {inventoryOut.ReasonRefuse}", inventoryOut.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }

            return msg;
        }
        private string DoInsertUpdate_InventoryDetail(DBM dbm, long InventoryID, List<AssetInventoryDetail> AssetInventoryDetailList, out List<AssetInventoryDetail> InventoryAssetsOut)
        {
            string msg = "";
            InventoryAssetsOut = new List<AssetInventoryDetail>();

            foreach (var item in AssetInventoryDetailList)
            {
                AssetInventoryDetail inventoryAssetDetail = new AssetInventoryDetail
                {
                    InventoryID = InventoryID,
                    ID = item.ID,
                    AssetID = item.AssetID,
                    Note = item.Note,
                    StateID = item.StateID
                };
                msg = inventoryAssetDetail.InsertUpdate(dbm, out AssetInventoryDetail assetInventoryDetail);
                if (msg.Length > 0) return msg;

                Asset.GetOneByAssetID(item.AssetID, out Asset outAsset);
                if (msg.Length > 0) return msg;

                msg = Log.WriteHistoryLog(dbm, $"Kiểm kê tài sản", outAsset.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;

                assetInventoryDetail.AssetCode = item.AssetCode;
                assetInventoryDetail.AssetModel = item.AssetModel;
                assetInventoryDetail.AssetSerial = item.AssetSerial;
                assetInventoryDetail.AssetTypeName = item.AssetTypeName;
                assetInventoryDetail.PlaceFullName = item.PlaceFullName;
                assetInventoryDetail.StateName = item.StateName;
                InventoryAssetsOut.Add(assetInventoryDetail);
            }

            return msg;
        }

        [HttpGet]
        public Result GetAssetInventoryStatus()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = GetAssetInventoryStatus(out List<AssetInventoryStatus> assetInventoryStatus);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return assetInventoryStatus.ToResultOk();
        }
        private string GetAssetInventoryStatus(out List<AssetInventoryStatus> assetInventoryStatus)
        {
            string msg = AssetInventoryStatus.GetAllStatusName(out assetInventoryStatus);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpGet]
        public Result GetAssetInventoryStates()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetAssetInventoryStates(out List<AssetInventoryState> assetInventoriesState);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return assetInventoriesState.ToResultOk();
        }
        private string DoGetAssetInventoryStates(out List<AssetInventoryState> assetInventoriesState)
        {
            string msg = AssetInventoryState.GetAllStateName(out assetInventoriesState);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpGet]
        public Result GetOne(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKTS, Role.ROLE_QLKKTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetOne(ObjectGuid, out AssetInventory assetInventory);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return assetInventory.ToResultOk();
        }
        private string DoGetOne(Guid ObjectGuid, out AssetInventory assetInventory)
        {
            assetInventory = null;

            string msg = CacheObject.GetAssetInventoryByGUID(ObjectGuid, out long inventoryID);
            if (msg.Length > 0) return msg;

            msg = AssetInventory.GetOne(inventoryID, out assetInventory);
            if (msg.Length > 0) return msg;

            if (assetInventory == null) return ("Phiếu kiểm kê không tồn tại ObjectGuid = " + ObjectGuid).ToMessageForUser();

            msg = AssetInventoryDetail.GetListByInventoryID(assetInventory.InventoryID, out List<AssetInventoryDetail> assetInventoryDetails);
            if (msg.Length > 0) return msg;

            msg = TransferHandlingLog.GetByObjectID(assetInventory.InventoryID, Constants.TransferHandling.PKK, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;
            assetInventory.TransferDirectionID = transferHandlingLog != null ? transferHandlingLog.TransferDirectionID : "";

            assetInventory.AssetInventoryDetails = assetInventoryDetails;

            return "";
        }

        [HttpGet]
        public Result GetListAccountUser()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = AccountUser.GetAll(UserToken.AccountID, out List<AccountUser> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
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
            dt = new DataTable();

            string msg = AssetInventory.GetSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpPost]
        public Result GetAssetInventoryDetailByAssetTypeIDsOrPlaceIDs([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetAssetInventoryDetailByAssetTypeIDsOrPlaceIDs(data, out List<AssetInventoryDetail> assetInventoryDetails);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return assetInventoryDetails.ToResultOk();
        }
        private string DoGetAssetInventoryDetailByAssetTypeIDsOrPlaceIDs([FromBody] JObject data, out List<AssetInventoryDetail> assetInventoryDetails)
        {
            assetInventoryDetails = new List<AssetInventoryDetail>();
            string msg = data.ToString("AssetTypeIDs", out string assetTypeIDs);
            if (msg.Length > 0) return msg;

            msg = data.ToString("PlaceIDs", out string placeIDs);
            if (msg.Length > 0) return msg;

            if (assetTypeIDs.Length == 0 && placeIDs.Length == 0) return "";

            msg = AssetInventoryDetail.GetAssetInventoryDetailByAssetTypeIDsOrPlaceIDs(assetTypeIDs, placeIDs, out assetInventoryDetails);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpPost]
        public Result GetListEasySearch([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListEasySearch(data, out int total, out List<InventorySearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoGetListEasySearch([FromBody] JObject data, out int total, out List<InventorySearchResult> lt)
        {
            lt = null;
            total = 0;

            string msg = DoGetListEasySearch_GetInventorySearch(data, out InventorySearch inventorySearch);
            if (msg.Length > 0) return msg;

            msg = DoGetList(inventorySearch, out lt, out total);
            return msg;
        }
        private string DoGetListEasySearch_GetInventorySearch([FromBody] JObject data, out InventorySearch ms)
        {
            ms = new InventorySearch();

            string msg = data.ToNumber("ObjectCategory", out int ObjectCategory);
            if (msg.Length > 0) return msg;

            msg = data.ToString("ObjectID", out string ObjectID);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("PageSize", out int PageSize);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("CurrentPage", out int CurrentPage);
            if (msg.Length > 0) return msg;

            msg = data.ToString("TextSearch", out string TextSearch);
            if (msg.Length > 0) return msg;

            ms.TextSearch = TextSearch;
            ms.CurrentPage = CurrentPage;
            ms.PageSize = PageSize;

            if (ObjectCategory == 1 || ObjectCategory == 2) ms.InventoryID = ObjectID.ToNumber(0); //1, 2 tìm kiếm theo mã Phiếu kiểm kê hoặc tìm kiếm theo tên Phiếu kiểm kê
            if (ObjectCategory == 3) ms.UserIDPerform = ObjectID.ToNumber(0); //3 tìm kiếm theo người thực hiện

            return "";
        }
        private string DoGetList(InventorySearch inventorySearch, out List<InventorySearchResult> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                inventorySearch.AccountID = UserToken.AccountID;
                inventorySearch.UserID = UserToken.UserID;

                /*string msg = AssetInventory.GetListSearchTotal(inventorySearch, out totalSearch);
                if (msg.Length > 0) return msg;

                msg = AssetInventory.GetListSearch(inventorySearch, out lt);
                if (msg.Length > 0) return msg;*/

                string msg = DataValidator.Validate(inventorySearch).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = AssetInventory.GetListPaging(inventorySearch, out lt, out totalSearch);
                if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    msg = DoGetListButtonFuction(item, UserToken.UserID, out ButtonShowPKK button);
                    if (msg.Length > 0) return msg;

                    item.ButtonShow = button;
                }

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private string DoGetListButtonFuction(InventorySearchResult PKK, int UserIDLogin, out ButtonShowPKK button)
        {
            button = new ButtonShowPKK();

            int statusID = PKK.StatusID;

            if (UserIDLogin == PKK.UserIDPerform)
            {
                if (statusID == Constants.StatusPKK.MT)
                {
                    button.Delete = true;
                    button.Edit = true;
                    button.SendApprove = true;
                }

                if (statusID == Constants.StatusPKK.TL) { button.Edit = true; button.Delete = true; button.SendApprove = true; }
                if (statusID == Constants.StatusPKK.ĐX) button.Restore = true;
                if (statusID == Constants.StatusPKK.TC) { button.Edit = true; button.Delete = true; }
            }

            if (UserIDLogin == PKK.UserIDApprover)
                if (statusID == Constants.StatusPKK.CD) { button.SendApprove = true; button.Approved = true; }

            button.ViewHistory = true;

            return "";

        }

        [HttpPost]
        public Result GetListAdvancedSearch([FromBody] JObject data)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = DoGetListAdvancedSearch(data, out int total, out List<InventorySearchResult> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch([FromBody] JObject data, out int Total, out List<InventorySearchResult> lt)
        {
            lt = null;
            Total = 0;

            string msg = data.ToObject("InventorySearch", out InventorySearch inventorySearch);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoGetList(inventorySearch, out lt, out Total);
            return msg;
        }

        [HttpPost]
        public Result DeleteAssetInventory([FromBody] JObject data)//Xóa phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKTS, Role.ROLE_QLPDX_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPKK.ĐX);
        }

        [HttpPost]
        public Result RestoreAssetInventory([FromBody] JObject data)//Phục hồi phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKTS, Role.ROLE_QLPDX_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPKK.MT);
        }

        [HttpPost]
        public Result RecoveAssetInventory([FromBody] JObject data) //Thu hồi chuyển duyệt PKK
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKTS, Role.ROLE_QLPDX_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPKK.MT);
        }

        [HttpPost]
        public Result ApproveAssetInventory([FromBody] JObject data)//Duyệt phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKTS, Role.ROLE_QLPDX_DUYET);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPKK.ĐD);
        }

        [HttpPost]
        public Result UnApproveAssetInventory([FromBody] JObject data)//Không duyệt phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLKKTS, Role.ROLE_QLPDX_DUYET);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPKK.TC);
        }

        [HttpPost]
        public Result UpdateStatusID([FromBody] JObject data, int StatusID)
        {
            string msg = UpdateStatusID(data, StatusID, UserToken.UserID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string UpdateStatusID([FromBody] JObject data, int StatusID, int UserID)
        {
            string logContent = "";
            string AssetInventoryReasonRefuse = "";

            string msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;

            if (StatusID == Constants.StatusPKK.TC)
            {
                msg = data.ToString("AssetInventoryReasonRefuse", out AssetInventoryReasonRefuse);

                if (msg.Length > 0) return msg;
                if (string.IsNullOrEmpty(AssetInventoryReasonRefuse)) return ("Bạn cần nhập lý do từ chối").ToMessageForUser();
                if (AssetInventoryReasonRefuse.Length < 20) return ("Bạn cần nhập nội dung lý do không duyệt tối thiểu 20 ký tự trở lên").ToMessageForUser();
            }

            msg = CacheObject.GetAssetInventoryByGUID(ObjectGuid, out long InventoryID);
            if (msg.Length > 0) return msg;

            msg = AssetInventory.GetOne(InventoryID, out AssetInventory assetInventory);
            if (msg.Length > 0) return msg;
            if (assetInventory == null) return ("Không tồn tại tài sản ID = " + InventoryID).ToMessageForUser();

            switch (StatusID)
            {
                case Constants.StatusPKK.MT:
                    if (assetInventory.StatusID != Constants.StatusPKK.CD && assetInventory.StatusID != Constants.StatusPKK.ĐX) return "Bạn chỉ được chuyển duyệt Phiếu kiểm kê hoặc thu hồi Phiếu kiểm kê khi phiếu đã ở trạng thái Mới tạo hoặc Chờ duyệt".ToMessageForUser();
                    logContent = assetInventory.StatusID == Constants.StatusPKK.ĐX ? "Khôi phục" : "Thu thồi Phiếu kiểm kê";
                    break;
                case Constants.StatusPKK.ĐX:
                    if (assetInventory.StatusID != Constants.StatusPKK.MT && assetInventory.StatusID != Constants.StatusPKK.TC) return "Bạn chỉ được xóa khi phiếu ở trạng thái Mới tạo hoặc Trả lại hoặc Từ chối".ToMessageForUser();
                    logContent = $"Xóa thông tin Phiếu kiểm kê";
                    break;
                case Constants.StatusPKK.CD:
                    if (assetInventory.StatusID != Constants.StatusPKK.MT) return "Bạn chỉ được gửi duyệt Phiếu kiểm kê khi phiếu đã ở trạng thái Mới tạo".ToMessageForUser();
                    logContent = "Gửi duyệt Phiếu kiểm kê";
                    break;
                case Constants.StatusPKK.ĐD:
                    if (assetInventory.StatusID != Constants.StatusPKK.CD) return "Bạn chỉ được duyệt Phiếu kiểm kê khi phiếu đã ở trạng thái Chờ duyệt".ToMessageForUser();
                    logContent = $"Duyệt Phiếu kiểm kê";
                    break;
                case Constants.StatusPKK.TC:
                    if (assetInventory.StatusID != Constants.StatusPKK.CD) return "Bạn chỉ được từ chối duyệt Phiếu kiểm kê khi phiếu đã ở trạng thái Chờ duyệt".ToMessageForUser();
                    logContent = $"Từ chối duyệt Phiểu kiểm kê {assetInventory.InventoryCode} với lý do: " + AssetInventoryReasonRefuse;
                    break;
                default:
                    Log.WriteHistoryLog("Không có trạng thái nào thỏa mãn yêu cầu", assetInventory.ObjectGuid, UserID, Common.GetClientIpAddress(Request));
                    return "Không có trạng thái nào thỏa mãn yêu cầu";
            }

            msg = UpdateStatusID_SaveToDB(assetInventory, StatusID, UserID, logContent, AssetInventoryReasonRefuse);
            if (msg.Length > 0) { return msg.ToMessageForUser(); }

            return msg;
        }
        private string UpdateStatusID_SaveToDB(AssetInventory assetInventory, int StatusID, int UserID, string logContent, string AssetInventoryReasonRefuse)
        {
            string msg = "";

            msg = AssetInventory.UpdateStatusID(new DBM(), assetInventory.InventoryID, StatusID);
            if (msg.Length > 0) return msg;

            msg = Log.WriteHistoryLog(logContent, assetInventory.ObjectGuid, UserID, Common.GetClientIpAddress(Request));
            return msg;
        }

        [HttpGet]
        public Result ViewAssetInventoryDetail(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoViewAssetInventoryDetail(ObjectGuid, out AssetInventoryViewDetail assetInventoryViewDetail);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return assetInventoryViewDetail.ToResultOk();
        }
        public string DoViewAssetInventoryDetail(Guid ObjectGuid, out AssetInventoryViewDetail assetInventoryViewDetail)
        {
            assetInventoryViewDetail = null;

            string msg = CacheObject.GetAssetInventoryByGUID(ObjectGuid, out long inventoryID);
            if (msg.Length > 0) return msg;

            msg = AssetInventoryViewDetail.ViewDetail(inventoryID, out assetInventoryViewDetail);
            if (msg.Length > 0) return msg;
            if (assetInventoryViewDetail == null) return ("Phiếu kiểm kê không tồn tại ID = " + inventoryID).ToMessageForUser();

            msg = TransferHandlingLog.GetByObjectID(assetInventoryViewDetail.InventoryID, Constants.TransferHandling.PKK, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;
            assetInventoryViewDetail.TransferDirectionID = transferHandlingLog != null ? transferHandlingLog.TransferDirectionID : "";

            msg = AssetInventoryDetail.GetListByInventoryID(assetInventoryViewDetail.InventoryID, out List<AssetInventoryDetail> assetInventoryDetails);
            if (msg.Length > 0) return msg;

            assetInventoryViewDetail.assetInventoryDetails = assetInventoryDetails;

            return "";
        }

        [HttpPost]
        public Result TransferHandling([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            string msg = DoTransferHandling(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoTransferHandling([FromBody] JObject data)
        {
            string msg = "";

            msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;

            msg = data.ToString("UserIDHandling", out string userIDApprover);
            if (msg.Length > 0) return msg;

            msg = data.ToString("Commenthandling", out string Commenthandling);
            if (msg.Length > 0) return msg;

            msg = data.ToString("TransferDirectionID", out string TransferDirectionID);
            if (msg.Length > 0) return msg;

            long UserIDApprover = userIDApprover.ToLong(0);

            msg = CacheObject.GetAssetInventoryByGUID(ObjectGuid, out long InventoryID);
            if (msg.Length > 0) return msg;
            if (InventoryID == 0) return ("Không có giá trị phù hợp với ObjectGuid=" + ObjectGuid).ToMessageForUser();

            msg = AssetInventory.GetOne(InventoryID, out AssetInventory assetInventory);
            if (msg.Length > 0) return msg;

            if (UserIDApprover == UserToken.UserID) return ("Bạn không được phép chuyển Phiếu kiểm kê cho chính mình").ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoTransferHandling_ObjectToDB(dbm, assetInventory, UserIDApprover);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at AssetHandOver DoTransferHandling";
            }


            dbm.CommitTransac();

            return msg;
        }

        private string DoTransferHandling_ObjectToDB(DBM dbm, AssetInventory assetInventory, long userIDApprover)
        {
            string msg = "";
            if (assetInventory.UserIDPerform == userIDApprover)
            {
                msg = AssetInventory.UpdateStatusID(dbm, assetInventory.InventoryID, Constants.StatusPKK.TL);
                if (msg.Length > 0) return msg;
            }
            else
            {
                msg = AssetInventory.UpdateUserIDApprover(dbm, assetInventory.InventoryID, userIDApprover);
                if (msg.Length > 0) return msg;
            }

            msg = Log.WriteHistoryLog(dbm, $"Bạn đã chuyển xử lý Phiểu kiểm kê cho: {AccountUser.GetUserNameByUserID(userIDApprover)} thành công. Nội dung ý kiến xử lý - {assetInventory.Commenthandling}", assetInventory.ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            TransferHandlingLog logt = new TransferHandlingLog
            {
                ObjectID = assetInventory.InventoryID,
                ObjectTypeID = Constants.TransferHandling.PKK,
                UserIDHandling = userIDApprover,
                Comment = assetInventory.Commenthandling,
                TransferDirectionID = assetInventory.TransferDirectionID
            };
            msg = logt.InsertUpdate(dbm, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;

            return "";
        }

        [HttpPost]
        public Result ExportAssetInventory([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            string msg = DoExportAssetInventory(data, out string filePath);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return filePath.ToResultOk();
        }
        private string DoExportAssetInventory([FromBody] JObject data, out string filePath)
        {
            filePath = "";
            string msg = data.ToObject("AssetInventories", out List<AssetInventory> assetInventories);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (assetInventories == null || assetInventories.Count == 0) return "Không tồn tại danh sách Phiếu kiểm kê";

            foreach (var item in assetInventories)
            {
                msg = DoGetOne(item.ObjectGuid, out AssetInventory assetInventory);
                if (msg.Length > 0) return msg;

                item.DeptID = assetInventory.DeptID;
                item.DeptCode = assetInventory.DeptCode;
                item.AccountName = assetInventory.AccountName;
                item.UserNameApprover = assetInventory.UserNameApprover;
                item.InventoryName = assetInventory.InventoryName;
                item.BeginDate = assetInventory.BeginDate;
                item.EndDate = assetInventory.EndDate;
                item.AssetInventoryDetails = assetInventory.AssetInventoryDetails;
            }

            msg = DoCreateExportFile(assetInventories, out filePath);
            if (msg.Length > 0) return msg;

            return "";
        }
        private string DoCreateExportFile(List<AssetInventory> assetInventories, out string filePath)
        {
            filePath = "";
            string fileName = "PhieuKiemKe_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            UtilitiesFile file = UtilitiesFile.GetInfoFile(DateTime.Now, fileName, ConfigurationManager.AppSettings["FolderFileExport"].ToString(), false);

            filePath = UtilitiesFile.GetUrlPage() + "/" + file.FilePathVirtual;

            string msg = FileReportInventoryExcel.CreateFile(assetInventories, UserToken.AccountID, file.FilePathPhysical);
            if (msg.Length > 0) return msg;

            return "";
        }
    }
}