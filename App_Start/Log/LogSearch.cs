using BSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class LogSearch
{
    public long ID { get; set; }
    public string UserName { get; set; }
    public string IPAddress { get; set; }
    public int LogType { get; set; }
    public string LogTypeName { get; set; }
    public string LogContent { get; set; }
    public DateTime CreateDate { get; set; }

    public static string GetListLogSearch(LogSearchInput logSearchInput, out List<LogSearch> ListLogSearch)
    {
        ListLogSearch = null;

        string msg = GetListSearchWithParameter(logSearchInput, out dynamic o);
        if (msg.Length > 0) return msg;

        return DBM.GetList("usp_Log_SelectSearch", o , out ListLogSearch);
    }
    private static string GetListSearchWithParameter(LogSearchInput logSearchInput, out dynamic o)
    {
        o = null;
        string msg = "";
        o = new
        {
            logSearchInput.TextSearch,
            logSearchInput.LogTypeID,
            logSearchInput.LogFrom,
            logSearchInput.LogTo
        };

        return msg;
    }

}
public class LogSearchInput
{
    public string TextSearch { get; set; }
    public int LogTypeID { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime LogFrom { get; set; }
    [JsonConverter(typeof(DateFormatConverter), "yyyy-MM-dd")]
    public DateTime LogTo { get; set; }
    public LogSearchInput()
    {
        TextSearch = "";
        LogTypeID = 0;

        DateTime dtDefault = DateTime.Parse("1900-01-01");
        LogFrom = LogTo = dtDefault;
    }
}
