using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;


public class FileReportWord
{
    public static string CreateFile(string pathFile, DataTable dt)
    {
        string msg = "";
        using (WordprocessingDocument document = WordprocessingDocument.Create(pathFile, WordprocessingDocumentType.Document))
        {
            // Add a main document part. 
            MainDocumentPart mainPart = document.AddMainDocumentPart();

            // Create the document structure and add some text.
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            SectionProperties SecPro = new SectionProperties();
            PageSize PSize = new PageSize();
            PSize.Width = 18000U;
            SecPro.Append(PSize);

            PageMargin pageMargin = new PageMargin() { Right = (UInt32Value)300, Left = (UInt32Value)300 };
            SecPro.Append(pageMargin);

            body.Append(SecPro);

            Paragraph para = GetPara(FileReport.TITLE, JustificationValues.Center, new Bold(), 30);

            body.Append(new ParagraphProperties(new Justification() { Val = JustificationValues.Center }),
                        para);

            var sizeBorder = (UInt32Value) 10;
            Table table = new Table();
            TableProperties props = new TableProperties(
                new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = sizeBorder },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = sizeBorder },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = sizeBorder },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = sizeBorder },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = sizeBorder },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = sizeBorder }));

            table.AppendChild<TableProperties>(props);

            var trHeader = new TableRow();
            for (var i = 0; i < FileReport.COLUMNS.Length; i++)
            {
                var tc = new TableCell();

                Paragraph paraHeader = GetPara(FileReport.COLUMNS[i].Name, JustificationValues.Center, new Bold(), 22);

                tc.Append(new ParagraphProperties(new Justification() { Val = JustificationValues.Center }),
                          new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = FileReport.COLUMNS[i].WidthWord.ToString() }, new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }),
                          paraHeader);

                trHeader.Append(tc);
            }
            table.Append(trHeader);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var tr = new TableRow();

                DataRow dr = dt.Rows[i];

                foreach (FileReportColumn col in FileReport.COLUMNS)
                {
                    FileReportCell cell = new FileReportCell(i, col.ID);
                    string val;
                    msg = cell.GetValue(dr, out val);
                    if (msg.Length > 0) return msg;

                    object align;
                    msg = col.GetAlign(FileReport.TYPE_WORD, out align);
                    if (msg.Length > 0) return msg;

                    AddCellToRow(tr, val, (JustificationValues)align, col.WidthWord);
                }        
                
                table.Append(tr);
            }

            body.Append(table);
            document.Close();
        }

        return "";
    }
    private static void AddCellToRow(TableRow tr, string value, JustificationValues align, int width)
    {
        var tc = new TableCell();
        Paragraph para = GetPara(value, align, null, 20);

        TableCellProperties tcp = new TableCellProperties();
        tcp.Append(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
        tc.Append(tcp);

        tc.Append(new ParagraphProperties(new Justification() { Val = align }),
                  new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = width.ToString() }, new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center }),
                  para);
        tr.Append(tc);
    }
    private static Paragraph GetPara(string value, JustificationValues align, Bold b, int fontSize)
    {
        RunProperties rp = new RunProperties();
        rp.Append(
            //new RunFonts() { HighAnsi = "Arial" },
                  new Color() { Val = "black" },
                  b,
                  new FontSize { Val = fontSize.ToString() },
                  new Justification() { Val = align });

        Paragraph para = new Paragraph();
        Run run = para.AppendChild(new Run());
        run.AppendChild(rp);
        run.AppendChild(new Text(value));

        return para;
    }
}