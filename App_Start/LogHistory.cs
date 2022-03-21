using BSS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

public class LogHistory
{
    public static string GetListHistory(Guid ObjectGuid, out DataTable dt)
    {
        return DBM.ExecStore("sp_LogHistory_SelectByObjectGUID", new { ObjectGuid }, out dt);
    }
}