using BSS;
using System.Collections.Generic;

namespace ASM_API.App_Start.AssetInventory
{
    public class AssetInventoryState
    {
        public int StateID { get; set; }
        public string StateName { get; set; }
        public bool IsActive { get; set; }

        public static string GetAllStateName(out List<AssetInventoryState> assetInventoriesState)
        {
            return DBM.GetList("usp_AssetInventoryState_GetAll", new { }, out assetInventoriesState);
        }
    }
}