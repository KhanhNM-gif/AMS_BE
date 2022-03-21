using ASM_API.App_Start.FileReport;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

public class FileReport
{
    public const string TYPE_PDF = "PDF";
    public const string TYPE_WORD = "WORD";
    public const string TYPE_EXCEL = "EXCEL";

    public static string TITLE = "BÁO CÁO CHI TIẾT NHIỆM VỤ";
    public static string ASSET_HANDOVER_TITLE = "BIÊN BẢN BÀN GIAO";

    public const string STT = "STT", AssetTypeName = "AssetTypeName", AssetCode = "AssetCode", SerialModel = "AssetSerial", PlaceFullName = "PlaceFullName",
        AssetDescription = "AssetDescription", Amount = "Amount";

    public static FileReportColumn[] COLUMNS = new FileReportColumn[] { 
         new  FileReportColumn("STT","STT", 5, 5f, 3, FileReportColumn.ALIGN_CENTER, FileReportCell.TYPEGETVALUE_INDEX), 
         new  FileReportColumn("MissionGroupName","Nhóm nhiệm vụ", 20, 14f, 12), 
         new  FileReportColumn("Delivery","Người giao", 22, 12f, 15), 
         new  FileReportColumn("Perform","Người chủ trì xử lý", 22, 12f, 15), 
         new  FileReportColumn("BeginDate", "Ngày bắt đầu", 12, 8f, 7, FileReportColumn.ALIGN_CENTER, FileReportCell.TYPEGETVALUE_FORMATDATE), 
         new  FileReportColumn("EndDate", "Ngày kết thúc", 12, 8f, 7, FileReportColumn.ALIGN_CENTER, FileReportCell.TYPEGETVALUE_FORMATDATE), 
         new  FileReportColumn("MissionContent", "Nội dung", 50, 28f, 30), 
         new  FileReportColumn("FinishDate", "Ngày hoàn thành", 12, 8f, 7, FileReportColumn.ALIGN_CENTER, FileReportCell.TYPEGETVALUE_FORMATDATE), 
         new  FileReportColumn("MissionStatusName", "Trạng thái", 15, 10f, 10) };

    public static FileReportColumn[] ASSET_HANDOVER_COLUMNS = new FileReportColumn[] {
         new  FileReportColumn(STT,STT, 5, 3f, 3, FileReportColumn.ALIGN_CENTER, FileReportCell.TYPEGETVALUE_INDEX),
         new  FileReportColumn(AssetTypeName,"Loại Tài sản", 20, 14f, 12),
         new  FileReportColumn(AssetCode,"Mã Tài sản", 22, 12f, 15),
         new  FileReportColumn(SerialModel,"Serial/Model", 22, 12f, 15),
         new  FileReportColumn(PlaceFullName, "Nơi để", 12, 8f, 7) };

    public static string GetColumnValue(int rowIndex, string column, AssetHandoverDetail assetHandoverDetail, out string columnValue)
    {
        columnValue = "";
        switch (column)
        {
            case STT:
                columnValue = rowIndex.ToString();
                break;
            case AssetTypeName:
                columnValue = assetHandoverDetail.AssetTypeName;
                break;
            case AssetCode:
                columnValue = assetHandoverDetail.AssetCode;
                break;
            case SerialModel:
                StringBuilder sb = new StringBuilder();
                sb.Append(!string.IsNullOrEmpty(assetHandoverDetail.AssetSerial) ? assetHandoverDetail.AssetSerial : "").Append("/").AppendLine(!string.IsNullOrEmpty(assetHandoverDetail.AssetModel) ? assetHandoverDetail.AssetModel : "");
                columnValue = sb.ToString();
                break;
            case PlaceFullName:
                columnValue = assetHandoverDetail.PlaceFullName;
                break;
            default:
                return "Chưa định nghĩa cột: " + column;
        }

        return "";
    }

    public static void GetAllDeptName(string DeptFullName, out List<string> deptNameLevel1List, out List<string> deptNameLevel2List)
    {
        deptNameLevel1List = new List<string>();
        deptNameLevel2List = new List<string>();

        string[] DeptName = !string.IsNullOrEmpty(DeptFullName) ? DeptFullName.Split(',') : DeptName = new string[0];

        foreach (string str in DeptName)
        {
            string[] getAllDeptLevel1 = str.Trim().Split('\\');
            string temDeptLevel1 = getAllDeptLevel1.Length > 0 ? getAllDeptLevel1[0] : "";
            string temDeptLevel2 = getAllDeptLevel1.Length > 1 ? getAllDeptLevel1[1] : "";

            deptNameLevel1List.Add(temDeptLevel1);
            deptNameLevel2List.Add(temDeptLevel2);
        }
    }
    public static void GetCurrentDateVietNamese(out string vietnamDay)
    {
        vietnamDay = MessageConstants.DAY + DateTime.Now.Day;
        vietnamDay += MessageConstants.MONTH + DateTime.Now.Month;
        vietnamDay += MessageConstants.YEAR + DateTime.Now.Year;
    }
}