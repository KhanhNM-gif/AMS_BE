using BSS;
using System.Collections.Generic;

namespace ASM_API.App_Start.Store
{
    public class ItemExportReceiptType
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Sort { get; set; }

        public static string GetList(out List<ItemExportReceiptType> lt)
        {
            return DBM.GetList("usp_StoreCategoryOut_GetList", out lt);
        }
    }
}