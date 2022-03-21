using BSS.DataValidator;
using System;
using System.Collections;
using System.Globalization;

/// <summary>
/// Summary description for IVANValidator
/// </summary>
static public class AMSValidator
{
    static AMSValidator()
    {
        DataValidator.AddRules("Username", new LengthInRangeValidationRule(3, 30));
        DataValidator.AddRules("Password", new LengthInRangeValidationRule(6, 30));

        DataValidator.AddRules(new string[] { "FullName" }, new NotEmptyValidationRule(),
                                                                new LengthInRangeValidationRule(3, 50));

        DataValidator.AddRules("BirthDate", new EmptyOrLengthInRangeValidationRule(5, 50));
        DataValidator.AddRules(new string[] { "UserDeptName", "UserNameCreate", "IsActive" }, new EmptyOrLengthInRangeValidationRule(0, 200));
        DataValidator.AddRules("SexName", new EmptyOrLengthInRangeValidationRule(0, 5));
        DataValidator.AddRules("Note", new EmptyOrLengthInRangeValidationRule(0, 255));
        DataValidator.AddRules("Opinion", new EmptyOrLengthInRangeValidationRule(10, 255));
        DataValidator.AddRules("StatusName", new EmptyOrLengthInRangeValidationRule(0, 20));

        DataValidator.AddRules(new string[] { "DeptFullName", "DeptFullNameParent", "ItemStatusIDs", "ItemTypeIDs" }, new EmptyOrLengthInRangeValidationRule(1, 1000));
        DataValidator.AddRules(new string[] { "Sex", "IsRequired", "IsUnit" }, new InRangeValidationRule(0, 10));
        DataValidator.AddRules(new string[] { "ObjectGuid" }, new CheckGUIDValidationRule());

        DataValidator.AddRules(new string[] { "UserIDDelegacy", "UserIDDelegacyed","DeptID", "DeptIDParent",
            "PositionID", "PositionIDParent","PlaceID", "PlaceIDParent", "DepotIDParent", "OrganizationTypeOrder",
            "OrganizationTypeID", "OrganizationAddressCity", "OrganizationAddressDistrict", "OrganizationAddressVillage",
            "AssetTypeID", "AssetTypeGroupID","RoleGroupID", "QLTS","QLVV", "QLPDX", "QLVP", "QLPNK", "QLPXK", "QLKKTS","QLKKVP","LTS","ND","TC","LVV","LVP","KHO","QLPB","QLCV","QLND","PQ","SDTS","TCL","QLPDXVP","KHOVP","BCTK_TS","BCTK_VP",
            "UserIDCreate","UserCreateID","AssetID","UserIDProcess","AssetTypeID","AssetQuantity","AssetYearProduction", "AssetTimeWarranty","ProducerID",
            "UserIDManager","AssetStatusID","AssetTypePropertyDataID", "ItemUnitStatusID",
            "UserIDHolding","AssetHandOverID","UserIDHandOver","ProcessType","AssetHandOverStatusID",
            "AssetApproveID","AssetApproveStatusID","UserIDApprove","AssetReturnID","UserIDAssetReturn",
            "AssetReturnStatusID","ProposalFormID","UserIDHandling","ProposalFormStatusID","ProposalFormAssetID",
            "Quantity","ProducerID","SupplierID","AssetRevokeID","UserIDAssetRevoke","Page","Size","IssueTypeID", "ItemTypeID",
            "IssueGroupID","CategorySearch","UserIDHandedOver","UserIDReturned","AccountID","UnitID","IssueStatusID", "ItemStatusID",
            "UserIDApprover", "StatusID", "ID","IssueID", "InventoryID", "ID", "StateID", "DepotID", "DiagramID", "LogTypeID","UserIDPerform",
            "UserID", "UserApproveID", "StoreStatusID", "StoreTypeID", "TransferDirectionID", "ItemID", "StoreID"}, new InRangeValidationRule(0, 100000000));

        DataValidator.AddRules(new string[] { "PlaceIDs", "AssetStatusIDs", "AssetTypeIDs", "UserIDHoldings", "SupplierIDs", "AssetCodes" }, new EmptyOrLengthInRangeValidationRule(0, 500));

        DataValidator.AddRules(new string[] { "AssetTypePropertyName" }, new LengthInRangeValidationRule(3, 255));
        DataValidator.AddRules(new string[] { "IsDelete", "IsSendApprove" }, new BoolValidationRule());

        DataValidator.AddRules(new string[] { "OrganizationObjectID","UrlAvatar","DeptName","DeptShortName","PositionName","PlaceName","DepotName","OrganizationTypeName","OrganizationName",
            "AssetTypeName","AssetName","AssetImagePath","AssetTypePropertyName","AssetColor","HandOverContent", "ItemName",
            "ApproveContent","ReturnContent","ProposalFormReason","RevokeContent","IssueTypeName","Reason","Content","AssetRevokeComment", "InventoryName", "InventoryCode" }, new LengthInRangeValidationRule(0, 255));
        DataValidator.AddRules(new string[] { "OrganizationCode", "AssetTypeCode", "AssetCode", "PlaceCode", "DepotCode", "ItemCode",
            "ProposalFormCode","IssueTypeCode","PositionCode","DeptCode","TaxCode","IssueCode","ItemImportReceiptCode" }, new LengthInRangeValidationRule(2, 50));
        DataValidator.AddRules(new string[] { "OrganizationAddressDetail", "AssetDescription", "OrganizationNote", "AssetTypeDescription", "PlaceDescription", "DepotDescription" }, new EmptyOrLengthInRangeValidationRule(1, 2000));
        DataValidator.AddRules(new string[] { "MobileUser", "OrganizationMobile" }, new EmptyOrIsAMobileNumberValidationRule());
        DataValidator.AddRules(new string[] { "TextSearch" }, new EmptyOrLengthInRangeValidationRule(0, 255));

        DataValidator.AddRules(new string[] { "DTDD", "ĐTDĐ", "Mobile", "Phone", "PhoneNumber" }, new NotEmptyValidationRule(), new LengthInRangeValidationRule(10, 11), new IsAMobileNumberValidationRule());

        DataValidator.AddRules(new string[] { "CreateDate", "LastUpdate", "BeginDateFrom", "BeginDateTo",
            "EndDateFrom", "EndDateTo", "FinishDateFrom", "FinishDateTo","BeginDate", "EndDate",
            "NewBeginDate", "NewEndDate", "ExtendDate", "FinishDate", "AssetDateIn", "AssetDateBuy",
            "HandOverDate", "ReturnDate", "AssetDateFrom", "AssetDateTo", "IssueBeginDate", "IssueEndDate",
            "ItemDateFrom", "ItemDateTo", "DateFrom", "DateTo","InputDate", "InvoiceDate", "RequestDate", "ExportDate", "ManufacturingDate", "ExpiryDate","ImportDate","VouchersDate"}, new NullOrDatetimeValidationRule());

        DataValidator.AddRules(new string[] { "OrganizationID" }, new InRangeValidationRule(0, 10000));
        DataValidator.AddRules(new string[] { "OrganizationObjectID", "AssetSerial", "AssetModel", "StoreCode" }, new LengthInRangeValidationRule(1, 50));
        DataValidator.AddRules(new string[] { "Invoice" }, new LengthInRangeValidationRule(0, 50));

        DataValidator.AddRules(new string[] { "DiagramName" }, new EmptyOrLengthInRangeValidationRule(1, 100));

        DataValidator.AddRules(new string[] { "RoleGroupName", "DiagramUrl" }, new EmptyOrLengthInRangeValidationRule(1, 500));

        DataValidator.AddRules(new string[] { "ListRoleDescription", "ProcessResult" }, new EmptyOrLengthInRangeValidationRule(1, 500));

        DataValidator.AddRules("FileName", new LengthInRangeValidationRule(1, 100));
        DataValidator.AddRules("FileExttension", new LengthInRangeValidationRule(1, 20));
        DataValidator.AddRules("FileSize", new InRangeValidationRule(1, 200000000));

        DataValidator.AddRules(new string[] { "PageSize", "CurrentPage" }, new InRangeValidationRule(0, 1000000));
        DataValidator.AddRules(new string[] { "LtStoreItemInput" }, new CheckListValidationRule());


        DataValidator.AddRules(new string[] { "ItemProposalFormID", "ItemUnitID", "UserIDHandling", "ItemProposalFormStatusID", "ItemProposalFormID", "ItemTypeID", "Quantity", "ManufacturerID" }, new InRangeValidationRule(0, 100000000));

        DataValidator.AddRules(new string[] { "VAT" }, new InRangeValidationRule(0, 100));

        DataValidator.AddRules(new string[] { "ItemProposalFormReason" }, new LengthInRangeValidationRule(3, 250));


        DataValidator.AddRules(new string[] { "InventoryStoreID", "UserHandingID" }, new InRangeValidationRule(0, 100000000));

        DataValidator.AddRules(new string[] { "QuantityActual", "QuantityInStore " }, new InRangeValidationRule(0, 100000));
        DataValidator.AddRules(new string[] { "InventoryStoreName", "Content" }, new NotEmptyValidationRule(),
                                                                new LengthInRangeValidationRule(3, 250));
        DataValidator.AddRules(new string[] { "VouchersNumber", "InvoiceNumber" }, new EmptyOrLengthInRangeValidationRule(0, 20));

    }
    static public void Init() { }

    public class NullOrInRangeValidationRule : ValidationRule
    {
        public int min { get; set; }
        public int max { get; set; }

        public NullOrInRangeValidationRule(int min, int max, int ruleType = RuleTypeCB1)
            : base(ruleType, "Giá trị không nằm trong khoảng: " + min + " - " + max + ".")
        {
            this.min = min;
            this.max = max;
        }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.ResultOk;

            if (string.IsNullOrEmpty(o.ToString())) return BSS.Result.ResultOk;

            int value = int.Parse(o.ToString());
            if (value < min || value > max)
                return BSS.Result.GetResultError(this);

            return BSS.Result.ResultOk;
        }
    }
    public class EmptyOrLengthInRangeValidationRule : ValidationRule
    {
        public long min { get; set; }
        public long max { get; set; }

        public EmptyOrLengthInRangeValidationRule(int min, int max, int ruleType = RuleTypeCB1)
            : base(ruleType, "Chiều dài xâu không nằm trong khoảng: " + min + " - " + max + ".")
        {
            this.min = min;
            this.max = max;
        }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.ResultOk;

            if (string.IsNullOrEmpty(o.ToString())) return BSS.Result.ResultOk;

            int len = o.ToString().Length;
            if (len < min || len > max)
                return BSS.Result.GetResultError(this);

            return BSS.Result.ResultOk;
        }
    }
    public class AllCharacterIsNumberValidationRule : ValidationRule
    {
        string check = "0123456789";
        public AllCharacterIsNumberValidationRule(int ruleType = RuleTypeCB1) : base(ruleType, "Chứa ký tự không phải là số") { }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.GetResultError("object is null");

            if (!DataValidator.AllChar(o.ToString(), check)) return BSS.Result.GetResultError(this);

            return BSS.Result.ResultOk;
        }
    }
    public class EmptyOrLengthValidationRule : ValidationRule
    {
        public long length { get; set; }
        public long max { get; set; }

        public EmptyOrLengthValidationRule(int length, int ruleType = RuleTypeCB1)
            : base(ruleType, "Chiều dài xâu không bằng " + length)
        {
            this.length = length;
        }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.ResultOk;

            if (string.IsNullOrEmpty(o.ToString())) return BSS.Result.ResultOk;

            int len = o.ToString().Length;
            if (len != length)
                return BSS.Result.GetResultError(this);

            return BSS.Result.ResultOk;
        }
    }
    public class BoolValidationRule : ValidationRule
    {
        public BoolValidationRule(int ruleType = RuleTypeCB1) : base(ruleType, "Không phải là bool") { }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.GetResultError("object is null");

            bool b;
            if (!bool.TryParse(o.ToString(), out b)) return BSS.Result.GetResultError(string.Format("\"{0}\" không phải là bool", o));

            return BSS.Result.ResultOk;
        }
    }
    public class MonthYearValidationRule : ValidationRule
    {
        public DateTime DateTimeMin { set; get; }
        public DateTime DateTimeMax { set; get; }
        public MonthYearValidationRule(DateTime DateTimeMin, DateTime DateTimeMax, int ruleType = RuleTypeCB1) : base(ruleType, "Không phải là tháng năm (Ví dụ: 01/2019)")
        {
            this.DateTimeMin = DateTimeMin;
            this.DateTimeMax = DateTimeMax;
        }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.GetResultError("object is null");
            if (o.ToString() == "") return BSS.Result.ResultOk;

            DateTime dt;
            if (!DateTime.TryParseExact("01/" + o.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return BSS.Result.GetResultError("Không đúng định dạng tháng/năm (Ví dụ: 01/2019)");

            if (dt < DateTimeMin || dt > DateTimeMax) return BSS.Result.GetResultError(string.Format("\"{0}\" không nằm trong dải từ {1} đến {2}", o, DateTimeMin.ToString("MM/yyyy"), DateTimeMax.ToString("MM/yyyy")));

            return BSS.Result.ResultOk;
        }
    }
    public class EmptyOrIsAListMobileNumberValidationRule : ValidationRule
    {
        string check = "0123456789";

        public EmptyOrIsAListMobileNumberValidationRule(int ruleType = RuleTypeCB1) : base(ruleType, "Không phải số điện thoại di động") { }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.ResultOk;

            string s = o.ToString();

            if (string.IsNullOrEmpty(s)) return BSS.Result.ResultOk;

            string[] mobile = s.Split(',');
            bool IsMobile = false;
            for (int i = 0; i < mobile.Length; i++)
            {
                if (DataValidator.AllChar(mobile[i], check) &&
                (((mobile[i].StartsWith("01") || mobile[i].StartsWith("028") || mobile[i].StartsWith("023") || mobile[i].StartsWith("02")) && (mobile[i].Length == 11)) || ((mobile[i].StartsWith("03") || mobile[i].StartsWith("05") || mobile[i].StartsWith("07") || mobile[i].StartsWith("08") || mobile[i].StartsWith("09")) && (mobile[i].Length == 10))))
                    IsMobile = true;
                else
                    return BSS.Result.GetResultError(this);
            }
            if (IsMobile) return BSS.Result.ResultOk;

            return BSS.Result.GetResultError(this);
        }
    }
    public class EmptyOrIsAMobileNumberValidationRule : ValidationRule
    {
        string check = "0123456789";

        public EmptyOrIsAMobileNumberValidationRule(int ruleType = RuleTypeCB1) : base(ruleType, "Không phải số điện thoại di động") { }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.ResultOk;

            string s = o.ToString();

            if (string.IsNullOrEmpty(s)) return BSS.Result.ResultOk;

            if (DataValidator.AllChar(s, check) &&
                (((s.StartsWith("01") || s.StartsWith("028") || s.StartsWith("023") || s.StartsWith("02")) && (s.Length == 11)) || ((s.StartsWith("03") || s.StartsWith("05") || s.StartsWith("07") || s.StartsWith("08") || s.StartsWith("09")) && (s.Length == 10)))) return BSS.Result.ResultOk;

            return BSS.Result.GetResultError(this);
        }
    }
    public class CheckDatetimeValidationRule : ValidationRule
    {
        public CheckDatetimeValidationRule(int ruleType = RuleTypeCB1) : base(ruleType, "Không đúng định dạng Datetime") { }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.GetResultError("object is null");

            string s = o.ToString();
            DateTime dt = DateTime.Now;
            if (DateTime.TryParse(s, out dt))
                if (dt >= new DateTime(1900, 01, 01) && dt < new DateTime(2086, 12, 31)) return BSS.Result.ResultOk;
                else return BSS.Result.GetResultError("Không nằm trong khoảng từ 01/01/1900 đến 31/12/2086");

            return BSS.Result.GetResultError(this);
        }
    }
    public class NullOrDatetimeValidationRule : ValidationRule
    {
        public NullOrDatetimeValidationRule(int ruleType = RuleTypeCB1) : base(ruleType, "Không đúng định dạng Datetime") { }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.ResultOk;

            string s = o.ToString();
            DateTime dt = DateTime.Now;
            if (DateTime.TryParse(s, out dt))
                if (dt == new DateTime(0001, 01, 01) || (dt >= new DateTime(1900, 01, 01) && dt < new DateTime(2086, 12, 31))) return BSS.Result.ResultOk;
                else return BSS.Result.GetResultError("Không nằm trong khoảng từ 01/01/1900 đến 31/12/2086");

            return BSS.Result.GetResultError(this);
        }
    }
    public class CheckGUIDValidationRule_IVAN : ValidationRule
    {
        public long min { get; set; }
        public long max { get; set; }

        public CheckGUIDValidationRule_IVAN(int ruleType = RuleTypeCB1) : base(ruleType, "Không phải GUID") { }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.GetResultError("object is null");
            if (string.IsNullOrWhiteSpace(o.ToString())) return BSS.Result.ResultOk;

            try
            {
                Guid.Parse(o.ToString());
            }
            catch (Exception)
            {
                return BSS.Result.GetResultError(this);
            }

            return BSS.Result.ResultOk;
        }
    }

    public class CheckListValidationRule : ValidationRule
    {
        public CheckListValidationRule(int ruleType = RuleTypeCB1) : base(ruleType, "Không phải GUID") { }
        override public BSS.Result Validate(object o)
        {
            if (o == null) return BSS.Result.GetResultError("object is null");
            try
            {
                IList list = (IList)o;
            }
            catch (Exception)
            {
                return BSS.Result.GetResultError(this);
            }

            return BSS.Result.ResultOk;
        }
    }
}