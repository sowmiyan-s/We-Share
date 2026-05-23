using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace WeShare.Core.Security
{
    public static class CertificateHelper
    {
        private static X509Certificate2? _cachedCert;
        private static readonly object _lock = new();

        public static X509Certificate2 GetSelfSignedCertificate()
        {
            if (_cachedCert != null) return _cachedCert;

            lock (_lock)
            {
                if (_cachedCert != null) return _cachedCert;

                using (var rsa = RSA.Create(2048))
                {
                    var request = new CertificateRequest(
                        "CN=WeShare",
                        rsa,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    // Add standard extensions required for TLS Web Server Authentication
                    request.CertificateExtensions.Add(
                        new X509BasicConstraintsExtension(false, false, 0, false));

                    request.CertificateExtensions.Add(
                        new X509KeyUsageExtension(
                            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                            true));

                    request.CertificateExtensions.Add(
                        new X509EnhancedKeyUsageExtension(
                            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") },
                            false));

                    var selfSigned = request.CreateSelfSigned(
                        DateTimeOffset.UtcNow.AddDays(-1),
                        DateTimeOffset.UtcNow.AddYears(5));

                    // Export as PFX and reload it to bind the private key properly for SslStream
                    var pfxBytes = selfSigned.Export(X509ContentType.Pfx);
                    _cachedCert = new X509Certificate2(pfxBytes, (string?)null, X509KeyStorageFlags.Exportable);
                    return _cachedCert;
                }
            }
        }
    }
}
