using ASM_API.App_Start.Ultilities;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;

namespace ASM_API.App_Start.PrintStamp
{
    public static class PrintStamp
    {
        public static string CreateFile(List<AssetViewDetail> assetViewDetails, string pathFile)
        {
            try
            {
                string msg = GetContent(assetViewDetails, out byte[] fileContent);
                if (msg.Length > 0) return msg;

                File.WriteAllBytes(pathFile, fileContent);

                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public static string GetContent(List<AssetViewDetail> assetViewDetails, out byte[] FileContent)
        {
            string msg = "";

            FileContent = null;

            msg = BSS.Common.GetSetting("PDFWidth", out string PDFWidth);
            if (msg.Length > 0) return msg;

            msg = BSS.Common.GetSetting("PDFHeight", out string PDFHeight);
            if (msg.Length > 0) return msg;

            float width = float.Parse(PDFWidth);
            float height = float.Parse(PDFHeight);

            using (var ms = new MemoryStream())
            {
                Document document = new Document(new Rectangle(width, height), 5, 5, 5, 5);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                msg = CreateContent(height, document, assetViewDetails);
                if (msg.Length > 0) return msg;

                document.Close();

                writer.Close();
                FileContent = ms.ToArray();
            }

            return msg;
        }

        private static string CreateContent(float height, Document document, List<AssetViewDetail> assetViewDetails)
        {
            string msg = "";
            msg = CreatePdfTable(height, assetViewDetails, out PdfPTable table);
            if (msg.Length > 0) return msg;

            document.Add(table);

            return msg;
        }

        private static string CreatePdfTable(float height, List<AssetViewDetail> assetViewDetails, out PdfPTable table)
        {
            string msg = "";
            int defaultCol = 1;
            int colSpan = 1;
            int rowSpan = 1;
            int paddingLeft = 5;
            int paddingBottom = 5;
            float defaultCellHeight = 25f;
            table = new PdfPTable(defaultCol) { WidthPercentage = 100 };
            table.DefaultCell.Border = Rectangle.NO_BORDER;
            foreach (var assetViewDetail in assetViewDetails)
            {
                if (string.IsNullOrEmpty(assetViewDetail.AssetCode)) continue;

                var innerTable = new PdfPTable(3) { WidthPercentage = 100, HorizontalAlignment = Element.ALIGN_CENTER };
                float[] columnWidths = new float[] { 30f, 35f, 35f };
                innerTable.SetWidths(columnWidths);

                Paragraph accountCode = GetParagraphBold(assetViewDetail.AccountCode, Element.ALIGN_CENTER);
                CreateCell(accountCode, defaultCellHeight, colSpan, rowSpan, paddingLeft, paddingBottom, innerTable);

                colSpan = 2;
                Paragraph AssetTypeName = GetParagraphBold(assetViewDetail.AssetTypeName);
                CreateCell(AssetTypeName, defaultCellHeight, colSpan, rowSpan, paddingLeft, paddingBottom, innerTable);

                float contentHeight = height - (defaultCellHeight + paddingBottom + 10);
                colSpan = 1;
                rowSpan = 3;
                PdfPCell cellQRCode = InitPdfCell(colSpan, rowSpan, paddingLeft, 0);
                cellQRCode.FixedHeight = contentHeight;
                msg = GenagateQRCode.GetQRCode(assetViewDetail.AssetCode, out byte[] arrQRCode);
                if (msg.Length > 0) return msg;

                Image ImageQRCode = Image.GetInstance(arrQRCode);
                cellQRCode.AddElement(ImageQRCode);
                innerTable.AddCell(cellQRCode);

                colSpan = 2;
                float heightCell = contentHeight / rowSpan;
                rowSpan = 1;
                paddingBottom = 0;
                CreateCell(ConmonConstants.AssetSerial, assetViewDetail.AssetSerial, heightCell, colSpan, rowSpan, paddingLeft, paddingBottom, innerTable);
                CreateCell(ConmonConstants.AssetCode, assetViewDetail.AssetCode, heightCell, colSpan, rowSpan, paddingLeft, paddingBottom, innerTable);
                CreateCell(ConmonConstants.DeptCode, assetViewDetail.DeptCode, heightCell, colSpan, rowSpan, paddingLeft, paddingBottom, innerTable);

                table.AddCell(innerTable);
            }

            return msg;
        }

        private static void CreateCell(Paragraph paragraph, float height, int colSpan, int rowSpan, int paddingLeft, int paddingBottom, PdfPTable innerTable)
        {
            PdfPCell cell = InitPdfCell(colSpan, rowSpan, paddingLeft, paddingBottom);
            cell.FixedHeight = height;
            cell.AddElement(paragraph);
            innerTable.AddCell(cell);
        }

        private static void CreateCell(string title, string value, float height, int colSpan, int rowSpan, int paddingLeft, int paddingBottom, PdfPTable innerTable)
        {
            Phrase DeptCode = CreatePhase(title, value);
            PdfPCell cell = InitPdfCell(colSpan, rowSpan, paddingLeft, paddingBottom);
            cell.FixedHeight = height;
            cell.AddElement(DeptCode);
            innerTable.AddCell(cell);
        }

        private static PdfPCell InitPdfCell(int colSpan = 0, int rowSpan = 0, int paddingLeft = 0, int paddingBottom = 0)
        {
            return new PdfPCell() { Colspan = colSpan, Rowspan = rowSpan, VerticalAlignment = Element.ALIGN_CENTER, HorizontalAlignment = Element.ALIGN_CENTER, Border = Rectangle.BOX, PaddingLeft = paddingLeft, PaddingBottom = paddingBottom };
        }

        private static Paragraph GetParagraphBold(string value, int align = 0)
        {
            return UltilitiesPDF.CreateParagraph(value, 12, UltilitiesPDF.FontWeight.Bold, align);
        }

        private static Phrase CreatePhase(string fontNormalTitle, string fontBoldTitle, int defaultFontSize = 12)
        {
            Phrase phrase = new Phrase();
            phrase.Add(UltilitiesPDF.CreateChunk(fontNormalTitle, defaultFontSize, UltilitiesPDF.FontWeight.Normal, Font.NORMAL, BaseColor.BLACK));
            phrase.Add(UltilitiesPDF.CreateChunk(fontBoldTitle, defaultFontSize, UltilitiesPDF.FontWeight.Bold, Font.NORMAL, BaseColor.BLACK));

            return phrase;
        }
    }
}