using ASM_API.App_Start.Template;
using BSS;
using BSS.DataValidator;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.Http;
using static ProposalFormViewDetail;
using static ProposalFormViewDetail.ProposalFormExportWord;

namespace WebAPI.Controllers
{
    public class ProposalFormController : Authentication
    {
        [HttpPost]
        public Result InsertUpdate([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoInsertUpdate(UserToken.UserID, data, out ProposalForm o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return o.ToResultOk();
        }
        private string DoInsertUpdate(int UserIDCreate, [FromBody] JObject data, out ProposalForm proposalFormout)
        {
            proposalFormout = new ProposalForm();

            string msg = data.ToObject("ProposalForm", out ProposalForm proposalForm);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoInsertUpdate_Validate(proposalForm);
            if (msg.Length > 0) return msg.ToMessageForUser();

            DBM dbm = new DBM();
            dbm.BeginTransac();
            try
            {
                msg = DoInsertUpdate_ObjectToDB(dbm, proposalForm, out proposalFormout, UserIDCreate);
                if (msg.Length > 0) { dbm.RollBackTransac(); return msg; }
            }
            catch (Exception ex)
            {
                dbm.RollBackTransac();
                return ex.ToString() + " at ProposalForm DoInsertUpdate";
            }

            dbm.CommitTransac();

            return msg;
        }
        private string DoInsertUpdate_ObjectToDB(DBM dbm, ProposalForm proposalForm, out ProposalForm proposalFormout, int UserIDCreate)
        {
            string msg = proposalForm.InsertUpdate(dbm, out proposalFormout);
            if (msg.Length > 0) return msg;

            // if (proposalFormout.ProposalFormStatusID == Constants.StatusPDX.CD)
            // {
            TransferHandlingLog log = new TransferHandlingLog
            {
                ObjectID = proposalFormout.ProposalFormID,
                ObjectTypeID = Constants.TransferHandling.PDX,
                UserIDHandling = proposalFormout.UserIDHandling,
                Comment = proposalForm.CommentHandling,
                TransferDirectionID = proposalForm.TransferDirectionID
            };
            msg = log.InsertUpdate(dbm, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;
            //   }

            if (proposalForm.ltProposalFormDetail.Count > 0)
            {
                msg = DoInsertUpdate_ProposalFormDetail(dbm, proposalFormout.ProposalFormID, proposalForm.ltProposalFormDetail, out List<ProposalFormDetail> ProposalFormDetailsOut);
                if (msg.Length > 0) return msg;
                proposalFormout.ltProposalFormDetail = ProposalFormDetailsOut;
            }
            msg = Log.WriteHistoryLog(dbm, proposalForm.ProposalFormID == 0 ? $"Thêm mới phiếu đề xuất thành công. Mã phiếu {proposalFormout.ProposalFormCode}" : $"Sửa phiếu đề xuất thành công. Mã phiếu {proposalFormout.ProposalFormCode}", proposalFormout.ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            if (proposalFormout.ProposalFormStatusID == Constants.StatusPDX.CD)
            {
                msg = Log.WriteHistoryLog(dbm, $"Chuyển xử lý Phiếu đề xuất cho người xử lý: {AccountUser.GetUserNameByUserID(proposalFormout.UserIDHandling)} thành công. Nội dung ý kiến xử lý - {proposalForm.CommentHandling}", proposalFormout.ObjectGuid, UserToken.UserID);
                if (msg.Length > 0) return msg;
            }

            return msg;
        }
        private string DoInsertUpdate_ProposalFormDetail(DBM dbm, long ProposalFormID, List<ProposalFormDetail> ProposalFormDetails, out List<ProposalFormDetail> ProposalFormDetailsOut)
        {
            string msg = "";
            ProposalFormDetailsOut = new List<ProposalFormDetail>();
            foreach (var item in ProposalFormDetails)
            {
                ProposalFormDetail proposal = new ProposalFormDetail
                {
                    ProposalFormID = ProposalFormID,
                    ID = item.ID,
                    AssetTypeID = item.AssetTypeID,
                    Note = item.Note,
                    Quantity = item.Quantity
                };
                msg = proposal.InsertUpdate(dbm, out ProposalFormDetail proposalOut);
                if (msg.Length > 0) return msg;
                ProposalFormDetailsOut.Add(proposalOut);
            }

            return msg;
        }
        private string DoInsertUpdate_Validate(ProposalForm data)
        {
            string msg = DataValidator.Validate(new
            {
                data.ProposalFormID,
                data.ProposalFormReason,
                data.UserIDHandling,
                data.ProposalFormStatusID,
            }).ToErrorMessage();
            if (msg.Length > 0) return msg.ToMessageForUser();

            data.UserIDCreate = UserToken.UserID;
            data.AccountID = UserToken.AccountID;

            data.ProposalFormStatusID = data.IsSendApprove == true ? data.ProposalFormStatusID = Constants.StatusPDX.CD : data.ProposalFormStatusID = Constants.StatusPDX.MT;

            if (data.ProposalFormID == 0)
            {
                msg = ProposalForm.GetTotalByDateCode(DateTime.Now.ToString("yyMMdd"), out int Total);
                if (msg.Length > 0) return msg;
                data.ProposalFormCode = "PDX_" + DateTime.Now.ToString("yyMMdd") + "_" + (Total + 1);
            }
            else
            {
                msg = ProposalForm.GetOne(data.ProposalFormID, out ProposalForm proposalForm);
                if (msg.Length > 0) return msg;
                data.ProposalFormCode = proposalForm.ProposalFormCode;
            }

            if (data.UserIDHandling == UserToken.UserID) return ("Bạn không được phép chuyển Phiếu đề xuất cho chính mình").ToMessageForUser();

            string logmsg = "";
            foreach (var item in data.ltProposalFormDetail)
            {
                msg = DataValidator.Validate(new
                {
                    item.ProposalFormID,
                    item.ID,
                    item.AssetTypeID,
                    item.Quantity,
                }).ToErrorMessage();
                if (msg.Length > 0) return msg.ToMessageForUser();

                msg = AssetType.GetOneByAssetTypeID(item.AssetTypeID, UserToken.AccountID, out AssetType assetType);
                if (msg.Length > 0) return msg;
                if (assetType == null) return ("Không có loại Tài sản có ID=" + item.AssetTypeID).ToMessageForUser();
                if (item.Quantity == 0) logmsg += assetType.AssetTypeName + " ";
            }
            if (logmsg.Length > 0)
                return ("Bạn chưa nhập vào số lượng cho: " + logmsg).ToMessageForUser();

            return msg;
        }
        [HttpGet]
        public Result GetOne(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoGetOne(ObjectGuid, out ProposalForm o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }
        public string DoGetOne(Guid ObjectGuid, out ProposalForm proposalOut)
        {
            proposalOut = null;


            string msg = CacheObject.GetProposalFormbyGUID(ObjectGuid, out long ProposalFormID);
            if (msg.Length > 0) return msg;

            msg = ProposalForm.GetOne(ProposalFormID, out proposalOut);
            if (msg.Length > 0) return msg;
            if (proposalOut == null) return ("Phiếu đề xuất không tồn tại ObjectGuid = " + ObjectGuid).ToMessageForUser();

            msg = TransferHandlingLog.GetByObjectID(proposalOut.ProposalFormID, Constants.TransferHandling.PDX, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;
            proposalOut.TransferDirectionID = transferHandlingLog != null ? transferHandlingLog.TransferDirectionID : "";


            msg = ProposalFormDetail.GetListByProposalFormID(proposalOut.ProposalFormID, out List<ProposalFormDetail> ltform);
            if (msg.Length > 0) return msg;

            proposalOut.ltProposalFormDetail = ltform;

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
            dt = new DataTable();

            string msg = ProposalForm.GetSuggestSearch(TextSearch, UserToken.AccountID, out dt);
            if (msg.Length > 0) return msg;

            return msg;
        }
        [HttpPost]
        public Result GetListEasySearch([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            //string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_IsVisitPage);
            //if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            string msg = DoDoGetListEasySearch(UserToken.UserID, data, out int total, out List<ProposalFormSearchResult> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return new { Data = lt, Total = total }.ToResultOk();
        }
        private string DoDoGetListEasySearch(int UserID, [FromBody] JObject data, out int total, out List<ProposalFormSearchResult> lt)
        {
            lt = null;
            total = 0;

            string msg = data.ToNumber("PageSize", out int PageSize);
            if (msg.Length > 0) return msg;

            msg = data.ToNumber("CurrentPage", out int CurrentPage);
            if (msg.Length > 0) return msg;

            ProposalFormSearch proposalFormSearch;
            msg = DoGetListEasySearch_GetProposalFormSearch(data, out proposalFormSearch);
            if (msg.Length > 0) return msg;

            proposalFormSearch.PageSize = PageSize;
            proposalFormSearch.CurrentPage = CurrentPage;

            msg = DoGetList(proposalFormSearch, out lt, out total);
            return msg;

        }
        private string DoGetListEasySearch_GetProposalFormSearch([FromBody] JObject data, out ProposalFormSearch ms)
        {
            ms = new ProposalFormSearch();

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
            ms.UserID = UserToken.UserID;

            if (ObjectCategory == 1) ms.ProposalFormID = ObjectID.ToNumber(0);
            if (ObjectCategory == 2) ms.UserIDCreate = ObjectID.ToNumber(0);
            if (ObjectCategory == 4) ms.AssetTypeID = ObjectID.ToNumber(0);

            if (ObjectCategory != 0) ms.TextSearch = "";

            return "";
        }
        private string DoGetList(ProposalFormSearch formSearch, out List<ProposalFormSearchResult> lt, out int totalSearch)
        {
            lt = null; totalSearch = 0;

            try
            {
                formSearch.AccountID = UserToken.AccountID;
                formSearch.UserID = UserToken.UserID;

                string msg = ProposalForm.GetListSearch(formSearch, out lt, out totalSearch);
                if (msg.Length > 0) return msg;

                msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_DUYET, out bool IsRoleApprove);
                if (msg.Length > 0) return msg;

                foreach (var item in lt)
                {
                    ButtonShowPDX b;

                    ProposalFormSearchResult PDX = new ProposalFormSearchResult
                    {
                        ProposalFormID = item.ProposalFormID,
                        ObjectGuid = item.ObjectGuid,
                        ProposalFormCode = item.ProposalFormCode,
                        ProposalFormReason = item.ProposalFormReason,
                        UserIDCreate = item.UserIDCreate,
                        CreateFullName = item.CreateFullName,
                        CreateUserName = item.CreateUserName,
                        CreateDate = item.CreateDate,
                        LastUpdate = item.LastUpdate,
                        ProposalFormStatusID = item.ProposalFormStatusID,
                        ProposalFormStatusName = item.ProposalFormStatusName,
                        AssetTypeName = item.AssetTypeName,
                        UserIDHandling = item.UserIDHandling,
                        UserHandlingFullName = item.UserHandlingFullName,
                        UserHandlingUserName = item.UserHandlingUserName,

                    };
                    msg = DoGetListButtonFuction(PDX, UserToken.UserID, IsRoleApprove, out b);
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
        private string DoGetListButtonFuction(ProposalFormSearchResult PDX, int UserIDLogin, bool IsRoleApprove, out ButtonShowPDX b)
        {
            b = new ButtonShowPDX();

            int s = PDX.ProposalFormStatusID;

            if (UserIDLogin == PDX.UserIDCreate)
            {
                if (s == Constants.StatusPDX.MT)
                {
                    b.Delete = true;
                    b.Edit = true;
                    b.SendApprove = true;
                }

                if (s == Constants.StatusPDX.TL) { b.Edit = true; b.Delete = true; }
                if (s == Constants.StatusPDX.DX) b.Restore = true;
                if (s == Constants.StatusPDX.TC) { b.Edit = true; b.Delete = true; b.SendApprove = true; }
            }

            if (UserIDLogin == PDX.UserIDHandling)
            {
                if (s == Constants.StatusPDX.CD)
                {
                    b.SendApprove = true;
                    if (IsRoleApprove) b.Approved = true;
                }
            }

            b.ViewHistory = true;
            return "";
        }
        [HttpPost]
        public Result GetListAdvancedSearch([FromBody] JObject data)
        {
            try
            {
                if (!ResultCheckToken.isOk) return ResultCheckToken;

                string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_IsVisitPage);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                msg = DoGetListAdvancedSearch(UserToken.UserID, data, out int total, out List<ProposalFormSearchResult> lt);
                if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

                return new { Data = lt, Total = total }.ToResultOk();
            }
            catch (Exception ex)
            {
                return Log.ProcessError(ex.ToString()).ToResultError();
            }
        }
        private string DoGetListAdvancedSearch(int UserID, [FromBody] JObject data, out int Total, out List<ProposalFormSearchResult> lt)
        {
            lt = null;
            Total = 0;

            string msg = data.ToObject("ProposalFormSearch", out ProposalFormSearch formSearch);
            if (msg.Length > 0) return msg.ToMessageForUser();

            msg = DoGetList(formSearch, out lt, out Total);
            return msg;
        }
        [HttpPost]
        public Result Delete([FromBody] JObject data)//Xóa phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPDX.DX);
        }

        [HttpPost]
        public Result Restore([FromBody] JObject data)//Phục hồi phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_CRUD);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPDX.MT);
        }
        [HttpPost]
        public Result Approve([FromBody] JObject data)//Duyệt phiếu
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_DUYET);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPDX.DD);
        }
        [HttpPost]
        public Result UnApprove([FromBody] JObject data)//Không duyệt phiếu
        {
            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_DUYET);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return UpdateStatusID(data, Constants.StatusPDX.TC);
        }
        public Result UpdateStatusID([FromBody] JObject data, int StatusID)
        {
            string msg = UpdateStatusID(data, StatusID, UserToken.UserID);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return "".ToResultOk();
        }
        private string UpdateStatusID([FromBody] JObject data, int StatusID, int UserID)
        {
            string logContent = "";
            string ProposalFormReasonRefuse = "";
            string msg = data.ToGuid("ObjectGuid", out Guid ObjectGuid);
            if (msg.Length > 0) return msg;
            if (StatusID == Constants.StatusPDX.TC)
            {
                msg = data.ToString("ProposalFormReasonRefuse", out ProposalFormReasonRefuse);
                if (msg.Length > 0) return msg;
                if (string.IsNullOrEmpty(ProposalFormReasonRefuse)) return ("Bạn cần nhập lý do từ chối").ToMessageForUser();
                if (ProposalFormReasonRefuse.Length < 20) return ("Bạn cần nhập nội dung lý do không duyệt tối thiểu 20 ký tự trở lên").ToMessageForUser();
            }

            msg = CacheObject.GetProposalFormbyGUID(ObjectGuid, out long ProposalFormID);
            if (msg.Length > 0) return msg;

            msg = ProposalForm.GetOne(ProposalFormID, out ProposalForm proposalForm);
            if (msg.Length > 0) return msg;
            if (proposalForm == null) return ("Không tồn tại tài sản ID = " + ProposalFormID).ToMessageForUser();

            if (StatusID == Constants.StatusPDX.MT)
            {
                if (proposalForm.ProposalFormStatusID != Constants.StatusPDX.DX) return "Bạn chỉ được Khôi phục khi phiếu ở trạng thái Đã xóa".ToMessageForUser();
                logContent = $"Khôi phục Phiếu đề xuất {proposalForm.ProposalFormCode} thành công!";
            }
            if (StatusID == Constants.StatusPDX.DX)
            {
                if (proposalForm.ProposalFormStatusID != Constants.StatusPDX.MT && proposalForm.ProposalFormStatusID != Constants.StatusPDX.TC) return "Bạn chỉ được xóa khi phiếu ở trạng thái tạo mới hoặc từ chối".ToMessageForUser();
                logContent = $"Xóa thông tin Phiếu đề xuất {proposalForm.ProposalFormCode} thành công!";
            }
            if (StatusID == Constants.StatusPDX.CD)
            {
                if (proposalForm.ProposalFormStatusID != Constants.StatusPDX.MT) return "Bạn chỉ được gửi duyệt phiếu đề xuất khi phiếu đã ở trạng thái mới tạo".ToMessageForUser();
                logContent = "Gửi duyệt phiếu đề xuất";
            }
            if (StatusID == Constants.StatusPDX.DD)
            {
                if (proposalForm.ProposalFormStatusID != Constants.StatusPDX.CD) return "Bạn chỉ được duyệt phiếu đề xuất khi phiếu đã ở trạng thái chờ duyệt".ToMessageForUser();
                logContent = $"Duyệt Phiếu đề xuất {proposalForm.ProposalFormCode} thành công!";
            }
            if (StatusID == Constants.StatusPDX.TC)
            {
                if (proposalForm.ProposalFormStatusID != Constants.StatusPDX.CD) return "Bạn chỉ được từ chối duyệt phiếu đề xuất khi phiếu đã ở trạng thái chờ duyệt".ToMessageForUser();
                logContent = $"Từ chối duyệt PĐX {proposalForm.ProposalFormCode} thành công";
            }
            msg = UpdateStatusID_SaveToDB(proposalForm, StatusID, UserID, logContent, ProposalFormReasonRefuse);
            if (msg.Length > 0) { return msg.ToMessageForUser(); }

            return msg;
        }
        private string UpdateStatusID_SaveToDB(ProposalForm ProposalForm, int StatusID, int UserID, string logContent, string ProposalFormReasonRefuse)
        {
            string msg = "";
            if (StatusID == Constants.StatusPDX.TC)
            {
                msg = ProposalForm.UpdateStatusID(new DBM(), ProposalForm.ProposalFormID, StatusID, ProposalFormReasonRefuse);
                if (msg.Length > 0) return msg;
            }
            else
            {
                msg = ProposalForm.UpdateStatusID(new DBM(), ProposalForm.ProposalFormID, StatusID);
                if (msg.Length > 0) return msg;
            }

            msg = Log.WriteHistoryLog(logContent, ProposalForm.ObjectGuid, UserID, Common.GetClientIpAddress(Request));
            return msg;
        }

        [HttpGet]
        public Result ViewDetail(Guid ObjectGuid)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = Role.Check(UserToken.UserID, Constants.TabID.QLPDX, Role.ROLE_QLPDX_IsVisitPage);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            msg = DoViewDetail(ObjectGuid, out ProposalFormViewDetail o);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return o.ToResultOk();
        }
        public string DoViewDetail(Guid ObjectGuid, out ProposalFormViewDetail proposalOut)
        {
            proposalOut = null;

            string msg = CacheObject.GetProposalFormbyGUID(ObjectGuid, out long ProposalFormID);
            if (msg.Length > 0) return msg;

            msg = ProposalFormViewDetail.ViewDetail(ProposalFormID, out proposalOut);
            if (msg.Length > 0) return msg;
            if (proposalOut == null) return ("Phiếu đề xuất không tồn tại ID " + ProposalFormID).ToMessageForUser();

            msg = TransferHandlingLog.GetByObjectID(proposalOut.ProposalFormID, Constants.TransferHandling.PDX, out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;
            proposalOut.TransferDirectionID = transferHandlingLog != null ? transferHandlingLog.TransferDirectionID : "";
            proposalOut.CommentHandling = transferHandlingLog != null ? transferHandlingLog.Comment : "";

            msg = ProposalFormDetail.GetListByProposalFormID(proposalOut.ProposalFormID, out List<ProposalFormDetail> ltform);
            if (msg.Length > 0) return msg;

            proposalOut.ltProposalFormDetail = ltform;

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

            msg = data.ToString("UserIDHandling", out string sUserIDHandling);
            if (msg.Length > 0) return msg;

            msg = data.ToString("Commenthandling", out string Commenthandling);
            if (msg.Length > 0) return msg;

            msg = data.ToString("TransferDirectionID", out string TransferDirectionID);
            if (msg.Length > 0) return msg;

            long UserIDHandling = sUserIDHandling.ToLong(0);

            msg = CacheObject.GetProposalFormbyGUID(ObjectGuid, out long ProposalFormID);
            if (msg.Length > 0) return msg;
            if (ProposalFormID == 0) return ("Không có giá trị phù hợp với ObjectGuid=" + ObjectGuid).ToMessageForUser();

            msg = ProposalForm.GetOne(ProposalFormID, out ProposalForm proposalFormDB);
            if (msg.Length > 0) return msg;

            if (UserIDHandling == UserToken.UserID) return ("Bạn không được phép chuyển Phiếu đề xuất cho chính mình").ToMessageForUser();

            if (proposalFormDB.UserIDCreate == UserIDHandling)
            {
                msg = ProposalForm.UpdateStatusID(new DBM(), ProposalFormID, Constants.StatusPDX.TL, "");
                if (msg.Length > 0) return msg;
            }
            else
            {
                msg = ProposalForm.UpdateTransferHanding(ProposalFormID, UserIDHandling);
                if (msg.Length > 0) return msg;
            }

            msg = Log.WriteHistoryLog(new DBM(), $"Chuyển xử lý Phiếu đề xuất cho người xử lý: {AccountUser.GetUserNameByUserID(UserIDHandling)} thành công. Nội dung ý kiến xử lý - {Commenthandling}", ObjectGuid, UserToken.UserID);
            if (msg.Length > 0) return msg;

            TransferHandlingLog logt = new TransferHandlingLog
            {
                ObjectID = ProposalFormID,
                ObjectTypeID = Constants.TransferHandling.PDX,
                UserIDHandling = UserIDHandling,
                Comment = Commenthandling,
                TransferDirectionID = TransferDirectionID
            };
            msg = logt.InsertUpdate(new DBM(), out TransferHandlingLog transferHandlingLog);
            if (msg.Length > 0) return msg;

            return msg;
        }
        [HttpPost]
        public Result DeleteAssetTypeDetail([FromBody] JObject data)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;

            string msg = DoDeleteAssetTypeDetail(data);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();
            return Result.GetResultOk();
        }
        private string DoDeleteAssetTypeDetail([FromBody] JObject data)
        {
            string msg = data.ToString("ID", out string sID);
            if (msg.Length > 0) return msg;

            long ID = sID.ToLong(0);

            msg = ProposalFormDetail.GetOneByID(ID, out ProposalFormDetail formDetail);
            if (msg.Length > 0) return msg;
            if (formDetail == null) return ("Không có loại tài sản ID = " + ID).ToMessageForUser();

            msg = ProposalFormDetail.Delete(formDetail.ID);
            if (msg.Length > 0) return msg;

            return msg;
        }
        [HttpGet]
        public Result GetListProposalByUserIDCreate(long UserIDCreate)
        {
            if (!ResultCheckToken.isOk) return ResultCheckToken;
            string msg = DoGetListProposalByUserIDCreate(UserIDCreate, out List<ProposalFormViewDetail> lt);
            if (msg.Length > 0) return Log.ProcessError(msg).ToResultError();

            return lt.ToResultOk();
        }
        public string DoGetListProposalByUserIDCreate(long UserIDCreate, out List<ProposalFormViewDetail> lt)
        {
            string msg = ProposalFormViewDetail.GetListViewDetailByUserIDCreate(UserIDCreate, out lt);
            if (msg.Length > 0) return msg;

            /*foreach (var item in lt)
             {
                 msg = ProposalFormDetail.GetListByProposalFormID(item.ProposalFormID, out List<ProposalFormDetail> ltform);
                 if (msg.Length > 0) return msg;

                 item.ltProposalFormDetail = ltform;
             }*/

            return msg;
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

            string msg = CacheObject.GetProposalFormbyGUID(ObjectGuid, out long itemProposalFormID);
            if (msg.Length > 0) return msg;

            msg = ProposalFormExportWord.GetOne(itemProposalFormID, out ProposalFormExportWord itemProposalFormExportWord);
            if (msg.Length > 0) return msg;

            LtProposalFormExportWord ltItemProposalFormExportWord = new LtProposalFormExportWord();
            if (msg.Length > 0) return msg;

            ltItemProposalFormExportWord.SetLtItemProposalFormExportWord(itemProposalFormID);

            string nameFileDoc = $"PhieuDeXuatTaiSan_{DateTime.Now.ToString("yyyyMMddHH")}.docx";
            string nameFilePDF = $"PhieuDeXuatTaiSan_{DateTime.Now.ToString("yyyyMMddHH")}.pdf";

            msg = BSS.Common.GetSetting("FolderFileExportPDXTS", out string FolderFileExportPNKTS);
            if (msg.Length > 0) return msg;

            var InfoFileWord = UtilitiesFile.GetInfoFile(DateTime.Now, nameFileDoc, FolderFileExportPNKTS, true);
            var InfoFilePDF = UtilitiesFile.GetInfoFile(DateTime.Now, nameFilePDF, FolderFileExportPNKTS, true);

            msg = WordDocument.FillTemplate(HttpContext.Current.Server.MapPath(@"\File\FileTemplate\TemplateInPDXTS.dotx"), InfoFileWord.FilePathPhysical, InfoFilePDF.FilePathPhysical, itemProposalFormExportWord, ltItemProposalFormExportWord);
            if (msg.Length > 0) return msg;

            UrlFile = InfoFilePDF.FilePathVirtual;

            return string.Empty;
        }
    }
}