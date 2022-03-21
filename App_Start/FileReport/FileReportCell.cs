using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;


public class FileReportCell
{
    public const int TYPEGETVALUE_INDEX = 1, TYPEGETVALUE_STRING = 2, TYPEGETVALUE_FORMATDATE = 3, TYPEGETVALUE_REMOVEHTML = 4;

    public int RowIndex { set; get; }
    public string ColumnID { set; get; }

    public FileReportCell(int RowIndex, string ColumnID)
    {
        this.RowIndex = RowIndex;
        this.ColumnID = ColumnID;
    }

    public string GetValue(DataRow dr, out string val)
    {
        val = "";

        var vID = FileReport.COLUMNS.Where(v => v.ID == ColumnID);
        if (vID.Count() > 0)
        {
            FileReportColumn col = vID.First();

            switch (col.TypeGetValue)
            {
                case FileReportCell.TYPEGETVALUE_INDEX:
                    val = (RowIndex + 1).ToString();
                    break;

                case FileReportCell.TYPEGETVALUE_STRING:
                    val = dr[ColumnID].ToString();
                    break;

                case FileReportCell.TYPEGETVALUE_FORMATDATE:
                    val = UtilitiesFormat.FormatDateToString(dr[ColumnID].ToString());
                    break;

                //case FileReportCell.TYPEGETVALUE_REMOVEHTML:
                //    val = UtilitiesHTML.DeleteHTMLAndWhitespace(dr[ColumnID].ToString());
                //    break;

                default:
                    val = dr[ColumnID].ToString();
                    break;
            }
        }
        else return "Chưa định nghĩa cột có ID " + ColumnID;

        return "";
    }
}