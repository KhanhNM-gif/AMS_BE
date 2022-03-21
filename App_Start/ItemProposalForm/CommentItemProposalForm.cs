using System;

namespace ASM_API.App_Start.ItemProposalForm
{
    public class CommentItemProposalForm
    {
        public CommentItemProposalForm()
        {
        }
        public CommentItemProposalForm(TransferHandlingLogView transferHandlingLog)
        {
            ItemProposalFormID = transferHandlingLog.ObjectID;
            Content = transferHandlingLog.Comment;
            UserCreateID = transferHandlingLog.UserIDHandling;
            CreateDate = transferHandlingLog.TransferDate;
            UserCreateDetail = transferHandlingLog.UserHandlingDetail;
        }

        public long ItemProposalFormID { get; set; }
        public string Content { get; set; }
        public long UserCreateID { get; set; }
        public string UserCreateDetail { get; set; }
        public DateTime? CreateDate { get; set; }

        /*        public string Insert(DBM dbm)
                {
                    string msg = dbm.SetStoreNameAndParams("usp_CommentItemProposalForm_Insert",
                      new
                      {
                          ItemProposalFormID,
                          Content,
                          UserCreateID
                      });
                    if (msg.Length > 0) return msg;

                    return dbm.ExecStore();
                }

                public static string GetListByItemProposalFormID(long ItemProposalFormID, out List<CommentItemProposalForm> ltCommentItemProposalForm)
                {
                    return DBM.GetList("usp_usp_CommentItemProposalForm_GetListByItemProposalFormID", new { ItemProposalFormID }, out ltCommentItemProposalForm);
                }*/
    }
}
