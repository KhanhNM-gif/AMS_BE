using BSS;
using System.Collections.Generic;

namespace ASM_API.App_Start.AssetInventory
{
    public class AssetInventoryStatus
    {
        public int InventoryStatusID { get; set; }
        public string InventoryStatusName { get; set; }
        public static string GetAllStatusName(out List<AssetInventoryStatus> assetInventoryStatus)
        {
            return DBM.GetList("usp_AssetInventoryStatus_GetAll", new { }, out assetInventoryStatus);
        }
    }
}