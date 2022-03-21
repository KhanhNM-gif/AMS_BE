using ASM_API.App_Start.TableModel;
using ASM_API.App_Start.Template;
using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ASM_API.App_Start.ItemProposalForm
{
    public class ItemProposalFormBase
    {
        public int ItemProposalFormTypeID { get; set; }
        public string ItemProposalFormCode { get; set; }
        public Guid ObjectGuid { get; set; }
        public bool IsSendApprove { get; set; }
        [Mapping("Lý do đề xuất", typeof(MappingObject))]
        public string ItemProposalFormReason { get; set; }

        //[Mapping("Thông tin chuyển xử lý", typeof(MappingObject))]
        [JsonIgnore]
        public string InfoTransferProcess { get; set; }
        [Mapping("Người xử lý", typeof(AccountUser))]
        public int UserIDHandling { get; set; }
        public int ItemProposalFormStatusID { get; set; }
        public string TransferDirectionID { get; set; } = "0";
    }
    interface ILtItemProposalFormDetail<T>
    {
        List<T> ltItemProposalFormDetail { get; set; }
    }

    public class ItemProposalForm : ItemProposalFormBase, ILtItemProposalFormDetail<ItemProposalFormDetail>, ILogUpdate<ItemProposalForm>
    {
        [JsonIgnore]
        public long ID { get; set; }
        [JsonIgnore]
        public int UserIDCreate { get; set; }
        [JsonIgnore]
        public int AccountID { get; set; }
        [JsonIgnore]
        public string InfoLogUpdate { get; set; }
        [Mapping("Danh sách vật phẩm", typeof(MappingList<ItemProposalFormDetail>))]
        public List<ItemProposalFormDetail> ltItemProposalFormDetail { get; set; }

        public string GetInfoChangeRequest() => InfoLogUpdate;

        public string InsertUpdate(DBM dbm, out ItemProposalForm au)
        {
            au = null;
            string msg = dbm.SetStoreNameAndParams("usp_ItemProposalForm_InsertUpdate",
                        new
                        {
                            ID,
                            ItemProposalFormTypeID,
                            ItemProposalFormCode,
                            ItemProposalFormReason,
                            //InfoTransferProcess,
                            UserIDCreate,
                            UserIDHandling,
                            ItemProposalFormStatusID,
                            AccountID,
                            TransferDirectionID
                        }
                        );
            if (msg.Length > 0) return msg;

            return dbm.GetOne(out au);
        }
        public static string GetTotalByDateCode(string DateCode, out int Total)
        {
            return DBM.ExecStore("usp_ItemProposalForm_GetByDateCode", new { DateCode }, out Total);
        }

        public static string GetOne(long ItemProposalFormID, int AccountID, out ItemProposalForm proposalForm)
        {
            return DBM.GetOne("usp_ItemProposalForm_GetByID", new { ItemProposalFormID, AccountID }, out proposalForm);
        }
        public static string GetSuggestSearch(string TextSearch, int AccountID, out DataTable lt)
        {
            return DBM.ExecStore("usp_ItemProposalForm_SuggestSearch", new { TextSearch, AccountID }, out lt);
        }
        public static string GetOneObjectGuid(Guid ObjectGuid, out long ItemproposalFormID)
        {
            ItemproposalFormID = 0;

            ItemProposalForm u;
            string msg = DBM.GetOne("usp_ItemProposalForm_GetByObjectGuid", new { ObjectGuid }, out u);
            if (msg.Length > 0) return msg;

            if (u == null) return ("Không tồn tại User có ObjectGuid = " + ObjectGuid).ToMessageForUser();
            ItemproposalFormID = u.ID;

            return msg;
        }
        public static string UpdateStatusID(DBM dbm, long ID, int accountID, int itemProposalFormStatusID)
        {
            string msg = dbm.SetStoreNameAndParams("usp_ItemProposalForm_UpdateStatus",
              new
              {
                  ID,
                  itemProposalFormStatusID,
                  accountID
              });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }
        public static string Delete(DBM dbm, long ID, int accountID)
        {
            string msg = dbm.SetStoreNameAndParams("usp_ItemProposalForm_DeleteByID",
              new
              {
                  ID,
                  accountID
              });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }
        public static string UpdateStatusID(DBM dbm, long ID, int accountID, int itemProposalFormStatusID, string itemProposalFormReasonRefuse)
        {
            string msg = dbm.SetStoreNameAndParams("usp_ItemProposalForm_UpdateStatusRefuse",
              new
              {
                  ID,
                  itemProposalFormStatusID,
                  itemProposalFormReasonRefuse,
                  accountID
              });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }

        public static string UpdateTransferHanding(DBM dbm, long ID, string InfoTransferProcess, long UserIDHandling)
        {
            string msg = dbm.SetStoreNameAndParams("usp_ItemProposalForm_UpdateTransferHanding",
              new
              {
                  ID,
                  InfoTransferProcess,
                  UserIDHandling
              });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }
        public static string GetUserIDHandlingByStatus(long UserIDHandling, int ProposalFormStatusID, out List<ItemProposalForm> proposalForm)
        {
            return DBM.GetList("usp_ItemProposalForm_GetUserIDHandlingByStatus", new { UserIDHandling, ProposalFormStatusID }, out proposalForm);
        }

        public string SetInfoChangeRequest(ItemProposalForm itemProposalForm_Old)
        {
            string logChange = "";
            List<string> LtChanges = new List<string>();

            string msg = TransferHandlingDirection.GetUserHandling(UserIDCreate, AccountID, this.TransferDirectionID, out List<AccountUser> outLtAccoutUser);
            if (msg.Length > 0) return msg;

            msg = ItemUnit.GetList(out var outItemUnits);
            if (msg.Length > 0) return msg;

            msg = ItemProposalFormDetail.GetListByProposalFormID(itemProposalForm_Old.ID, out var itemProposalFormDetails_Old);
            if (msg.Length > 0) return msg;
            itemProposalForm_Old.ltItemProposalFormDetail = itemProposalFormDetails_Old;

            string SEPARATOR = "; ";
            msg = this.GetUpdateInfo3(itemProposalForm_Old, SEPARATOR, out logChange);

            var ALDRRemove = itemProposalForm_Old.ltItemProposalFormDetail
                .Except(this.ltItemProposalFormDetail, new IModelCompare<ItemProposalFormDetail>())
                .Select(x => $"Xóa {x.ItemCode}");
            LtChanges.AddRange(ALDRRemove);

            logChange += string.IsNullOrEmpty(logChange) ? "" : SEPARATOR + (LtChanges.Any() ? ("Sửa Danh sách tài sản: " + string.Join(SEPARATOR, LtChanges)) : "");

            InfoLogUpdate = logChange;

            return string.Empty;
        }
    }
    public class ItemProposalFormStatus
    {
        public int ID { get; set; }
        public string ItemProposalFormStatusName { get; set; }
        public string ItemProposalFormStatusSort { get; set; }
        public string ItemProposalFormStatusColor { get; set; }
        public static string GetListStatus(out List<ItemProposalFormStatus> lt)
        {
            return DBM.GetList("usp_ItemProposalFormStatus_SelectAll", new { }, out lt);
        }
    }
    public class ItemProposalFormModify : ItemProposalFormBase, ILtItemProposalFormDetail<ItemProposalFormDetailModify>
    {
        [JsonIgnore]
        public long ID { get; set; }
        public List<ItemProposalFormDetailModify> ltItemProposalFormDetail { get; set; }

        public static string GetOne(long ItemProposalFormID, int AccountID, out ItemProposalFormModify proposalForm)
        {
            return DBM.GetOne("usp_ItemProposalForm_GetByID", new { ItemProposalFormID, AccountID }, out proposalForm);
        }
    }
    public class ItemProposalFormSearchResult : ItemProposalFormBase
    {

        public int UserIDCreate { get; set; }
        public string CreateFullName { get; set; }
        public string CreateUserName { get; set; }
        public DateTime CreateDate { get; set; }
        public string ItemProposalFormTypeName { get; set; }
        public DateTime LastUpdate { get; set; }
        public string ItemProposalFormStatusName { get; set; }
        public int UserIDHandling { get; set; }
        public string UserHandlingFullName { get; set; }
        public string UserHandlingUserName { get; set; }
        public ButtonShowPDXVP ButtonShow { get; set; }

        public static string GetListSearch(ItemProposalFormSearch formSearch, out List<ItemProposalFormSearchResult> lt, out int total)
        {
            lt = null;
            total = 0;

            dynamic o;
            string msg = GetListSearch_Parameter(formSearch, out o);
            if (msg.Length > 0) return msg;

            return Paging.ExecByStore("usp_ItemProposalForm_SelectSearch", "p.ID", o, out lt, out total);
        }

        public static string GetListByUserCreateID(int UserIDCreate, out List<ItemProposalFormSearchResult> lt)
        {
            return DBM.GetList("usp_ItemProposalForm_GetByUserIDCreate", new { UserIDCreate }, out lt);
        }
        private static string GetListSearch_Parameter(ItemProposalFormSearch formSearch, out dynamic o)
        {
            o = new
            {
                formSearch.TextSearch,
                formSearch.ID,
                formSearch.ItemProposalFormTypeID,
                formSearch.UserIDHandings,
                formSearch.UserCreateIDs,
                formSearch.StatusIDs,
                formSearch.UserID,
                formSearch.ItemTypeIDs,
                formSearch.CreateDateFrom,
                formSearch.isEasySearch,
                formSearch.CreateDateTo,
                formSearch.AccountID,
                formSearch.PageSize,
                formSearch.CurrentPage,
                formSearch.ProcessingDateFrom,
                formSearch.ProcessingDateTo
            };

            return string.Empty;
        }
    }
    public class ItemProposalFormViewDetail : ItemProposalFormBase, ILtItemProposalFormDetail<ItemProposalFormDetailModify>
    {
        [JsonIgnore]
        public long ID { get; set; }
        public long UserIDCreate { get; set; }
        public string UserCreateDetail { get; set; }
        public string UserHandlingDetail { get; set; }
        public string ItemProposalFormStatusName { get; set; }
        public string ItemProposalFormTypeName { get; set; }
        public string AssetTypeName { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastUpdate { get; set; }
        public DateTime? ProcessingDate { get; set; }
        public string InfoTransferProcess { get; set; }
        public List<ItemProposalFormDetailModify> ltItemProposalFormDetail { get; set; }
        public List<CommentItemProposalForm> ltCommentItemProposalForm { get; set; }
        public ButtonShowHandlingPDXVP ButtonShowHandlingPDXVP { get; set; }

        public static string ViewDetail(long ID, out ItemProposalFormViewDetail proposalForm)
        {
            return DBM.GetOne("usp_ItemProposalForm_ViewDetail", new { ID }, out proposalForm);
        }
    }

    public abstract class ItemProposalFormHanding
    {
        public Guid ObjectGuid { get; set; }
        public abstract string GetContent(string UserName);
        public abstract string ValidateInput(int StatusID);
        public abstract int GetStatusChange();
        public abstract string GetReason();
    }

    #region Class Người tạo cập nhật trạng thái phiếu đề xuất
    public abstract class InputUpdateStatusItemProposalForm : ItemProposalFormHanding { }
    public class InputDeleteItemProposalForm : InputUpdateStatusItemProposalForm
    {
        public override string GetContent(string UserName)
        {
            return $"Xóa: <b>{UserName}</b> xóa Phiếu đề xuất Vật phẩm. Trạng thái phiếu <b>Đã xóa</b>.";
        }

        public override string GetReason()
        {
            return "";
        }

        public override int GetStatusChange()
        {
            return Constants.StatusPDXVP.DX;
        }

        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPDXVP.MT && StatusID != Constants.StatusPDXVP.TL) return "Bạn chỉ được xóa Phiếu đề xuất vật phẩm khi phiếu mới tạo hoặc đã được trả lại".ToMessageForUser();

            return string.Empty;
        }
    }
    public class InputRestoreItemProposalForm : InputUpdateStatusItemProposalForm
    {
        public override string GetContent(string UserName)
        {
            return $"Khôi phục:<b>{UserName}</b> khôi phục Phiếu đề xuất Vật phẩm. Trạng thái phiếu đã <b>Mới tạo</b>.";
        }

        public override string GetReason()
        {
            return "";
        }

        public override int GetStatusChange()
        {
            return Constants.StatusPDXVP.MT;
        }

        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPDXVP.DX) return "Bạn chỉ được khôi phục Phiếu đề xuất vật phẩm khi phiếu đã xóa".ToMessageForUser();

            return string.Empty;
        }
    }
    #endregion

    #region Class Người duyệt xử lý phiếu đề xuất
    public abstract class InputHandlingItemProposalForm : ItemProposalFormHanding { }

    public class InputReturnItemProposalForm : InputTransferHandleItemProposalForm
    {
        public string InfoTransferProcess { get; set; }
        [JsonIgnore]
        public override int UserTransferHandleID { get; set; }

        public InputReturnItemProposalForm(string reasonRefuse)
        {
            InfoTransferProcess = reasonRefuse;
        }
        public override string GetContent(string UserName)
        {
            return $"Trả lại Phiếu đề xuất vật phẩm: <b>{UserName}</b> trả lại Phiếu đề xuất Vật phẩm. Trạng thái phiếu <b>Trả lại</b>.  Lý do: {InfoTransferProcess}";
        }
        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPDXVP.CXL) return "Bạn chỉ được trả lại Phiếu đề xuất Vật phẩm khi phiếu đã ở trạng thái chờ duyệt".ToMessageForUser();

            if (string.IsNullOrEmpty(InfoTransferProcess) || InfoTransferProcess.Length < 3 || InfoTransferProcess.Length > 255) return "Lý do Trả lại là trường thông tin bắt buộc, có độ dài từ 3 đến 255 ký tự".ToMessageForUser();

            return string.Empty;
        }
        public override int GetStatusChange()
        {
            return Constants.StatusPDXVP.TL;
        }
        public override string GetReason()
        {
            return InfoTransferProcess;
        }

    }
    /// <summary>
    /// Bỏ việc Duyệt trực tiếp phiếu đề xuât phải duyệt thông qua 
    /// </summary>
    /*public class InputApproveItemProposalForm : InputHandlingItemProposalForm
    {
        public override string GetContent(string UserName)
        {
            return $"Duyệt Phiếu đề xuất Vật phẩm: <b>{UserName}</b> đã duyệt phiếu đề xuất vật phẩm";
        }
        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPDXVP.CXL) return "Bạn chỉ được duyệt Phiếu đề xuất Vật phẩm khi phiếu đã ở trạng thái chờ duyệt".ToMessageForUser();

            return string.Empty;
        }
        public override int GetStatusChange()
        {
            return Constants.StatusPDXVP.DX;
        }
        public override string GetReason()
        {
            return "";
        }
    }*/

    public class InputTransferHandleItemProposalForm : InputHandlingItemProposalForm
    {
        public string InfoTransferProcess { get; set; }
        public virtual int UserTransferHandleID { get; set; }
        [JsonIgnore]
        public string UserTransferHandleName { get; set; }
        public override string GetContent(string UserName)
        {
            return $"Chuyển xử lý phiếu đề xuất vật phẩm: <b>{UserName}</b> chuyển phiếu đề xuất vật phẩm cho <b>{UserTransferHandleName}</b>";
        }
        public override string GetReason()
        {
            return InfoTransferProcess;
        }

        public override int GetStatusChange()
        {
            return Constants.StatusPDXVP.TL;
        }

        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPDX.CD) return $"Chỉ được chuyển phiếu ở trạng thái chờ duyệt".ToMessageForUser();

            return string.Empty;
        }
    }
    #endregion



    public class ItemProposalFormExportWord : TemplateExportWord
    {
        public ItemProposalFormExportWord() { }
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
        public string HinhThucDeXuat { get; set; }
        public string LoaiDeXuat { get; set; }
        public static string GetOne(long ItemProposalFormID, out ItemProposalFormExportWord outItemProposalFormExportWord)
        {
            return DBM.GetOne("usp__ItemProposalFormExportWord", new { ItemProposalFormID }, out outItemProposalFormExportWord);
        }
        public Dictionary<string, string> GetDictionaryReplace()
        {
            return new Dictionary<string, string>()
            {
                {"DonViCap1", DonViCap1.ToUpper()},
                {"DonViCap2", DonViCap2},
                {"LoaiDeXuat",LoaiDeXuat },
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
                {"HinhThucDeXuat",HinhThucDeXuat }
            };
        }

        public class LtItemProposalFormExportWord : TableDocument
        {
            public List<ItemProposalFormDetailExportWord> ltItemProposalFormDetailExportWord { get; set; }
            private string title { get; set; } = "DanhSachVatPham";
            private bool hasFooterTable { get; set; } = false;
            public string SetLtItemProposalFormExportWord(long ItemProposalFormID)
            {
                string msg = DBM.GetList("usp_ItemProposalFormExportWord_GetList", new { ItemProposalFormID }, out List<ItemProposalFormDetailExportWord> outlt);
                if (msg.Length > 0) return msg;
                int i = 0;
                outlt.ForEach(x => x.STT = ++i);
                ltItemProposalFormDetailExportWord = outlt;

                return string.Empty;
            }
            public object[] GetFooterTable()
            {
                return null;
            }
            public DataTable GetDataTable() => ltItemProposalFormDetailExportWord.ToDataTable();
            public string GetTitle() => title;
            public bool HasFooterTable() => hasFooterTable;
        }

        public class ItemProposalFormDetailExportWord
        {
            public int STT { get; set; }
            public string LoaiVatPham { get; set; }
            public string MaVPAndTenVP { get; set; }
            public int SL { get; set; }
            public string DVT { get; set; }
        }



    }


}
