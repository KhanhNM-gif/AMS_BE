using ASM_API.App_Start.Store;
using ASM_API.App_Start.StoreManagement;
using BSS;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace ASM_API.Controllers
{
    public class StoreManagementController : Authentication
    {
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
            string msg = StoreManagement.GetSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpPost]
        public Result GetListEasySearch(StoreManagementEasySearch storeManagementEasySearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.KHOVP, Role.ROLE_KHOVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDoGetListEasySearch(storeManagementEasySearch, out int total, out List<StoreManagement> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total, TotalItemInStore = lt.Sum(x => x.QuantityInStore) }.ToResultOk();
        }
        private string DoDoGetListEasySearch(StoreManagementEasySearch itemProposalFormEasySearch, out int total, out List<StoreManagement> lt)
        {
            lt = null;
            total = 0;

            string msg = DoGetListEasySearch_GetStoreManagementSearch(itemProposalFormEasySearch, out StoreManagementSearch outItemproposalFormSearch);
            if (msg.Length > 0) return msg;

            outItemproposalFormSearch.PageSize = itemProposalFormEasySearch.PageSize;
            outItemproposalFormSearch.CurrentPage = itemProposalFormEasySearch.CurrentPage;
            //outItemproposalFormSearch.StatusIDs = "1,2,3";// chỉ hiện trạng thái MT,CD,DD trên ds easy search

            msg = DoGetList(outItemproposalFormSearch, out lt, out total);
            return msg;

        }
        private string DoGetListEasySearch_GetStoreManagementSearch(StoreManagementEasySearch storeManagementEasySearch, out StoreManagementSearch ms)
        {
            ms = new StoreManagementSearch();

            ms.TextSearch = storeManagementEasySearch.TextSearch;
            ms.CurrentPage = storeManagementEasySearch.CurrentPage;
            ms.PageSize = storeManagementEasySearch.PageSize;

            if (storeManagementEasySearch.ObjectCategory == 1) ms.ItemTypes = storeManagementEasySearch.ObjectID.ToString();
            if (storeManagementEasySearch.ObjectCategory == 2) ms.ItemID = storeManagementEasySearch.ObjectID.ToNumber(0);
            if (storeManagementEasySearch.ObjectCategory == 3) ms.PlaceIDs = storeManagementEasySearch.ObjectID.ToString();
            if (storeManagementEasySearch.ObjectCategory == 4) ms.BatchID = storeManagementEasySearch.ObjectID.ToNumber(0);

            if (storeManagementEasySearch.ObjectCategory > 0) ms.TextSearch = "";

            return "";
        }
        private string DoGetList(StoreManagementSearch formSearch, out List<StoreManagement> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                formSearch.AccountID = UserToken.AccountID;
                formSearch.UserID = UserToken.UserID;

                string msg = StoreManagement.GetListPaging(formSearch, out lt, out totalSearch);
                if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    msg = DoGetListButtonFuction(item, out ButtonShowQLK b);
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
        private string DoGetListButtonFuction(StoreManagement sm, out ButtonShowQLK b)
        {
            b = new ButtonShowQLK();

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_CRUD, out bool isRole);
            if (msg.Length > 0) return msg;
            b.CreateItemProposalForm = isRole && (sm.StatusID == 1 || sm.StatusID == 2);

            return "";
        }

        [HttpPost]
        public Result GetListAdvancedSearch(StoreManagementSearch data)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = Role.Check(UserToken.UserID, Constants.TabID.KHOVP, Role.ROLE_KHOVP_IsVisitPage);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                msg = DoGetListAdvancedSearch(data, out int total, out List<StoreManagement> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total, TotalItemInStore = lt.Sum(x => x.QuantityInStore) }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch(StoreManagementSearch storeSearch, out int Total, out List<StoreManagement> lt)
        {
            Total = 0; lt = null;
            string msg;

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
        public Result GetListBatch(long ItemID, int PlaceID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.KHOVP, Role.ROLE_KHOVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetListBatch(ItemID, PlaceID, out var outLtImportBatchDetailView);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return outLtImportBatchDetailView.ToResultOk();
        }
        private string DoGetListBatch(long ItemID, int PlaceID, out List<ImportBatchDetailView> outLtImportBatchDetailView)
        {
            string msg = ImportBatchDetailView.GetListInfoBatch(ItemID, PlaceID, out outLtImportBatchDetailView);
            if (msg.Length > 0) return msg;

            return string.Empty;
        }

        [HttpGet]
        public Result GetListStatus()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = StoreManagementStatus.GetList(out var lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpPost]
        public Result ExportExportReceiptAS(StoreManagementASExport storeSearchAS)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.KHOVP, Role.ROLE_KHOVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoExportExportReceipt(storeSearchAS, out string FileUrl);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return FileUrl.ToResultOk();
        }

        [HttpPost]
        public Result ExportExportReceiptES(StoreManagementESExport storeSearchAS)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.KHOVP, Role.ROLE_KHOVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetListEasySearch_GetStoreManagementSearch(storeSearchAS, out StoreManagementSearch storeSearch);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoExportExportReceipt(storeSearch, out string FileUrl);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return FileUrl.ToResultOk();
        }
        private string DoExportExportReceipt(StoreManagementSearch storeSearch, out string FileUrl)
        {
            FileUrl = null;

            storeSearch.AccountID = UserToken.AccountID;
            storeSearch.UserID = UserToken.UserID;

            string msg;

            if (storeSearch.CreateDateCategoryID == 0) storeSearch.DateFrom = storeSearch.DateTo = null;
            else
            {
                msg = StoreItemSearchCategoryDateID.GetDateByCategoryID(storeSearch.CreateDateCategoryID, storeSearch.DateFrom, storeSearch.DateTo, out DateTime fromDate, out DateTime toDate);
                if (msg.Length > 0) return msg;

                storeSearch.DateFrom = fromDate;
                storeSearch.DateTo = toDate;
            }

            msg = StoreManagement.GetListPaging(storeSearch, out var lt, out int _);
            if (msg.Length > 0) return msg;

            int i = 0;

            List<StoreManagementExcel> ltStoreManagementExcel = new List<StoreManagementExcel>();

            foreach (var item in lt)
            {
                msg = ImportBatchDetailView.GetListInfoBatch(item.ItemID, item.PlaceID, out var batchs);
                if (msg.Length > 0) return msg;

                var lt2 = new List<StoreManagementExcel>();
                if (batchs.Any())
                    lt2 = batchs.Select(x => new StoreManagementExcel(++i, item, x)).ToList();
                else lt2.Add(new StoreManagementExcel(++i, item, new ImportBatchDetailView()));

                ltStoreManagementExcel.AddRange(lt2);
            }

            string datetimeLabel = storeSearch.DateFrom is null ? "" : $"Từ ngày { storeSearch.DateFrom.GetValueOrDefault().ToString("dd/MM/yyyy")} ";
            datetimeLabel += storeSearch.DateTo is null ? "" : $"đến ngày { storeSearch.DateTo.GetValueOrDefault().ToString("dd/MM/yyyy")}";

            if (!string.IsNullOrEmpty(datetimeLabel))
                datetimeLabel = $"({char.ToUpper(datetimeLabel[0])}{datetimeLabel.Substring(1)})";

            msg = FillDataExportReceipt(datetimeLabel, ltStoreManagementExcel, out FileUrl);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string FillDataExportReceipt(string LableTime, List<StoreManagementExcel> lt, out string urlFile)
        {
            string nameFile = $"DanhSachPhieKiemKe_{DateTime.Now.ToString("yyyyMMddHH")}.xlsx";
            urlFile = $@"\File\FileExport\{DateTime.Now.Year}\{DateTime.Now.Month}\{DateTime.Now.Day}\{Guid.NewGuid()}\{nameFile}";

            string msg = BSS.Common.GetSetting("FolderFileExportDSKKVP", out string FolderFileExportDSKKVP);
            if (msg.Length > 0) return msg;

            var InfoFile = UtilitiesFile.GetInfoFile(DateTime.Now, nameFile, FolderFileExportDSKKVP, true);
            try
            {
                using (ExcelPackage pack = new ExcelPackage(new FileInfo(HttpContext.Current.Server.MapPath(@"\File\FileTemplate\TemplatePKKVP.xlsx"))))
                {
                    ExcelWorksheet ws = pack.Workbook.Worksheets["DanhSachNhapHang_KV05012022-112"];
                    ws.Cells["A2"].Value = LableTime;
                    var dt = lt.ToDataTable();
                    ws.Cells["A5"].LoadFromDataTable(dt, false);

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
        public Result ViewDetail(long ImportBatchID, long ItemID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.KHOVP, Role.ROLE_KHOVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoViewDetail(ImportBatchID, ItemID, out var storeManagementViewDetail);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return storeManagementViewDetail.ToResultOk();
        }
        private string DoViewDetail(long ImportBatchID, long ItemID, out ImportBatchViewDetail outImportBatchViewDetail)
        {
            string msg = ImportBatchViewDetail.GetOne(ImportBatchID, UserToken.AccountID, ItemID, out outImportBatchViewDetail);
            if (msg.Length > 0) return msg;
            if (outImportBatchViewDetail is null) return $"ImportBatchID={ImportBatchID},ItemID={ItemID} => ImportBatchViewDetail is null";

            msg = ImportBatchDetailView2.GetListInfoBatch(ItemID, outImportBatchViewDetail.PlaceID, out var lt);
            if (msg.Length > 0) return msg;
            outImportBatchViewDetail.ltImportBatchDetailView = lt;

            return string.Empty;
        }
    }
}