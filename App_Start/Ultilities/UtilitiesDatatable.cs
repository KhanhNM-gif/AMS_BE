using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

public static class UtilitiesDatatable
{
    public static string GetDtPaging(DataTable dt, int PageSize, int CurrentPage, out DataTable dtPaging)
    {
        dtPaging = dt.Clone();
        if (PageSize == 0 || CurrentPage == 0) dtPaging = dt;
        else for (int i = PageSize * (CurrentPage - 1); i < PageSize * CurrentPage && i < dt.Rows.Count; i++)
                dtPaging.ImportRow(dt.Rows[i]);

        return "";
    }
}