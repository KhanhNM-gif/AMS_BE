using System;

/// <summary>
/// Summary description for Constants
/// </summary>
public class Constants
{
    public class PageGUID
    {
        public static Guid MAIN = Guid.Parse("fec90645-3c0c-42fc-bb7a-85132efead67"),
                           TAI_SAN = Guid.Parse("19306e3f-0f7c-47cc-85d2-17ef623d9666"),
                           VAT_PHAM = Guid.Parse("4d210065-6ed1-4ccd-b4c8-2b23dbd68492");
        public static string GetPage(int tabID, out Guid page)
        {
            switch (tabID)
            {
                case TabID.QLTS:
                    page = Constants.PageGUID.TAI_SAN;
                    break;
                case TabID.QLVP:
                    page = Constants.PageGUID.VAT_PHAM;
                    break;
                default:
                    page = Guid.Empty;
                    return "Không tồn tại tabID = " + tabID;
            }

            return "";
        }
    }

    public class ObjectType
    {
        public const int Asset = 1,
                         User = 2,
                         Issue = 3,
                         ProposalForm = 4,
                         AssetInventory = 5,
                         Place = 6,
                         Diagram = 7,
                         Item = 8,
                         Store = 9,
                         ItemProposalForm = 10,
                         InventoryStore = 11;
    }
    public class TabID
    {
        public const int QLTS = 1,
                        QLPDX = 2,
                        QLVV = 3,
                        QLVP = 4,
                        QLPNK = 5,
                        QLPXK = 6,
                        QLKKTS = 7,
                        QLKKVP = 8,
                        LTS = 9,
                        ND = 10,
                        TC = 11,
                        LVV = 12,
                        LVP = 13,
                        KHO = 14,
                        QLPB = 15,
                        QLCV = 16,
                        QLND = 17,
                        PQ = 18,
                        SDTS = 19,
                        TCL = 20,
                        PNK = 21,
                        QLPDXVP = 22,
                        KHOVP = 23,
                        BCTK_TS = 24,
                        BCTK_VP = 25;
    }
    public class TypePropertyData
    {
        public const int TEXT = 1,
                         INT = 2,
                         DATE = 3,
                         SELECT = 4,
                         LIST = 5,
                         CHECKBOX = 6,
                         PASSWORD = 7;
    }
    public class AssetTypeGroup
    {
        public const int TAISAN = 1,
                         VATPHAM = 2;
    }
    public class GroupTab
    {
        public const int TAISAN = 1,
                         VATPHAM = 2,
                         CAUHINH = 3,
                         QUANTRIHETHONG = 4;
    }
    public class StatusAsset
    {
        public const int MT = 1,
                         ĐX = 2,
                         CD = 3,
                         TC = 4,
                         ĐD_TK = 5,
                         CXN_BG = 6,
                         ĐSD = 7,
                         CXN_T = 8,
                         KXN_T = 9,
                         //CXN_KK = 10,
                         KX = 11,
                         DTL = 12,
                         SDNHH = 13,
                         DDNHH = 14;
    }

    public class ItemProposalFormType
    {
        public const int DXN = 1,
                         DXX = 2;
    }

    public class StatusPDX
    {
        public const int MT = 1,
                         CD = 2,
                         DD = 3,
                         TC = 4,
                         DBG = 5,
                         DX = 6,
                         TL = 7;
    }

    public class StatusPDXVP
    {
        public const int MT = 1,    //Mới tạo
                         CXL = 2,   //Chờ xử lý
                                    //TC = 4,    //Từ chối
                         TL = 5,    //Trả lại
                         DX = 6,    //Đã xóa
                         DHT = 7;   //Đã hoàn thành
    }
    public class StatusPKKVP
    {
        public const int MT = 1,    //Mới tạo
                         CXL = 2,   //Chờ xử lý
                         TL = 3,    //Trả lại
                         DX = 4,    //Đã xóa
                         DAXONG = 5;   //Đã hoàn thành
    }
    public class OrganizationType
    {
        public const int NCC = 1,
                         NBH_SC = 2,
                         NBT_BD = 3,
                         HSX = 5;
    }
    public class IssueGroup
    {
        public const int SU_CO = 1,
                         BAOHANH_SUACHUA = 2,
                         BAOTRI_BAODUONG = 3;
    }
    public class IssueDate
    {
        public const int NGAY = 1,
                         TUAN = 2,
                         THANG = 3,
                         NAM = 3;
    }
    public class IssueStatus
    {
        public const int X = 1, // đã xóa
                         CXL = 2, // chưa xử lý
                         DXL = 3, // đang xử lý
                         DX = 4,// đã xong
                         KXL = 5; // không xử lý

    }
    public class ItemStatus
    {
        public const int MT = 1,
                         DX = 2,
                         CD = 3,
                         DDTK = 4,
                         TC = 5;
    }
    public class SexType
    {
        public const int MALE = 1,
                        FEMALE = 2;
    }
    public class RoleGroup
    {
        public const int ADMIN = 1,
                        QLK = 2,
                        KTTC = 3,
                        NQL = 4;
    }
    public class TransferHandling
    {
        public const int PDX = 1,
                        PXK = 2,
                        PNK = 3,
                        PKK = 4,
                        PDXVP = 5,
                        PKKVP = 6;
    }
    public class StatusPKD
    {
        public const int MT = 1,
                         TL = 2,
                         ĐX = 3,
                         TC = 4,
                         CD = 5;
    }

    public class StatusPKK
    {
        public const int MT = 1,
                         TL = 2,
                         ĐX = 3,
                         TC = 4,
                         CD = 5,
                         ĐD = 6,
                         TH = 7;
    }
    public class StatusItem
    {
        public const int MT = 1,
                         ĐX = 2,
                         CD = 3,
                         ĐD_TK = 4,
                         TC = 5,
                         SHH_SD = 6,
                         QH_SD = 7,
                         KX = 8;
    }
    public class ItemDate
    {
        public const int DAY = 1,
                         WEEK = 2,
                         MONTH = 3,
                         YEAR = 4;
    }
    public class StatusPNK
    {
        public const int MT = 1,
                         ĐX = 2,
                         CD = 3,
                         TL = 4,
                         TC = 5,
                         ĐD = 6;
    }
}
