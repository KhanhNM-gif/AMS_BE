using ASM_API.App_Start.ItemImportReceipt;
using ASM_API.App_Start.ItemProposalForm;
using ASM_API.App_Start.Store;
using ASM_API.App_Start.TableModel;
using ASM_API.App_Start.Template;
using BSS;
using BSS.DataValidator;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using static Constants;

public class StoreController : Authentication
{
    [HttpPost]
    public Result InsertOrUpdateExportReceipt([FromBody] ItemExportReceipt input)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPXK, Role.ROLE_QLPXK_CRUD);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = DoInsertOrUpdateExportReceipt(input);
        if (msg.Length > 0) return Log.ProcessError(msg.ToMessageForUser()).ToResultError();

        return "".ToResultOk();
    }
    private string DoInsertOrUpdateExportReceipt(ItemExportReceipt input)
    {
        string msg = DoInsertOrUpdateExportReceipt_Validate(input);
        if (msg.Length > 0) return msg;

        msg = DoInsertOrUpdateExportReceipt_SetData(input);
        if (msg.Length > 0) return msg;

        DBM dbm = new DBM();
        dbm.BeginTransac();
        try
        {
            msg = DoInsertOrUpdateExportReceipt_ToDB(dbm, input, out var outItemImportReceipt);
            if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
        }
        catch (Exception ex)
        {
            dbm.RollBackTransac();
            return ex.ToString();
        }

        dbm.CommitTransac();

        return "";
    }
    private string DoInsertOrUpdateExportReceipt_Validate(ItemExportReceipt input)
    {
        string msg = msg = DataValidator.Validate(new
        {
            input.ObjectGuid,
            input.Note,
            input.PlaceID,
        }).ToErrorMessage();
        if (msg.Length > 0) return msg.ToMessageForUser();

        CacheObject.GetItemProposalFormbyGUID(input.ObjectGuidItemProposalForm, out long ItemProposalFormID);
        if (msg.Length > 0) return msg;
        input.ItemProposalFormID = ItemProposalFormID;

        msg = ItemProposalForm.GetOne(input.ItemProposalFormID, UserToken.AccountID, out ItemProposalForm outItemProposalForm);
        if (msg.Length > 0) return msg;
        if (outItemProposalForm.ItemProposalFormTypeID != 2) return "Phiếu đề xuất không tồn tại";
        if (outItemProposalForm.UserIDHandling != UserToken.UserID) return "Bạn không phải người xử lý phiếu đề xuất này".ToMessageForUser();
        if (outItemProposalForm.ItemProposalFormStatusID == Constants.StatusPDXVP.DHT) return "Bạn đã hoàn thành việc nhập kho cho phiếu đề xuất Vật Phẩm".ToMessageForUser();
        input.itemProposalForm = outItemProposalForm;

        msg = ItemExportReceiptType.GetList(out var lt);
        if (msg.Length > 0) return msg;
        if (!lt.Exists(x => x.ID == input.ItemExportReceiptTypeID)) return "Loại xuất không tồng tại";

        if (input.ExportDate == null) "Chưa chọn ngày xuất kho".ToMessageForUser();

        msg = Place.GetOneByPlaceID(input.PlaceID, UserToken.AccountID, out Place outplace);
        if (msg.Length > 0) return msg;
        if (outplace is null || outplace.PlaceType != ConmonConstants.TYPE_IS_DEPOT) return "Kho không tồn tại";

        msg = UserManagementPlace.CheckRoleManagement(UserToken.UserID, input.PlaceID, out var userManagementPlace);
        if (msg.Length > 0) return msg;
        if (userManagementPlace is null) return "Bạn không có quyền Quản lý kho này".ToMessageForUser();

        msg = NumberItemStore.GetListByPlaceID(input.PlaceID, out var ltNumberItemStore);
        if (msg.Length > 0) return msg;

        msg = ItemProposalFormDetail.GetListByProposalFormID(outItemProposalForm.ID, out var ltItemProposalForm);
        if (msg.Length > 0) return msg;

        foreach (var item in input.ltItemExport)
        {
            msg = DataValidator.Validate(new
            {
                item.ItemID
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = Item.GetOneByItemID(item.ItemID, out Item outItem);
            if (msg.Length > 0) return msg;
            if (outItem is null) return ("Không có Vật phẩm có ID = " + item.ItemID).ToMessageForUser();
            item.ItemCode = outItem.ItemCode;

            var itemProposalFormDetail = ltItemProposalForm.Where(x => x.ItemID == item.ItemID).FirstOrDefault();
            if (msg.Length > 0) return msg;

            if (itemProposalFormDetail is null) "Vật phẩm không nằm trong phiếu đề xuất".ToMessageForUser();
            item.Quantity = itemProposalFormDetail.Quantity;

            var itemInStore = ltNumberItemStore.Where(x => x.ItemID == item.ItemID).FirstOrDefault();
            if (itemInStore is null) return $"Vật phẩm có ID = {item.ItemID} không có trong kho".ToMessageForUser();
            if (itemInStore.Quality < item.Quantity) return $"Số lượng Vật phẩm có ID = {item.ItemID} trong kho không đủ".ToMessageForUser();
        }

        msg = ItemProposalFormDetail.GetListByProposalFormID(input.ItemProposalFormID, out var ltout);
        if (msg.Length > 0) return msg;

        var ltexcept = input.ltItemExport.Except(ltout, new IModelCompare<IKeyCompare>());
        if (ltexcept.Any()) return $"Vật phẩm {string.Join(",", ltexcept.Select(x => x.GetKey()))} không nằm trong phiếu đề xuất";

        ltexcept = ltout.Except(input.ltItemExport, new IModelCompare<IKeyCompare>());
        if (ltexcept.Any()) return $"Vật phẩm {string.Join(",", ltexcept.Select(x => x.GetKey()))} có trong phiếu đề xuất nhưng không có trong phiếu nhập kho";

        return "";
    }
    private string DoInsertOrUpdateExportReceipt_SetData(ItemExportReceipt input)
    {
        input.UserIDCreate = UserToken.UserID;
        input.AccountID = UserToken.AccountID;
        input.ItemExportReceiptCode = $"PXKVP{DateTime.Now.ToString("yyMMddhhmmss")}";

        string msg;

        foreach (var item in input.ltItemExport)
        {
            msg = item.SetListImportBatch(input.PlaceID);
            if (msg.Length > 0) return msg;
        }

        return string.Empty;
    }
    private string DoInsertOrUpdateExportReceipt_ToDB(DBM dbm, ItemExportReceipt input, out ItemExportReceipt outItemImportReceipt)
    {
        string msg = input.InsertOrUpdate(dbm, out outItemImportReceipt);
        if (msg.Length > 0) return msg;

        if (input.ItemExportReceiptID == 0 && input.ItemProposalFormID != 0)
        {
            TransferHandlingLog log = new TransferHandlingLog
            {
                ObjectID = input.ItemProposalFormID,
                ObjectTypeID = Constants.TransferHandling.PDXVP,
                UserIDHandling = UserToken.UserID,
                Comment = "Hoàn thành xuất kho Vật phẩm",
                TransferDirectionID = input.itemProposalForm.TransferDirectionID
            };
            msg = log.InsertUpdate(dbm, out TransferHandlingLog _);
            if (msg.Length > 0) return msg;
        }

        msg = ItemExportReceiptDetail.InsertByDataTable(dbm, outItemImportReceipt.ItemExportReceiptID, input.ltItemExport, out List<ItemExportReceiptDetail> outLt);
        if (msg.Length > 0) return msg;
        outItemImportReceipt.ltItemExport = outLt;

        msg = ItemProposalForm.UpdateStatusID(dbm, input.ItemProposalFormID, UserToken.AccountID, Constants.StatusPDXVP.DHT);
        if (msg.Length > 0) return msg;

        foreach (var item in input.ltItemExport)
        {
            if (item.ltImportBatch != null)
            {
                msg = ImportBatchDetail.InsertUpdateByDataType(dbm, item.ltImportBatch);
                if (msg.Length > 0) return msg;
            }

        }

        msg = Log.WriteHistoryLog(dbm, $"Tạo phiếu xuất kho", outItemImportReceipt.ObjectGuid, UserToken.UserID);
        if (msg.Length > 0) return msg;

        msg = Log.WriteHistoryLog(dbm, $"Xuất kho theo phiếu đề xuất: <b>{input.itemProposalForm.ItemProposalFormCode}</b> thành công", input.itemProposalForm.ObjectGuid, UserToken.UserID);
        if (msg.Length > 0) return msg;

        return "";
    }

    [HttpGet]
    public Result GetListInfoBatch(Guid ObjectGuid, int PlaceID)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = DoGetListInfoBatch(ObjectGuid, PlaceID, out var dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return dt.ToResultOk();
    }

    private string DoGetListInfoBatch(Guid ObjectGuid, int PlaceID, out List<ItemProposalExportReceipt> itemProposalExportReceipts)
    {
        itemProposalExportReceipts = null;

        string msg = Role.Check(UserToken.UserID, TabID.QLPXK, Role.ROLE_QLPXK_CRUD);
        if (msg.Length > 0) return msg;

        msg = CacheObject.GetItemProposalFormbyGUID(ObjectGuid, out var ID);
        if (msg.Length > 0) return msg;

        msg = Place.GetOneByPlaceID(PlaceID, UserToken.AccountID, out Place outplace);
        if (msg.Length > 0) return msg;
        if (outplace is null || outplace.PlaceType != ConmonConstants.TYPE_IS_DEPOT) return "Kho không tồn tại";

        msg = UserManagementPlace.CheckRoleManagement(UserToken.UserID, PlaceID, out var userManagementPlace);
        if (msg.Length > 0) return msg;
        if (userManagementPlace is null) return "Bạn không có quyền Quản lý kho này".ToMessageForUser();

        msg = ItemProposalForm.GetOne(ID, UserToken.AccountID, out var outItemProposalForm);
        if (msg.Length > 0) return msg;
        if (outItemProposalForm.ItemProposalFormTypeID != Constants.ItemProposalFormType.DXX) return "Đây không phải phiếu xuất kho";
        if (outItemProposalForm.UserIDHandling != UserToken.UserID) return "Bạn không phải là người xử lý phiếu".ToMessageForUser();

        msg = ItemProposalExportReceipt.GetListByProposalFormID(ID, PlaceID, out itemProposalExportReceipts);
        if (msg.Length > 0) return msg;

        foreach (var item in itemProposalExportReceipts)
        {
            msg = ImportBatchDetailView.GetListInfoBatch(item.ItemID, PlaceID, out var lt);
            if (msg.Length > 0) return msg;

            item.ltImportBatchDetailView = lt;
        }

        return string.Empty;
    }

    [HttpGet]
    public Result GetListItemExportReceiptType()
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPXK, Role.ROLE_QLPXK_CRUD);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = ItemExportReceiptType.GetList(out var outLtItemExportReceiptType);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return outLtItemExportReceiptType.ToResultOk();
    }

    [HttpPost]
    public Result InsertOrUpdateImportReceipt([FromBody] ItemImportReceipt input)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_CRUD);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = DoInsertOrUpdateImportReceipt(input);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return "".ToResultOk();
    }
    private string DoInsertOrUpdateImportReceipt(ItemImportReceipt input)
    {
        string msg = DoInsertOrUpdateImportReceipt_Validate(input);
        if (msg.Length > 0) return msg;

        DoInsertOrUpdateImportReceipt_SetData(input);
        if (msg.Length > 0) return msg;

        DBM dbm = new DBM();
        dbm.BeginTransac();
        try
        {
            msg = DoInsertOrUpdateImportReceipt_ToDB(dbm, input, out var outItemImportReceipt);
            if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
        }
        catch (Exception ex)
        {
            dbm.RollBackTransac();
            return ex.ToString();
        }

        dbm.CommitTransac();

        return "";
    }
    private string DoInsertOrUpdateImportReceipt_Validate(ItemImportReceipt input)
    {
        string msg = msg = DataValidator.Validate(new
        {
            input.ObjectGuid,
            input.Note,
            input.PlaceID,
            input.CreateDate,
            input.ImportDate,
            input.SupplierID,
            input.VouchersNumber,
            input.InvoiceNumber,
            input.VouchersDate
        }).ToErrorMessage();
        if (msg.Length > 0) return msg.ToMessageForUser();

        CacheObject.GetItemProposalFormbyGUID(input.ObjectGuidItemProposalForm, out long ItemProposalFormID);
        if (msg.Length > 0) return msg;
        input.ItemProposalFormID = ItemProposalFormID;

        msg = ItemProposalForm.GetOne(input.ItemProposalFormID, UserToken.AccountID, out ItemProposalForm outItemProposalForm);
        if (msg.Length > 0) return msg;
        if (outItemProposalForm.ItemProposalFormTypeID != 1) return "Phiếu đề xuất không tồn tại";
        if (outItemProposalForm.UserIDHandling != UserToken.UserID) return "Bạn không phải người xử lý phiếu đề xuất này".ToMessageForUser();
        if (outItemProposalForm.ItemProposalFormStatusID == Constants.StatusPDXVP.DHT) return "Bạn đã hoàn thành việc xuất kho cho phiếu đề xuất Vật Phẩm".ToMessageForUser();
        input.ItemProposalForm = outItemProposalForm;

        msg = ItemImportReceiptType.GetList(out var lt);
        if (msg.Length > 0) return msg;
        if (!lt.Exists(x => x.ID == input.ItemImportReceiptTypeID)) return "Loại nhập không tồng tại";

        if (input.ImportDate == null) "Chưa chọn ngày nhập kho".ToMessageForUser();

        msg = Place.GetOneByPlaceID(input.PlaceID, UserToken.AccountID, out Place outplace);
        if (msg.Length > 0) return msg;
        if (outplace is null || outplace.PlaceType != ConmonConstants.TYPE_IS_DEPOT) return "Kho không tồn tại";

        msg = UserManagementPlace.CheckRoleManagement(UserToken.UserID, input.PlaceID, out var userManagementPlace);
        if (msg.Length > 0) return msg;
        if (userManagementPlace is null) return "Bạn không có quyền Quản lý kho này".ToMessageForUser();

        msg = Organization.GetList(UserToken.AccountID, out List<Organization> outOrganization);
        if (msg.Length > 0) return msg;
        if (!outOrganization.Exists(x => x.OrganizationTypeID == 1 && x.OrganizationID == input.SupplierID)) return "Nhà cung cấp không tồn tại";

        msg = ItemProposalFormDetail.GetListByProposalFormID(input.ItemProposalFormID, out var ltout);
        if (msg.Length > 0) return msg;

        foreach (var item in input.ltItemImport)
        {
            msg = DataValidator.Validate(new
            {
                item.ID,
                item.ItemID,
                item.ManufacturerID,
                item.ManufacturingDate,
                item.ExpiryDate,
                item.VAT,
                item.Quantity
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = Item.GetOneByItemID(item.ItemID, out Item outItem);
            if (msg.Length > 0) return msg;
            if (outItem is null) return ("Không có Vật phẩm có ID = " + item.ID).ToMessageForUser();
            item.ItemCode = outItem.ItemCode;

            var itemInItemProposalForm = ltout.FirstOrDefault(x => x.ItemID.Equals(item.ItemID));
            if (itemInItemProposalForm is null) return $"Vật phẩm {item.ID} không nằm trong phiếu đề xuất";
            item.Quantity = itemInItemProposalForm.Quantity;
            item.VAT = input.VAT;

            if (!outOrganization.Exists(x => x.OrganizationTypeID == 4 && x.OrganizationID == item.ManufacturerID)) return "Nhà sản không tồn tại";

            //if (item.ManufacturingDate > DateTime.Now) return "Ngày sản xuất và hạn sử dụng không được không phù hợp";
            if ((item.ManufacturingDate != null && item.ExpiryDate != null))
                if (item.ManufacturingDate >= item.ExpiryDate) return "Ngày sản xuất phải nhỏ hơn Hạn sử dụng";
        }

        var ltexcept = ltout.Except(input.ltItemImport, new IModelCompare<IKeyCompare>());
        if (ltexcept.Any()) return $"Vật phẩm {string.Join(",", ltexcept.Select(x => x.GetKey()))} có trong phiếu đề xuất nhưng không có trong phiếu nhập kho";

        return "";
    }
    private void DoInsertOrUpdateImportReceipt_SetData(ItemImportReceipt input)
    {
        input.UserIDCreate = UserToken.UserID;
        input.AccountID = UserToken.AccountID;
        input.ItemImportReceiptCode = $"PNKVP{DateTime.Now.ToString("yyMMddhhmmss")}";


    }
    private string DoInsertOrUpdateImportReceipt_ToDB(DBM dbm, ItemImportReceipt input, out ItemImportReceipt outItemImportReceipt)
    {
        string msg = input.InsertOrUpdate(dbm, out outItemImportReceipt);
        if (msg.Length > 0) return msg;

        if (input.ItemImportReceiptID == 0 && input.ItemProposalFormID != 0)
        {
            TransferHandlingLog log = new TransferHandlingLog
            {
                ObjectID = input.ItemProposalFormID,
                ObjectTypeID = Constants.TransferHandling.PDXVP,
                UserIDHandling = UserToken.UserID,
                Comment = "Hoàn thành nhập kho Vật phẩm",
                TransferDirectionID = input.ItemProposalForm.TransferDirectionID
            };
            msg = log.InsertUpdate(dbm, out TransferHandlingLog _);
            if (msg.Length > 0) return msg;
        }

        msg = ItemImportReceiptDetail.InsertByDataTable(dbm, outItemImportReceipt.ItemImportReceiptID, input.ltItemImport, out var outLt);
        if (msg.Length > 0) return msg;
        outItemImportReceipt.ltItemImport = outLt;

        msg = ItemProposalForm.UpdateStatusID(dbm, input.ItemProposalFormID, UserToken.AccountID, Constants.StatusPDXVP.DHT);
        if (msg.Length > 0) return msg;

        var ltItemMangeBatch = input.ltItemImport.Where(x => x.isBatchManagement);

        if (ltItemMangeBatch.Any())
        {
            msg = ImportBatch.GetTotal(DateTime.Now.Date, UserToken.AccountID, out int total);
            if (msg.Length > 0) return msg;

            ImportBatch importBatch = new ImportBatch()
            {
                ImportBatchCode = $"LO_{DateTime.Now.ToString("yyMMdd")}_{(total + 1).ToString().PadLeft(3, '0')}",
                StoreItemID = outItemImportReceipt.ItemImportReceiptID
            };

            msg = importBatch.Insert(dbm, out var outImportBatch);
            if (msg.Length > 0) return msg;

            var lt = ltItemMangeBatch.Select(x => new ImportBatchDetail(outImportBatch.ImportBatchID, x)).ToList();
            msg = ImportBatchDetail.InsertUpdateByDataType(dbm, lt);
            if (msg.Length > 0) return msg;
        }

        msg = Log.WriteHistoryLog(dbm, $"Tạo phiếu nhập kho", outItemImportReceipt.ObjectGuid, UserToken.UserID);
        if (msg.Length > 0) return msg;

        msg = Log.WriteHistoryLog(dbm, $"Nhập kho theo phiếu đề xuất: <b>{input.ItemProposalForm.ItemProposalFormCode}</b> thành công", input.ItemProposalForm.ObjectGuid, UserToken.UserID);
        if (msg.Length > 0) return msg;

        return "";
    }

    [HttpGet]
    public Result GetListItemImportReceiptType()
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_CRUD);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = ItemImportReceiptType.GetList(out var outLtItemImportReceiptType);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return outLtItemImportReceiptType.ToResultOk();
    }

    [HttpGet]
    public Result GetListItemInStore(long ItemID)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_CRUD);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = ItemImportReceipt.GetListItemInStore(ItemID, UserToken.UserID, out DataTable dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return dt.ToResultOk();
    }

    [HttpGet]
    public Result GetSuggestSearch(string TextSearch, int TypeStore)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        DataTable dt;
        string msg = DoGetSuggestSearch(TextSearch, TypeStore, out dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
        return dt.ToResultOk();
    }
    private string DoGetSuggestSearch(string TextSearch, int TypeStore, out DataTable dt)
    {
        string msg = ItemImportReceipt.GetSuggestSearch(TextSearch, TypeStore, UserToken.AccountID, out dt);
        if (msg.Length > 0) return msg;

        return msg;
    }

    /// <summary>
    /// Tìm kiếm đơn giản
    /// </summary>
    /// <param name="input">các tham số tìm kiếm</param>
    /// <returns>danh sách phiếu nhập kho</returns>
    [HttpPost]
    public Result GetListEasySearch([FromBody] StoreEasySearch input)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = DoGetListEasySearch(input, out int total, out List<StoreSearchResult> lt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return new { Data = lt, Total = total }.ToResultOk();
    }
    private string DoGetListEasySearch([FromBody] StoreEasySearch input, out int Total, out List<StoreSearchResult> lt)
    {
        lt = new List<StoreSearchResult>();
        Total = 0;

        string msg = DoGetListEasySearch_GetStoreSearch(input, out StoreSearch storeSearch);
        if (msg.Length > 0) return msg;

        msg = DoGetList(storeSearch, out lt, out Total);
        if (msg.Length > 0) return msg;


        return msg;
    }
    private string DoGetListEasySearch_GetStoreSearch(StoreEasySearch input, out StoreSearch storeSearch)
    {
        storeSearch = new StoreSearch();

        storeSearch.CurrentPage = input.CurrentPage;
        storeSearch.PageSize = input.PageSize;
        storeSearch.TextSearch = input.TextSearch;
        storeSearch.TypeStore = input.TypeStore;

        if (input.ObjectCategory > 0) storeSearch.TextSearch = "";

        if (input.ObjectCategory == 1 || input.ObjectCategory == 3 || input.ObjectCategory == 4) storeSearch.StoreItemIDs = input.ObjectID.ToString();
        if (input.ObjectCategory == 2) storeSearch.ItemProposalFormIDs = input.ObjectID.ToString();

        return "";
    }
    [HttpPost]
    public Result GetListAdvancedSearch([FromBody] StoreSearch storeSearch)
    {
        try
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListAdvancedSearch(storeSearch, out int total, out List<StoreSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        catch (Exception ex)
        {
            return Log.ProcessError(ex.ToString()).ToResultError();
        }
    }
    private string DoGetListAdvancedSearch([FromBody] StoreSearch itemSearch, out int Total, out List<StoreSearchResult> lt)
    {
        lt = null;
        Total = 0;

        string msg = DoGetList(itemSearch, out lt, out Total);
        if (msg.Length > 0) return msg;

        return "";
    }
    private string DoGetList(StoreSearch storeSearch, out List<StoreSearchResult> lt, out int totalSearch)
    {
        lt = null; totalSearch = 0;

        try
        {
            if (storeSearch == null) return "Tham số không được phép null".ToMessageForUser();

            storeSearch.AccountID = UserToken.AccountID;
            storeSearch.UserID = UserToken.UserID;

            string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_ViewAll, out bool isViewAll);
            if (msg.Length > 0) return msg;
            storeSearch.IsViewAll = isViewAll;

            if (storeSearch.CreateDateCategoryID == 0) storeSearch.DateFrom = storeSearch.DateTo = DateTime.Parse("1900-01-01");
            else
            {
                msg = StoreItemSearchCategoryDateID.GetDateByCategoryID(storeSearch.CreateDateCategoryID, storeSearch.DateFrom, storeSearch.DateTo, out DateTime fromDate, out DateTime toDate);
                if (msg.Length > 0) return msg;
                storeSearch.DateFrom = fromDate;
                storeSearch.DateTo = toDate;
            }

            //string msg = DataValidator.Validate(itemSearch).ToErrorMessage();
            //if (msg.Length > 0) return msg.ToMessageForUser();

            msg = StoreSearch.GetListPaging(storeSearch, out lt, out totalSearch);
            if (msg.Length > 0) return msg;

            return msg;
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    /// <summary>
    /// Lấy thông tin mã Phiếu nhập kho, xuất kho
    /// TypeStore = 1 - nhập kho
    /// TypeStore = 2 - xuất kho
    /// </summary>
    /// <param name="TypeStore"></param>
    /// <returns></returns>
    [HttpGet]
    public Result GetListStoreItemCode(int TypeStore)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_ViewAll, out bool isViewAll);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = StoreSearch.SelectStoreItemCode(UserToken.AccountID, UserToken.UserID, TypeStore, isViewAll, out DataTable dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return dt.ToResultOk();
    }

    /// <summary>
    /// Lấy thông tin hóa đơn
    /// TypeStore = 1 - nhập kho
    /// TypeStore = 2 - xuất kho
    /// </summary>
    /// <param name="TypeStore"></param>
    /// <returns></returns>
    [HttpGet]
    public Result GetListInvoiceNumber(int TypeStore)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_ViewAll, out bool isViewAll);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = StoreSearch.SelectInvoiceNumber(UserToken.AccountID, UserToken.UserID, TypeStore, isViewAll, out DataTable dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return dt.ToResultOk();
    }

    /// <summary>
    /// Lấy thông tin chứng từ
    /// TypeStore = 1 - nhập kho
    /// TypeStore = 2 - xuất kho
    /// </summary>
    /// <param name="TypeStore"></param>
    /// <returns></returns>
    [HttpGet]
    public Result GetListVouchersNumber(int TypeStore)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_ViewAll, out bool isViewAll);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = StoreSearch.SelectVouchersNumber(UserToken.AccountID, UserToken.UserID, TypeStore, isViewAll, out DataTable dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return dt.ToResultOk();
    }

    /// <summary>
    /// lấy thông tin mã phiếu đề xuất nhập, xuất
    /// TypeStore = 1 - nhập kho
    /// TypeStore = 2 - xuất kho
    /// </summary>
    /// <param name="TypeStore"></param>
    /// <returns></returns>
    [HttpGet]
    public Result GetListItemProposalFormCode(int TypeStore)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_ViewAll, out bool isViewAll);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = StoreSearch.SelectItemProposalFormCode(UserToken.AccountID, UserToken.UserID, TypeStore, isViewAll, out DataTable dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return dt.ToResultOk();
    }

    /// <summary>
    /// Lấy thông tin loại nhập
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public Result GetListStoreCategoryIn()
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = ItemImportReceiptType.GetList(out List<ItemImportReceiptType> dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return dt.ToResultOk();
    }

    [HttpPost]
    public Result ExportExportReceiptAS(StoreSearchExportExcel storeSearch)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_IsVisitPage);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = DoExportExportReceipt(storeSearch, out string FileUrl);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return FileUrl.ToResultOk();
    }

    [HttpPost]
    public Result ExportExportReceiptES(StoreEasySearch storeSearchES)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_IsVisitPage);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = DoGetListEasySearch_GetStoreSearch(storeSearchES, out StoreSearch storeSearch);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        msg = DoExportExportReceipt(storeSearch, out string FileUrl);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return FileUrl.ToResultOk();
    }
    private string DoExportExportReceipt(StoreSearch storeSearch, out string FileUrl)
    {
        FileUrl = null;

        storeSearch.AccountID = UserToken.AccountID;
        storeSearch.UserID = UserToken.UserID;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_ViewAll, out bool isViewAll);
        if (msg.Length > 0) return msg;
        storeSearch.IsViewAll = isViewAll;

        msg = StoreSearch.GetListPaging(storeSearch, out List<StoreSearchResult> lt, out int _);
        if (msg.Length > 0) return msg;

        int i = 0;
        var lt2 = lt.Select(x => new ItemImportReceiptExcel(++i, x)).ToList();

        string datetimeLabel = "";
        if (lt.Any()) datetimeLabel = $"(Từ ngày { lt.Select(x => x.DateIn).Min().ToString("dd/MM/yyyy")} đến ngày { lt.Select(x => x.DateIn).Max().ToString("dd/MM/yyyy")})";

        msg = FillDataExportReceipt(datetimeLabel, lt2, out FileUrl);
        if (msg.Length > 0) return msg;

        return msg;
    }
    private string FillDataExportReceipt(string LableTime, List<ItemImportReceiptExcel> lt, out string urlFile)
    {
        string nameFile = $"DanhSachSoPhieuNhapKho_{DateTime.Now.ToString("yyyyMMddHH")}.xlsx";
        urlFile = $@"\File\FileExport\{DateTime.Now.Year}\{DateTime.Now.Month}\{DateTime.Now.Day}\{Guid.NewGuid()}\{nameFile}";

        string msg = BSS.Common.GetSetting("FolderFileExportDSPNK", out string FolderFileExportDSPNK);
        if (msg.Length > 0) return msg;

        var InfoFile = UtilitiesFile.GetInfoFile(DateTime.Now, nameFile, FolderFileExportDSPNK, true);
        try
        {
            using (ExcelPackage pack = new ExcelPackage(new FileInfo(HttpContext.Current.Server.MapPath(@"\File\FileTemplate\TemplateDSPNK.xlsx"))))
            {
                ExcelWorksheet ws = pack.Workbook.Worksheets["DanhSachNhapHang_KV05012022-112"];
                ws.Cells["A2"].Value = LableTime;
                var dt = lt.ToDataTable();
                ws.Cells["A5"].LoadFromDataTable(dt, false);
                ws.Name = $"DanhSachSoPhieuNhapKho_{DateTime.Now.ToString("yyyyMMddHH")}.xlsx";
                /*using (var range = ws.Cells[2, 1, 35, dt.Columns.Count])
                {
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Font.Size = 12;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.White);
                    range.Style.Font.Color.SetColor(Color.Black);
                }

                for (int i = 5; i < dt.Rows.Count + 1; i++)
                    ws.Row(i).Height = 15;*/

                pack.SaveAs(new FileInfo(InfoFile.FilePathPhysical));
                urlFile = InfoFile.FilePathVirtual;
            }
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }

        return string.Empty;
    }

    [HttpGet]
    public Result ViewDetailImportReceipt(Guid ObjectGuid)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = DoViewDetailImportReceipt(ObjectGuid, out var outItem);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return outItem.ToResultOk();
    }
    private string DoViewDetailImportReceipt(Guid ObjectGuid, out ItemImportReceiptViewDetail outItemImportReceiptViewDetail)
    {
        outItemImportReceiptViewDetail = null;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_IsVisitPage);
        if (msg.Length > 0) return msg;

        msg = CacheObject.GetStoreIDbyGUID(ObjectGuid, out long storeID);
        if (msg.Length > 0) return msg;

        msg = ItemImportReceiptViewDetail.ViewDetail(storeID, out outItemImportReceiptViewDetail);
        if (msg.Length > 0) return msg;

        msg = ItemImportReceiptDetailView.GetOne(storeID, out var outlt);
        if (msg.Length > 0) return msg;

        outItemImportReceiptViewDetail.ltItemImport = outlt;

        return string.Empty;
    }

    [HttpGet]
    public Result ViewDetailExportReceipt(Guid ObjectGuid)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = DoViewDetailExportReceipt(ObjectGuid, out var outItem);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return outItem.ToResultOk();
    }
    private string DoViewDetailExportReceipt(Guid ObjectGuid, out ItemExportReceiptViewDetail outItemImportReceiptViewDetail)
    {
        outItemImportReceiptViewDetail = null;

        string msg = Role.Check(UserToken.UserID, TabID.QLPXK, Role.ROLE_QLPXK_IsVisitPage);
        if (msg.Length > 0) return msg;

        msg = CacheObject.GetStoreIDbyGUID(ObjectGuid, out long storeID);
        if (msg.Length > 0) return msg;

        msg = ItemExportReceiptViewDetail.ViewDetail(storeID, out outItemImportReceiptViewDetail);
        if (msg.Length > 0) return msg;

        msg = ItemExportReceiptDetailView.GetList(storeID, out var outlt);
        if (msg.Length > 0) return msg;
        outItemImportReceiptViewDetail.ltItemExport = outlt;

        return string.Empty;
    }


    [HttpPost]
    public Result PrintImportReceiptPDF(Guid ObjectGuid)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = DoPrintImportReceiptPDF(ObjectGuid, out string urlFile);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return urlFile.ToResultOk();

    }
    private string DoPrintImportReceiptPDF(Guid ObjectGuid, out string UrlFile)
    {
        UrlFile = "";

        string msg = CacheObject.GetStoreIDbyGUID(ObjectGuid, out long itemProposalFormID);
        if (msg.Length > 0) return msg;

        msg = ItemImportReceiptExportWord.GetOne(itemProposalFormID, out ItemImportReceiptExportWord itemImportReceiptExportWord);
        if (msg.Length > 0) return msg;

        LtItemImportReceiptExportWord ltItemProposalFormExportWord = new LtItemImportReceiptExportWord();
        msg = ltItemProposalFormExportWord.SetLtItemProposalFormExportWord(itemProposalFormID);
        if (msg.Length > 0) return msg;

        itemImportReceiptExportWord.SetTienVietBangChu(ltItemProposalFormExportWord.GetAmount());

        string nameFileDoc = $"PhieuNhapKho_{DateTime.Now.ToString("yyyyMMddHH")}.docx";
        string nameFilePDF = $"PhieuNhapKho_{DateTime.Now.ToString("yyyyMMddHH")}.pdf";

        msg = BSS.Common.GetSetting("FolderFileExportDSPNK", out string FolderFileExportPNK);
        if (msg.Length > 0) return msg;

        var InfoFileWord = UtilitiesFile.GetInfoFile(DateTime.Now, nameFileDoc, FolderFileExportPNK, true);
        var InfoFilePDF = UtilitiesFile.GetInfoFile(DateTime.Now, nameFilePDF, FolderFileExportPNK, true);

        msg = WordDocument.FillTemplate(HttpContext.Current.Server.MapPath(@"\File\FileTemplate\TemplatePNK.dotx"), InfoFileWord.FilePathPhysical, InfoFilePDF.FilePathPhysical, itemImportReceiptExportWord, ltItemProposalFormExportWord);
        if (msg.Length > 0) return msg;

        UrlFile = InfoFilePDF.FilePathVirtual;

        return string.Empty;
    }

    /// <summary>
    /// GetListSearch Mã phiếu nhập
    /// </summary>
    /// <param name="TextSearch"></param>
    /// <returns></returns>
    [HttpGet]
    public Result GetListItemImportReceiptCode(string TextSearch)
    {
        if (!ResultCheckToken.isOk) return ResultCheckToken;

        string msg = DoGetListItemImportReceiptCode(TextSearch, out DataTable dt);
        if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        return dt.ToResultOk();
    }
    private string DoGetListItemImportReceiptCode(string textSearch, out DataTable dt)
    {
        dt = null;

        string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_IsVisitPage);
        if (msg.Length > 0) return msg;

        msg = ItemImportReceipt.GetListItemImportReceiptCode(textSearch, out dt);
        if (msg.Length > 0) return msg;

        return string.Empty;
    }
    /*    /// <summary>
        /// GetListSearch Số hóa đơn
        /// </summary>
        /// <param name="TextSearch"></param>
        /// <returns></returns>
        [HttpGet]
        public Result GetListInvoiceNumber(string TextSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListInvoiceNumber(TextSearch, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return dt.ToResultOk();
        }
        private string DoGetListInvoiceNumber(string textSearch, out DataTable dt)
        {
            dt = null;

            string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_IsVisitPage);
            if (msg.Length > 0) return msg;

            msg = ItemImportReceipt.GetListInvoiceNumber(textSearch, out dt);
            if (msg.Length > 0) return msg;

            return string.Empty;
        }

        /// <summary>
        /// GetListSearch Số chứng từ
        /// </summary>
        /// <param name="TextSearch"></param>
        /// <returns></returns>
        [HttpGet]
        public Result GetListVouchersNumber(string TextSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListVouchersNumber(TextSearch, out DataTable dt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return dt.ToResultOk();
        }
        private string DoGetListVouchersNumber(string textSearch, out DataTable dt)
        {
            dt = null;

            string msg = Role.Check(UserToken.UserID, TabID.QLPNK, Role.ROLE_QLPNK_IsVisitPage);
            if (msg.Length > 0) return msg;

            msg = ItemImportReceipt.GetListVouchersNumber(textSearch, out dt);
            if (msg.Length > 0) return msg;

            return string.Empty;
        }*/
}
