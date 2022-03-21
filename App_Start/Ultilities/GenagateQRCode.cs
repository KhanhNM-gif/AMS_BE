using QRCoder;
using System.Drawing;

namespace ASM_API.App_Start.Ultilities
{
    public static class GenagateQRCode
    {
        public static string GetQRCode(string value, out byte[] arrQRCode)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.M);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            arrQRCode = ImageToByte(qrCodeImage);

            return "";
        }

        private static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
    }
}