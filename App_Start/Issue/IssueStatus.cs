using BSS;
using System.Collections.Generic;

namespace ASM_API.App_Start.Issue
{
    public class IssueStatus : IMappingSingleField
    {
        public int IssueStatusID { get; set; }
        public string IssueStatusName { get; set; }
        public string IssueStatusShort { get; set; }

        public static string GetStatusList(out List<IssueStatus> issueStatuses)
        {
            return DBM.GetList("usp_IssueStatus_GetIssueStatusList", new { }, out issueStatuses);
        }
        public string GetDifferences(object obj_new, object obj_old, out string strChange)
        {
            string msg = MappingSingleField.GetDifferences(this, obj_new, obj_old, out strChange);
            if (msg.Length > 0) return msg;

            return string.Empty;
        }

        public string GetName() => IssueStatusName;

        public string GetOne(object k, out IMappingSingleField outModel)
        {
            outModel = null;

            string msg = DBM.GetOne("usp_IssueStatus_GetOne", new { IssueStatusID = (int)k }, out IssueStatus issueStatuse);
            if (msg.Length > 0) return msg;
            outModel = issueStatuse;

            return msg;

        }
    }
}