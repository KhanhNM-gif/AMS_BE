
using BSS;
using BSS.DataValidator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

public class Partner
{    
    public int PartnerID { get; set; }
    public Guid PartnerGUID { get; set; }
    public string PartnerName { get; set; }
    public string PartnerToken { get; set; }
    public string PartnerIP { get; set; }

    public static string GetOne(Guid PartnerGUID, out Partner o)
    {
        return DBM.GetOne("usp_Partner_SelectByPartnerGUID", new { PartnerGUID }, out o);
    }    
}