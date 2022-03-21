using DocumentFormat.OpenXml.Office2010.ExcelAc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASM_API.App_Start.FileReport
{
    public class HandoverAsset
    {
        public const string TYPE_PDF = "PDF";
        public const int FONT_SIZE_PDF = 12;
        public const int FONT_SIZE_TABLE_PDF = 10;

        public const string STT = "STT", AssetCode = "AssetCode", SerialModel = "AssetModel", PlaceFullName = "PlaceFullName",
            AssetDescription = "AssetDescription", Amount = "Amount";

        public static FileReportColumn[] ASSET_COLUMNS = new FileReportColumn[] {
             new FileReportColumn(STT, STT, 5, 3f, 3, FileReportColumn.ALIGN_CENTER, FileReportCell.TYPEGETVALUE_INDEX),
             new FileReportColumn(AssetCode, "Mã tài sản", 20, 20f, 20),
             new FileReportColumn(SerialModel, "Serial/Model", 20, 20f, 20),
             new FileReportColumn(PlaceFullName, "Nơi để", 16, 7f, 6, FileReportColumn.ALIGN_RIGHT, FileReportCell.TYPEGETVALUE_INDEX),
             new FileReportColumn(AssetDescription, "Danh mục thiết bị / Cấu hình", 20, 20f, 20),
             new FileReportColumn(Amount, "Số lượng", 16, 7f, 6, FileReportColumn.ALIGN_RIGHT, FileReportCell.TYPEGETVALUE_INDEX) };

    public static string GetColumnValue(int rowIndex, string column, Asset asset, out string columnValue)
        {
            columnValue = "";
            switch (column)
            {
                case STT:
                    columnValue = rowIndex.ToString();
                    break;
                case AssetCode:
                    columnValue = asset.AssetCode;
                    break;
                case SerialModel:
                    StringBuilder sb = new StringBuilder();
                    sb.Append(asset.AssetSerial).Append("/").AppendLine(asset.AssetModel);
                    columnValue = sb.ToString();
                    break;
                case PlaceFullName:
                    columnValue = asset.PlaceFullName;
                    break;
                case AssetDescription:
                    columnValue = asset.AssetDescription;
                    break;
                case Amount:
                    columnValue = "1";
                    break;
                default:
                    return "Chưa định nghĩa cột: " + column;
            }

            return "";
        }

        public static void GetCurrentDateVietNamese(out string vietnamDay)
        {
            vietnamDay = MessageConstants.HN;
            vietnamDay += MessageConstants.DAY + DateTime.Now.Day;
            vietnamDay += MessageConstants.MONTH + DateTime.Now.Month;
            vietnamDay += MessageConstants.YEAR + DateTime.Now.Year;
        }

        public static void GetDeptName(string DeptFullName, out string DeptName)
        {
            string[] AllDeptNameAfterSplited = DeptFullName.Split('\\');
            if (AllDeptNameAfterSplited.Length > 1) DeptName = string.Join(",", AllDeptNameAfterSplited);
            else DeptName = DeptFullName;
        }
    }
}
