using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ASM_API.App_Start.InventoryStore
{
    public class InventoryStoreBase
    {
        [JsonIgnore]
        public long InventoryStoreID { get; set; }
        public Guid ObjectGuid { get; set; }
        [Mapping("Tên phiếu kiểm kê", typeof(MappingObject))]
        public string InventoryStoreName { get; set; }
        public virtual string InventoryStoreCode { get; set; }
        public virtual int UserCreateID { get; set; }
        [Mapping("Kho", typeof(Place))]
        public int PlaceID { get; set; }
        public string BatchIDs { get; set; }

        [Mapping("Nội dung", typeof(Place))]
        public string Content { get; set; }
        public string TransferDirectionID { get; set; }
        [Mapping("Người xử lý", typeof(Place))]
        public int UserHandingID { get; set; }
        public virtual DateTime CreateDate { get; set; }
        public virtual DateTime LastUpdate { get; set; }
        public virtual int StatusID { get; set; }
    }

    public class InventoryStore : InventoryStoreBase, ILogUpdate<InventoryStore>
    {
        [JsonIgnore]
        public long InventoryStoreID { get; set; }
        [JsonIgnore]
        public long AccountID { get; set; }
        [JsonIgnore]
        public override int UserCreateID { get; set; }
        [JsonIgnore]
        public override DateTime CreateDate { get; set; }
        [JsonIgnore]
        public override DateTime LastUpdate { get; set; }
        [JsonIgnore]
        public override int StatusID { get; set; }
        [JsonIgnore]
        public List<ImportBatch> ltBatchID { get { return BatchIDs.Split(',').Select(x => new ImportBatch() { ImportBatchID = x.ToNumber(0) }).ToList(); } }
        public bool IsSendApprove { get; set; }
        public List<InventoryStoreDetail> ltInventoryStoreDetail { get; set; }
        [JsonIgnore]
        public string InfoLogUpdate { get; set; }

        public string InsertUpdate(DBM dbm, out InventoryStore outInventoryStore)
        {
            outInventoryStore = null;

            string msg = dbm.SetStoreNameAndParams("usp_InventoryStore_InsertOrUpdate", new
            {
                InventoryStoreID,
                AccountID,
                InventoryStoreName,
                UserCreateID,
                PlaceID,
                BatchIDs,
                Content,
                TransferDirectionID,
                UserHandingID,
                StatusID,
                InventoryStoreCode
            });
            if (msg.Length > 0) return msg;

            return dbm.GetOne(out outInventoryStore);
        }

        public static string GetOneObjectGuid(Guid ObjectGuid, out long InventoryStoreID)
        {
            InventoryStoreID = 0;

            InventoryStore u;
            string msg = DBM.GetOne("usp_InventoryStore_GetByObjectGuid", new { ObjectGuid }, out u);
            if (msg.Length > 0) return msg;

            if (u == null) return ("Không tồn tại User có ObjectGuid = " + ObjectGuid).ToMessageForUser();
            InventoryStoreID = u.InventoryStoreID;

            return msg;
        }

        public static string GetOne(long InventoryStoreID, int AccountID, out InventoryStore outInventoryStore)
        {
            return DBM.GetOne("usp_InventoryStore_GetOne", new { AccountID, InventoryStoreID }, out outInventoryStore);
        }

        public static string GetSuggestSearch(string TextSearch, int UserID, int AccountID, out DataTable dt)
        {
            return DBM.ExecStore("usp_InventoryStore_SuggestSearch", new { AccountID, TextSearch, UserID }, out dt);
        }

        public string SetInfoChangeRequest(InventoryStore oldInventoryStore)
        {
            string msg = InventoryStoreDetail.GetListByInventoryStoreID(oldInventoryStore.InventoryStoreID, out var InventoryStoreDetails_Old);
            if (msg.Length > 0) return msg;
            oldInventoryStore.ltInventoryStoreDetail = InventoryStoreDetails_Old;

            string SEPARATOR = "; ";
            msg = this.GetUpdateInfo3(oldInventoryStore, SEPARATOR, out string logChange);
            if (msg.Length > 0) return msg;

            InfoLogUpdate = logChange;

            return string.Empty;
        }
        public static string GetTotalByDateCode(DateTime Date, out int Total)
        {
            return DBM.ExecStore("usp_InventoryStore_GetByDateCode", new { Date }, out Total);
        }

        public string GetInfoChangeRequest() => InfoLogUpdate;

        public static string UpdateStatusID(DBM dbm, long ID, int AccountID, int InventoryStoreStatusID)
        {
            string msg = dbm.SetStoreNameAndParams("usp_InventoryStore_UpdateStatus",
              new
              {
                  ID,
                  InventoryStoreStatusID,
                  AccountID
              });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }

        public static string UpdateUserHanding(DBM dbm, long ID, int AccountID, int UserIDHandling)
        {
            string msg = dbm.SetStoreNameAndParams("usp_InventoryStore_UpdateUserHanding",
              new
              {
                  ID,
                  AccountID,
                  UserIDHandling
              });
            if (msg.Length > 0) return msg;

            return dbm.ExecStore();
        }
    }

    public class InventoryStoreSearchResult : InventoryStoreBase
    {
        [JsonIgnore]
        public override int UserCreateID { get; set; }
        public override int StatusID { get; set; }
        public override DateTime CreateDate { get; set; }
        [JsonIgnore]
        public override DateTime LastUpdate { get; set; }
        public string PlaceFullName { get; set; }
        public string BatchCodes { get; set; }
        public DateTime? ProcessingDate { get; set; }
        public string StatusName { get; set; }
        public string UserCreateName { get; set; }
        public string UserHandingName { get; set; }
        public ButtonShowPKKVP ButtonShow { get; set; }

        public static string GetListSearch(InventoryStoreSearch formSearch, out List<InventoryStoreSearchResult> lt, out int total)
        {
            return Paging.ExecByStore("usp_InventoryStoreSearchResult_GetListSearch", "InventoryStore.InventoryStoreID", formSearch, out lt, out total);
        }
    }
    public class InventoryStoreModify : InventoryStoreBase
    {
        public List<InventoryStoreDetailView> ltInventoryStoreDetailView { get; set; }
        [JsonIgnore]
        public override int UserCreateID { get; set; }
        [JsonIgnore]
        public override int StatusID { get; set; }
        [JsonIgnore]
        public override DateTime CreateDate { get; set; }
        [JsonIgnore]
        public override DateTime LastUpdate { get; set; }
        public string PlaceName { get; set; }

        public static string GetOne(long InventoryStoreID, int AccountID, out InventoryStoreModify outInventoryStore)
        {
            return DBM.GetOne("usp_InventoryStore_GetOne", new { AccountID, InventoryStoreID }, out outInventoryStore);
        }
    }

    public class InventoryStoreViewDetail : InventoryStoreBase
    {
        [JsonIgnore]
        public override int UserCreateID { get; set; }
        [JsonIgnore]
        public override int StatusID { get; set; }
        public string PlaceFullName { get; set; }
        public DateTime? ProcessingDate { get; set; }
        public string StatusName { get; set; }
        public string UserCreateDetail { get; set; }
        public string UserHandingDetail { get; set; }
        public string BatchCodes { get; set; }
        public List<InventoryStoreDetailView> ltInventoryStoreDetailView { get; set; }
        public List<TransferHandlingLogView> ltTransferHandlingLogView { get; set; }

        public static string GetListSearch(long InventoryStoreID, int AccountID, out InventoryStoreViewDetail outItem)
        {
            return DBM.GetOne("usp_InventoryStoreViewDetail_GetOne", new
            {
                InventoryStoreID,
                AccountID
            }, out outItem);
        }
    }

    public abstract class InventoryStoreHanding
    {
        public Guid ObjectGuid { get; set; }
        public abstract string GetContent(string UserName, string InventoryStoreCode = "");
        public abstract string ValidateInput(int StatusID);
        public abstract int GetStatusChange();
        public abstract string GetReason();
    }

    #region Class Người tạo cập nhật trạng thái phiếu kiểm kê
    public abstract class InputUpdateStatusInventoryStore : InventoryStoreHanding { }
    public class InputDeleteInventoryStore : InputUpdateStatusInventoryStore
    {
        public override string GetContent(string UserName, string InventoryStoreCode = "")
        {
            return $"Xóa: <b>{UserName}</b> xóa Phiếu kiểm kê Kho: {InventoryStoreCode}. Trạng thái phiếu <b>Đã xóa</b>.";
        }

        public override string GetReason()
        {
            return "";
        }

        public override int GetStatusChange()
        {
            return Constants.StatusPKKVP.DX;
        }

        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPKKVP.MT && StatusID != Constants.StatusPKKVP.TL) return "Bạn chỉ được xóa Phiếu kiểm kê Kho khi phiếu Lưu nháp hoặc đã được Trả lại".ToMessageForUser();

            return string.Empty;
        }
    }
    public class InputRestoreInventoryStore : InputUpdateStatusInventoryStore
    {
        public override string GetContent(string UserName, string InventoryStoreCode = "")
        {
            return $"Khôi phục:<b>{UserName}</b> khôi phục Phiếu kiểm kê Kho: {InventoryStoreCode}. Trạng thái phiếu đã <b>Lưu nháp</b>.";
        }

        public override string GetReason()
        {
            return "";
        }

        public override int GetStatusChange()
        {
            return Constants.StatusPKKVP.MT;
        }

        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPKKVP.DX) return "Bạn chỉ được khôi phục Phiếu kiểm kê Kho khi phiếu đã Xóa".ToMessageForUser();

            return string.Empty;
        }
    }
    #endregion

    #region Class Người duyệt xử lý phiếu kiểm kê
    public abstract class InputHandlingInventoryStore : InventoryStoreHanding
    {
        public string InfoTransferProcess { get; set; }
    }

    public class InputReturnInventoryStore : InputTransferHandleInventoryStore
    {
        [JsonIgnore]
        public override int UserTransferHandleID { get; set; }

        public InputReturnInventoryStore(string reasonRefuse)
        {
            InfoTransferProcess = reasonRefuse;
        }
        public override string GetContent(string UserName, string InventoryStoreCode = "")
        {
            return $"Trả lại Phiếu kiểm kê Kho: <b>{UserName}</b> trả lại Phiếu kiểm kê Kho. Trạng thái phiếu <b>Trả lại</b>. Lý do: {InfoTransferProcess}";
        }
        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPKKVP.CXL) return "Bạn chỉ được trả lại Phiếu kiểm kê Kho khi phiếu đã ở trạng thái Chờ duyệt".ToMessageForUser();

            if (string.IsNullOrEmpty(InfoTransferProcess) || InfoTransferProcess.Length < 3 || InfoTransferProcess.Length > 255) return "Lý do Trả lại là trường thông tin bắt buộc, có độ dài từ 3 đến 255 ký tự".ToMessageForUser();

            return string.Empty;
        }
        public override int GetStatusChange()
        {
            return Constants.StatusPKKVP.TL;
        }
        public override string GetReason()
        {
            return InfoTransferProcess;
        }

    }
    public class InputApproveInventoryStore : InputHandlingInventoryStore
    {
        public override string GetContent(string UserName, string InventoryStoreCode = "")
        {
            return $"Xác nhận Phiếu kiểm kê Kho: <b>{UserName}</b> đã xác nhận Phiếu kiểm kê Kho: {InventoryStoreCode}";
        }
        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPKKVP.CXL) return "Bạn chỉ được duyệt Phiếu kiểm kê Kho khi phiếu đã ở trạng thái Chờ duyệt".ToMessageForUser();

            return string.Empty;
        }
        public override int GetStatusChange()
        {
            return Constants.StatusPKKVP.DAXONG;
        }
        public override string GetReason()
        {
            return "";
        }
    }

    public class InputTransferHandleInventoryStore : InputHandlingInventoryStore
    {
        public virtual int UserTransferHandleID { get; set; }
        [JsonIgnore]
        public string UserTransferHandleName { get; set; }
        public override string GetContent(string UserName, string InventoryStoreCode = "")
        {
            return $"Chuyển xử lý Phiếu kiểm kê Kho: <b>{UserName}</b> chuyển Phiếu kiểm kê Kho: {InventoryStoreCode} cho <b>{UserTransferHandleName}</b>";
        }
        public override string GetReason()
        {
            return InfoTransferProcess;
        }

        public override int GetStatusChange()
        {
            return Constants.StatusPKKVP.TL;
        }

        public override string ValidateInput(int StatusID)
        {
            if (StatusID != Constants.StatusPKKVP.CXL) return $"Chỉ được chuyển phiếu ở trạng thái Chờ duyệt".ToMessageForUser();

            if (string.IsNullOrEmpty(InfoTransferProcess) || InfoTransferProcess.Length < 3 || InfoTransferProcess.Length > 255) return "Lý do Trả lại là trường thông tin bắt buộc, có độ dài từ 3 đến 255 ký tự".ToMessageForUser();

            return string.Empty;
        }
    }
    #endregion
}