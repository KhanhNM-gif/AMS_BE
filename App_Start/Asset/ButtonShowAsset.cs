public class ButtonShowAsset
{
    public bool Add { get; set; } //Thêm
    public bool Edit { get; set; } //Sửa
    public bool Delete { get; set; } //Xóa
    public bool Restore { get; set; } //Khôi phục
    public bool SendApprove { get; set; }//Gửi duyệt
    public bool CancelSendApprove { get; set; }//Hủy gửi duyệt
    public bool Approve { get; set; }//Duyệt
    public bool Handover { get; set; }//Bàn giao tài sản
    public bool ComfirmHandover { get; set; }//Xác nhận bàn giao tài sản
    public bool Return { get; set; }//Trả tài sản
    public bool ComfirmReturn { get; set; }//Xác nhận trả tài sản
    public bool CancelReturn { get; set; }//Trả tài sản
    public bool Revoke { get; set; }//thu hồi
    public bool Inventory { get; set; }//Kiểm kê
    public bool WaitInventory { get; set; }//Chờ xác nhận Kiểm kê 
    public bool CancelInventory { get; set; }//Hủy chờ Kiểm kê
    public bool UpdateInventory { get; set; }//Cập nhật Kiểm kê
    public bool SendApproveInventory { get; set; }//Gửi duyệt Kiểm kê
    public bool ComfirmInventory { get; set; }//Duyệt Kiểm kê
    public bool ViewHistory { get; set; }//Xem lịch sử
    public bool InQRCode { get; set; }//in tem
    public bool InHandover { get; set; }//in biên bản bàn giao
    public bool MovePlaceLiquidate { get; set; }//chuyển kho thanh lý
}
public class ButtonShowPDX
{
    public bool Edit { get; set; } //Sửa
    public bool Delete { get; set; } //Xóa
    public bool Restore { get; set; } //Khôi phục
    public bool SendApprove { get; set; }// chuyển duyệt
    public bool Approved { get; set; }//Duyệt
    public bool ViewHistory { get; set; }//xem lịch sử
}

public class ButtonShowPDXVP
{
    public bool Edit { get; set; } //Sửa
    public bool Delete { get; set; } //Xóa
    public bool View { get; set; } //Xóa
    public bool Restore { get; set; } //Khôi phục
    //public bool SendApprove { get; set; }// chuyển duyệt
    public bool Approved { get; set; }//Duyệt
    public bool ViewHistory { get; set; }//xem lịch sử
}

public class ButtonShowQLK
{
    public bool CreateItemProposalForm { get; set; } //Sửa
}

public class ButtonShowHandlingPDXVP
{
    public bool Handle { get; set; } //Xử lý
    public bool TransferHandle { get; set; } //Chuyển xử lý
}

public class ButtonShowPKK
{
    public bool Edit { get; set; } //Sửa
    public bool Delete { get; set; } //Xóa
    public bool Restore { get; set; } //Khôi phục
    public bool SendApprove { get; set; }// chuyển duyệt
    public bool Approved { get; set; }//Duyệt
    public bool ViewHistory { get; set; }//xem lịch sử
}
public class ButtonShowPKKVP
{
    public bool Edit { get; set; } //Sửa
    public bool Delete { get; set; } //Xóa
    public bool Restore { get; set; } //Khôi phục
    public bool Approved { get; set; }//Duyệt
    public bool ViewHistory { get; set; }//xem lịch sử
}