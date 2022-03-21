using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using static Constants;

namespace WebAPI.Controllers
{
    public class AssetController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertUpdate(data, out Asset assetOut);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return assetOut.ToResultOk();
        }
        private string DoInsertUpdate([FromBody] JObject data, out Asset assetOut)
        {
            assetOut = new Asset();
            string msg = data.ToObject("Asset", out Asset assetInput);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_Validate(assetInput, out bool isUpdate, out Asset assetDB);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_PrepareData(assetInput);
            if (msg.Length > 0) return msg;

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoInsertUpdate_ObjectToDB(dbm, assetInput, isUpdate, assetDB, out assetOut);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at Asset DoInsertUpdate ";
            }

            dbm.CommitTransac();

            return msg;
        }
        private string DoInsertUpdate_Validate(Asset assetInput, out bool isUpdate, out Asset assetDB)
        {
            isUpdate = false;
            assetDB = null;

            string msg = "";
            msg = DataValidator.Validate(new
            {
                assetInput.ObjectGuid,
                assetInput.AssetTypeID,
                assetInput.AssetCode,
                assetInput.PlaceID,
                assetInput.UserIDApprove,
                assetInput.UserIDCreate,
                assetInput.AssetStatusID,
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (assetInput.AssetTypeID == 0) return ("Đã chưa chọn loại tài sản").ToMessageForUser();

            if (!string.IsNullOrEmpty(assetInput.AssetSerial))
                if (assetInput.AssetSerial.Length > 200) return ("Serial chỉ được phép nhập tối đa 200 ký tự").ToMessageForUser();

            if (!string.IsNullOrEmpty(assetInput.AssetModel))
                if (assetInput.AssetModel.Length > 200) return ("Model chỉ được phép nhập tối đa 200 ký tự").ToMessageForUser();

            if (assetInput.UserIDCreate == 0 || assetInput.UserIDHolding == 0) return ("Bạn chưa chọn người quản lý tài sản").ToMessageForUser();
            if (assetInput.ObjectGuid != Guid.Empty)
            {
                msg = Asset.GetOneByGuid(assetInput.ObjectGuid, out assetDB);
                if (msg.Length > 0) return msg;

                assetInput.AssetID = assetDB.AssetID;
                isUpdate = true;
            }

            if (assetInput.ListAssetProperty == null) assetInput.ListAssetProperty = new List<AssetProperty>();

            if (assetInput.AssetDateIn < assetInput.AssetDateBuy) return ("Bạn không được nhập Ngày mua lớn hơn Ngày nhập").ToMessageForUser();

            if (assetInput.AssetDateBuy > DateTime.Now) return ("Ngày mua không thể lớn hơn ngày hiện tại").ToMessageForUser();

            msg = Asset.GetByAssetCode(assetInput.AssetCode, UserToken.AccountID, out Asset assetExistCode);
            if (msg.Length > 0) return msg;
            if (assetExistCode != null && assetInput.AssetID != assetExistCode.AssetID) return ("Đã tồn tại Mã tài sản trong hệ thống. Bạn hãy nhập Mã tài sản khác").ToMessageForUser();

            //kiểm tra serial, model tài sản đã tồn tại
            msg = Asset.CheckExistAsset(assetInput.AssetModel, assetInput.AssetSerial, UserToken.AccountID, out Asset assetExist);
            if (msg.Length > 0) return msg;
            if (assetExist != null && assetInput.AssetID != assetExist.AssetID) return ("Đã tồn tại dữ liệu Model, Serial: " + assetInput.AssetModel + ", " + assetInput.AssetSerial + " trong hệ thống").ToMessageForUser();

            //Kiểm tra trường hợp gửi approve nhưng không chọn người duyệt
            if (assetInput.IsSendApprove && assetInput.UserIDApprove == 0) return ("Bạn chưa chọn Người duyệt tài sản").ToMessageForUser();

            //kiểm tra input thuộc tính động
            //validate kiểu dữ liệu thuộc tính tài sản
            if (assetInput.ListAssetProperty != null)
            {
                msg = AssetProperty.ValidateProperty(assetInput.ListAssetProperty);
                if (msg.Length > 0) return msg.ToMessageForUser();
            }

            if (!string.IsNullOrEmpty(assetInput.AssetImageContentBase64))
            {
                //validate file upload lên có phải là ảnh
                byte[] dataAssetImage = System.Convert.FromBase64String(assetInput.AssetImageContentBase64);
                MemoryStream ms = new MemoryStream(dataAssetImage);
                msg = ImageChecker.Check(ms, out bool isImage);
                if (msg.Length > 0) return msg;
                if (!isImage) return "File upload không phải là dạng ảnh, bạn chỉ được phép upload dưới định dạng .jpg,jpeg,png,bpm".ToMessageForUser();
            }
            return msg;
        }
        private string DoInsertUpdate_PrepareData(Asset assetInput)
        {
            string msg = "";
            //trường hợp nhấn ghi lại và gửi duyệt thì set trạng thái của tài sản là chờ duyệt
            if (assetInput.IsSendApprove) assetInput.AssetStatusID = StatusAsset.CD;
            else assetInput.AssetStatusID = StatusAsset.MT;

            assetInput.AccountID = UserToken.AccountID;

            if (!string.IsNullOrEmpty(assetInput.AssetImageContentBase64))
            {
                //lưu ảnh trên server và trả về đường dẫn ảnh
                msg = DoInsertUpdate_PrepareData_GetPathtImage(assetInput, out string imagePath);
                if (msg.Length > 0) return msg;
                assetInput.AssetImagePath = imagePath;
            }

            return msg;
        }
        private string DoInsertUpdate_PrepareData_GetPathtImage(Asset asset, out string urlFile)
        {
            string msg = "";
            urlFile = "";
            try
            {
                byte[] fileContent = Convert.FromBase64String(asset.AssetImageContentBase64);

                Guid guid = Guid.NewGuid();
                msg = BSS.Common.GetSetting("PathFileImageAsset", out string PathFileImageAsset);
                if (msg.Length > 0) return msg;

                string folderFileImageAsset = HttpContext.Current.Server.MapPath(PathFileImageAsset);

                folderFileImageAsset = folderFileImageAsset + "/" + guid;
                if (!Directory.Exists(folderFileImageAsset)) Directory.CreateDirectory(folderFileImageAsset);

                File.WriteAllBytes(folderFileImageAsset + "/" + asset.AssetImageName, fileContent);

                urlFile = PathFileImageAsset + "/" + guid + "/" + asset.AssetImageName;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return msg;
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, Asset assetInput, bool isUpdate, Asset assetDB, out Asset assetOut)
        {
            //ghi lại thông tin Tài sản
            string msg = assetInput.InsertUpdate(dbm, out assetOut);
            if (msg.Length > 0) return msg;

            //xóa thuộc tính cũ của tài sản khi thay đổi loại tài sản
            if (assetDB != null && assetInput.AssetTypeID != assetDB.AssetTypeID)
            {
                msg = AssetProperty.DeletePropertyByAssetID(dbm, assetInput.AssetID);
                if (msg.Length > 0) return msg;
            }

            // ghi lại giá trị thuộc tính động của Tài sản
            if (assetInput.ListAssetProperty.Count > 0)
            {
                msg = DoInsertUpdate_AssetProperty(dbm, assetOut.AssetID, assetInput.ListAssetProperty, out List<AssetProperty> outassetProperties, isUpdate);
                if (msg.Length > 0) return msg;
                assetOut.ListAssetProperty = outassetProperties;
            }

            msg = Log.WriteHistoryLog(dbm, assetInput.AssetID == 0 ? "Thêm tài sản" : "Sửa tài sản", assetOut.ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            //trường hợp nhấn gửi duyệt
            if (assetInput.IsSendApprove)
            {
                msg = AssetApprove.Insert(dbm, assetOut.AssetID.ToString(), "Yêu cầu duyệt thông tin Tài sản cần quản lý", assetInput.UserIDApprove);
                if (msg.Length > 0) return msg;

                msg = Log.WriteHistoryLog(dbm, "Yêu cầu duyệt thông tin Tài sản cần quản lý", assetOut.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return msg;
        }
        private string DoInsertUpdate_AssetProperty(DBM dbm, long AssetID, List<AssetProperty> ltassetProperty, out List<AssetProperty> outassetProperties, bool isUpdate = false)
        {
            outassetProperties = new List<AssetProperty>();
            string msg = "";

            //nếu cập nhật tài sản thì xóa các thuộc tính động trước đó
            if (isUpdate)
            {
                msg = AssetProperty.DeletePropertyByAssetID(dbm, AssetID);
                if (msg.Length > 0) return msg;
            }

            foreach (var item in ltassetProperty)
            {
                AssetProperty assproperty = new AssetProperty
                {
                    //nếu cập nhật tài sản thì gán lại ID = 0
                    ID = isUpdate ? 0 : item.ID,
                    AssetID = AssetID,
                    AssetPropertyID = item.AssetPropertyID,
                    AssetPropertyName = item.AssetPropertyName,
                    Value = item.Value,
                };

                msg = assproperty.InsertUpdate(dbm, out AssetProperty asspropertyOut);
                if (msg.Length > 0) return msg;

                outassetProperties.Add(asspropertyOut);
            }
            return msg;
        }

        [HttpGet]
        public Result GetListAssetByObjectGuids(string ObjectGuids)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Asset.GetAssetIDsByObjectGuids(ObjectGuids, out string AssetIDs);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = Asset.GetSearchByAssetIDs(AssetIDs, UserToken.AccountID, out DataTable lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpGet]
        public Result GetListAsset()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Asset.GetAssetList(out List<Asset> assetList);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            if (assetList.Count == 0) return "Không tồn tại danh sách tài sản".ToResultError();

            return assetList.ToResultOk();
        }

        [HttpGet]
        public Result GetListAssetCode()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Asset.GetAssetCodeList(out DataTable Data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return Data.ToResultOk();
        }

        [HttpGet]
        public Result GetOne(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            Asset o;
            msg = Asset.GetOneByGuid(ObjectGuid, out o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            if (o == null) return o.ToResultOk();

            msg = AssetProperty.GetListByAssetID(o.AssetID, out List<AssetProperty> typeProperties);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            o.ListAssetProperty = typeProperties;

            return o.ToResultOk();
        }
        [HttpGet]
        public Result GetOneByAssetCode(string AssetCode)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = Asset.ViewDetailByAssetCode(AssetCode, UserToken.AccountID, out AssetViewDetail o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            if (o == null) return o.ToResultOk();

            return o.ToResultOk();
        }

        [HttpGet]
        public Result ViewDetail(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoViewDetail(ObjectGuid, out AssetViewDetail o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }
        public string DoViewDetail(Guid ObjectGuid, out AssetViewDetail o)
        {
            o = null;

            string msg = CacheObject.GetAssetIDbyGUID(ObjectGuid, out long assetID);
            if (msg.Length > 0) return msg;

            msg = Asset.AssetViewDetailByGuid(assetID, UserToken.AccountID, out o);
            if (msg.Length > 0) return msg;
            if (o == null) return "AssetViewDetail == null";

            msg = AssetProperty.GetListByAssetID(o.AssetID, out List<AssetProperty> typeProperties);
            if (msg.Length > 0) return msg;

            o.ListAssetProperty = typeProperties;

            msg = DoViewDetail_GetUsePerformance(assetID, out string strUsePerformance);
            if (msg.Length > 0) return msg;

            o.UsePerformance = strUsePerformance;

            return "";
        }
        private string DoViewDetail_GetUsePerformance(long AssetID, out string strUsePerformance)
        {
            strUsePerformance = "";

            string msg = Asset.GetOneByAssetID(AssetID, out Asset asset);
            if (msg.Length > 0) return msg;

            msg = AssetUse.GetListHistoryUse(AssetID, out List<AssetUse> LtAssetUse);
            if (msg.Length > 0) return msg;

            List<AssetUse> LtAssetUse_Order = LtAssetUse.OrderBy(v => v.ExecutionDate).ToList();
            msg = AssetUse.GetSumTimeUse(LtAssetUse_Order, out double TotalTimeUse);
            if (msg.Length > 0) return msg;

            DateTime AssetDateBuy = (asset.AssetDateBuy.HasValue ? asset.AssetDateBuy.Value : asset.CreateDate);
            double dUsePerformance = TotalTimeUse / (DateTime.Now - AssetDateBuy).TotalDays * 100;
            strUsePerformance = (int)Math.Round(dUsePerformance) + "%";


            return msg;
        }

        Dictionary<string, string> MappingColumnExcelExport = new Dictionary<string, string>() {
                {STT,STT },
                {"AssetTypeName","Loại tài sản" },
                {"AssetCode","Mã Tài sản" },
                {"AssetSerial","Serial" },
                {"AssetModel","Model" },
                {"PlaceName","Nơi để" },
                {"SupplierName","Nhà cung cấp" },
                {"ProducerName","Hãng sản xuất" },
                {"AssetDateIn","Ngày nhập" },
                {"AssetDateBuy","Ngày mua" },
                {"AssetColor","Màu sắc" },
                {"UserNameHolding","Người quản lý" },
                {"AssetDescription","Ghi chú" },
                {"UsePerformance","Hiệu suất sử dụng" },
                {"AssetStatusName","Trạng thái" },
                };
        private const string STT = "STT", UsePerformance = "Hiệu suất sử dụng";
        [HttpPost]
        public Result ExportEasySearch(AssetEasySearchExport data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListEasySearch(data, out int _, out var lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoExportAsset(lt, true, out string PathFile);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return PathFile.ToResultOk();
        }
        [HttpPost]
        public Result ExportAdvancedSearch(AssetSearchExport data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListAdvancedSearch(data, out int _, out var lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoExportAsset(lt, false, out string PathFile);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return PathFile.ToResultOk();
        }
        private string DoExportAsset(List<AssetSearchResult> lt, bool IsEasySearch, out string urlFile)
        {
            string msg = ""; urlFile = "";
            try
            {
                List<AssetExport> ltexport = new List<AssetExport>();
                foreach (var item in lt)
                {
                    AssetExport export = new AssetExport();
                    msg = BSS.Common.CopyObjectPropertyData(item, export);
                    if (msg.Length > 0) return msg;
                    ltexport.Add(export);
                }

                msg = BSS.Common.GetSetting("FolderFileExport", out string FolderFileExport);
                if (msg.Length > 0) return msg;

                urlFile = FolderFileExport + "/" + "danhsachtaisan_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

                ExcelPackage pack = new ExcelPackage();

                var grltAsset = ltexport.GroupBy(v => v.AssetTypeID);
                foreach (var itemAssetType in grltAsset)
                {
                    DataTable ldt = new DataTable();

                    //Header
                    //Thuộc tính chính
                    foreach (var header in MappingColumnExcelExport) ldt.Columns.Add(header.Value);

                    //Thuộc tính động
                    msg = AssetTypeProperty.GetListByAssetTypeID(itemAssetType.Key, out List<AssetTypeProperty> ltAssetTypeProperty);
                    if (msg.Length > 0) return msg;

                    foreach (var itemAssetTypeProperty in ltAssetTypeProperty) if (!ldt.Columns.Contains(itemAssetTypeProperty.AssetTypePropertyName)) ldt.Columns.Add(itemAssetTypeProperty.AssetTypePropertyName);

                    //Giá trị

                    DataTable dtAsset = itemAssetType.ToList().ToDataTable();

                    for (int i = 0; i < dtAsset.Rows.Count; i++)
                    {
                        DataRow drAsset = dtAsset.Rows[i];
                        DataRow dr = ldt.NewRow();

                        msg = CacheObject.GetAssetIDbyGUID(Guid.Parse(drAsset["ObjectGuid"].ToString()), out long assetID);
                        if (msg.Length > 0) return msg;

                        //Thuộc tính chính   
                        for (int j = 0; j < MappingColumnExcelExport.Count; j++)
                        {
                            string ColumnName = ldt.Columns[j].ColumnName;
                            if (ColumnName == STT) dr[ColumnName] = i + 1;
                            else if (ColumnName == UsePerformance)
                            {
                                msg = DoViewDetail_GetUsePerformance(assetID, out string strUsePerformance);
                                if (msg.Length > 0) return msg;

                                dr[ColumnName] = strUsePerformance;
                            }
                            else
                            {
                                var vColumnExcel = MappingColumnExcelExport.Where(v => v.Value == ColumnName);
                                var nameProperty = vColumnExcel.First().Key;
                                if (dtAsset.Columns.Contains(nameProperty)) dr[ColumnName] = drAsset[nameProperty];
                            }
                        }

                        //Thuộc tính động
                        msg = AssetProperty.GetListByAssetID(assetID, out List<AssetProperty> ltAssetProperty);
                        if (msg.Length > 0) return msg;

                        DataTable dtAssetProperty = ltAssetProperty.ToDataTable();

                        //Thuộc tính chính   
                        for (int j = MappingColumnExcelExport.Count; j < ldt.Columns.Count; j++)
                        {
                            string ColumnName = ldt.Columns[j].ColumnName;

                            foreach (DataRow drAssetProperty in dtAssetProperty.Rows)
                            {
                                if (drAssetProperty["AssetTypePropertyName"].ToString() == ColumnName)
                                    dr[ColumnName] = drAssetProperty["Value"];
                            }
                        }

                        ldt.Rows.Add(dr);
                    }

                    ExcelWorksheet ws = pack.Workbook.Worksheets.Add(itemAssetType.FirstOrDefault().AssetTypeName);
                    ws.Cells["A1"].LoadFromDataTable(ldt, true);
                    ws.Column(1).Width = 5;

                    for (int i = 1; i < ldt.Rows.Count + 2; i++)
                        for (int j = 1; j < ldt.Columns.Count + 1; j++)
                        {
                            if (i == 1) ws.Cells[1, j].Style.Font.Bold = true;

                            var c = ws.Cells[i, j];
                            c.AutoFitColumns();

                            c.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            c.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            c.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            c.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        }
                }

                pack.SaveAs(new System.IO.FileInfo(HttpContext.Current.Server.MapPath(urlFile)));
                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        [HttpPost]
        public Result GetListEasySearch([FromBody] AssetEasySearch assetEasySearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoGetListEasySearch(assetEasySearch, out int total, out List<AssetSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoGetListEasySearch(AssetEasySearch assetEasySearch, out int Total, out List<AssetSearchResult> lt)
        {
            lt = null;
            Total = 0;

            string msg = DoGetListEasySearch_GetAssetSearch(assetEasySearch, out AssetSearch AssetSearch);
            if (msg.Length > 0) return msg;

            return DoGetList(AssetSearch, out lt, out Total);
        }
        private string DoGetListEasySearch_GetAssetSearch(AssetEasySearch assetEasySearch, out AssetSearch ms)
        {
            ms = new AssetSearch();

            ms.TextSearch = assetEasySearch.TextSearch;
            ms.CurrentPage = assetEasySearch.CurrentPage;
            ms.PageSize = assetEasySearch.PageSize;
            ms.CategorySearch = AssetSearch.DONGIAN;
            ms.AssetStatusIDs = StatusAsset.KX.ToString();

            if (assetEasySearch.ObjectCategory > 0) ms.TextSearch = "";

            if (assetEasySearch.ObjectCategory == 1) ms.AssetTypeIDs = assetEasySearch.ObjectID.ToString();
            if (assetEasySearch.ObjectCategory == 2 || assetEasySearch.ObjectCategory == 3) ms.AssetID = assetEasySearch.ObjectID.ToNumber(0);
            if (assetEasySearch.ObjectCategory == 4) ms.UserID = assetEasySearch.ObjectID.ToNumber(0);
            if (assetEasySearch.ObjectCategory == 5) ms.PlaceIDs = assetEasySearch.ObjectID.ToString();

            return "";
        }

        [HttpPost]
        public Result GetListAdvancedSearch([FromBody] AssetSearch AssetSearch)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = DoGetListAdvancedSearch(AssetSearch, out int total, out List<AssetSearchResult> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch(AssetSearch AssetSearch, out int Total, out List<AssetSearchResult> lt)
        {
            lt = null;
            Total = 0;

            AssetSearch.CategorySearch = AssetSearch.NANGCAO;
            string msg = DoGetList(AssetSearch, out lt, out Total);
            if (msg.Length > 0) return msg;

            InsertSPVAdvancedSearch(AssetSearch);

            return "";
        }
        private string DoGetList(AssetSearch AssetSearch, out List<AssetSearchResult> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                AssetSearch.AccountID = UserToken.AccountID;
                AssetSearch.UserID = UserToken.UserID;

                string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_VIEWALL, out bool isViewAll);
                if (msg.Length > 0) return msg;
                AssetSearch.ViewAll = isViewAll;

                if (AssetSearch.AssetDateTo is DateTime AssetDateTo) AssetSearch.AssetDateTo = AssetDateTo.Date.AddDays(1);

                msg = DataValidator.Validate(new
                {
                    AssetSearch.AccountID,
                    AssetSearch.AssetCodes,
                    AssetSearch.AssetDateTo,
                    AssetSearch.AssetDateFrom,
                    AssetSearch.AssetID,
                    AssetSearch.AssetStatusIDs,
                    AssetSearch.AssetTypeIDs,
                    AssetSearch.CategorySearch,
                    AssetSearch.CurrentPage,
                    AssetSearch.PageSize,
                    AssetSearch.PlaceIDs,
                    AssetSearch.SupplierIDs,
                    AssetSearch.TextSearch,
                    AssetSearch.UserID,
                    AssetSearch.UserIDHoldings
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();



                msg = Asset.GetListPaging(AssetSearch, out lt, out totalSearch);
                if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    ButtonShowAsset b;

                    msg = AssetProperty.GetDayExpiredByAssetID(item.AssetID, out string ExpriedDayStr);
                    if (msg.Length > 0) return msg;

                    if (!string.IsNullOrEmpty(ExpriedDayStr))
                    {
                        item.ExpriedDay = ExpriedDayStr.ToNumber(-1);
                    }

                    Asset asset = new Asset
                    {
                        AssetID = item.AssetID,
                        ObjectGuid = item.ObjectGuid,
                        AssetStatusID = item.AssetStatusID,
                        AssetStatusName = item.AssetStatusName,
                        UserIDCreate = item.UserIDCreate,
                        UserIDApprove = item.UserIDApprove,
                        UserIDHolding = item.UserIDHolding,
                        UserIDHandover = item.UserIDHandover,
                        UserIDReturn = item.UserIDReturn
                    };
                    msg = DoGetListButtonFuction(asset, UserToken.UserID, out b);
                    if (msg.Length > 0) return msg;

                    item.AssetStatusName = asset.AssetStatusName;
                    item.AssetStatusID = asset.AssetStatusID;
                    item.ExpiryDate = asset.ExpiryDate;
                    item.ButtonShow = b;
                }
                //sắp xếp theo tài liệu
                lt = lt.OrderBy(x => x.ExpiryDate).ToList();

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private string DoGetListButtonFuction(Asset asset, int UserIDLogin, out ButtonShowAsset b)
        {
            b = new ButtonShowAsset();
            int s = asset.AssetStatusID;
            string msg;
            if (UserIDLogin == asset.UserIDCreate)
            {
                if (s == StatusAsset.MT)
                {
                    b.Delete = true;
                    b.Edit = true;
                    b.SendApprove = true;
                }

                if (s == StatusAsset.ĐX) b.Restore = true;
                if (s == StatusAsset.CD) { b.CancelSendApprove = true; }
                if (s == StatusAsset.ĐD_TK) { b.Handover = true; }
                if (s == StatusAsset.TC) { b.Edit = true; b.Delete = true; }
            }
            msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_THUHOI, out bool isRole_ThuHoi);
            if (msg.Length > 0) return msg;

            if (isRole_ThuHoi && s != StatusAsset.DTL)
            {
                if (s != StatusAsset.MT && s != StatusAsset.ĐX && s != StatusAsset.CD
                 && s != StatusAsset.ĐD_TK && s != StatusAsset.TC) { b.Revoke = true; }
            }
            if (UserIDLogin == asset.UserIDApprove)
                if (s == StatusAsset.CD) b.Approve = true;

            if (UserIDLogin == asset.UserIDHolding)
            {
                if (s == StatusAsset.ĐSD || s == StatusAsset.KXN_T) { b.Return = true; }
                if (s == StatusAsset.CXN_T) b.CancelReturn = true;
            }
            if (UserIDLogin == asset.UserIDHandover)
                if (s == StatusAsset.CXN_BG) b.ComfirmHandover = true;

            if (UserIDLogin == asset.UserIDReturn)
                if (s == StatusAsset.CXN_T) b.ComfirmReturn = true;

            msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_DCKTL, out bool isRole_ChuyenKhoThanhLy);
            if (msg.Length > 0) return msg;
            if (isRole_ChuyenKhoThanhLy && asset.AssetStatusID != StatusAsset.DTL)
            {
                msg = AssetProperty.GetDayExpiredByAssetID(asset.AssetID, out string TotalExpriedDay);
                if (msg.Length > 0) return msg;
                if (!string.IsNullOrEmpty(TotalExpriedDay))
                {
                    int Total = TotalExpriedDay.ToNumber(-1);
                    if (Total > 0 && Total <= 3) { asset.AssetStatusName = "Sắp đến ngày hết hạn"; asset.AssetStatusID = 13; }
                    if (Total <= 0) { asset.AssetStatusName = "Đã đến ngày hết hạn"; asset.AssetStatusID = 14; b.MovePlaceLiquidate = true; asset.ExpiryDate = 1; }
                }
            }
            msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_XNBGTS, out bool isRole_BanGiao);
            if (msg.Length > 0) return msg;

            if (isRole_BanGiao)
            {
                if (s == StatusAsset.ĐD_TK) b.Handover = true;
                if (s == StatusAsset.CXN_BG) b.InHandover = true;
            }

            msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_CRUD, out bool IsEdit);
            if (msg.Length > 0) return msg;
            if (IsEdit && s == StatusAsset.MT)
            {
                b.Delete = true;
                b.Edit = true;
                b.SendApprove = true;
            }
            if (IsEdit && s == StatusAsset.ĐD_TK) b.Edit = true;

            b.ViewHistory = true;

            if (s != StatusAsset.DTL) b.InQRCode = true;

            return "";
        }
        private void InsertSPVAdvancedSearch(AssetSearch assetSearch)
        {
            SPV.InsertSPVSearchAsset(UserToken.UserID, PageGUID.TAI_SAN, new
            {
                assetSearch.AssetID,
                assetSearch.PlaceIDs,
                assetSearch.AccountID,
                assetSearch.UserID,
                assetSearch.TextSearch,
                assetSearch.CategorySearch,
                assetSearch.AssetStatusIDs,
                assetSearch.AssetTypeIDs,
                assetSearch.UserIDHoldings,
                assetSearch.SupplierIDs,
                AssetDateFrom = assetSearch.AssetDateFrom == null ? null : assetSearch.AssetDateFrom?.ToString("yyyy-MM-dd"),
                AssetDateTo = assetSearch.AssetDateTo == null ? null : assetSearch.AssetDateTo?.ToString("yyyy-MM-dd"),
                assetSearch.InputDate,
                assetSearch.CurrentPage,
                assetSearch.PageSize,
                assetSearch.AssetCodes
            });
        }

        [HttpPost]
        public Result Delete([FromBody] JObject data)//Xóa TS
        {
            return UpdateStatusID(data, StatusAsset.ĐX);
        }
        [HttpPost]
        public Result Restore([FromBody] JObject data)//Phục hồi TS
        {
            return UpdateStatusID(data, StatusAsset.MT);
        }
        public Result UpdateStatusID([FromBody] JObject data, int StatusID)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = UpdateStatusID(data, StatusID, UserToken.UserID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return "".ToResultOk();
        }
        private string UpdateStatusID([FromBody] JObject data, int StatusID, int UserID)
        {
            string logContent = "";
            string msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;

            msg = Asset.GetOneByGuid(ObjectGuid, out Asset asset);
            if (msg.Length > 0) return msg;
            if (asset == null) return ("Không tồn tại tài sản Guid = " + ObjectGuid).ToMessageForUser();

            if (StatusID == StatusAsset.MT)
            {
                if (asset.AssetStatusID != StatusAsset.ĐX) return "Bạn chỉ được Khôi phục khi Tài sản ở trạng thái Đã xóa".ToMessageForUser();
                logContent = "Khôi phục Tài sản";
            }
            if (StatusID == StatusAsset.ĐX)
            {
                if (asset.AssetStatusID != StatusAsset.MT && asset.AssetStatusID != StatusAsset.TC) return "Bạn chỉ được xóa khi Tài sản ở trạng thái Mới tạo và Từ chối".ToMessageForUser();
                logContent = "Xóa Tài sản";
            }
            msg = UpdateStatusID_SaveToDB(asset, StatusID, UserID, logContent);
            if (msg.Length > 0) { return msg; }

            return msg;
        }
        private string UpdateStatusID_SaveToDB(Asset asset, int StatusID, int UserID, string logContent)
        {
            string msg = Asset.UpdateStatusID(new DBM(), asset.AssetID, StatusID, UserToken.AccountID);
            if (msg.Length > 0) return msg;

            msg = Log.WriteHistoryLog(logContent, asset.ObjectGuid, UserID, Common.GetClientIpAddress(Request));
            return msg;
        }

        [HttpGet]
        public Result GetListStatus()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            List<AssetStatus> lt;
            string msg = AssetStatus.GetListStatus(out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return lt.ToResultOk();
        }

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

            string msg = Asset.GetSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }

        Dictionary<string, string> MappingColumnExcel = new Dictionary<string, string>() {
                {"STT","STT" },
                {"Mã Tài sản","AssetCode" },
                {"Serial","AssetSerial" },
                {"Model","AssetModel" },
                {"Màu sắc","AssetColor" },
                {"Hãng sản xuất","ProducerName" },
                {"Nhà cung cấp","SupplierName" },
                {"Ngày nhập","StrAssetDateIn" },
                {"Ngày mua","StrAssetDateBuy" },
            };
        [HttpPost]
        public Result ExportTemplateExcel([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoExportTemplateExcel(data, UserToken.UserID, out string urlFile);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return urlFile.ToResultOk();
        }
        private string DoExportTemplateExcel([FromBody] JObject data, int UserID, out string urlFile)
        {
            string msg = "";
            urlFile = "";

            msg = data.ToNumber("AssetTypeID", out int AssetTypeID);
            if (msg.Length > 0) return msg;
            if (AssetTypeID <= 0) return ("Bạn chưa chọn Loại tài sản").ToMessageForUser();

            msg = AssetTypeProperty.GetListByAssetTypeID(AssetTypeID, out List<AssetTypeProperty> lt);
            if (msg.Length > 0) return msg;

            DataTable dt = new DataTable();
            foreach (var item in MappingColumnExcel) dt.Columns.Add(item.Key, typeof(System.String));

            foreach (var item in lt) dt.Columns.Add(item.AssetTypePropertyName, typeof(System.String));

            for (int i = 0; i < 3; i++)
            {
                DataRow dr = dt.NewRow();
                dr["STT"] = i + 1;
                dr["Mã Tài sản"] = "MATS00" + i + 1;
                dr["Serial"] = "Serial123" + i + 1;
                dr["Model"] = "Model202" + i + 1;
                dr["Màu sắc"] = "Màu đen";
                dr["Hãng sản xuất"] = "Samsung" + i + 1;
                dr["Nhà cung cấp"] = "Công ty TNHH ABC" + i + 1;
                dr["Ngày nhập"] = DateTime.Now.ToString("dd/MM/yyyy");
                dr["Ngày mua"] = DateTime.Now.ToString("dd/MM/yyyy");
                dt.Rows.Add(dr);
            }

            msg = ExporExcelTemplateAsset(dt, out urlFile);
            if (msg.Length > 0) return msg;

            return msg;
        }
        public string ExporExcelTemplateAsset(DataTable dt, out string urlFile)
        {
            urlFile = "";
            try
            {
                string msg = BSS.Common.GetSetting("FolderFileExport", out string FolderFileExport);
                if (msg.Length > 0) return msg;

                urlFile = FolderFileExport + "/" + "TemplateImportAsset_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";

                using (ExcelPackage pack = new ExcelPackage())
                {
                    ExcelWorksheet ws = pack.Workbook.Worksheets.Add("Danh sách tài sản");
                    ws.Cells["A1"].LoadFromDataTable(dt, true);
                    ws.Column(1).Width = 5;
                    for (int i = 2; i < dt.Columns.Count + 1; i++)
                        ws.Column(i).Width = 25;

                    pack.SaveAs(new System.IO.FileInfo(HttpContext.Current.Server.MapPath(urlFile)));
                }
                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [HttpPost]
        public Result ImportExcel()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoImportExcel(out List<AssetImportExcel> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
        private string DoImportExcel(out List<AssetImportExcel> lt)
        {
            string msg = "";
            lt = null;
            try
            {
                msg = DoImportExcel_GetDataTable(out int AssetTypeID, out int placeID, out int AssetTypeGroupID, out DataTable dt);
                if (msg.Length > 0) return msg;

                msg = DoImportExcel_ConvertDataTableToList(AssetTypeID, dt, out lt);
                if (msg.Length > 0) return msg;

                msg = DoImportExcel_Validate(AssetTypeGroupID, lt);
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = DoImportExcel_ConvertToObject(lt, AssetTypeID, placeID, out List<Asset> ltAsset);
                if (msg.Length > 0) return msg;

                DBM dbm = new DBM();
                dbm.BeginTransac();

                try
                {
                    msg = DoImportExcel_ObjectToDB(dbm, ltAsset);
                    if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
                }
                catch (Exception ex)
                {
                    dbm.RollBackTransac();
                    return ex.ToString() + " at Asset DoImportExcel";
                }

                dbm.CommitTransac();

                return msg;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private string DoImportExcel_GetDataTable(out int AssetTypeID, out int PlaceID, out int AssetTypeGroupID, out DataTable dt)
        {
            string msg = ""; dt = null; AssetTypeID = 0; PlaceID = 0; AssetTypeGroupID = 0;

            var httpContext = HttpContext.Current;
            if (httpContext.Request.Files.Count == 0) return "Bạn chưa chọn File".ToMessageForUser();

            msg = WebHelper.GetStringFromRequestForm(HttpContext.Current, "AssetTypeGroupID", out string assetType_GroupID);
            if (msg.Length > 0) return msg;
            AssetTypeGroupID = assetType_GroupID.ToNumber(-1);
            if (AssetTypeGroupID < 0) return ("Giá trị Nhóm Tài sản bạn nhập không hợp lệ: " + assetType_GroupID).ToMessageForUser();

            msg = WebHelper.GetStringFromRequestForm(HttpContext.Current, "AssetTypeID", out string assettype_id);
            if (msg.Length > 0) return msg;
            AssetTypeID = assettype_id.ToNumber(-1);
            if (AssetTypeID < 0) return ("Giá trị Loại tài sản bạn nhập không hợp lệ: " + assettype_id).ToMessageForUser();

            msg = WebHelper.GetStringFromRequestForm(HttpContext.Current, "PlaceID", out string place_id);
            if (msg.Length > 0) return msg;
            PlaceID = place_id.ToNumber(-1);
            if (PlaceID < 0) return ("Giá trị nơi để bạn nhập không hợp lệ: " + place_id).ToMessageForUser();

            msg = BSS.Common.GetSetting("FolderFileUpload", out string FolderFileUpload);
            if (msg.Length > 0) return msg;

            string pathFileUpload = FolderFileUpload + "/" + Guid.NewGuid();
            if (!Directory.Exists(pathFileUpload)) Directory.CreateDirectory(pathFileUpload);

            HttpPostedFile httpPostedFile = httpContext.Request.Files[0];
            string pathFile = pathFileUpload + "/" + httpPostedFile.FileName;
            httpPostedFile.SaveAs(pathFile);

            msg = BSS.Common.GetDataTableFromExcelFile(pathFile, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string DoImportExcel_ConvertDataTableToList(int AssetTypeID, DataTable dt, out List<AssetImportExcel> lt)
        {
            string msg = ""; lt = null;

            msg = AssetTypeProperty.GetListByAssetTypeID(AssetTypeID, out List<AssetTypeProperty> ltAssetTypeProperty);
            if (msg.Length > 0) return msg;

            DataTable dt2 = new DataTable();
            foreach (var columnName in dt.Rows[0].ItemArray) dt2.Columns.Add(columnName.ToString());
            for (int i = 1; i < dt.Rows.Count; i++) dt2.Rows.Add(dt.Rows[i].ItemArray);

            msg = DoImportExcel_ConvertDataTableToList_Validate(ltAssetTypeProperty, dt2);
            if (msg.Length > 0) return msg;

            msg = DoImportExcel_ConvertDataTableToList_SetValue(ltAssetTypeProperty, dt2, out lt);
            if (msg.Length > 0) return msg;

            msg = DoImportExcel_ConvertDataTableToList_RemoveItemEmpty(lt);
            if (msg.Length > 0) return msg;

            return msg;
        }
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
        private string DoImportExcel_ConvertDataTableToList_SetValue(List<AssetTypeProperty> ltAssetTypeProperty, DataTable dt, out List<AssetImportExcel> lt)
        {
            foreach (DataColumn column in dt.Columns)
                foreach (var item in MappingColumnExcel)
                    if (item.Key == column.ColumnName) dt.Columns[column.ColumnName].ColumnName = item.Value;

            string msg = BSS.Convertor.DataTableToList<AssetImportExcel>(dt, out lt);
            if (msg.Length > 0) return msg;

            for (int i = 0; i < lt.Count; i++)
            {
                List<AssetProperty> ListAssetProperty = new List<AssetProperty>();
                foreach (var item in ltAssetTypeProperty)
                {
                    AssetProperty AssetProperty = new AssetProperty
                    {
                        AssetPropertyName = item.AssetTypePropertyName,
                        AssetTypePropertyID = item.AssetTypePropertyID,
                        Value = dt.Rows[i][item.AssetTypePropertyName].ToString()
                    };
                    ListAssetProperty.Add(AssetProperty);
                };
                lt[i].ListAssetProperty = ListAssetProperty;
            }

            return msg;
        }
        private string DoImportExcel_ConvertDataTableToList_RemoveItemEmpty(List<AssetImportExcel> lt)
        {
            List<AssetImportExcel> ltEmpty = new List<AssetImportExcel>();
            foreach (var item in lt) if (string.IsNullOrWhiteSpace(item.STT) && string.IsNullOrWhiteSpace(item.AssetCode) && string.IsNullOrWhiteSpace(item.AssetSerial) && string.IsNullOrWhiteSpace(item.AssetModel)) ltEmpty.Add(item);
            foreach (var item in ltEmpty) lt.Remove(item);

            return "";
        }
        private string DoImportExcel_Validate(int AssetTypeGroupID, List<AssetImportExcel> lt)
        {
            string msgError = "";
            string msg = AssetType.GetAll(AssetTypeGroupID, UserToken.AccountID, out List<AssetType> ltAssetType);
            if (msg.Length > 0) return msg;

            msg = Organization.GetList(UserToken.AccountID, out List<Organization> ltorganization);
            if (msg.Length > 0) return msg;

            foreach (var item in lt)
            {
                List<string> ltError = new List<string>();

                //string msgErr = BSS.DataValidator.DataValidator.Validate(new { item.AssetCode, item.AssetModel, item.AssetSerial }).ToErrorMessage();
                //if (msgErr.Length > 0) ltError.Add(msgErr);

                string AssetCode = item.AssetCode == null ? "" : item.AssetCode.Trim().Replace("/", "\\");
                if (AssetCode.Length == 0) ltError.Add("Mã Tài sản không được để trống");
                else
                {
                    msg = Asset.GetByAssetCode(AssetCode, UserToken.AccountID, out Asset assetExistCode);
                    if (msg.Length > 0) return msg;
                    if (assetExistCode != null) ltError.Add("Đã tồn tại Mã tài sản " + AssetCode);
                }

                string AssetSerial = item.AssetSerial == null ? "" : item.AssetSerial.Trim().Replace("/", "\\");
                string AssetModel = item.AssetModel == null ? "" : item.AssetModel.Trim().Replace("/", "\\");
                if (AssetSerial.Length == 0 || AssetModel.Length == 0) ltError.Add("Serial và Model không được để trống");
                else
                {
                    msg = Asset.CheckExistAsset(AssetModel, AssetSerial, UserToken.AccountID, out Asset assetExist);
                    if (msg.Length > 0) return msg;
                    if (assetExist != null) ltError.Add("Đã tồn tại Serial và Model " + AssetSerial + "/" + AssetModel);
                }

                string ProducerName = item.ProducerName == null ? "" : item.ProducerName.Trim().Replace("/", "\\");
                if (ProducerName.Length > 0)
                {
                    var vProducerName = ltorganization.Where(v => v.OrganizationName != null && v.OrganizationName.ToLower() == ProducerName.ToLower());
                    if (vProducerName.Count() == 0) ltError.Add("Không tồn tại Hãng sản xuất " + ProducerName);
                    else item.ProducerID = vProducerName.First().OrganizationID;
                }

                string SupplierName = item.SupplierName == null ? "" : item.SupplierName.Trim().Replace("/", "\\");
                if (SupplierName.Length > 0)
                {
                    var vSupplierName = ltorganization.Where(v => v.OrganizationName != null && v.OrganizationName.ToLower() == SupplierName.ToLower());
                    if (vSupplierName.Count() == 0) ltError.Add("Không tồn tại Nhà cung cấp " + SupplierName);
                    else item.SupplierID = vSupplierName.First().OrganizationID;
                }

                if (!string.IsNullOrEmpty(item.StrAssetDateBuy))
                {
                    if (!DateTime.TryParseExact(item.StrAssetDateBuy, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                        ltError.Add("Ngày mua " + item.StrAssetDateBuy + " không hợp lệ, ngày mua phải có định dạng dd/MM/yyyy");
                    else item.AssetDateBuy = dateTime;

                }

                if (!string.IsNullOrEmpty(item.StrAssetDateIn))
                {
                    if (!DateTime.TryParseExact(item.StrAssetDateIn, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                        ltError.Add("Ngày nhập " + item.StrAssetDateIn + " không hợp lệ, ngày nhập phải có định dạng dd/MM/yyyy");
                    else item.AssetDateIn = dateTime;
                }

                if (item.AssetDateIn < item.AssetDateBuy)
                {
                    ltError.Add("Bạn không thể nhập Ngày mua > Ngày nhập");
                }

                msg = AssetProperty.ValidateProperty(item.ListAssetProperty);
                if (msg.Length > 0) ltError.Add(msg);

                if (ltError.Count > 0) msgError += "\n " + item.AssetCode + "\n " + string.Join("\n", ltError) + "\n ";
            }
            if (msgError.Length > 0) return "Dữ liệu file excel không hợp lệ như sau:\n" + msgError;

            return "";
        }
        private string DoImportExcel_ConvertToObject(List<AssetImportExcel> lt, int AssetTypeID, int PlaceID, out List<Asset> ltAsset)
        {
            string msg = "";
            ltAsset = new List<Asset>();
            foreach (var item in lt)
            {
                Asset Asset = new Asset();
                msg = BSS.Common.CopyObjectPropertyData(item, Asset);
                if (msg.Length > 0) return msg;

                Asset.AssetTypeID = AssetTypeID;
                Asset.PlaceID = PlaceID;
                Asset.UserIDCreate = UserToken.UserID;
                Asset.UserIDHolding = UserToken.UserID;
                Asset.AssetStatusID = StatusAsset.MT;
                Asset.AccountID = UserToken.AccountID;

                Asset.ListAssetProperty = item.ListAssetProperty;

                ltAsset.Add(Asset);
            }

            return msg;
        }
        private string DoImportExcel_ObjectToDB(DBM dbm, List<Asset> ltAsset)
        {
            string msg = "";

            foreach (var item in ltAsset)
            {
                msg = item.InsertUpdate(dbm, out Asset assetNew);
                if (msg.Length > 0) return msg;

                msg = DoInsertUpdate_AssetProperty(dbm, assetNew.AssetID, item.ListAssetProperty, out List<AssetProperty> outassetProperties);
                if (msg.Length > 0) return msg;

                msg = Log.WriteHistoryLog(dbm, "Thêm Tài sản bằng file excel", assetNew.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }
            return msg;
        }

        [HttpPost]
        public Result PrintStamp([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoPrintStamp(data, out string url);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return url.ToResultOk();
        }
        private string DoPrintStamp([FromBody] JObject data, out string filePath)
        {
            filePath = "";
            string msg = DoAssetIDs(data, out string assetIDs);
            if (msg.Length > 0) return msg;

            msg = Asset.AssetViewDetailByListAssetID(assetIDs, UserToken.AccountID, out List<AssetViewDetail> assetViewDetail);
            if (msg.Length > 0) return msg;

            msg = DoCreateFile(assetViewDetail, out filePath);
            if (msg.Length > 0) return msg;

            return msg;
        }
        private string DoAssetIDs([FromBody] JObject data, out string assetIDs)
        {
            assetIDs = "";

            string msg = data.ToObject("assetViewDetailLst", out List<AssetViewDetail> assetViewDetails);
            if (msg.Length > 0) return msg;
            if (assetViewDetails.Count == 0) return "Bạn phải chọn tài sản để in tem";

            List<long> assetIDList = new List<long>();
            foreach (var item in assetViewDetails)
            {
                msg = CacheObject.GetAssetIDbyGUID(item.ObjectGuid, out long assetID);
                if (msg.Length > 0) return msg;

                assetIDList.Add(assetID);
            }

            assetIDs = string.Join(",", assetIDList.ToArray());

            return "";
        }
        private string DoCreateFile(List<AssetViewDetail> assetViewDetails, out string filePath)
        {
            string fileName = "barcode_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
            UtilitiesFile file = UtilitiesFile.GetInfoFile(DateTime.Now, fileName, ConfigurationManager.AppSettings["FolderFileReport"].ToString(), false);

            filePath = UtilitiesFile.GetUrlPage() + "/" + file.FilePathVirtual;
            string msg = ASM_API.App_Start.PrintStamp.PrintStamp.CreateFile(assetViewDetails, file.FilePathPhysical);
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpPost]
        public Result MoveAssetToPlace([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, TabID.QLTS, Role.ROLE_QLTS_DCKTL, out bool isRole_ChuyenKhoThanhLy);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoMoveAssetToPlace_Validate(data, out long AssetID, out int PlaceID, out string PlaceName, out string Note);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoMoveAssetToPlace_DB(dbm, AssetID, PlaceID, PlaceName, Note);
                if (msg.Length > 0) { dbm.RollBackTransac(); return Log.ProcessError(msg).ToResultError(); }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return (ex.ToString() + " at Asset DoImportExcel").ToResultError();
            }



            dbm.CommitTransac();
            return "".ToResultOk();
        }
        private string DoMoveAssetToPlace_Validate([FromBody] JObject data, out long AssetID, out int PlaceID, out string PlaceName, out string Note)
        {
            AssetID = PlaceID = 0;
            PlaceName = Note = "";

            string msg = data.ToGuid("AssetObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("PlaceID", out PlaceID);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = data.ToString("Note", out Note);
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (PlaceID <= 0) return ("Bạn cần chọn nơi để cho tài sản").ToMessageForUser();

            msg = Place.GetOneByPlaceID(PlaceID, UserToken.AccountID, out Place place);
            if (msg.Length > 0) return msg;
            if (place == null) return ("Không tồn tại mã nơi để PlaceID = " + PlaceID).ToMessageForUser();
            PlaceName = place.PlaceName;

            msg = CacheObject.GetAssetIDbyGUID(ObjectGuid, out AssetID);
            if (msg.Length > 0) return msg;

            msg = AssetProperty.GetDayExpiredByAssetID(AssetID, out string TotalExpriedDay);
            if (msg.Length > 0) return msg;
            if (string.IsNullOrEmpty(TotalExpriedDay))
            {
                return ("Tài sản không có giá trị ngày hết hạn vì thế không đủ điều kiện chuyển kho thành lý ").ToMessageForUser();
            }

            return "";
        }
        private string DoMoveAssetToPlace_DB(DBM dbm, long AssetID, int PlaceID, string PlaceName, string Note)
        {
            Asset assetInput = new Asset();
            assetInput.AssetID = AssetID;
            assetInput.AssetStatusID = StatusAsset.DTL;
            assetInput.UserIDHolding = UserToken.UserID;
            assetInput.PlaceID = PlaceID;
            assetInput.AccountID = UserToken.AccountID;

            string msg = assetInput.UpdateMoveAssetToPlace(dbm, out Asset outAsset);
            if (msg.Length > 0) return msg;

            msg = Log.WriteHistoryLog(dbm, "Điều chuyển tài sản vào kho thanh lý. Kho thanh lý: " + PlaceName + ", Ghi chú: " + Note + "", outAsset.ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            return "";
        }
    }
}