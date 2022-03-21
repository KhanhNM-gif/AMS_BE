using ASM_API.App_Start.Template;
using BSS;
using System;
using System.Collections.Generic;
using System.Data;

public class ProposalForm
{
    public long ProposalFormID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string ProposalFormCode { get; set; }
    public string ProposalFormReason { get; set; }
    public string CommentHandling { get; set; }
    public int UserIDCreate { get; set; }
    public int UserIDHandling { get; set; }
    public int ProposalFormStatusID { get; set; }
    public bool IsSendApprove { get; set; }
    public int AccountID { get; set; }
    public string ProposalFormStatusName { get; set; }
    public string TransferDirectionID { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<ProposalFormDetail> ltProposalFormDetail { get; set; }
    public string InsertUpdate(DBM dbm, out ProposalForm au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_ProposalForm_InsertUpdate",
                    new
                    {
                        ProposalFormID,
                        ProposalFormCode,
                        ProposalFormReason,
                        UserIDCreate,
                        UserIDHandling,
                        ProposalFormStatusID,
                        AccountID
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public static string GetAll(out List<ProposalForm> proposalForm)
    {
        return DBM.GetList("usp_ProposalForm_GetAll", new { }, out proposalForm);
    }
    public static string GetOne(long ProposalFormID, out ProposalForm proposalForm)
    {
        return DBM.GetOne("usp_ProposalForm_GetByID", new { ProposalFormID }, out proposalForm);
    }
    public static string GetTotalByDateCode(string DateCode, out int Total)
    {
        return DBM.ExecStore("usp_ProposalForm_GetByDateCode", new { DateCode }, out Total);
    }
    public static string GetSuggestSearch(string TextSearch, int AccountID, out DataTable lt)
    {
        return DBM.ExecStore("usp_ProposalForm_SuggestSearch", new { TextSearch, AccountID }, out lt);
    }
    public static string GetOneObjectGuid(Guid ObjectGuid, out long proposalFormID)
    {
        proposalFormID = 0;

        ProposalForm u;
        string msg = DBM.GetOne("usp_ProposalForm_GetByObjectGuid", new { ObjectGuid }, out u);
        if (msg.Length > 0) return msg;

        if (u == null) return ("Không tồn tại Phiếu kiểm kê tài sản có ObjectGuid = " + ObjectGuid).ToMessageForUser();
        proposalFormID = u.ProposalFormID;

        return msg;
    }
    public static string GetListSearch(ProposalFormSearch formSearch, out List<ProposalFormSearchResult> lt, out int total)
    {
        lt = null; total = 0;
        dynamic para;
        string msg = GetListSearch_Parameter(formSearch, out para);
        if (msg.Length > 0) return msg;

        msg = Paging.ExecByStore(@"usp_ProposalForm_SelectSearch", "p.ProposalFormID", para, out lt, out total);
        if (msg.Length > 0) return msg;

        return "";

    }
    public static string GetListSearchTotal(ProposalFormSearch formSearch, out int total)
    {
        total = 0;

        dynamic o;
        string msg = GetListSearch_Parameter(formSearch, out o);
        if (msg.Length > 0) return msg;

        return DBM.ExecStore("usp_ProposalForm_SelectSearch_Total", o, out total);
    }
    private static string GetListSearch_Parameter(ProposalFormSearch formSearch, out dynamic o)
    {
        return GetListSearch_Parameter(false, formSearch, out o);
    }
    private static string GetListSearch_Parameter(bool IsReport, ProposalFormSearch formSearch, out dynamic o)
    {
        o = null;
        string msg = "";
        o = new
        {
            formSearch.TextSearch,
            formSearch.ProposalFormID,
            formSearch.UserID,
            formSearch.StatusID,
            formSearch.AssetTypeID,
            formSearch.DateFrom,
            formSearch.DateTo,
            formSearch.AccountID,
            formSearch.UserIDCreate,
            formSearch.UserIDHandling,
            formSearch.PageSize,
            formSearch.CurrentPage
        };

        return msg;
    }

    public static string UpdateStatusID(DBM dbm, long ProposalFormID, int ProposalFormStatusID)
    {
        string msg = dbm.SetStoreNameAndParams("usp_ProposalForm_UpdateStatus",
          new
          {
              ProposalFormID,
              ProposalFormStatusID
          });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string UpdateStatusID(DBM dbm, long ProposalFormID, int ProposalFormStatusID, string ProposalFormReasonRefuse)
    {
        string msg = dbm.SetStoreNameAndParams("usp_ProposalForm_UpdateStatusRefuse",
          new
          {
              ProposalFormID,
              ProposalFormStatusID,
              ProposalFormReasonRefuse
          });
        if (msg.Length > 0) return msg;

        return dbm.ExecStore();
    }
    public static string UpdateTransferHanding(long ProposalFormID, long UserIDHandling)
    {
        return DBM.ExecStore("usp_ProposalForm_UpdateTransferHanding", new { ProposalFormID, UserIDHandling });
    }
    public static string GetUserIDHandlingByStatus(long UserIDHandling, int ProposalFormStatusID, out List<ProposalForm> proposalForm)
    {
        return DBM.GetList("usp_ProposalForm_GetUserIDHandlingByStatus", new { UserIDHandling, ProposalFormStatusID }, out proposalForm);
    }

}
public class ProposalFormDetail
{
    public long ID { get; set; }
    public long ProposalFormID { get; set; }
    public int AssetTypeID { get; set; }
    public string AssetTypeName { get; set; }
    public int Quantity { get; set; }
    public string Note { get; set; }
    public string InsertUpdate(DBM dbm, out ProposalFormDetail au)
    {
        au = null;
        string msg = dbm.SetStoreNameAndParams("usp_ProposalFormDetail_InsertUpdate",
                    new
                    {
                        ID,
                        ProposalFormID,
                        AssetTypeID,
                        Quantity,
                        Note
                    }
                    );
        if (msg.Length > 0) return msg;

        return dbm.GetOne(out au);
    }
    public static string GetOneByID(long ID, out ProposalFormDetail ProposalFormDetail)
    {
        return DBM.GetOne("usp_ProposalFormDetail_GetByID", new { ID }, out ProposalFormDetail);
    }
    public static string GetListByProposalFormID(long ProposalFormID, out List<ProposalFormDetail> ProposalFormDetails)
    {
        return DBM.GetList("usp_ProposalFormDetail_GetListByFormID", new { ProposalFormID }, out ProposalFormDetails);
    }
    public static string Delete(long ID)
    {
        return DBM.ExecStore("usp_ProposalFormDetail_Delete", new { ID });
    }
}
public class ProposalFormDetailHandover
{
    public int ProposalFormID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string ProposalFormCode { get; set; }
    public string ProposalFormReason { get; set; }
    public int UserCreateID { get; set; }
    public int UserHandlingID { get; set; }
    public int ProposalFormStatusID { get; set; }
    public int SendApprove { get; set; }
    public string ProposalFormStatusName { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<AssetProcessingFlow> ltassetProcessingFlows { get; set; }

    public static string GetOne(int ProposalFormID, out ProposalFormDetailHandover handover)
    {
        return DBM.GetOne("usp_ProposalForm_GetByID", new { ProposalFormID }, out handover);
    }
}
public class ProposalFormSearch
{
    public string TextSearch { get; set; }
    public int StatusID { get; set; }
    public int AssetTypeID { get; set; }
    public long ProposalFormID { get; set; }
    public long UserID { get; set; }
    public int UserIDCreate { get; set; }
    public int UserIDHandling { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int AccountID { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public ProposalFormSearch()
    {
        StatusID = 0;
        AssetTypeID = 0;
        CurrentPage = 0;
        PageSize = 0;
        ProposalFormID = 0;
        UserID = 0;
        UserIDCreate = 0;
        UserIDHandling = 0;
        DateTime dtDefault = DateTime.Parse("1900-01-01");
        DateFrom = DateTo = dtDefault;
    }
}
public class ProposalFormSearchResult
{
    public long ProposalFormID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string ProposalFormCode { get; set; }
    public string ProposalFormReason { get; set; }
    public int UserIDCreate { get; set; }
    public string CreateFullName { get; set; }
    public string CreateUserName { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public int ProposalFormStatusID { get; set; }
    public string ProposalFormStatusName { get; set; }
    public string AssetTypeName { get; set; }
    public int UserIDHandling { get; set; }
    public string UserHandlingFullName { get; set; }
    public string UserHandlingUserName { get; set; }
    public ButtonShowPDX ButtonShow { get; set; }
}
public class ProposalFormViewDetail
{
    public long ProposalFormID { get; set; }
    public Guid ObjectGuid { get; set; }
    public string ProposalFormCode { get; set; }
    public string ProposalFormReason { get; set; }
    public string ProposalFormReasonRefuse { get; set; }
    public string CommentHandling { get; set; }
    public long UserIDCreate { get; set; }
    public string UserCreateDetail { get; set; }
    public string UserHandlingDetail { get; set; }
    public long UserIDHandling { get; set; }
    public string ProposalFormStatusName { get; set; }
    public string AssetTypeName { get; set; }
    public string TransferDirectionID { get; set; }
    public List<ProposalFormDetail> ltProposalFormDetail { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastUpdate { get; set; }
    public static string ViewDetail(long ProposalFormID, out ProposalFormViewDetail proposalForm)
    {
        return DBM.GetOne("usp_ProposalForm_ViewDetail", new { ProposalFormID }, out proposalForm);
    }
    public static string GetListViewDetailByUserIDCreate(long UserIDCreate, out List<ProposalFormViewDetail> lt)
    {
        return DBM.GetList("usp_ProposalForm_GetByUserIDCreate", new { UserIDCreate }, out lt);
    }


    public class ProposalFormExportWord : TemplateExportWord
    {
        public ProposalFormExportWord() { }
        public string DonViCap1 { get; set; }
        public string DonViCap2 { get; set; }
        public DateTime NgayTaoPhieu { get; set; }
        public string BenDeXuat { get; set; }
        public string PhuTrachNguoiDeXuat { get; set; }
        public string ChucVuPhuTrachNguoiDeXuat { get; set; }
        public string BenTiepNhan { get; set; }
        public string NguoiDeXuat { get; set; }
        public string ChucVuNguoiDeXuat { get; set; }
        public string PhuTrachNguoiTiepNhan { get; set; }
        public string ChucVuPhuTrachNguoiTiepNhan { get; set; }
        public string NguoiTiepNhan { get; set; }
        public string ChucVuNguoiTiepNhan { get; set; }
        public string LyDoDeXuat { get; set; }
        public static string GetOne(long ProposalFormID, out ProposalFormExportWord outProposalFormExportWord)
        {
            return DBM.GetOne("usp_ProposalForm_ProposalFormExportWord", new { ProposalFormID }, out outProposalFormExportWord);
        }
        public Dictionary<string, string> GetDictionaryReplace()
        {
            return new Dictionary<string, string>()
            {
                {"DonViCap1", DonViCap1.ToUpper()},
                {"DonViCap2", DonViCap2},
                {"NgayTaoPhieu",$"Ngày {NgayTaoPhieu.Day} Tháng {NgayTaoPhieu.Month} Năm {NgayTaoPhieu.Year}" },
                {"ThoiGianTaoPhieu",$"{NgayTaoPhieu.ToString("HH")}h{NgayTaoPhieu.ToString("mm")}, Ngày {NgayTaoPhieu.Day} Tháng {NgayTaoPhieu.Month} Năm {NgayTaoPhieu.Year}" },
                {"BenDeXuat",BenDeXuat },
                {"PhuTrachNguoiDeXuat",PhuTrachNguoiDeXuat},
                {"ChucVuPhuTrachNguoiDeXuat",ChucVuPhuTrachNguoiDeXuat },
                {"BenTiepNhan",BenTiepNhan },
                {"NguoiDeXuat",NguoiDeXuat },
                {"ChucVuNguoiDeXuat",ChucVuNguoiDeXuat},
                {"PhuTrachNguoiTiepNhan",PhuTrachNguoiTiepNhan },
                {"ChucVuPhuTrachNguoiTiepNhan",ChucVuPhuTrachNguoiTiepNhan },
                {"NguoiTiepNhan",NguoiTiepNhan},
                {"ChucVuNguoiTiepNhan",ChucVuNguoiTiepNhan },
                {"LyDoDeXuat",LyDoDeXuat },
            };
        }

        public class LtProposalFormExportWord : TableDocument
        {
            public List<ProposalFormDetailExportWord> ltProposalFormDetailExportWord { get; set; }
            private string title { get; set; } = "DanhSachTaiSan";
            private bool hasFooterTable { get; set; } = false;
            public string SetLtItemProposalFormExportWord(long ProposalFormID)
            {
                string msg = DBM.GetList("usp_ProposalFormExportWord_GetList", new { ProposalFormID }, out List<ProposalFormDetailExportWord> outlt);
                if (msg.Length > 0) return msg;
                ltProposalFormDetailExportWord = outlt;

                return string.Empty;
            }
            public object[] GetFooterTable()
            {
                return null;
            }
            public DataTable GetDataTable() => ltProposalFormDetailExportWord.ToDataTable();
            public string GetTitle() => title;
            public bool HasFooterTable() => hasFooterTable;
        }

        public class ProposalFormDetailExportWord
        {
            public int STT { get; set; }
            public string AssetTypeName { get; set; }
            public int SL { get; set; }
            public string DVT => "Cái";
        }
    }
}