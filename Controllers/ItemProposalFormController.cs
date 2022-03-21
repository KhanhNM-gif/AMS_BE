using ASM_API.App_Start.ItemProposalForm;
using ASM_API.App_Start.Template;
using BSS;
using BSS.DataValidator;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Http;
using static ASM_API.App_Start.ItemProposalForm.ItemProposalFormExportWord;

namespace ASM_API.Controllers
{
    public class ItemProposalFormController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate(ItemProposalForm itemProposalForm)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertUpdate(UserToken.UserID, itemProposalForm, out ItemProposalForm o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }
        private string DoInsertUpdate(int UserIDCreate, ItemProposalForm itemProposalForm, out ItemProposalForm outItemProposalForm)
        {
            outItemProposalForm = new ItemProposalForm();

            string msg = DoInsertUpdate_Validate(itemProposalForm, out var itemProposalFormInDB);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = SetData(itemProposalForm, itemProposalFormInDB);
            if (msg.Length > 0) return msg;

            DBM dbm = new DBM();
            dbm.BeginTransac();

            try
            {
                msg = DoInsertUpdate_ObjectToDB(dbm, itemProposalForm, out outItemProposalForm, UserIDCreate);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at ItemProposalForm DoInsertUpdate";
            }

            dbm.CommitTransac();

            return msg;
        }
        private string DoInsertUpdate_Validate(ItemProposalForm dataInput, out ItemProposalForm outItemProposalForm)
        {
            outItemProposalForm = null;

            string msg = DataValidator.Validate(new
            {
                dataInput.ID,
                dataInput.ItemProposalFormReason,
                dataInput.UserIDHandling,
                dataInput.ItemProposalFormStatusID,
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            if (dataInput.UserIDHandling == UserToken.UserID) return ("Bạn không được phép chuyển Phiếu đề xuất cho chính mình").ToMessageForUser();
            if (dataInput.ItemProposalFormTypeID != 1 && dataInput.ItemProposalFormTypeID != 2) return "Bạn chưa chọn Hình thức đề xuất".ToMessageForUser();

            if (dataInput.UserIDHandling == 0) return "Bạn phải chọn người xử lý".ToMessageForUser();

            msg = Role.Check(dataInput.UserIDHandling, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_DUYET, out bool IsRole);
            if (msg.Length > 0) return msg;
            if (!IsRole) return "Người xử lý không có quyền duyệt phiếu".ToMessageForUser();

            string logmsg = "";
            List<long> duplicates = dataInput.ltItemProposalFormDetail.GroupBy(x => x.ItemID).Where(g => g.Count() > 1).Select(x => x.Key).ToList();
            msg = Item.GetListItemByItemIDs(string.Join(",", duplicates), UserToken.AccountID, out List<Item> outlt);
            if (msg.Length > 0) return msg;
            if (outlt.Any()) return $"Vậy phẩm {string.Join(",", outlt.Select(x => x.ItemName))} đang được chọn nhiều lần trong danh sách Vật phẩm đề xuất. Vui lòng kiểm tra lại";

            msg = ItemUnit.GetList(out List<ItemUnit> outItemUnit);
            if (msg.Length > 0) return msg;

            //if (string.IsNullOrEmpty(dataInput.InfoTransferProcess) || dataInput.InfoTransferProcess.Length < 3 || dataInput.InfoTransferProcess.Length > 255) return "Ý kiến xử lý là trường thông tin bắt buộc, có độ dài từ 3 đến 255 ký tự".ToMessageForUser();

            if (dataInput.ObjectGuid != Guid.Empty)
            {
                msg = CacheObject.GetItemProposalFormbyGUID(dataInput.ObjectGuid, out long itemProposalFormID);
                if (msg.Length > 0) return msg;

                msg = ItemProposalForm.GetOne(itemProposalFormID, UserToken.AccountID, out outItemProposalForm);
                if (msg.Length > 0) return msg;
                if (outItemProposalForm == null) return $"Phiều đề xuất Vật phẩm có itemProposalFormID= {itemProposalFormID} không tồn tại";
                if (outItemProposalForm.UserIDCreate != UserToken.UserID) return $"Bạn không thể sửa Phiếu đề xuất Vật phẩm của người dùng khác tạo ra".ToMessageForUser();

                if (outItemProposalForm.ItemProposalFormStatusID != Constants.StatusPDXVP.TL && outItemProposalForm.ItemProposalFormStatusID != Constants.StatusPDXVP.MT) return "Bạn chỉ có thể sửa phiếu ở trạng thái Mới tạo hoặc Trả lại".ToMessageForUser();
            }

            if (dataInput.ltItemProposalFormDetail is null || !dataInput.ltItemProposalFormDetail.Any()) return "Bạn chưa chọn Vật phẩm đề xuất".ToMessageForUser();

            foreach (var item in dataInput.ltItemProposalFormDetail)
            {
                msg = DataValidator.Validate(new
                {
                    item.ID,
                    item.ItemUnitID,
                    item.ItemTypeID,
                    item.Quantity,
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = Item.GetOneByItemID(item.ItemID, out Item outItem);
                if (msg.Length > 0) return msg;
                if (outItem is null) return ("Không có Vật phẩm có ID = " + item.ItemTypeID).ToMessageForUser();

                if (!outItemUnit.Exists(x => x.ItemUnitID.Equals(item.ItemUnitID))) return $"Không có Đơn vị tính có ID = {item.ItemUnitID}".ToString();

                item.ItemTypeID = outItem.ItemTypeID;
                item.ItemCode = outItem.ItemCode;

                if (item.Quantity == 0) logmsg += outItem.ItemName + " ";
            }
            if (logmsg.Length > 0)
                return ("Bạn chưa nhập vào số lượng cho: " + logmsg).ToMessageForUser();

            return msg;
        }
        private string SetData(ItemProposalForm proposalFromItemItem, ItemProposalForm itemProposalFormInDB)
        {
            string msg;

            proposalFromItemItem.UserIDCreate = UserToken.UserID;
            proposalFromItemItem.AccountID = UserToken.AccountID;
            proposalFromItemItem.ItemProposalFormStatusID = proposalFromItemItem.IsSendApprove == true ? proposalFromItemItem.ItemProposalFormStatusID = Constants.StatusPDXVP.CXL : proposalFromItemItem.ItemProposalFormStatusID = Constants.StatusPDXVP.MT;

            if (itemProposalFormInDB == null)
            {
                msg = ItemProposalForm.GetTotalByDateCode(DateTime.Now.ToString("yyMMdd"), out int Total);
                if (msg.Length > 0) return msg;
                proposalFromItemItem.ItemProposalFormCode = "PDXVP_" + DateTime.Now.ToString("yyMMdd") + "_" + (Total + 1);
            }
            else
            {
                proposalFromItemItem.ID = itemProposalFormInDB.ID;
                proposalFromItemItem.ItemProposalFormCode = itemProposalFormInDB.ItemProposalFormCode;

                msg = proposalFromItemItem.SetInfoChangeRequest(itemProposalFormInDB);
                if (msg.Length > 0) return msg;
            }

            return string.Empty;
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, ItemProposalForm itemProposalForm, out ItemProposalForm outItemProposalForm, int UserIDCreate)
        {
            string msg = itemProposalForm.InsertUpdate(dbm, out outItemProposalForm);
            if (msg.Length > 0) return msg;

            if (itemProposalForm.IsSendApprove)
            {
                TransferHandlingLog log = new TransferHandlingLog
                {
                    ObjectID = outItemProposalForm.ID,
                    ObjectTypeID = Constants.TransferHandling.PDXVP,
                    UserIDHandling = outItemProposalForm.UserIDCreate,
                    Comment = "Chuyển xử lý Phiếu đề xuất Vật Phẩm",
                    TransferDirectionID = itemProposalForm.TransferDirectionID
                };
                msg = log.InsertUpdate(dbm, out TransferHandlingLog _);
                if (msg.Length > 0) return msg;
            }

            if (itemProposalForm.ltItemProposalFormDetail.Count > 0)
            {
                msg = DoInsertUpdate_ProposalFormDetail(dbm, outItemProposalForm.ID, itemProposalForm.ltItemProposalFormDetail, out List<ItemProposalFormDetail> outItemProposalFormDetails);
                if (msg.Length > 0) return msg;
                outItemProposalForm.ltItemProposalFormDetail = outItemProposalFormDetails;
            }
            msg = Log.WriteHistoryLog(dbm, itemProposalForm.ID == 0 ? $"Tạo Phiếu đề xuất Vật phẩm: <b>{UserToken.UserName}</b> tạo mới Phiếu đề xuất Vật phẩm. Trạng thái Phiếu đề xuất là <b>Mới tạo</b>" : $"Cập nhật: {itemProposalForm.GetInfoChangeRequest()}", outItemProposalForm.ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            if (itemProposalForm.IsSendApprove)
            {
                msg = Log.WriteHistoryLog(dbm, $"Chuyển xử lý Phiếu đề xuất Vật phẩm: <b>{UserToken.UserName}</b> chuyển xử lý Phiếu đề xuất Vật phẩm cho { AccountUser.GetUserNameByUserID(outItemProposalForm.UserIDHandling)}. Trạng thái Phiếu đề xuất là <b>Chờ xử lý</b>"
                    , outItemProposalForm.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }

            return msg;
        }
        private string DoInsertUpdate_ProposalFormDetail(DBM dbm, long ID, List<ItemProposalFormDetail> itemProposalFormDetails, out List<ItemProposalFormDetail> ItemProposalFormDetailsOut)
        {
            string msg = ItemProposalFormDetail.InsertUpdateByDataTable(dbm, itemProposalFormDetails, ID, out ItemProposalFormDetailsOut);
            if (msg.Length > 0) return msg;
            return msg;
        }


        [HttpGet]
        public Result GetOne(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetOne(ObjectGuid, out ItemProposalFormModify o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }
        private string DoGetOne(Guid ObjectGuid, out ItemProposalFormModify proposalOut)
        {
            proposalOut = null;


            string msg = CacheObject.GetItemProposalFormbyGUID(ObjectGuid, out long itemProposalFormID);
            if (msg.Length > 0) return msg;

            msg = ItemProposalFormModify.GetOne(itemProposalFormID, UserToken.AccountID, out proposalOut);
            if (msg.Length > 0) return msg;
            if (proposalOut == null) return ("Phiếu đề xuất vật phẩm không tồn tại ObjectGuid = " + ObjectGuid).ToMessageForUser();

            /*msg = TransferHandlingLog.GetByObjectID(proposalOut.ID, Constants.TransferHandling.PDX, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;
            proposalOut.TransferDirectionID = transferHandlingLog != null ? transferHandlingLog.TransferDirectionID : "";*/

            msg = ItemProposalFormDetailModify.GetListByProposalFormID(proposalOut.ID, out List<ItemProposalFormDetailModify> ltform);
            if (msg.Length > 0) return msg;

            proposalOut.ltItemProposalFormDetail = ltform;

            return "";
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
            string msg = ItemProposalForm.GetSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        [HttpPost]
        public Result GetListEasySearch(ItemProposalFormEasySearch itemProposalFormSearch)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoDoGetListEasySearch(UserToken.UserID, itemProposalFormSearch, out int total, out List<ItemProposalFormSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoDoGetListEasySearch(int UserID, ItemProposalFormEasySearch itemProposalFormEasySearch, out int total, out List<ItemProposalFormSearchResult> lt)
        {
            lt = null;
            total = 0;

            string msg = DoGetListEasySearch_GetProposalFormSearch(itemProposalFormEasySearch, out ItemProposalFormSearch outItemproposalFormSearch);
            if (msg.Length > 0) return msg;

            outItemproposalFormSearch.PageSize = itemProposalFormEasySearch.PageSize;
            outItemproposalFormSearch.CurrentPage = itemProposalFormEasySearch.CurrentPage;
            outItemproposalFormSearch.isEasySearch = true;
            //outItemproposalFormSearch.StatusIDs = "1,2,3";// chỉ hiện trạng thái MT,CD,DD trên ds easy search

            msg = DoGetList(outItemproposalFormSearch, out lt, out total);
            return msg;

        }
        private string DoGetListEasySearch_GetProposalFormSearch(ItemProposalFormEasySearch itemProposalFormEasySearch, out ItemProposalFormSearch ms)
        {
            ms = new ItemProposalFormSearch();

            ms.TextSearch = itemProposalFormEasySearch.TextSearch;
            ms.CurrentPage = itemProposalFormEasySearch.CurrentPage;
            ms.PageSize = itemProposalFormEasySearch.PageSize;

            if (itemProposalFormEasySearch.ObjectCategory == 1) ms.ID = itemProposalFormEasySearch.ObjectID.ToNumber(0);
            if (itemProposalFormEasySearch.ObjectCategory == 2) ms.UserCreateIDs = itemProposalFormEasySearch.ObjectID.ToString();
            if (itemProposalFormEasySearch.ObjectCategory == 3) ms.UserIDHandings = itemProposalFormEasySearch.ObjectID.ToString();

            if (itemProposalFormEasySearch.ObjectCategory > 0) ms.TextSearch = "";

            return "";
        }
        private string DoGetList(ItemProposalFormSearch formSearch, out List<ItemProposalFormSearchResult> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                formSearch.AccountID = UserToken.AccountID;
                formSearch.UserID = UserToken.UserID;

                string msg = ItemProposalFormSearchResult.GetListSearch(formSearch, out lt, out totalSearch);
                if (msg.Length > 0) return msg;

                msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_DUYET, out bool IsRoleApprove);
                if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    msg = DoGetListButtonFuction(item, UserToken.UserID, IsRoleApprove, out ButtonShowPDXVP b);
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
        private string DoGetListButtonFuction(ItemProposalFormSearchResult PDX, int UserIDLogin, bool IsRoleApprove, out ButtonShowPDXVP b)
        {
            b = new ButtonShowPDXVP();

            int s = PDX.ItemProposalFormStatusID;

            if (UserIDLogin == PDX.UserIDCreate)
            {
                if (s == Constants.StatusPDXVP.MT)
                {
                    b.Delete = true;
                    b.Edit = true;
                }

                if (s == Constants.StatusPDXVP.DX) b.Restore = true;

                if (s == Constants.StatusPDXVP.TL) { b.Edit = true; b.Delete = true; }
            }

            if (UserIDLogin == PDX.UserIDHandling && s == Constants.StatusPDXVP.CXL && IsRoleApprove) b.Approved = true;

            b.ViewHistory = true;
            return "";
        }
        [HttpPost]
        public Result GetListAdvancedSearch(ItemProposalFormSearch data)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_IsVisitPage);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                msg = DoGetListAdvancedSearch(UserToken.UserID, data, out int total, out List<ItemProposalFormSearchResult> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch(int UserID, ItemProposalFormSearch formSearch, out int Total, out List<ItemProposalFormSearchResult> lt)
        {
            formSearch.isEasySearch = false;

            string msg = DoGetList(formSearch, out lt, out Total);
            return msg;
        }

        [HttpGet]
        public Result ViewDetail(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoViewDetail(ObjectGuid, out ItemProposalFormViewDetail o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }
        private string DoViewDetail(Guid ObjectGuid, out ItemProposalFormViewDetail ItemproposalOut)
        {
            ItemproposalOut = null;

            string msg = CacheObject.GetItemProposalFormbyGUID(ObjectGuid, out long itemProposalFormID);
            if (msg.Length > 0) return msg;

            msg = ItemProposalFormViewDetail.ViewDetail(itemProposalFormID, out ItemproposalOut);
            if (msg.Length > 0) return msg;
            if (ItemproposalOut == null) return ("Phiếu đề Vật Phẩm xuất không tồn tại ID " + itemProposalFormID).ToMessageForUser();

            msg = ItemProposalFormDetailModify.GetListByProposalFormID(itemProposalFormID, out List<ItemProposalFormDetailModify> ltform);
            if (msg.Length > 0) return msg;
            ItemproposalOut.ltItemProposalFormDetail = ltform;

            msg = TransferHandlingLogView.GetList(itemProposalFormID, Constants.TransferHandling.PDXVP, ItemproposalOut.TransferDirectionID, out var ltCommentItemProposalForm);
            if (msg.Length > 0) return msg;
            ItemproposalOut.ltCommentItemProposalForm = ltCommentItemProposalForm.Select(x => new CommentItemProposalForm(x) { }).ToList();


            msg = SetButtonFuntion(ItemproposalOut);
            if (msg.Length > 0) return msg;

            return "";
        }
        private string SetButtonFuntion(ItemProposalFormViewDetail ItemproposalOut)
        {
            ButtonShowHandlingPDXVP ButtonShowHandlingPDXVP = new ButtonShowHandlingPDXVP();
            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_DUYET, out bool IsRoleHandle);
            if (msg.Length > 0) return msg;

            if (ItemproposalOut.UserIDHandling == UserToken.UserID)
            {
                ButtonShowHandlingPDXVP.Handle = IsRoleHandle;
                ButtonShowHandlingPDXVP.TransferHandle = true;
            }

            ItemproposalOut.ButtonShowHandlingPDXVP = ButtonShowHandlingPDXVP;

            return string.Empty;
        }

        [HttpPost]
        public Result Return(InputReturnItemProposalForm input)//Trả lại phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoTransferHandle(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }

        [HttpPost]
        public Result TransferHandle(InputTransferHandleItemProposalForm input) //Chuyển xử lý
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoTransferHandle(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        private string DoTransferHandle(InputTransferHandleItemProposalForm input)
        {
            string msg = DoTransferHandle_Validate(input, out ItemProposalForm itemProposalForm);
            if (msg.Length > 0) return msg;

            try
            {
                DBM dbm = new DBM();
                dbm.BeginTransac();

                msg = TransferHandleUpdateToDB(dbm, itemProposalForm, input);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }

                dbm.CommitTransac();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return string.Empty;
        }

        private string DoTransferHandle_Validate(InputTransferHandleItemProposalForm input, out ItemProposalForm itemProposalForm)
        {
            string msg = ValidateHanding(input, out itemProposalForm);
            if (msg.Length > 0) return msg;

            if (input is InputReturnItemProposalForm) input.UserTransferHandleID = itemProposalForm.UserIDCreate;

            msg = AccountUser.GetOneByUserID(input.UserTransferHandleID, out var accountUser);
            if (msg.Length > 0) return msg;
            if (accountUser == null) return "Người duyệt không tồn tại".ToMessageForUser();
            input.UserTransferHandleName = accountUser.UserName;

            if (input.UserTransferHandleID == itemProposalForm.UserIDHandling) return "Bạn đang là người xử lý phiếu. Vui lòng kiểm tra lại".ToMessageForUser();

            msg = Role.Check(input.UserTransferHandleID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_DUYET, out bool IsRole);
            if (msg.Length > 0) return msg;
            if (!IsRole) return "Người chuyển duyệt không có quyền duyệt".ToMessageForUser();

            if (string.IsNullOrEmpty(input.GetReason()) || input.GetReason().Length < 3 || input.GetReason().Length > 255) return "Ý kiến xử lý là trường thông tin bắt buộc, có độ dài từ 3 đến 255 ký tự".ToMessageForUser();

            return string.Empty;
        }
        private string TransferHandleUpdateToDB(DBM dbm, ItemProposalForm itemProposalForm, InputTransferHandleItemProposalForm input)
        {
            string msg = ItemProposalForm.UpdateTransferHanding(dbm, itemProposalForm.ID, input.InfoTransferProcess, input.UserTransferHandleID);
            if (msg.Length > 0) return msg;

            TransferHandlingLog log = new TransferHandlingLog
            {
                ObjectID = itemProposalForm.ID,
                ObjectTypeID = Constants.TransferHandling.PDXVP,
                UserIDHandling = UserToken.UserID,
                Comment = input.GetReason(),
                TransferDirectionID = itemProposalForm.TransferDirectionID
            };
            msg = log.InsertUpdate(dbm, out TransferHandlingLog _);
            if (msg.Length > 0) return msg;

            if (input.UserTransferHandleID == itemProposalForm.UserIDCreate)
            {
                msg = ItemProposalForm.UpdateStatusID(dbm, itemProposalForm.ID, UserToken.AccountID, input.GetStatusChange());
                if (msg.Length > 0) return msg;
            }

            msg = Log.WriteHistoryLog(dbm, input.GetContent(UserToken.UserName), itemProposalForm.ObjectGuid, UserToken.UserID, Common.GetClientIpAddress(Request));
            if (msg.Length > 0) return msg;

            return msg;
        }

        [HttpGet]
        public Result GetListStatus()
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            List<ItemProposalFormStatus> lt;
            string msg = ItemProposalFormStatus.GetListStatus(out lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpPost]
        public Result Delete(InputDeleteItemProposalForm input)//Xóa phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoUpdateStatus(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }
        [HttpPost]
        public Result Restore(InputRestoreItemProposalForm input)//Khôi phục phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoUpdateStatus(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }

        /// <summary>
        /// Bỏ việc Duyệt trực tiếp phiếu đề xuât phải duyệt thông qua
        /// </summary>
        /*[HttpPost]
        public Result Approve(InputApproveItemProposalForm input)//Duyệt phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoUpdateStatus(input);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return "".ToResultOk();
        }*/


        private string DoUpdateStatus(ItemProposalFormHanding input)
        {
            string msg = ValidateHanding(input, out var itemProposalForm);
            if (msg.Length > 0) { return msg.ToMessageForUser(); }

            msg = UpdateStatusID_SaveToDB(itemProposalForm, input.GetStatusChange(), UserToken.UserID, input.GetContent(UserToken.UserName), input.GetReason());
            if (msg.Length > 0) { return msg.ToMessageForUser(); }

            return msg;
        }
        private string ValidateHanding(ItemProposalFormHanding input, out ItemProposalForm itemProposalForm)
        {
            itemProposalForm = null;

            string msg = CacheObject.GetItemProposalFormbyGUID(input.ObjectGuid, out long itemProposalFormID);
            if (msg.Length > 0) return msg;

            msg = ItemProposalForm.GetOne(itemProposalFormID, UserToken.AccountID, out itemProposalForm);
            if (msg.Length > 0) return msg;
            if (itemProposalForm == null) return ("Không tồn tại phiếu đề xuất có ID = " + itemProposalFormID).ToMessageForUser();

            if (input is InputHandlingItemProposalForm)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_QLPDXVP_DUYET);
                if (msg.Length > 0) return msg;
                if (itemProposalForm.UserIDHandling != UserToken.UserID) return "Bạn không phải là Tài khoản xử lý Phiếu đề xuất vật phẩm này".ToMessageForUser();
            }
            else if (input is InputUpdateStatusItemProposalForm)
            {
                msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDXVP, Role.ROLE_KHO_CRUD);
                if (msg.Length > 0) return msg;
                if (itemProposalForm.UserIDCreate != UserToken.UserID) return "Bạn không phải Tài khoản tạo Phiếu đê xuất vật phẩm này".ToMessageForUser();
            }

            msg = input.ValidateInput(itemProposalForm.ItemProposalFormStatusID);
            if (msg.Length > 0) return msg.ToMessageForUser();

            return string.Empty;
        }

        private string UpdateStatusID_SaveToDB(ItemProposalForm ItemProposalForm, int StatusID, int UserID, string logContent, string itemProposalFormReasonRefuse)
        {
            string msg;

            if (StatusID == Constants.StatusPDXVP.TL) msg = ItemProposalForm.UpdateStatusID(new DBM(), ItemProposalForm.ID, UserToken.AccountID, StatusID, itemProposalFormReasonRefuse);
            else msg = ItemProposalForm.UpdateStatusID(new DBM(), ItemProposalForm.ID, UserToken.AccountID, StatusID);
            if (msg.Length > 0) return msg;


            msg = Log.WriteHistoryLog(logContent, ItemProposalForm.ObjectGuid, UserID, Common.GetClientIpAddress(Request));
            return msg;
        }

        [HttpGet]
        public Result GetListProposalByUserIDCreate(int UserIDCreate)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            string msg = ItemProposalFormSearchResult.GetListByUserCreateID(UserIDCreate, out List<ItemProposalFormSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }

        [HttpPost]
        public Result ExportFilePDF(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoExportFilePDF(ObjectGuid, out string urlFile);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return urlFile.ToResultOk();

        }
        private string DoExportFilePDF(Guid ObjectGuid, out string UrlFile)
        {
            UrlFile = "";

            string msg = CacheObject.GetItemProposalFormbyGUID(ObjectGuid, out long itemProposalFormID);
            if (msg.Length > 0) return msg;

            msg = ItemProposalFormExportWord.GetOne(itemProposalFormID, out ItemProposalFormExportWord itemProposalFormExportWord);
            if (msg.Length > 0) return msg;

            LtItemProposalFormExportWord ltItemProposalFormExportWord = new LtItemProposalFormExportWord();
            if (msg.Length > 0) return msg;

            ltItemProposalFormExportWord.SetLtItemProposalFormExportWord(itemProposalFormID);

            string nameFileDoc = $"PhieuDeXuat_{DateTime.Now.ToString("yyyyMMddHH")}.docx";
            string nameFilePDF = $"PhieuDeXuat_{DateTime.Now.ToString("yyyyMMddHH")}.pdf";

            msg = BSS.Common.GetSetting("FolderFileExportPDX", out string FolderFileExportPNK);
            if (msg.Length > 0) return msg;

            var InfoFileWord = UtilitiesFile.GetInfoFile(DateTime.Now, nameFileDoc, FolderFileExportPNK, true);
            var InfoFilePDF = UtilitiesFile.GetInfoFile(DateTime.Now, nameFilePDF, FolderFileExportPNK, true);

            msg = WordDocument.FillTemplate(HttpContext.Current.Server.MapPath(@"\File\FileTemplate\TemplateInPDX.dotx"), InfoFileWord.FilePathPhysical, InfoFilePDF.FilePathPhysical, itemProposalFormExportWord, ltItemProposalFormExportWord);
            if (msg.Length > 0) return msg;

            UrlFile = InfoFilePDF.FilePathVirtual;

            return string.Empty;
        }


        //[HttpPost]
        //public Result Restore([FromBody] JObject data)//Phục hồi phiếu
        //{
        //    if (!ResultCheckToken.isOk) return ResultCheckToken;

        //    string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_CRUD);
        //    if (msg.Length > 0) return Log.ProcessError(msg).ToResultError()

        //    return UpdateStatusID(data, Constants.StatusPDX.MT);
        //}
        //[HttpPost]
        //public Result TransferHandling([FromBody] JObject data)
        //{
        //    if (!ResultCheckToken.isOk) return ResultCheckToken;
        //    string msg = DoTransferHandling(data);
        //    if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        //    return "".ToResultOk();
        //}
        //private string DoTransferHandling([FromBody] JObject data)
        //{
        //    string msg = "";

        //    msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
        //    if (msg.Length > 0) return msg;

        //    msg = data.ToString("UserIDHandling", out string sUserIDHandling);
        //    if (msg.Length > 0) return msg;

        //    msg = data.ToString("Commenthandling", out string Commenthandling);
        //    if (msg.Length > 0) return msg;

        //    msg = data.ToString("TransferDirectionID", out string TransferDirectionID);
        //    if (msg.Length > 0) return msg;

        //    long UserIDHandling = sUserIDHandling.ToLong(0);

        //    msg = CacheObject.GetProposalFormbyGUID(ObjectGuid, out long ProposalFormID);
        //    if (msg.Length > 0) return msg;
        //    if (ProposalFormID == 0) return ("Không có giá trị phù hợp với ObjectGuid=" + ObjectGuid).ToMessageForUser();

        //    msg = ProposalForm.GetOne(ProposalFormID, out ProposalForm proposalFormDB);
        //    if (msg.Length > 0) return msg;

        //    if (UserIDHandling == UserToken.UserID) return ("Bạn không được phép chuyển Phiếu đề xuất cho chính mình").ToMessageForUser();

        //    if (proposalFormDB.UserIDCreate == UserIDHandling)
        //    {
        //        msg = ProposalForm.UpdateStatusID(new DBM(), ProposalFormID, Constants.StatusPDX.TL, "");
        //        if (msg.Length > 0) return msg;
        //    }
        //    else
        //    {
        //        msg = ProposalForm.UpdateTransferHanding(ProposalFormID, UserIDHandling);
        //        if (msg.Length > 0) return msg;
        //    }

        //    msg = Log.WriteHistoryLog(new DBM(), $"Chuyển xử lý Phiếu đề xuất cho người xử lý: {AccountUser.GetUserNameByUserID(UserIDHandling)} thành công. Nội dung ý kiến xử lý - {Commenthandling}", ObjectGuid, UserToken.UserID);
        //    if (msg.Length > 0) return msg;

        //    TransferHandlingLog logt = new TransferHandlingLog
        //    {
        //        ObjectID = ProposalFormID,
        //        ObjectTypeID = Constants.TransferHandling.PDX,
        //        UserIDHandling = UserIDHandling,
        //        Comment = Commenthandling,
        //        TransferDirectionID = TransferDirectionID
        //    };
        //    msg = logt.InsertUpdate(new DBM(), out TransferHandlingLog transferHandlingLog);
        //    if (msg.Length > 0) return msg;

        //    return msg;
        //}
        //[HttpPost]
        //public Result DeleteAssetTypeDetail([FromBody] JObject data)
        //{
        //    if (!ResultCheckToken.isOk) return ResultCheckToken;

        //    string msg = DoDeleteAssetTypeDetail(data);
        //    if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
        //    return Result.GetResultOk();
        //}
        //private string DoDeleteAssetTypeDetail([FromBody] JObject data)
        //{
        //    string msg = data.ToString("ID", out string sID);
        //    if (msg.Length > 0) return msg;

        //    long ID = sID.ToLong(0);

        //    msg = ProposalFormDetail.GetOneByID(ID, out ProposalFormDetail formDetail);
        //    if (msg.Length > 0) return msg;
        //    if (formDetail == null) return ("Không có loại tài sản ID = " + ID).ToMessageForUser();

        //    msg = ProposalFormDetail.Delete(formDetail.ID);
        //    if (msg.Length > 0) return msg;

        //    return msg;
        //}
        //[HttpGet]
        //public Result GetListProposalByUserIDCreate(long UserIDCreate)
        //{
        //    if (!ResultCheckToken.isOk) return ResultCheckToken;
        //    string msg = DoGetListProposalByUserIDCreate(UserIDCreate, out List<ProposalFormViewDetail> lt);
        //    if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

        //    return lt.ToResultOk();
        //}
        //public string DoGetListProposalByUserIDCreate(long UserIDCreate, out List<ProposalFormViewDetail> lt)
        //{
        //    lt = new List<ProposalFormViewDetail>();

        //    string msg = ProposalFormViewDetail.GetListViewDetailByUserIDCreate(UserIDCreate, out lt);
        //    if (msg.Length > 0) return msg;
        //    if (lt.Count == 0) return ("Danh sách trống ").ToMessageForUser();

        //    foreach (var item in lt)
        //    {
        //        msg = ProposalFormDetail.GetListByProposalFormID(item.ProposalFormID, out List<ProposalFormDetail> ltform);
        //        if (msg.Length > 0) return msg;

        //        item.ltProposalFormDetail = ltform;
        //    }

        //    return msg;
        //}
    }
}