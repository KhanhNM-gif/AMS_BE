using BSS;
using System.Collections.Generic;

namespace ASM_API.App_Start.Store
{
    public class NumberItemStore
    {
        public int PlaceID { get; set; }
        public long ItemID { get; set; }
        public int Quality { get; set; }

        public static string GetListByPlaceID(int PlaceID, out List<NumberItemStore> lt)
        {
            return DBM.GetList("usp_NumberItemStore_GetListByPlaceID", new { PlaceID }, out lt);
        }

    }
}