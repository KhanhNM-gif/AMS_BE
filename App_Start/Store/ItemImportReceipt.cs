using ASM_API.App_Start;
using ASM_API.App_Start.ItemProposalForm;
using ASM_API.App_Start.Template;
using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

interface ILtItemImport<T>
{
    List<T> ltItemImport { get; set; }
}
public class ItemImportReceiptBase
{
    public virtual long ItemImportReceiptID { get; set; }
    public virtual int UserIDCreate { get; set; }
    public virtual string ItemImportReceiptCode { get; set; }
    public virtual long ItemProposalFormID { get; set; }
    public virtual Guid ItemProposalFormObjectGuid { get; set; }
    public virtual string ItemProposalFormCode { get; set; }
    public Guid ObjectGuid { get; set; }
    public int ItemImportReceiptTypeID { get; set; }
    public int VAT { get; set; }
    public int PlaceID { get; set; }
    [JsonIgnore]
    public DateTime? CreateDate { get; set; }
    public DateTime? ImportDate { get; set; }
    public int SupplierID { get; set; }
    public string InvoiceNumber { get; set; }
    public string VouchersNumber { get; set; }
    public DateTime? VouchersDate { get; set; }
    public string Note { get; set; }
}
public class ItemImportReceipt : ItemImportReceiptBase, ILtItemImport<ItemImportReceiptDetail>
{
    public Guid ObjectGuidItemProposalForm { get; set; }
    [JsonIgnore]
    public override long ItemImportReceiptID { get; set; }
    [JsonIgnore]
    public override int UserIDCreate { get; set; }
    [JsonIgnore]
    public override string ItemImportReceiptCode { get; set; }
    [JsonIgnore]
    public int AccountID { get; set; }
    [JsonIgnore]
    public long ItemProposalFormID { get; set; }
    [JsonIgnore]
    public ItemProposalForm ItemProposalForm { get; set; }
    public List<ItemImportReceiptDetail> ltItemImport { get; set; }
    public string InsertOrUpdate(DBM dbm, out ItemImportReceipt itemImportReceipt)
    {
        itemImportReceipt = null;

        string msg = dbm.SetStoreNameAndParams("usp_StoreItem_InsertUpdate", new
        {
            ItemProposalFormID,
            ItemImportReceiptTypeID,
            PlaceID,
            ItemImportReceiptCode,
            ImportDate,
            SupplierID,
            InvoiceNumber,
            VouchersNumber,
            VouchersDate,
            Note,
            UserIDCreate,
            AccountID,
            VAT
        });
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out itemImportReceipt);
    }
    public static string GetTotalInYear(int Year, int AccountID, out int Total)
    {
        return DBM.ExecStore("usp_ItemImportReceipt_GetTotalInYear", new { Year, AccountID }, out Total);
    }

    public static string GetListItemInStore(long ItemID, int UserID, out DataTable dt)
    {
        return DBM.ExecStore("usp_Item_GetInfoItemByPlaceID", new { ItemID, UserID }, out dt);
    }
    public static string GetSuggestSearch(string TextSearch, int TypeStore, int AccountID, out DataTable dt)
    {
        return DBM.ExecStore("usp_StoreItem_SuggestSearch", new { TextSearch, TypeStore, AccountID }, out dt);
    }
    public static string GetListPaging(StoreSearch storeSearch, out List<AssetSearchResult> lt, out int total)
    {
        string msg = Paging.ExecByStore(@"usp_ItemImportReceipt_SelectSearch", "a.AssetID", storeSearch, out lt, out total);
        if (msg.Length > 0) return msg;

        return "";
    }

    public static string GetOneByStoreID(long StoreID, out ItemImportReceipt outStore)
    {
        string msg = DBM.GetOne("usp_ItemImportReceipt_GetOneByStoreID", new { StoreID }, out outStore);
        if (msg.Length > 0) return msg;
        if (outStore == null) return "Không tồn tại Phiếu nhập kho có StoreID = " + StoreID;

        return msg;
    }

    public static string GetOneObjectGuid(Guid ObjectGuid, out long storeID)
    {
        storeID = 0;

        string msg = DBM.GetOne("usp_ItemImportReceipt_GetStoreIDByObjectGuid", new { ObjectGuid }, out ItemImportReceipt outStore);
        if (msg.Length > 0) return msg;
        if (outStore == null) return "Không tồn tại Phiếu nhập kho có ObjectGuid = " + ObjectGuid;

        storeID = outStore.ItemImportReceiptID;

        return msg;
    }

    public static string GetListItemImportReceiptCode(string textSearch, out DataTable dt)
    {
        return DBM.ExecStore("usp_StoreItem_GetListItemImportReceiptCode", new { textSearch }, out dt);
    }
    public static string GetListVouchersNumber(string textSearch, out DataTable dt)
    {
        return DBM.ExecStore("usp_StoreItem_GetListVouchersNumber", new { textSearch }, out dt);
    }
    public static string GetListInvoiceNumber(string textSearch, out DataTable dt)
    {
        return DBM.ExecStore("usp_StoreItem_GetListInvoiceNumber", new { textSearch }, out dt);
    }
}

public class ItemImportReceiptViewDetail : ItemImportReceiptBase, ILtItemImport<ItemImportReceiptDetailView>
{
    [JsonIgnore]
    public override long ItemImportReceiptID { get; set; }
    public string ItemImportReceiptTypeName { get; set; }
    public string PlaceName { get; set; }
    public string SupplierName { get; set; }
    public List<ItemImportReceiptDetailView> ltItemImport { get; set; } = new List<ItemImportReceiptDetailView>();
    public long PriceAfterTax => ltItemImport.Sum(x => (long)(x.PretaxPrice * x.Quantity)) * (100 + VAT) / 100;

    public static string ViewDetail(long ItemImportReceiptID, out ItemImportReceiptViewDetail outItemImportReceiptViewDetail)
    {
        return DBM.GetOne("usp_ItemImportReceiptViewDetail_GetOne", new { ItemImportReceiptID }, out outItemImportReceiptViewDetail);
    }
}
public class StoreEasySearch
{
    public int ObjectCategory { get; set; }
    public int ObjectID { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public string TextSearch { get; set; }
    public int TypeStore { get; set; }
}
public class StoreSearch
{
    public string TextSearch { get; set; }
    public int TypeStore { get; set; }
    public string StoreItemIDs { get; set; } = "";
    public string ItemTypeIDs { get; set; }
    public string ItemIDs { get; set; }
    [JsonIgnore]
    public string StoreItemCode { get; set; }
    public string ItemProposalFormIDs { get; set; }
    [JsonIgnore]
    public string ItemProposalFormCode { get; set; }
    public string StoreCategoryInIDs { get; set; }
    public string PlaceIDs { get; set; }
    public string OrganizationIDs { get; set; }
    public string InvoiceNumbers { get; set; }
    public string VouchersNumbers { get; set; }
    [JsonIgnore]
    public long UserID { get; set; }
    public string UserIDCreates { get; set; }
    public int CreateDateCategoryID { get; set; } = 0;
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    [JsonIgnore]
    public bool IsViewAll { get; set; }
    [JsonIgnore]
    public int AccountID { get; set; }
    public virtual int CurrentPage { get; set; }
    public virtual int PageSize { get; set; }
    public StoreSearch()
    {
        TextSearch = StoreItemCode = ItemProposalFormCode = StoreCategoryInIDs = PlaceIDs = OrganizationIDs = InvoiceNumbers = VouchersNumbers = UserIDCreates = "";
        UserID = 0;
        TypeStore = AccountID = 0;
        CurrentPage = 1;
        PageSize = 50;
        DateTime dtDefault = DateTime.Parse("1900-01-01");
        DateFrom = DateTo = dtDefault;
    }

    public static string GetListPaging(StoreSearch storeSearch, out List<StoreSearchResult> lt, out int total)
    {
        lt = null; total = 0;

        string msg = GetListPaging_Parameter(storeSearch, out dynamic para);
        if (msg.Length > 0) return msg;

        msg = Paging.ExecByStore(@"usp_StoreItem_SelectSearch", "si.StoreItemID", para, out lt, out total);
        if (msg.Length > 0) return msg;

        return "";
    }
    private static string GetListPaging_Parameter(StoreSearch storeSearch, out dynamic o)
    {
        o = new
        {
            storeSearch.TextSearch,
            storeSearch.TypeStore,
            storeSearch.StoreItemIDs,
            storeSearch.StoreItemCode,
            storeSearch.ItemProposalFormIDs,
            storeSearch.ItemProposalFormCode,
            storeSearch.StoreCategoryInIDs,
            storeSearch.PlaceIDs,
            storeSearch.ItemIDs,
            storeSearch.ItemTypeIDs,
            storeSearch.OrganizationIDs,
            storeSearch.InvoiceNumbers,
            storeSearch.VouchersNumbers,
            storeSearch.UserID,
            storeSearch.UserIDCreates,
            storeSearch.DateFrom,
            storeSearch.DateTo,
            storeSearch.IsViewAll,
            storeSearch.AccountID,
            storeSearch.PageSize,
            storeSearch.CurrentPage
        };

        return "";
    }
    public static string SelectStoreItemCode(int AccountID, int UserID, int TypeStore, bool isViewAll, out DataTable dt)
    {
        return DBM.ExecStore("usp_StoreItem_SelectStoreItemCode", new { AccountID, UserID, TypeStore, isViewAll }, out dt);
    }
    public static string SelectItemProposalFormCode(int AccountID, int UserID, int TypeStore, bool isViewAll, out DataTable dt)
    {
        return DBM.ExecStore("usp_ItemProposalForm_SelectItemProposalFormCode", new { AccountID, UserID, TypeStore, isViewAll }, out dt);
    }
    public static string SelectInvoiceNumber(int AccountID, int UserID, int TypeStore, bool isViewAll, out DataTable dt)
    {
        return DBM.ExecStore("usp_StoreItem_SelectInvoiceNumber", new { AccountID, UserID, TypeStore, isViewAll }, out dt);
    }
    public static string SelectVouchersNumber(int AccountID, int UserID, int TypeStore, bool isViewAll, out DataTable dt)
    {
        return DBM.ExecStore("usp_StoreItem_SelectVouchersNumber", new { AccountID, UserID, TypeStore, isViewAll }, out dt);
    }



}
public class StoreItemSearchCategoryDateID
{
    public const int
                    TAT_CA = 0,
                    HOM_QUA = 1,
                    HOM_NAY = 2,
                    TRONG_TUAN = 3,
                    TRONG_THANG = 4,
                    KHOANG_THOI_GIAN = 99;
    //0 Tất cả
    //1 Hôm qua    
    //2	Hôm nay
    //3	Trong tuần
    //4	Trong tháng
    //99 Khoảng thời gian
    public static string GetDateByCategoryID(int categoryDate, DateTime? DateFrom, DateTime? DateTo, out DateTime from, out DateTime to)
    {
        from = to = DateTime.Parse("1900-01-01");
        if (categoryDate == KHOANG_THOI_GIAN)
        {
            if (!DateFrom.HasValue) return "";
            if (!DateTo.HasValue) return "";
            if (DateFrom == from && DateTo == to) return "";

            from = DateFrom.Value.Date;
            to = DateTo.Value.Date.AddDays(1).AddMilliseconds(-1);
        }
        else
        {
            DateTime now = DateTime.Now.Date;
            switch (categoryDate)
            {
                case TAT_CA:
                    to.AddDays(1).AddMilliseconds(-1);
                    break;
                case HOM_QUA:
                    from = now.AddDays(-1);
                    to = now.AddMilliseconds(-1);
                    break;
                case HOM_NAY:
                    from = now;
                    to = now.AddDays(1).AddMilliseconds(-1);
                    break;
                case TRONG_TUAN:
                    from = now.AddDays(-((now.DayOfWeek) == 0 ? 7 : (int)now.DayOfWeek) + 1);
                    to = from.AddDays(7).AddMilliseconds(-1);
                    break;
                case TRONG_THANG:
                    from = new DateTime(now.Year, now.Month, 1);
                    to = from.AddMonths(1).AddMilliseconds(-1);
                    break;
                default:
                    return ("Chưa định nghĩa CategoryDate = " + categoryDate);
            }
        }

        return "";
    }
}

public class ItemImportReceiptExcel
{
    public int STT { get; set; }
    public string ItemImportReceiptCode { get; set; }
    public string ItemProposalFormCode { get; set; }
    public string ItemImportReceiptType { get; set; }
    public string Store { get; set; }
    public string UserImport { get; set; }
    public string DateIn { get; set; }
    public string Supplier { get; set; }
    public string Status { get; set; }

    public ItemImportReceiptExcel(int STT, StoreSearchResult store)
    {
        this.STT = STT;
        ItemImportReceiptCode = store.StoreItemCode;
        ItemProposalFormCode = store.ItemProposalFormCode;
        ItemImportReceiptType = store.StoreCategoryInName.TrimEnd();
        Store = store.PlaceFullName;
        UserImport = store.Username;
        DateIn = store.DateIn.ToString("dd/MM/yyyy");
        Supplier = store.OrganizationName;
        Status = "Đã nhập hàng";
    }
}

public class StoreSearchExportExcel : StoreSearch
{
    [JsonIgnore]
    public override int CurrentPage { get; set; }
    [JsonIgnore]
    public override int PageSize { get; set; }

    public StoreSearchExportExcel() : base()
    {
        CurrentPage = 1;
        PageSize = 1000;
    }
}

public class StoreSearchResult
{
    public long StoreItemID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string StoreItemCode { get; set; }
    public string InvoiceNumber { get; set; }
    public string VouchersNumber { get; set; }
    public long ItemProposalFormID { get; set; }
    public string ItemProposalFormCode { get; set; }
    public Guid ObjectGuidItemProposalForm { get; set; }
    public int StoreCategoryInID { get; set; }
    public string StoreCategoryInName { get; set; }
    public string PlaceFullName { get; set; }
    public string PlaceName { get; set; }
    public string OrganizationID { get; set; }
    public string OrganizationName { get; set; }
    public string Username { get; set; }
    public DateTime DateIn { get; set; }
}
public class StoreItemResult
{
    public int ItemType { get; set; }
    public string ItemName { get; set; }
    public string SupplierName { get; set; }
    public string UnitName { get; set; }
    public float Price { get; set; }
    public float Quantity { get; set; }
}

public class ItemImportReceiptExportWord : TemplateExportWord
{
    public ItemImportReceiptExportWord() { }
    public long SetTienVietBangChu(long TienVietBangChu) => this.TienVietBangChu = TienVietBangChu;
    public string DonVi { get; set; }
    public string BoPhan { get; set; }
    public DateTime NgayTaoPhieu { get; set; }
    public string SoPhieu { get; set; }
    public string KhoNhap { get; set; }
    public string DiaDiemKhoNhap { get; set; }
    public long TienVietBangChu { get; set; }
    public string ChungTuGoc { get; set; }
    public Dictionary<string, string> GetDictionaryReplace()
    {
        return new Dictionary<string, string>()
            {
                {"paramDonVi", DonVi},
                {"paramBoPhan", BoPhan},
                {"paramNgayTaoPhieu",$"Ngày {NgayTaoPhieu.Day} tháng {NgayTaoPhieu.Month} năm {NgayTaoPhieu.Year}" },
                {"paramSoPhieu",SoPhieu },
                {"paramKhoNhap",KhoNhap },
                {"paramDiaDiemKhoNhap","HH1 Dương Đình Nghệ Cầu Giấy" },
                {"paramTienVietBangChu",Utils.MoneyToText(TienVietBangChu," đồng")},
                {"paramSoChungTuGocKemTheo",ChungTuGoc }
            };
    }
    public static string GetOne(long ItemImportReceiptID, out ItemImportReceiptExportWord outItemImportReceiptExportWord)
    {
        return DBM.GetOne("usp_ItemImportReceiptExportWord_GetOne", new { ItemImportReceiptID }, out outItemImportReceiptExportWord);
    }
}

public class LtItemImportReceiptExportWord : TableDocument
{
    public List<ItemImportReceiptExportWordDetail> ltItemImportReceiptExportWord { get; set; }
    private string title { get; set; } = "DsVP";
    private bool hasFooterTable { get; set; } = true;
    public long GetAmount() => ltItemImportReceiptExportWord.Sum(x => x.Amount);

    public object[] GetFooterTable()
    {
        return new object[]
        {
                "","Cộng","","","","","",GetAmount().ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE"))
        };
    }


    public string SetLtItemProposalFormExportWord(long ItemProposalFormID)
    {
        string msg = DBM.GetList("usp_ItemItemImportReceiptExportWord_GetList", new { ItemProposalFormID }, out List<ItemImportReceiptExportWordDetail> outlt);
        if (msg.Length > 0) return msg;
        int i = 0;
        outlt.ForEach(x => x.STT = ++i);
        ltItemImportReceiptExportWord = outlt;

        return string.Empty;
    }
    public DataTable GetDataTable()
    {
        DataTable dt = new DataTable();
        dt.Columns.Add("", typeof(int));
        dt.Columns.Add("", typeof(string));
        dt.Columns.Add("", typeof(string));
        dt.Columns.Add("", typeof(string));
        dt.Columns.Add("", typeof(float));
        dt.Columns.Add("", typeof(float));
        dt.Columns.Add("", typeof(string));
        dt.Columns.Add("", typeof(string));

        foreach (var item in ltItemImportReceiptExportWord)
        {
            dt.Rows.Add(
                item.STT,
                item.ItemName,
                item.Code,
                item.ItemUnit,
                item.QuanlityVoucher,
                item.QuanlityReal,
                item.UnitPrice.ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE")),
                item.Amount.ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE")));
        }

        return dt;
    }
    public string GetTitle() => title;
    public bool HasFooterTable() => hasFooterTable;
}

public class ItemImportReceiptExportWordDetail
{
    public int STT { get; set; }
    public string ItemName { get; set; }
    public string Code { get; set; }
    public string ItemUnit { get; set; }
    public float QuanlityVoucher { get; set; }
    public float QuanlityReal { get; set; }
    public long UnitPrice { get; set; }
    public long Amount { get { return (long)(UnitPrice * QuanlityReal); } }
}
