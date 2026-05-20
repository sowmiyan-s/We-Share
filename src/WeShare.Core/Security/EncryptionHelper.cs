using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WeShare.Core.Security
{
    public static class EncryptionHelper
    {
        // App-wide key for basic transport encryption. 
        // In a production app, this would be negotiated per session via Diffie-Hellman.
        private static readonly byte[] SystemKey = SHA256.HashData(Encoding.UTF8.GetBytes("WeShare_Secure_P2P_2026"));

        public static byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = SystemKey;
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            
            // Prepend IV
            ms.Write(aes.IV, 0, aes.IV.Length);
            
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
            }
            return ms.ToArray();
        }

        public static byte[] Decrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = SystemKey;
            
            byte[] iv = new byte[aes.BlockSize / 8];
            Array.Copy(data, 0, iv, 0, iv.Length);
            
            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(new MemoryStream(data, iv.Length, data.Length - iv.Length), decryptor, CryptoStreamMode.Read))
            {
                cs.CopyTo(ms);
            }
            return ms.ToArray();
        }

        public static CryptoStream CreateEncryptionStream(Stream baseStream, bool leaveOpen = false)
        {
            // NOTE: Do NOT use 'using var aes' here — the AES instance must stay alive
            // as long as the returned CryptoStream is in use. The encryptor holds a
            // reference to the AES object internally, so disposing AES before the
            // CryptoStream finishes would corrupt all in-flight transfers.
            var aes = Aes.Create();
            aes.Key = SystemKey;
            aes.GenerateIV();
            
            // Write IV to the stream first so the receiver can decrypt
            baseStream.Write(aes.IV, 0, aes.IV.Length);
            
            var encryptor = aes.CreateEncryptor();
            return new CryptoStream(baseStream, encryptor, CryptoStreamMode.Write, leaveOpen);
        }

        public static CryptoStream CreateDecryptionStream(Stream baseStream, bool leaveOpen = false)
        {
            // NOTE: Do NOT use 'using var aes' here — same reason as CreateEncryptionStream.
            // The AES instance must outlive the returned CryptoStream.
            var aes = Aes.Create();
            aes.Key = SystemKey;
            
            byte[] iv = new byte[aes.BlockSize / 8];
            int read = 0;
            while (read < iv.Length)
            {
                int r = baseStream.Read(iv, read, iv.Length - read);
                if (r <= 0) throw new IOException("Could not read IV");
                read += r;
            }
            
            var decryptor = aes.CreateDecryptor(aes.Key, iv);
            return new CryptoStream(baseStream, decryptor, CryptoStreamMode.Read, leaveOpen);
        }
    }
}
