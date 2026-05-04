using System;
using System.IO;
using System.Security.Cryptography;

namespace WeShare.Core.Security
{
    public class EncryptionHelper
    {
        public static byte[] GenerateSessionKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            return aes.Key;
        }

        public static byte[] Encrypt(byte[] data, byte[] key, out byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        public static byte[] Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherText);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var msPlain = new MemoryStream();
            
            cs.CopyTo(msPlain);
            return msPlain.ToArray();
        }
    }
}
