using BSS;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ASM_API.App_Start.StoreManagement
{
    public class StoreManagementStatus
    {
        public int StoreManagementStatusID { get; set; }
        public string StoreManagementStatusName { get; set; }
        [JsonIgnore]
        public int Sort { get; set; }
        [JsonIgnore]
        public bool IsActive { get; set; }

        public static string GetList(out List<StoreManagementStatus> lt)
        {
            return DBM.GetList("usp_StoreManagementStatus_GetList", out lt);
        }

    }
}