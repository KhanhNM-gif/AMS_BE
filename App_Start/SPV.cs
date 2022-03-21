using BSS;
using BSS.DataValidator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class SPV
{
    public int ID { get; set; }
    public int UserID { get; set; }
    public Guid PageGuid { get; set; }
    public string SPVObject { get; set; }

    static SPV()
    {
        DataValidator.AddRules(new string[] { "SPVObject" }, new CheckSPVValidationRule());
        DataValidator.AddRules(new string[] { "ControlId", "ControlType" }, new LengthInRangeValidationRule(0, 50),
            new NoSpecialCharacterExtensionValidationRule());
        DataValidator.AddRules(new string[] { "ControlValue" }, new LengthInRangeValidationRule(1, 4000));
    }

    public string Insert()
    {
        return Insert(UserID, PageGuid, SPVObject);
    }
    public static string Insert(int UserID, Guid PageGuid, List<SPVControl> lt)
    {
        return Insert(UserID, PageGuid, JsonConvert.SerializeObject(lt));
    }
    public static string Insert(int UserID, Guid PageGuid, string SPVObject)
    {
        return DBM.ExecStore("sp_SPV_Insert", new { UserID, PageGuid, SPVObject });
    }

    public static string Clear(string userId)
    {
        return DBM.ExecStore("sp_SPV_ClearSPV", new { UserId = userId });
    }

    public static string Get(int UserID, Guid PageGuid, out List<SPVControl> lt)
    {
        lt = new List<SPVControl>();

        string SPVObject;
        string msg = Get(UserID, PageGuid, out SPVObject);
        if (msg.Length > 0) return msg;

        if (string.IsNullOrEmpty(SPVObject)) return "";

        if (SPVObject != null) lt = JsonConvert.DeserializeObject<List<SPVControl>>(SPVObject);
        return "";
    }
    public static string Get(int UserID, Guid PageGuid, out string SPVObject)
    {
        return DBM.ExecStore("sp_SPV_Select", new { UserID, PageGuid }, out SPVObject);
    }

    public static void InsertTab(int UserID, int TabID)
    {
        List<SPVControl> lt = new List<SPVControl>();
        SPVControl c = new SPVControl(SPVControl.CONTROLID_PAGEMAIN_TAB, TabID.ToString());
        lt.Add(c);
        string msg = Insert(UserID, Constants.PageGUID.MAIN, lt);
        if (msg.Length > 0) Log.WriteErrorLog(msg, new { UserID, TabID });
    }
    public static string GetPageMain(int UserID, out int tabID)
    {
        tabID = 0;

        List<SPVControl> lt;
        string msg = Get(UserID, Constants.PageGUID.MAIN, out lt);
        if (msg.Length > 0) return msg;

        var vSPVControl = lt.Where(v => v.ControlId == SPVControl.CONTROLID_PAGEMAIN_TAB);
        if (vSPVControl.Count() == 0) tabID = Constants.TabID.QLTS;
        else tabID = int.Parse(vSPVControl.First().ControlValue);

        return msg;
    }
    public static string GetSearchItem(int UserID, Guid guid, out object item)
    {

        item = null;

        string SPVObject;
        string msg = Get(UserID, guid, out SPVObject);
        if (msg.Length > 0) return msg;

        if (SPVObject != null) { item = JsonConvert.DeserializeObject<object>(SPVObject); }
        return "";
    }
    public static void InsertSPVSearchAsset(int userID, Guid pageGuid, object assetSearch)
    {
        InsertSPVSearchObject(userID, pageGuid, assetSearch);
    }
    public static void InsertSPVSearchItem(int userID, Guid pageGuid, object itemSearch)
    {
        InsertSPVSearchObject(userID, pageGuid, itemSearch);
    }
    private static void InsertSPVSearchObject(int UserID, Guid PageGuid, object SPVObjectSearch)
    {
        string msg = Insert(UserID, PageGuid, JsonConvert.SerializeObject(SPVObjectSearch));
        if (msg.Length > 0) Log.WriteErrorLog(msg, new { UserID, PageGuid });
    }

    public class CheckSPVValidationRule : ValidationRule
    {
        public CheckSPVValidationRule(int ruleType = RuleTypeCB1) : base(ruleType, "Không phải đối tượng spv hợp lệ") { }
        override public Result Validate(object o)
        {
            if (o == null) return Result.GetResultError("object is null");

            List<SPVControl> listSC = null;
            string msg = BSS.Convertor.JsonToObject(o, out listSC);
            if (msg.Length > 0) return Result.GetResultError(this);

            if (listSC == null) return Result.GetResultError(this);

            var ps = typeof(SPVControl).GetProperties();
            List<string> es = new List<string>();
            string val = string.Empty;
            foreach (var sc in listSC)
            {
                es.Clear();
                foreach (var p in ps)
                {
                    val = p.GetValue(sc, null) as string;
                    if (val == null) return string.Format("{0} is null", p.Name).ToResultError();

                    if (string.IsNullOrWhiteSpace(val)) es.Add(p.Name);
                }
                msg = DataValidator.Validate(sc, es.ToArray()).ToErrorMessage();
                if (msg.Length > 0) return Result.GetResultError(this);
            }

            return Result.ResultOk;
        }
    }
}

public class SPVControl
{
    public string ControlId { get; set; }
    public string ControlValue { get; set; }
    public string ControlType { get; set; }

    public const string CONTROLID_PAGEMAIN_TAB = "Tab";

    public SPVControl()
    {

    }
    public SPVControl(string ControlId, string ControlValue, string ControlType)
    {
        this.ControlId = ControlId;
        this.ControlValue = ControlValue;
        this.ControlType = ControlType;
    }
    public SPVControl(string ControlId, string ControlValue)
    {
        this.ControlId = ControlId;
        this.ControlValue = ControlValue;
        this.ControlType = "";
    }
}