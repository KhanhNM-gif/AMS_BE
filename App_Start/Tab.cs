using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class Tab
{
    public int TabID { get; set; }
    public string TabName { get; set; }
    public int NumberShow { get; set; }
    public bool IsVisitTab { get; set; }
    public bool IsFocusTab { get; set; }
    public string GroupTabName { get; set; }

    public static List<Tab> GetListTab()
    {
        return new List<Tab>
        {
             new Tab { TabName= "Quản lý Tài sản", TabID = Constants.TabID.QLTS,GroupTabName = "Tài sản"},
             new Tab { TabName= "Quản lý Phiếu đề xuất", TabID = Constants.TabID.QLPDX,GroupTabName = "Tài sản"},
             new Tab { TabName= "Quản lý Vụ việc", TabID = Constants.TabID.QLVV,GroupTabName = "Tài sản"},
             new Tab { TabName= "Quản lý Phiếu kiểm kê", TabID = Constants.TabID.QLKKTS,GroupTabName = "Tài sản"},
             new Tab { TabName= "Sơ đồ tài sản", TabID = Constants.TabID.SDTS,GroupTabName = "Tài sản"},

             new Tab { TabName= "Quản lý Vật phẩm", TabID = Constants.TabID.QLVP,GroupTabName = "Vật phẩm"},
             new Tab { TabName= "Quản lý Phiếu đề xuất", TabID = Constants.TabID.QLPDXVP,GroupTabName = "Vật phẩm"},
             new Tab { TabName= "Nhập kho", TabID = Constants.TabID.QLPNK,GroupTabName = "Vật phẩm"},
             new Tab { TabName= "Xuất kho", TabID = Constants.TabID.QLPXK,GroupTabName = "Vật phẩm"},
             new Tab { TabName= "Quản lý Kiểm kê", TabID = Constants.TabID.QLKKVP,GroupTabName = "Vật phẩm"},
             new Tab { TabName= "Kho Vật phẩm", TabID = Constants.TabID.KHOVP,GroupTabName = "Vật phẩm"},

             new Tab { TabName= "Tài sản", TabID = Constants.TabID.BCTK_TS,GroupTabName = "Báo cáo thông kê"},
             new Tab { TabName= "Vật phẩm", TabID = Constants.TabID.BCTK_VP,GroupTabName = "Báo cáo thông kê"},

             new Tab { TabName= "Quản lý Loại tài sản", TabID = Constants.TabID.LTS,GroupTabName = "Cấu hình"},
             new Tab { TabName= "Quản lý Nơi để", TabID = Constants.TabID.ND,GroupTabName = "Cấu hình"},
             new Tab { TabName= "Quản lý Tổ chức", TabID = Constants.TabID.TC,GroupTabName = "Cấu hình"},
             new Tab { TabName= "Quản Lý Loại vụ việc", TabID = Constants.TabID.LVV,GroupTabName = "Cấu hình"},
             new Tab { TabName= "Quản Lý Loại vật phẩm", TabID = Constants.TabID.LVP,GroupTabName = "Cấu hình"},
             new Tab { TabName= "Quản Lý Kho", TabID = Constants.TabID.KHO,GroupTabName = "Cấu hình"},

             new Tab { TabName= "Quản Lý Phòng Ban", TabID = Constants.TabID.QLPB,GroupTabName = "Quản trị hệ thống"},
             new Tab { TabName= "Quản Lý Chức vụ", TabID = Constants.TabID.QLCV,GroupTabName = "Quản trị hệ thống"},
             new Tab { TabName= "Quản Lý Người Dùng", TabID = Constants.TabID.QLND,GroupTabName = "Quản trị hệ thống"},
             new Tab { TabName= "Quản lý Nhóm quyền", TabID = Constants.TabID.PQ,GroupTabName = "Quản trị hệ thống"},
             new Tab { TabName= "Tra cứu Log", TabID = Constants.TabID.TCL,GroupTabName = "Quản trị hệ thống"},
        };
    }

    public static string GetTabName(int TabID)
    {
        var vTab = GetListTab().Where(v => v.TabID == TabID);
        if (vTab.Count() == 0) return "";
        else return vTab.First().TabName;
    }
}