namespace ASM_API.App_Start.ItemImportReceipt
{
    public class ItemImportReceiptEasySearch
    {
        public int ObjectCategory { get; set; }
        public string ObjectID { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public string TextSearch { get; set; }
        public ItemImportReceiptEasySearch()
        {
            ObjectCategory = 0;
            ObjectID = "";
            PageSize = 50;
            CurrentPage = 1;
            TextSearch = "";
        }
    }
}