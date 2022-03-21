using BSS;
using System.Collections.Generic;

namespace ASM_API.App_Start.InventoryStore
{
    public class InventoryStoreStatus
    {
        public int InventoryStoreStatusID { get; set; }

        public string InventoryStoreStatusName { get; set; }

        public static string GetList(out List<InventoryStoreStatus> lt)
        {
            return DBM.GetList("usp_InventoryStoreStatus_GetList", out lt);
        }
    }
}