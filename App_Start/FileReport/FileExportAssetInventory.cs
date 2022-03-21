using ASM_API.App_Start.AssetInventory;

namespace ASM_API.App_Start.FileReport
{
    public class FileExportAssetInventory
    {
        public const string TYPE_PDF = "PDF";
        public const string TYPE_WORD = "WORD";
        public const string TYPE_EXCEL = "EXCEL";

        public const string STT = "STT", AssetName = "AssetName", DVT = "DVT",
        NumberOfBooks = "NumberOfBooks", NumberActual = "NumberActual",
        ChenhLech = "ChenhLech", PCCL = "PCCL", Description = "Description";

        public static FileReportColumn[] COLUMNS = new FileReportColumn[] {
         new  FileReportColumn(STT, STT, 8, 5f, 3, FileReportColumn.ALIGN_CENTER, FileReportCell.TYPEGETVALUE_INDEX),
         new  FileReportColumn(AssetName,"Tên trang bị kỹ thuật - VT - TS", 25, 14f, 12),
         new  FileReportColumn(DVT,"ĐVT", 5, 12f, 15),
         new  FileReportColumn(NumberOfBooks,"SL sổ sách", 8, 12f, 15),
         new  FileReportColumn(NumberActual, "SLKK thực tế", 15, 8f, 7),
         new  FileReportColumn(ChenhLech, "Chênh lệch", 5, 8f, 7),
         new  FileReportColumn(PCCL, "Phân cấp chất lượng", 10, 28f, 30),
         new  FileReportColumn(Description, "Thuyết minh thừa, thiếu", 20, 10f, 10) };

        public static string GetColumnValue(int rowIndex, string column, AssetInventoryDetail assetInventoryDetail, out string columnValue)
        {
            columnValue = "";
            switch (column)
            {
                case STT:
                    columnValue = rowIndex.ToString();
                    break;
                case AssetName:
                    columnValue = assetInventoryDetail.AssetTypeName;
                    break;
                case DVT:
                    columnValue = "Cái";
                    break;
                case NumberOfBooks:
                    columnValue = assetInventoryDetail.AssetTypeTotal.ToString();
                    break;
                case NumberActual:
                    columnValue = "";
                    break;
                case ChenhLech:
                    columnValue = "";
                    break;
                case PCCL:
                    columnValue = "";
                    break;
                case Description:
                    columnValue = "";
                    break;
                default:
                    return "Chưa định nghĩa cột: " + column;
            }

            return "";
        }
    }
}