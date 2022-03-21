using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ASM_API.App_Start.FileReport
{
    public class FileExportHandoverAssetPDF
    {
        public static string CreateFile(string pathFile, AssetHandOverExport assetHandOverExport, AssetSenderHandOver assetSenderHandOver)
        {
            try
            {
                byte[] fileContent;
                string msg = GetContent(assetHandOverExport, assetSenderHandOver, out fileContent);
                if (msg.Length > 0) return msg;

                File.WriteAllBytes(pathFile, fileContent);

                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        public static string GetContent(AssetHandOverExport assetHandOverExport, AssetSenderHandOver assetSenderHandOver, out byte[] FileContent)
        {
            string msg = "";

            FileContent = null;

            using (var ms = new MemoryStream())
            {
                Document document = new Document(iTextSharp.text.PageSize.A3, 25, 25, 25, 25);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                msg = CreateContent(document, assetHandOverExport, assetSenderHandOver);
                if (msg.Length > 0) return msg;

                document.Close();

                writer.Close();

                FileContent = ms.ToArray();
            }

            return msg;
        }

        private static string CreateContent(Document document, AssetHandOverExport assetHandOverExport, AssetSenderHandOver assetSenderHandOver)
        {
            string msg = "";
            try
            {
                PdfPTable tableHeader = new PdfPTable(2);
                float[] columnWidths = new float[] { 40f, 60f };
                tableHeader.SetWidths(columnWidths);

                tableHeader.WidthPercentage = 100f;
                tableHeader.HorizontalAlignment = Element.ALIGN_CENTER;
                tableHeader.DefaultCell.Border = iTextSharp.text.Rectangle.NO_BORDER;

                HandoverAsset.GetDeptName(assetHandOverExport.DeptFullName, out string department);
                string tab = "";
                for (int i = 0; i <= 30; i++) tab += " ";

                StringBuilder sb = new StringBuilder();
                sb.Append(assetHandOverExport.Name).Append(tab).Append(tab).Append(MessageConstants.CHXHCNVN);
                document.Add(GetParagraphBold(sb.ToString(), Element.ALIGN_LEFT));

                sb = new StringBuilder();
                sb.Append(department.Trim()).Append(tab).Append(tab).Append(tab).Append(MessageConstants.DLTDHP);
                document.Add(GetParagraphBold(sb.ToString(), Element.ALIGN_LEFT));

                HandoverAsset.GetCurrentDateVietNamese(out string CurrentDateVietNamese);
                document.Add(GetParagraph_Table(CurrentDateVietNamese, Element.ALIGN_RIGHT));

                document.Add(GetParagraph_Bold(MessageConstants.TITLE_FILE, Element.ALIGN_CENTER));

                Paragraph para = new Paragraph();
                para = GetParagraph_Bold(MessageConstants.LOCATION, Element.ALIGN_LEFT);
                para.Add(GetParagraph_Title(assetHandOverExport.Address, Element.ALIGN_LEFT));
                document.Add(para);

                para = new Paragraph();
                para = GetParagraph_Bold(MessageConstants.USERNAME_HANDOVER, Element.ALIGN_LEFT);
                para.Add(GetParagraph_Title(assetHandOverExport.UserName, Element.ALIGN_LEFT));
                document.Add(para);

                para = new Paragraph();
                para = GetParagraph_Bold(MessageConstants.USERNAME_RECEIVER, Element.ALIGN_LEFT);
                para.Add(GetParagraph_Title(assetHandOverExport.UserNameReceiver, Element.ALIGN_LEFT));
                document.Add(para);

                document.Add(UltilitiesPDF.pBreakOneLine);

                PdfPTable tM = AddColumnsHeader(HandoverAsset.ASSET_COLUMNS);
                tM.WidthPercentage = 100f;

                msg = CreateDataTable(assetSenderHandOver.ltAsset, HandoverAsset.ASSET_COLUMNS, tM);
                if (msg.Length > 0) return msg;
                document.Add(tM);
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            return msg;
        }

        private static string CreateDataTable(List<Asset> listAsset, FileReportColumn[] arrFileReportColumn, PdfPTable pdfPTable)
        {
            int rowIndex = 0;
            string msg = "";

            foreach (var item in listAsset)
            {
                rowIndex += 1;
                foreach (FileReportColumn col in arrFileReportColumn)
                {
                    FileReportCell cell = new FileReportCell(rowIndex, col.ID);

                    msg = HandoverAsset.GetColumnValue(rowIndex, cell.ColumnID, item, out string columnValue);
                    if (msg.Length > 0) return msg;

                    msg = col.GetAlign(HandoverAsset.TYPE_PDF, out object align);
                    if (msg.Length > 0) return msg;

                    AddCellToTable(pdfPTable, columnValue, (int)align);
                }
            }
            return msg;
        }

        private static PdfPTable AddColumnsHeader(FileReportColumn[] arrFileReportColumn)
        {
            PdfPTable table = new PdfPTable(arrFileReportColumn.Length);

            float[] columnWidthsM = arrFileReportColumn.Select(v => v.WidthPDF).ToArray();
            table.SetWidths(columnWidthsM);
            foreach (FileReportColumn col in arrFileReportColumn) AddCellToTableHeader(table, col.Name);

            return table;
        }

        private static Paragraph GetParagraphBold(string str, int ALIGN)
        {
            return UltilitiesPDF.CreateParagraph(str, HandoverAsset.FONT_SIZE_PDF, UltilitiesPDF.FontWeight.Bold, ALIGN, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
        }
        private static Paragraph GetParagraph_Bold(string str, int ALIGN)
        {
            return UltilitiesPDF.CreateParagraph(str, 14, UltilitiesPDF.FontWeight.Bold, ALIGN, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
        }

        private static Paragraph GetParagraph_Title(string str, int ALIGN)
        {
            return UltilitiesPDF.CreateParagraph(str, 14, UltilitiesPDF.FontWeight.Normal, ALIGN, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
        }

        private static void AddCellToTableHeader(PdfPTable table, string value)
        {
            table.AddCell(UltilitiesPDF.CreateCellBorder(GetParagraph_TableHeader(value), 1, Element.ALIGN_CENTER));
        }

        private static Paragraph GetParagraph_TableHeader(string str)
        {
            return UltilitiesPDF.CreateParagraph(str, HandoverAsset.FONT_SIZE_TABLE_PDF, UltilitiesPDF.FontWeight.Bold, Element.ALIGN_CENTER, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
        }

        private static void AddCellToTable(PdfPTable table, string value, int align)
        {
            table.AddCell(UltilitiesPDF.CreateCellBorder(GetParagraph_Table(value, align), 1, align));
        }

        private static Paragraph GetParagraph_Table(string str, int ALIGN)
        {
            return UltilitiesPDF.CreateParagraph(str, HandoverAsset.FONT_SIZE_TABLE_PDF, UltilitiesPDF.FontWeight.Normal, ALIGN, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
        }
    }
}