using ASM_API.App_Start.FileReport;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FileReportPDF
{
    public static string CreateAssetHandoverFile(ReportHandOver reportHandOver, string pathFile)
    {
        try
        {
            byte[] fileContent;
            string msg = GetContentAssetHandover(reportHandOver, out fileContent);
            if (msg.Length > 0) return msg;

            File.WriteAllBytes(pathFile, fileContent);

            return "";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    public static string GetContentAssetHandover(ReportHandOver reportHandOver, out byte[] FileContent)
    {
        string msg = "";

        FileContent = null;

        using (var ms = new MemoryStream())
        {
            Document document = new Document(iTextSharp.text.PageSize.A3, 25, 25, 25, 25);
            PdfWriter writer = PdfWriter.GetInstance(document, ms);
            document.Open();

            msg = ContentAssetHandover(document, reportHandOver);
            if (msg.Length > 0) return msg;

            document.Close();

            writer.Close();

            FileContent = ms.ToArray();
        }

        return msg;
    }

    private static string ContentAssetHandover(Document document, ReportHandOver reportHandOver)
    {
        string msg = "";
        try
        {
            FileReport.GetAllDeptName(reportHandOver.DeptNameOfUserHolding, out List<string> deptHoldingLevel1List, out List<string> deptHoldingLevel2List);

            string deptHoldingLevel1 = string.Join<string>(", ", deptHoldingLevel1List);
            string deptHoldingLevel2 = string.Join<string>(", ", deptHoldingLevel2List);

            int PdfColumns = 2;
            PdfPTable pdfPTablePreambleContent = new PdfPTable(PdfColumns);
            pdfPTablePreambleContent.WidthPercentage = 100f;
            AddTableHeaderNoBorder(pdfPTablePreambleContent, deptHoldingLevel1.Length > 0 ? deptHoldingLevel1.ToUpper() : "", UltilitiesPDF.FontWeight.Normal);
            AddTableHeaderNoBorder(pdfPTablePreambleContent, MessageConstants.CHXHCNVN);

            AddTableHeaderNoBorder(pdfPTablePreambleContent, deptHoldingLevel2);
            AddTableHeaderNoBorder(pdfPTablePreambleContent, MessageConstants.DLTDHP);
            document.Add(pdfPTablePreambleContent);

            FileReport.GetCurrentDateVietNamese(out string CurrentDateVietNamese);
            document.Add(GetParagraph_Table(CurrentDateVietNamese, Element.ALIGN_RIGHT));

            document.Add(GetParagraphBold(FileReport.ASSET_HANDOVER_TITLE, Element.ALIGN_CENTER));
            document.Add(UltilitiesPDF.pBreakOneLine);

            string reportStartAt = DateTime.Now.TimeOfDay.Hours + "h" + DateTime.Now.TimeOfDay.Minutes;
            var listItem = new ListItem(CreatePhase("Tiến hành vào hồi ", reportStartAt));
            listItem.Add(CreatePhase(", Ngày ", DateTime.Now.Day.ToString()));
            listItem.Add(CreatePhase(" Tháng ", DateTime.Now.Month.ToString()));
            listItem.Add(CreatePhase(" Năm ", DateTime.Now.Year.ToString()));
            document.Add(listItem);

            string participantsTitle = "Thành phần tham gia bàn giao gồm có:";
            document.Add(GetParagraph_Title(participantsTitle, Element.ALIGN_LEFT));

            string representedByHolding = $"A - Đại diện bên bàn giao: {(deptHoldingLevel2)}";
            document.Add(GetParagraphBold(representedByHolding, Element.ALIGN_LEFT));

            int STT = 1;
            if (!string.IsNullOrEmpty(reportHandOver.FullNameOfUserManagerHolding))
            {
                DoUserDetail(document, STT, reportHandOver.FullNameOfUserManagerHolding, reportHandOver.PostionNameOfUserManagerHolding);
                STT += 1;
            }

            if (!string.IsNullOrEmpty(reportHandOver.FullNameOfUserHolding)) DoUserDetail(document, STT, reportHandOver.FullNameOfUserHolding, reportHandOver.PostionNameOfUserHolding);

            document.Add(UltilitiesPDF.pBreakOneLine);
            FileReport.GetAllDeptName(reportHandOver.DeptNameOfUserHandover, out List<string> deptHandOverLevel1List, out List<string> deptHandOverLevel2List);

            string deptHandOverLevel2 = string.Join<string>(", ", deptHandOverLevel2List);

            string representedByHandover = $"B - Đại diện bên tiếp nhận: {(deptHandOverLevel2)}";
            document.Add(GetParagraphBold(representedByHandover, Element.ALIGN_LEFT));

            STT = 1;
            if (!string.IsNullOrEmpty(reportHandOver.FullNameOfUserManagerHandover))
            {
                DoUserDetail(document, STT, reportHandOver.FullNameOfUserManagerHandover, reportHandOver.PostionNameOfUserManagerHandover);
                STT += 1;
            }

            if (!string.IsNullOrEmpty(reportHandOver.FullNameOfUserHandover)) DoUserDetail(document, STT, reportHandOver.FullNameOfUserHandover, reportHandOver.PostionNameOfUserHandover);

            document.Add(UltilitiesPDF.pBreakOneLine);

            string TitleHandOverFor = " tiến hành bàn giao cho ";
            if (deptHoldingLevel2.Length > 1 && deptHandOverLevel2.Length > 1)
            {
                listItem = new ListItem(CreatePhase("", deptHoldingLevel2));
                listItem.Add(CreatePhase(TitleHandOverFor, deptHandOverLevel2));
            }
            else
            {
                listItem = new ListItem(CreatePhase("", reportHandOver.FullNameOfUserHolding));
                listItem.Add(CreatePhase(TitleHandOverFor, reportHandOver.FullNameOfUserHandover));
            }
            listItem.Add(CreatePhase(". Cụ thể như sau: ", ""));
            document.Add(listItem);

            document.Add(UltilitiesPDF.pBreakOneLine);

            PdfPTable pdfpTableColumns = AddColumnsHeader(FileReport.ASSET_HANDOVER_COLUMNS);
            pdfpTableColumns.WidthPercentage = 100;
            msg = CreateDataTable(reportHandOver, FileReport.ASSET_HANDOVER_COLUMNS, pdfpTableColumns);
            if (msg.Length > 0) return msg;

            document.Add(pdfpTableColumns);
            document.Add(UltilitiesPDF.pBreakOneLine);

            string contentReportend = "Biên bản được lập thành 02 bản có pháp lý như nhau, mỗi bên giữ 01 bản. Biên bản kết thúc vào hồi ....... cùng ngày";
            document.Add(GetParagraph_Title(contentReportend, Element.ALIGN_LEFT));
            document.Add(UltilitiesPDF.pBreakOneLine);

            string HandOverContent = $"\nNội dung: {reportHandOver.HandOverContent}";
            document.Add(GetParagraph_Title(HandOverContent, Element.ALIGN_LEFT));
            document.Add(UltilitiesPDF.pBreakOneLine);

            string representedByUserHoldingTitle = "đại diện bên giao".ToUpper();
            string representedByUserHandoverTitle = "đại diện bên nhận".ToUpper();
            PdfPTable pdfTableEndContent = new PdfPTable(PdfColumns);
            AddTableHeaderNoBorder(pdfTableEndContent, representedByUserHoldingTitle);
            AddTableHeaderNoBorder(pdfTableEndContent, representedByUserHandoverTitle);
            AddTableHeaderNoBorder(pdfTableEndContent, !string.IsNullOrEmpty(reportHandOver.PostionNameOfUserManagerHolding) ? reportHandOver.PostionNameOfUserManagerHolding.ToUpper() : "");
            AddTableHeaderNoBorder(pdfTableEndContent, !string.IsNullOrEmpty(reportHandOver.PostionNameOfUserManagerHandover) ? reportHandOver.PostionNameOfUserManagerHandover.ToUpper() : "");
            document.Add(pdfTableEndContent);

            PdfPTable pdfTableUserManagerDetail = new PdfPTable(PdfColumns);
            AddTableHeaderNoBorder(pdfTableUserManagerDetail, reportHandOver.FullNameOfUserManagerHolding, UltilitiesPDF.FontWeight.Normal);
            AddTableHeaderNoBorder(pdfTableUserManagerDetail, reportHandOver.FullNameOfUserManagerHandover, UltilitiesPDF.FontWeight.Normal);
            document.Add(pdfTableUserManagerDetail);
            document.Add(UltilitiesPDF.pBreakOneLine);

            PdfPTable pdfTableUserDetail = new PdfPTable(PdfColumns);
            AddTableHeaderNoBorder(pdfTableUserDetail, !string.IsNullOrEmpty(reportHandOver.PostionNameOfUserHolding) ? reportHandOver.PostionNameOfUserHolding.ToUpper() : "");
            AddTableHeaderNoBorder(pdfTableUserDetail, !string.IsNullOrEmpty(reportHandOver.PostionNameOfUserHandover) ? reportHandOver.PostionNameOfUserHandover.ToUpper() : "");
            AddTableHeaderNoBorder(pdfTableUserDetail, reportHandOver.FullNameOfUserHolding, UltilitiesPDF.FontWeight.Normal);
            AddTableHeaderNoBorder(pdfTableUserDetail, reportHandOver.FullNameOfUserHandover, UltilitiesPDF.FontWeight.Normal);
            document.Add(pdfTableUserDetail);
        }
        catch (Exception ex)
        {
            msg = ex.Message;
        }
        return msg;
    }
    public static string CreateFile(string pathFile, List<Asset> ltAsset)
    {
        try
        {
            byte[] fileContent;
            string msg = GetContent(ltAsset, out fileContent);
            if (msg.Length > 0) return msg;

            File.WriteAllBytes(pathFile, fileContent);

            return "";
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }
    public static string GetContent(List<Asset> ltAsset, out byte[] FileContent)
    {
        string msg = "";

        FileContent = null;

        using (var ms = new MemoryStream())
        {
            Document document = new Document(iTextSharp.text.PageSize.A3, 25, 25, 25, 25);
            PdfWriter writer = PdfWriter.GetInstance(document, ms);
            document.Open();

            msg = CreateContent(document, writer, ltAsset);
            if (msg.Length > 0) return msg;

            document.Close();

            writer.Close();

            FileContent = ms.ToArray();
        }

        return msg;
    }

    private static string CreateContent(Document document, PdfWriter writer, List<Asset> ltAsset)
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

            document.Add(UltilitiesPDF.CreateParagraph(FileReport.TITLE, 14, UltilitiesPDF.FontWeight.Bold, Element.ALIGN_CENTER, iTextSharp.text.Font.NORMAL, BaseColor.BLACK));
            document.Add(UltilitiesPDF.pBreakOneLine);

            PdfPTable tM = new PdfPTable(3);
            float[] columnWidthsM = new float[] { 14f, 8f, 14f };
            tM.SetWidths(columnWidthsM);
            tM.SpacingAfter = 10;

            tM.WidthPercentage = 100f;
            tM.HorizontalAlignment = Element.ALIGN_CENTER;
            tM.DefaultCell.Border = iTextSharp.text.Rectangle.NO_BORDER;
            tM.SplitLate = false;

            foreach (FileReportColumn col in FileReport.COLUMNS)
                tM.AddCell(UltilitiesPDF.CreateCellBorder(UltilitiesPDF.CreateParagraph(col.Name, 10, UltilitiesPDF.FontWeight.Bold, Element.ALIGN_CENTER, iTextSharp.text.Font.NORMAL, BaseColor.BLACK), 1, Element.ALIGN_CENTER));

            document.Add(tM);
        }
        catch (Exception ex)
        {
            msg = ex.Message;
        }
        return msg;
    }

    private static PdfPTable AddColumnsHeader(FileReportColumn[] arrFileReportColumn)
    {
        PdfPTable table = new PdfPTable(arrFileReportColumn.Length);

        float[] columnWidthsM = arrFileReportColumn.Select(v => v.WidthPDF).ToArray();
        table.SetWidths(columnWidthsM);
        foreach (FileReportColumn col in arrFileReportColumn) AddCellToTableHeaderBorder(table, col.Name);

        return table;
    }

    private static string CreateDataTable(ReportHandOver reportHandOver, FileReportColumn[] arrFileReportColumn, PdfPTable pdfPTable)
    {
        int rowIndex = 0;
        string msg = "";

        foreach (var item in reportHandOver.AssetHandoverDetail)
        {
            rowIndex += 1;
            foreach (FileReportColumn col in arrFileReportColumn)
            {
                FileReportCell cell = new FileReportCell(rowIndex, col.ID);

                msg = FileReport.GetColumnValue(rowIndex, cell.ColumnID, item, out string columnValue);
                if (msg.Length > 0) return msg;

                msg = col.GetAlign(HandoverAsset.TYPE_PDF, out object align);
                if (msg.Length > 0) return msg;

                AddCellToTable(pdfPTable, columnValue, (int)align);
            }
        }
        return msg;
    }

    private static void DoUserDetail(Document document, int stt, string Fullname, string PostionName)
    {
        PdfPTable pdfPTableUserDetail = new PdfPTable(2);

        List usernameList = new List() { IndentationLeft = 0f, ListSymbol = new Chunk("") };
        usernameList.Add(new ListItem(CreatePhase($"{stt} - Họ Tên: ", Fullname)));

        PdfPCell cellUserName = InitPdfCell();
        cellUserName.AddElement(usernameList);
        pdfPTableUserDetail.AddCell(cellUserName);

        List postionNameList = new List() { IndentationLeft = 0f, ListSymbol = new Chunk("") };
        postionNameList.Add(new ListItem(CreatePhase("Chức vụ: ", !string.IsNullOrEmpty(PostionName) ? PostionName : "")));

        PdfPCell cellPostion = InitPdfCell();
        cellPostion.AddElement(postionNameList);
        pdfPTableUserDetail.AddCell(cellPostion);

        document.Add(pdfPTableUserDetail);
    }

    private static PdfPCell InitPdfCell()
    {
        return new PdfPCell() { VerticalAlignment = Element.ALIGN_CENTER, HorizontalAlignment = Element.ALIGN_CENTER, Border = Rectangle.NO_BORDER };
    }

    private static Paragraph GetParagraphBold(string str, int ALIGN)
    {
        return UltilitiesPDF.CreateParagraph(str, HandoverAsset.FONT_SIZE_PDF, UltilitiesPDF.FontWeight.Bold, ALIGN, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
    }

    private static Paragraph GetParagraph_Title(string str, int ALIGN)
    {
        return UltilitiesPDF.CreateParagraph(str, HandoverAsset.FONT_SIZE_PDF, UltilitiesPDF.FontWeight.Normal, ALIGN, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
    }

    private static Phrase CreatePhase(string fontNormalTitle, string fontBoldTitle, int defaultFontSize = 12)
    {
        Phrase phrase = new Phrase();
        phrase.Add(UltilitiesPDF.CreateChunk(fontNormalTitle, defaultFontSize, UltilitiesPDF.FontWeight.Normal, Font.NORMAL, BaseColor.BLACK));
        phrase.Add(UltilitiesPDF.CreateChunk(fontBoldTitle, defaultFontSize, UltilitiesPDF.FontWeight.Bold, Font.NORMAL, BaseColor.BLACK));

        return phrase;
    }

    private static void AddCellToTableHeaderBorder(PdfPTable table, string value)
    {
        table.AddCell(UltilitiesPDF.CreateCellBorder(GetParagraph_TableHeader(value), 1, Element.ALIGN_CENTER));
    }
    private static void AddTableHeaderNoBorder(PdfPTable table, string value, string fontBold = UltilitiesPDF.FontWeight.Bold, int textAlign = Element.ALIGN_CENTER)
    {
        table.AddCell(UltilitiesPDF.CreateCellDefault(GetParagraph_TableHeader(value, fontBold, textAlign), 1, textAlign));
    }

    private static Paragraph GetParagraph_TableHeader(string str, string fontBold = UltilitiesPDF.FontWeight.Bold, int textAlign = Element.ALIGN_CENTER)
    {
        return UltilitiesPDF.CreateParagraph(str, HandoverAsset.FONT_SIZE_PDF, fontBold, textAlign, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
    }

    private static void AddCellToTable(PdfPTable table, string value, int align)
    {
        table.AddCell(UltilitiesPDF.CreateCellBorder(GetParagraph_Table(value, align), 1, align));
    }

    private static Paragraph GetParagraph_Table(string str, int ALIGN)
    {
        return UltilitiesPDF.CreateParagraph(str, HandoverAsset.FONT_SIZE_PDF, UltilitiesPDF.FontWeight.Normal, ALIGN, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
    }
}