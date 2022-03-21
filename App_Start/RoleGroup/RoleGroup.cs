using System;
using System.Collections.Generic;
using BSS;
using System.Data;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

public class RoleGroupSearch
{
    public string TextSearch { get; set; }
    public int RoleGroupID { get; set; }
    public long UserIDCreate { get; set; }
    public int StatusID { get; set; }
    public int AccountID { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public RoleGroupSearch()
    {
        RoleGroupID = 0;
        UserIDCreate = 0;
        StatusID = -1;
        PageSize = 0;
        CurrentPage = 0;
    }
}