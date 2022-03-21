using BSS;
using System.Collections.Generic;

namespace ASM_API.App_Start.ItemImportReceipt
{
    public class ItemImportReceiptType
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Sort { get; set; }

        public static string GetList(out List<ItemImportReceiptType> lt)
        {
            return DBM.GetList("usp_StoreCategoryIn_GetList", out lt);
        }
    }
}