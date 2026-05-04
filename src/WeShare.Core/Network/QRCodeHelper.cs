using System.IO;
using QRCoder;

namespace WeShare.Core.Network
{
    public static class QRCodeHelper
    {
        /// <summary>Generates a QR code PNG as a byte array for the given URL.</summary>
        public static byte[] GeneratePng(string text, int pixelsPerModule = 8)
        {
            using var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            var qr   = new PngByteQRCode(data);
            return qr.GetGraphic(pixelsPerModule,
                darkColorRgba:  new byte[] { 14,  165, 233, 255 },   // #0EA5E9 (cyan)
                lightColorRgba: new byte[] {  8,  14,  26,  255 });   // #080E1A (bg)
        }
    }
}
