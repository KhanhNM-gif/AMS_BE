using ASM_API.App_Start.AssetInventory;
using ASM_API.App_Start.FileReport;
using BSS;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Generic;
using System.Linq;

public class FileReportInventoryExcel
{
    public static string CreateFile(List<AssetInventory> assetInventories, int AccountID, string pathFile)
    {
        string msg = "";
        using (ExcelPackage pack = new ExcelPackage())
        {
            foreach (var assetInventory in assetInventories)
            {
                if (string.IsNullOrEmpty(assetInventory.DeptCode)) return ("Mã đơn vị không tồn tại").ToMessageForUser();

                msg = AccountDept.GetOneByDeptID(assetInventory.DeptID, AccountID, out AccountDept deptDB);
                if (msg.Length > 0) return msg;

                ExcelWorksheet ws = pack.Workbook.Worksheets.Add(string.IsNullOrEmpty(assetInventory.DeptCode) ? deptDB.DeptName : assetInventory.DeptCode);
                ws.Cells.Style.Font.Name = "Times New Roman";
                int fontSize = 11;
                ws.Cells.Style.Font.Size = fontSize;

                CreateContentHeader(ws, 1, 1, 1, 2, assetInventory.AccountName.ToUpper(), ExcelHorizontalAlignment.Center, fontSize);

                CreateContentHeader(ws, 1, 3, 1, 15, assetInventory.InventoryName.ToUpper(), ExcelHorizontalAlignment.Center, 16);

                CreateContentHeader(ws, 2, 1, 5, 2, "", ExcelHorizontalAlignment.Center, fontSize);

                CreateContentHeader(ws, 2, 3, 2, 3, "Ngày kiểm kê", ExcelHorizontalAlignment.Left, fontSize);

                string dateTime = assetInventory.BeginDate.ToString("dd/MM/yyyy") + " - " + assetInventory.EndDate.ToString("dd/MM/yyyy");
                CreateContentHeader(ws, 2, 4, 2, 15, dateTime, ExcelHorizontalAlignment.Left, fontSize, false);

                CreateContentHeader(ws, 3, 3, 3, 3, "Gồm có", ExcelHorizontalAlignment.Left, fontSize);

                CreateContentHeader(ws, 3, 4, 3, 15, "", ExcelHorizontalAlignment.Left, fontSize);
                CreateContentHeader(ws, 4, 4, 4, 15, "", ExcelHorizontalAlignment.Left, fontSize);
                CreateContentHeader(ws, 5, 4, 5, 15, "", ExcelHorizontalAlignment.Left, fontSize);

                int rowStartTable = 7;
                msg = CreateColumnsHeader(ws);
                if (msg.Length > 0) return msg;

                int rowIndex = 0; int startRow = 8;
                var groupAssetTypeName = assetInventory.AssetInventoryDetails.GroupBy(x => x.AssetTypeName);

                foreach (var groups in groupAssetTypeName)
                {
                    rowIndex++;
                    startRow++;

                    AssetInventoryDetail assetInventoryDetail = new AssetInventoryDetail();
                    assetInventoryDetail.AssetTypeName = groups.Key;
                    assetInventoryDetail.AssetTypeTotal = groups.Count();

                    msg = EachCellExport(assetInventoryDetail, rowIndex, startRow, ws);
                    if (msg.Length > 0) return msg;
                }

                ExcelRange excelRangeTable = ws.Cells[rowStartTable, 1, startRow, 15];
                Border Border = excelRangeTable.Style.Border;
                SetBorder(new ExcelBorderItem[] { Border.Top, Border.Right, Border.Bottom, Border.Left });

                int noteRow = startRow + 4;
                CreateContentHeader(ws, noteRow, 1, noteRow, 1, "Ghi chú", ExcelHorizontalAlignment.Left, fontSize);

                pack.SaveAs(new System.IO.FileInfo(pathFile));

                break;
            }
        }

        return "";
    }
    private static void CreateContentHeader(ExcelWorksheet ws, int fromRow, int fromCol, int toRow, int toCol, string value, ExcelHorizontalAlignment alignment, int fontSize, bool fontBold = true)
    {
        ExcelRange excelRangeTitle = ws.Cells[fromRow, fromCol, toRow, toCol];
        excelRangeTitle.Merge = true;
        excelRangeTitle.Style.HorizontalAlignment = alignment;
        excelRangeTitle.Value = value;
        excelRangeTitle.Style.Font.Bold = fontBold;
        excelRangeTitle.Style.Font.Size = fontSize;

        ws.Column(3).Width = 20;
        ws.Column(6).Width = 8;
        ws.Column(8).Width = 9;
    }
    private static void BindingCellValue(ExcelWorksheet ws, int fromRow, int fromCol, int toRow, int toCol, string value, ExcelHorizontalAlignment alignment, int fontSize = 11)
    {
        bool fontBold = false;
        CreateContentHeader(ws, fromRow, fromCol, toRow, toCol, value, alignment, fontSize, fontBold);
    }

    private static string CreateColumnsHeader(ExcelWorksheet ws)
    {
        ExcelRangeBase cell = ws.Cells;
        int fromRow = 7; int toRow = 8;
        int fontSize = 12;
        for (int i = 0; i < FileExportAssetInventory.COLUMNS.Length; i++)
        {
            switch (FileExportAssetInventory.COLUMNS[i].ID)
            {
                case FileExportAssetInventory.STT:
                    cell = MergeCell(ws, fromRow, 1, toRow, 1);
                    SetStyleCell(cell, FileExportAssetInventory.COLUMNS[i].Name, ws, i, FileExportAssetInventory.COLUMNS[i].WidthExcel, ExcelHorizontalAlignment.Center, fontSize);
                    break;
                case FileExportAssetInventory.AssetName:
                    cell = MergeCell(ws, fromRow, 2, toRow, 4);
                    break;
                case FileExportAssetInventory.DVT:
                    cell = MergeCell(ws, fromRow, 5, toRow, 5);
                    SetStyleCell(cell, FileExportAssetInventory.COLUMNS[i].Name, ws, i, FileExportAssetInventory.COLUMNS[i].WidthExcel, ExcelHorizontalAlignment.Center, fontSize);
                    break;
                case FileExportAssetInventory.NumberOfBooks:
                    cell = MergeCell(ws, fromRow, 6, toRow, 6);
                    break;
                case FileExportAssetInventory.NumberActual:
                    cell = MergeCell(ws, fromRow, 7, toRow, 7);
                    break;
                case FileExportAssetInventory.ChenhLech:
                    cell = MergeCell(ws, fromRow, 8, 7, 9);
                    SetStyleCell(cell, FileExportAssetInventory.COLUMNS[i].Name, ws, i, FileExportAssetInventory.COLUMNS[i].WidthExcel, ExcelHorizontalAlignment.Center, fontSize);

                    cell = MergeCell(ws, 8, 8, 8, 8);
                    SetStyleCell(cell, "Thừa", ws, i, 5, ExcelHorizontalAlignment.Center, fontSize, false);

                    cell = MergeCell(ws, 8, 9, 8, 9);
                    SetStyleCell(cell, "Thiếu", ws, i, 5, ExcelHorizontalAlignment.Center, fontSize, false);
                    continue;
                case FileExportAssetInventory.PCCL:
                    cell = MergeCell(ws, fromRow, 10, 7, 12);
                    SetStyleCell(cell, FileExportAssetInventory.COLUMNS[i].Name, ws, i, FileExportAssetInventory.COLUMNS[i].WidthExcel, ExcelHorizontalAlignment.Center, fontSize);

                    cell = MergeCell(ws, 8, 10, toRow, 10);
                    SetStyleCell(cell, "A", ws, i, 0, ExcelHorizontalAlignment.Center, fontSize, false);

                    cell = MergeCell(ws, 8, 11, toRow, 11);
                    SetStyleCell(cell, "B", ws, i, 0, ExcelHorizontalAlignment.Center, fontSize, false);

                    cell = MergeCell(ws, 8, 12, toRow, 12);
                    SetStyleCell(cell, "C", ws, i, 0, ExcelHorizontalAlignment.Center, fontSize, false);
                    continue;
                case FileExportAssetInventory.Description:
                    cell = MergeCell(ws, fromRow, 13, toRow, 15);
                    break;
                default:
                    return "Không tồn tại cột: " + FileExportAssetInventory.COLUMNS[i].ID;
            }

            SetStyleCell(cell, FileExportAssetInventory.COLUMNS[i].Name, ws, i, FileExportAssetInventory.COLUMNS[i].WidthExcel, ExcelHorizontalAlignment.Center, fontSize);
        }
        return "";
    }
    private static ExcelRangeBase MergeCell(ExcelWorksheet ws, int fromRow, int fromCol, int toRow, int toCol)
    {
        return ws.Cells[fromRow, fromCol, toRow, toCol];
    }
    private static string EachCellExport(AssetInventoryDetail assetInventoryDetail, int rowIndex, int row, ExcelWorksheet ws)
    {
        string msg = "";
        foreach (var item in FileExportAssetInventory.COLUMNS)
        {
            FileReportCell cell = new FileReportCell(rowIndex, item.ID);
            msg = FileExportAssetInventory.GetColumnValue(rowIndex, cell.ColumnID, assetInventoryDetail, out string columnValue);
            if (msg.Length > 0) return msg;

            switch (item.ID)
            {
                case FileExportAssetInventory.STT:
                    BindingCellValue(ws, row, 1, row, 1, columnValue, ExcelHorizontalAlignment.Center);
                    break;
                case FileExportAssetInventory.AssetName:
                    BindingCellValue(ws, row, 2, row, 4, columnValue, ExcelHorizontalAlignment.Left);
                    break;
                case FileExportAssetInventory.DVT:
                    BindingCellValue(ws, row, 5, row, 5, columnValue, ExcelHorizontalAlignment.Center);
                    break;
                case FileExportAssetInventory.NumberOfBooks:
                    BindingCellValue(ws, row, 6, row, 6, columnValue, ExcelHorizontalAlignment.Center);
                    break;
                case FileExportAssetInventory.NumberActual:
                    BindingCellValue(ws, row, 7, row, 7, columnValue, ExcelHorizontalAlignment.Center);
                    break;
                case FileExportAssetInventory.ChenhLech:
                    BindingCellValue(ws, row, 8, row, 8, columnValue, ExcelHorizontalAlignment.Center);
                    break;
                case FileExportAssetInventory.PCCL:
                    BindingCellValue(ws, row, 10, row, 10, columnValue, ExcelHorizontalAlignment.Center);
                    break;
                case FileExportAssetInventory.Description:
                    BindingCellValue(ws, row, 13, row, 13, columnValue, ExcelHorizontalAlignment.Center);
                    break;
                default:
                    return "Không tồn tại cột: " + item.ID;
            }
        }
        return msg;

    }
    private static void SetStyleCell(ExcelRangeBase cell, string cellValue, ExcelWorksheet ws, int i, int widthCell, ExcelHorizontalAlignment alignment, int fontSize, bool mergeCell = true)
    {
        cell.Merge = mergeCell;
        cell.Value = cellValue;
        cell.Style.Font.Bold = true;
        cell.Style.Font.Size = fontSize;
        cell.Style.HorizontalAlignment = alignment;
        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

        if (widthCell > 0) ws.Column(i + 1).Width = widthCell;
        ws.Column(i + 1).Style.WrapText = true;
    }
    private static void SetBorder(ExcelBorderItem[] arr)
    {
        foreach (var item in arr)
        {
            item.Style = ExcelBorderStyle.Thin;
            item.Color.SetColor(System.Drawing.Color.Black);
        }
    }
}