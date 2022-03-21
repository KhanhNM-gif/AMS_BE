using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

public class FileReportColumn
{
    public const int ALIGN_LEFT = 1, ALIGN_RIGHT = 2, ALIGN_CENTER = 3;

    public string ID { set; get; }
    public string Name { set; get; }
    public int WidthExcel { set; get; }
    public float WidthPDF { set; get; }
    public int WidthWord { set; get; }
    public int Align { set; get; }
    public int TypeGetValue { set; get; }

    public FileReportColumn(string ID, string Name, int WidthExcel, float WidthPDF, int WidthWord)
    {
        this.ID = ID;
        this.Name = Name;
        this.WidthExcel = WidthExcel;
        this.WidthPDF = WidthPDF;
        this.WidthWord = WidthWord;
        this.Align = ALIGN_LEFT;
        this.TypeGetValue = FileReportCell.TYPEGETVALUE_STRING;
    }
    public FileReportColumn(string ID, string Name, int WidthExcel, float WidthPDF, int WidthWord, int Align, int TypeGetValue)
    {
        this.ID = ID;
        this.Name = Name;
        this.WidthExcel = WidthExcel;
        this.WidthPDF = WidthPDF;
        this.WidthWord = WidthWord;
        this.Align = Align;
        this.TypeGetValue = TypeGetValue;
    }

    public string GetAlign(string typeFile, out object val)
    {
        val = "";

        if (Align == ALIGN_CENTER)
        {
            if (typeFile == FileReport.TYPE_PDF) val = iTextSharp.text.Element.ALIGN_CENTER;
            if (typeFile == FileReport.TYPE_WORD) val = DocumentFormat.OpenXml.Math.JustificationValues.Center;
            if (typeFile == FileReport.TYPE_EXCEL) val = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        }
        else
            if (Align == ALIGN_RIGHT)
            {
                if (typeFile == FileReport.TYPE_PDF) val = iTextSharp.text.Element.ALIGN_RIGHT;
                if (typeFile == FileReport.TYPE_WORD) val = DocumentFormat.OpenXml.Math.JustificationValues.Right;
                if (typeFile == FileReport.TYPE_EXCEL) val = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
            }
            else
            {
                if (typeFile == FileReport.TYPE_PDF) val = iTextSharp.text.Element.ALIGN_LEFT;
                if (typeFile == FileReport.TYPE_WORD) val = DocumentFormat.OpenXml.Math.JustificationValues.Left;
                if (typeFile == FileReport.TYPE_EXCEL) val = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
            }

        return "";
    }
}