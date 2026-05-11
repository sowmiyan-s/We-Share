using System;
using QRCoder;

namespace WeShare.Core.Services
{
    public static class QrCodeService
    {
        public static byte[] GenerateQrCodePng(string text)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }
    }
}
