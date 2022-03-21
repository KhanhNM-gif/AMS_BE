using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

/// <summary>
/// Summary description for UltilitiesPDF
/// </summary>

public class UltilitiesPDF
{
    public static int DefaultFontSize { get; set; }
    private static int defaultFontSize
    {
        get { return DefaultFontSize == 0 ? 11 : DefaultFontSize; }
        set { DefaultFontSize = value; }
    }

    public static Paragraph pBreakTwoLine = CreateParagraph("\n\n");
    public static Paragraph pBreakOneLine = CreateParagraph("\n");
    public static BaseColor BaseColor_GREEN = new BaseColor(0, 128, 59);
    public static void SetdefaultFontSize(int fontSize)
    {
        defaultFontSize = fontSize;
    }
    public class FontWeight
    {
        public const string Bold = "timesbd.TTF";
        public const string Normal = "times.TTF";
        public const string BoldItalic = "timesbi.TTF";
        public const string Italic = "timesi.TTF";
    }

    public static Chunk CreateChunk(string str, int fontSize, string fontWeight, int fontStyle, BaseColor color)
    {
        string TIME_TFF = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontWeight);
        BaseFont bf = BaseFont.CreateFont(TIME_TFF, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
        iTextSharp.text.Font f = new iTextSharp.text.Font(bf, fontSize, fontStyle, color);
        Chunk chk = new Chunk(str, f);
        return chk;
    }

    public static PdfPCell CreateCellBorderColor(string str, int colspan, BaseColor baseColor)
    {
        return CreateCellBorderColor(str, colspan, baseColor, BaseColor.BLACK);
    }
    public static PdfPCell CreateCellBorderColor(Paragraph paragraph, int colspan, BaseColor baseColor)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, HorizontalAlignment = Element.ALIGN_LEFT, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 5f, MinimumHeight = 20f, BackgroundColor = baseColor };
        cell.AddElement(paragraph);
        return cell;
    }
    public static PdfPCell CreateCellBorderColor(string str, int colspan, BaseColor baseColor, BaseColor textColor)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, HorizontalAlignment = Element.ALIGN_LEFT, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 5f, MinimumHeight = 20f, BackgroundColor = baseColor };
        cell.AddElement(CreateParagraph(str, 11, FontWeight.Bold, Element.ALIGN_LEFT, Font.NORMAL, textColor));
        return cell;
    }
    public static PdfPCell CreateCellBorderColor(string str, int colspan, int rowspan, BaseColor baseColor, BaseColor textColor)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, Rowspan = rowspan, HorizontalAlignment = Element.ALIGN_LEFT, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 5f, MinimumHeight = 20f, BackgroundColor = baseColor };
        cell.AddElement(CreateParagraph(str, 11, FontWeight.Bold, Element.ALIGN_LEFT, Font.NORMAL, textColor));
        return cell;
    }
    public static PdfPCell CreateCellBorderPadding(Paragraph paragraph, int colspan)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, HorizontalAlignment = Element.ALIGN_LEFT, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 5f, PaddingTop = 10f, PaddingBottom = 10f, MinimumHeight = 20f };
        cell.AddElement(paragraph);
        return cell;
    }
    public static PdfPCell CreateCellBorderPadding(Paragraph paragraph, int colspan, int align, int verticalAlign)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, HorizontalAlignment = align, VerticalAlignment = verticalAlign, Padding = 5f, PaddingTop = 10f, PaddingBottom = 10f, MinimumHeight = 20f };
        cell.AddElement(paragraph);
        return cell;
    }

    public static PdfPCell CreateCellBorder(Paragraph paragraph, int colspan)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, HorizontalAlignment = Element.ALIGN_LEFT, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 5f, PaddingTop = 2.5f, PaddingBottom = 2.5f, MinimumHeight = 20f };
        cell.AddElement(paragraph);
        return cell;
    }
    public static PdfPCell CreateCellBorder(string str, int colspan)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, HorizontalAlignment = Element.ALIGN_LEFT, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 5f, PaddingTop = 2.5f, PaddingBottom = 2.5f, MinimumHeight = 20f };
        cell.AddElement(CreateParagraph(str));
        return cell;
    }
    public static PdfPCell CreateCellBorder(string str, int colspan, int align)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, UseAscender = true, HorizontalAlignment = align, VerticalAlignment = Element.ALIGN_MIDDLE };
        cell.AddElement(CreateParagraph(str, align));
        return cell;
    }
    public static PdfPCell CreateCellBorder(Paragraph paragraph, int colspan, int align)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, UseAscender = true, HorizontalAlignment = align, VerticalAlignment = Element.ALIGN_MIDDLE, Padding = 5f, MinimumHeight = 20f };
        cell.AddElement(paragraph);
        return cell;
    }
    public static PdfPCell CreateCellBorder(Paragraph paragraph, int colspan, int align, int verticalAlight)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, UseAscender = true, HorizontalAlignment = align, VerticalAlignment = verticalAlight, Padding = 5f, PaddingTop = 2.5f, PaddingBottom = 2.5f, MinimumHeight = 20f };
        cell.AddElement(paragraph);
        return cell;
    }

    public static PdfPCell CreateCellDefaultTop(IElement paragraph, int colspan, int align)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, HorizontalAlignment = align, Border = PdfPCell.NO_BORDER, VerticalAlignment = Element.ALIGN_TOP, PaddingTop = 2.5f, PaddingBottom = 2.5f, MinimumHeight = 20f };
        cell.AddElement(paragraph);
        return cell;
    }
    public static PdfPCell CreateCellDefault(Paragraph paragraph, int colspan, int align)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, HorizontalAlignment = align, Border = PdfPCell.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE, PaddingTop = 2.5f, PaddingBottom = 2.5f, MinimumHeight = 20f };
        cell.AddElement(paragraph);
        return cell;
    }
    public static PdfPCell CreateCellDefault(string str, int colspan)
    {
        PdfPCell cell = new PdfPCell() { Colspan = colspan, Border = PdfPCell.NO_BORDER, VerticalAlignment = Element.ALIGN_MIDDLE, PaddingTop = 2.5f, PaddingBottom = 2.5f, MinimumHeight = 20f };
        cell.AddElement(CreateParagraph(str));
        return cell;
    }
    public static Phrase CreatePhrase(string str)
    {
        return CreatePhrase(str, defaultFontSize, FontWeight.Normal, Font.NORMAL, BaseColor.BLACK);
    }
    public static Phrase CreatePhrase(string str, int fontSize)
    {
        return CreatePhrase(str, fontSize, FontWeight.Normal, Font.NORMAL, BaseColor.BLACK);
    }
    public static Phrase CreatePhrase(string str, int fontSize, string fontWeight)
    {
        return CreatePhrase(str, fontSize, fontWeight, Font.NORMAL, BaseColor.BLACK);
    }
    public static Phrase CreatePhrase(string str, int fontSize, string fontWeight, int fontStyle)
    {
        return CreatePhrase(str, fontSize, fontWeight, fontStyle, BaseColor.BLACK);
    }
    public static Phrase CreatePhrase(string str, int fontSize, string fontWeight, int fontStyle, BaseColor color)
    {
        string TIME_TFF = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontWeight);
        BaseFont bf = BaseFont.CreateFont(TIME_TFF, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
        iTextSharp.text.Font f = new iTextSharp.text.Font(bf, fontSize, fontStyle, color);
        return new Phrase(str, f);
    }
    public static List CreateParagraphValue(string str, int fontSize, string fontWeight, int ALIGN)
    {
        Paragraph p = CreateParagraph(string.IsNullOrEmpty(str) ? " " : str, fontSize, fontWeight, ALIGN, Font.NORMAL, BaseColor.BLACK);
        return CreateParagraphValue(p, fontSize, fontWeight, ALIGN);
    }
    public static List CreateParagraphValue(Paragraph paragraph, int fontSize, string fontWeight, int ALIGN)
    {
        List lt = new List(false, false, 8) { IndentationLeft = 0f };
        lt.SetListSymbol(":");
        lt.Add(new ListItem(paragraph) { IndentationLeft = 0f });
        return lt;
    }
    public static List CreateParagraphValue(string str, int ALIGN)
    {
        return CreateParagraphValue(string.IsNullOrEmpty(str) ? " " : str, defaultFontSize, FontWeight.Normal, ALIGN);
    }
    public static List CreateParagraphValue(Paragraph paragraph, int ALIGN)
    {
        return CreateParagraphValue(paragraph, defaultFontSize, FontWeight.Normal, ALIGN);
    }
    public static Paragraph CreateParagraph(string str, int ALIGN)
    {
        return CreateParagraph(str, defaultFontSize, FontWeight.Normal, ALIGN, Font.NORMAL, BaseColor.BLACK);
    }
    public static Paragraph CreateParagraph(string str)
    {
        return CreateParagraph(str, defaultFontSize, FontWeight.Normal, Element.ALIGN_LEFT, Font.NORMAL, BaseColor.BLACK);
    }
    public static Anchor HyperLink(string str, string href, int fontSize, string fontWeight)
    {
        string TIME_TFF = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontWeight);
        //Create a base font object making sure to specify IDENTITY-H
        BaseFont bf = BaseFont.CreateFont(TIME_TFF, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
        //Create a specific font object
        iTextSharp.text.Font f = new iTextSharp.text.Font(bf, fontSize, Font.UNDERLINE, new BaseColor(0, 0, 255));
        Anchor anchor = new Anchor(str, f);
        anchor.Reference = href;
        return anchor;
    }
    public static Paragraph CreateParagraph(string str, int fontSize, string fontWeight, int ALIGN)
    {
        return CreateParagraph(str, fontSize, fontWeight, ALIGN, Font.NORMAL, BaseColor.BLACK);
    }
    public static Paragraph CreateParagraph(string str, int fontSize, string fontWeight, int ALIGN, int fontStyle, BaseColor color)
    {
        string TIME_TFF = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fontWeight);
        //Create a base font object making sure to specify IDENTITY-H
        BaseFont bf = BaseFont.CreateFont(TIME_TFF, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
        //Create a specific font object
        iTextSharp.text.Font f = new iTextSharp.text.Font(bf, fontSize, fontStyle, color);
        Paragraph p = new Paragraph(str, f);
        p.Alignment = ALIGN;
       
        return p;
    }
    public static Paragraph CreateParagraph(List<Phrase> ltPhrase)
    {
        Paragraph p = new Paragraph();
        foreach (var item in ltPhrase)
        {
            p.Add(item);
        }
        return p;
    }
    public static Phrase CreateCheckbox(bool isChecked)
    {
        BaseFont fontBase = BaseFont.CreateFont("C:\\Windows\\fonts\\wingding.ttf", BaseFont.IDENTITY_H, false);
        iTextSharp.text.Font wFont = new iTextSharp.text.Font(fontBase, 16f, Font.NORMAL);
        string cbChecked = "\u00FE";
        string cbUnchecked = "\u00A8";

        string chChar = isChecked ? cbChecked : cbUnchecked;
        return new Phrase(chChar, wFont);
    }
    public partial class Header : PdfPageEventHelper
    {
        public IElement HeaderContent { get; set; }
        public override void OnStartPage(PdfWriter writer, Document doc)
        {
            doc.Add(HeaderContent);
        }
    }
    public partial class Footer : PdfPageEventHelper
    {
        public IElement FooterContent { get; set; }
        public override void OnEndPage(PdfWriter writer, Document doc)
        {
            try
            {
                PdfPTable table = new PdfPTable(1);
                float[] columnWidths = new float[] { 100f };
                table.TotalWidth = doc.PageSize.Width - doc.LeftMargin - doc.RightMargin; //this centers [table]
                table.SetWidths(columnWidths);

                table.WidthPercentage = 100f;
                table.HorizontalAlignment = Element.ALIGN_CENTER;
                table.DefaultCell.Border = iTextSharp.text.Rectangle.NO_BORDER;

                iTextSharp.text.Image imgLogo = iTextSharp.text.Image.GetInstance(HttpContext.Current.Server.MapPath("/img/footer.png"));
                imgLogo.ScalePercent(45f);
                imgLogo.Alignment = Image.ALIGN_CENTER;
                PdfPCell cLeft = new PdfPCell() { VerticalAlignment = Element.ALIGN_MIDDLE, HorizontalAlignment = Element.ALIGN_CENTER, Border = Rectangle.NO_BORDER };
                cLeft.AddElement(imgLogo);
                table.AddCell(cLeft);

                table.WriteSelectedRows(0, -1, doc.LeftMargin, (doc.BottomMargin + table.TotalHeight), writer.DirectContent);
            }
            catch (Exception ex)
            {

                throw;
            }

        }
    }
    public static PdfPCell AddSignStamp(PdfContentByte cb, string watermark)
    {
        float leading = 5;
        Paragraph title = CreateParagraph("Đã được ký điện tử bởi\n", 10, FontWeight.Normal, Element.ALIGN_CENTER);
        title.SetLeading(leading, 0);
        Paragraph signedBy = CreateParagraph(watermark, 10, FontWeight.Bold, Element.ALIGN_CENTER);
        iTextSharp.text.Image ImageStampSigned = iTextSharp.text.Image.GetInstance(HttpContext.Current.Server.MapPath("/img/chuky.png"));
        PdfPCell c = new PdfPCell() { PaddingLeft = 15f, PaddingRight = 15f, FixedHeight = ImageStampSigned.ScaledHeight };
        c.HorizontalAlignment = Element.ALIGN_CENTER;
        c.VerticalAlignment = Element.ALIGN_MIDDLE;
        c.Border = 0;

        c.CellEvent = new CellEvent() { CellImage = ImageStampSigned };
        title.SpacingAfter = leading * 0.5f;
        c.AddElement(title);
        c.AddElement(signedBy);

        return c;
    }
    public class CellEvent : IPdfPCellEvent
    {
        public Image CellImage;
        public CellEvent()
        {
        }
        public void CellLayout(PdfPCell cell, Rectangle position, PdfContentByte[] canvases)
        {
            PdfContentByte cb = canvases[PdfPTable.BACKGROUNDCANVAS];
            CellImage.ScaleToFit(170f, 170f);

            float left = position.Left + (position.Width - CellImage.ScaledWidth) / 2;
            float bottom = position.Bottom + (position.Height - CellImage.ScaledHeight) / 2;
            CellImage.SetAbsolutePosition(left, bottom);

            cb.AddImage(CellImage);
        }
    }
}