using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;



namespace ASM_API.App_Start.Template
{
    /*public class ItemProposalFormExportWord
    {
        public ItemProposalFormExportWord() { }
        public ItemProposalFormExportWord(string numericalOrder, string itemName, string itemCode, string itemUnitName, string accordingDocument, string actuallyImported, long unitPrice, long amount)
        {
            NumericalOrder = numericalOrder;
            ItemName = itemName;
            ItemCode = itemCode;
            ItemUnitName = itemUnitName;
            AccordingDocument = accordingDocument;
            ActuallyImported = actuallyImported;
            UnitPrice = unitPrice.ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE"));
            strAmount = amount.ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE"));
            Amount = amount;
        }
        public long GetAmount() => Amount;
        public string NumericalOrder { get; set; }
        private long Amount { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string ItemUnitName { get; set; }
        public string AccordingDocument { get; set; }
        public string ActuallyImported { get; set; }
        public string UnitPrice { get; set; }
        public string strAmount { get; set; }
    }

    public class LtItemProposalFormExportWord : TableDocument
    {
        public List<ItemProposalFormExportWord> ltItemProposalFormExportWord { get; set; }
        private string title { get; set; } = "DsVP";
        private bool hasFooterTable { get; set; } = true;

        public object[] GetFooterTable()
        {
            return new object[]
            {
                "","Cộng","","","","","",ltItemProposalFormExportWord.Sum(x=>x.GetAmount()).ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE"))
            };
        }
        public DataTable GetDataTable() => ltItemProposalFormExportWord.ToDataTable();
        public string GetTitle() => title;
        public bool HasFooterTable() => hasFooterTable;
    }*/
    public interface TemplateExportWord
    {
        Dictionary<string, string> GetDictionaryReplace();
    }

    public interface TableDocument
    {
        DataTable GetDataTable();
        string GetTitle();
        bool HasFooterTable();
        object[] GetFooterTable();
    }

    public static class WordDocument
    {
        public static string FillTemplate(string pathTemplate, string outputDoc, string outputPDF, TemplateExportWord templateExportWord, params TableDocument[] TD)
        {
            try
            {
                using (WordprocessingDocument document = WordprocessingDocument.CreateFromTemplate(pathTemplate))
                {
                    var body = document.MainDocumentPart.Document.Body;

                    //Lấy các Paragraph là con trực tiếp của body
                    var paragraphs = body.Elements<Paragraph>().ToList();

                    //Add thêm các Paragraph bên trong Table
                    var Table = body.Descendants<Table>();
                    paragraphs.AddRange(Table.SelectMany(x => x.Elements<TableRow>()).SelectMany(x => x.Elements<TableCell>()).SelectMany(x => x.Elements<Paragraph>()));

                    //Lấy các text bên trong Content Controller
                    var texts = paragraphs.SelectMany(p => p.Elements<SdtRun>()).SelectMany(p => p.Elements<SdtContentRun>()).SelectMany(p => p.Elements<Run>()).SelectMany(p => p.Elements<Text>());

                    //Cập nhật text bên trong Content Controller
                    var dictionaryReplace = templateExportWord.GetDictionaryReplace();
                    foreach (var item in texts)
                    {
                        if (!dictionaryReplace.TryGetValue(item.Text, out string value)) continue;

                        item.Text = value;
                    }

                    //Lấy các ds Table add vào Dictionary
                    var tableProperties = body.Descendants<TableProperties>().Where(tp => tp.TableCaption != null).ToDictionary(x => x.TableCaption.Val, x => (Table)x.Parent);

                    // Cập nhập table params TableDocument[] TD đầu vào
                    foreach (TableDocument tableDocument in TD)
                    {
                        if (tableProperties.TryGetValue(tableDocument.GetTitle(), out Table table))
                        {
                            // Lấy cột hàng cuối để sử dụng làm Properties
                            var tableRow = (TableRow)table.LastChild;

                            foreach (DataRow row in tableDocument.GetDataTable().Rows)
                                table.Append(new TableRow(CreateArrayTableCell(tableRow, row.ItemArray)));

                            if (tableDocument.HasFooterTable())
                                table.Append(new TableRow(CreateArrayTableCell(tableRow, tableDocument.GetFooterTable(), true)));

                            // Xóa bỏ hàng sử dụng làm Properties
                            tableRow.Remove();
                        }
                    }

                    document.SaveAs(outputDoc).Close();

                    Spire.Doc.Document documentPDF = new Spire.Doc.Document(outputDoc);
                    documentPDF.SaveToFile(outputPDF, Spire.Doc.FileFormat.PDF);

                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return "";
        }

        private static OpenXmlElement[] CreateArrayTableCell(TableRow tableRow, object[] ltElement, bool isBold = false)
        {
            List<OpenXmlElement> tableCells = new List<OpenXmlElement>();

            TableCell[] ltTc = tableRow.Descendants<TableCell>().ToArray();

            for (int i = 0; i < ltTc.Length; i++)
            {
                var paragraph = ltTc[i].GetFirstChild<Paragraph>();

                RunProperties runProperties = (RunProperties)paragraph.GetFirstChild<Run>().GetFirstChild<RunProperties>().Clone();
                if (isBold) runProperties.Bold = new Bold();

                ParagraphProperties paragraphProperties = (ParagraphProperties)paragraph.GetFirstChild<ParagraphProperties>().Clone();

                TableCell tableCell = new TableCell(new Paragraph(paragraphProperties, new Run(runProperties, new Text(ltElement[i].ToString()))));
                tableCells.Add(tableCell);
            }

            return tableCells.ToArray();
        }

    }
}