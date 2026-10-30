using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using KKR.PrintServer.Core.Logging;

namespace KKR.PrintServer.Core.Services;

public class CertificateService : ICertificateService
{
    private readonly string _certDirectory;
    private readonly string _pfxPath;
    private readonly string _cerPath;
    private readonly string _pemPath;
    private const string CertPassword = "KKRPrintServerSecurePassword2026!";
    private X509Certificate2? _cachedCert;

    public CertificateService(string certDirectory)
    {
        _certDirectory = certDirectory;
        if (!Directory.Exists(_certDirectory))
        {
            Directory.CreateDirectory(_certDirectory);
        }

        _pfxPath = Path.Combine(_certDirectory, "kkr-print-server.pfx");
        _cerPath = Path.Combine(_certDirectory, "kkr-print-server.cer");
        _pemPath = Path.Combine(_certDirectory, "kkr-print-server.pem");
    }

    public string GetCertificateCerPath() => _cerPath;
    public string GetCertificatePemPath() => _pemPath;

    public string GetCertificateThumbprint()
    {
        var cert = GetOrCreateServerCertificate(new[] { "localhost", Environment.MachineName }, Array.Empty<string>());
        return cert.Thumbprint;
    }

    public X509Certificate2 GetOrCreateServerCertificate(string[] dnsNames, string[] ipAddresses)
    {
        if (_cachedCert != null)
        {
            return _cachedCert;
        }

        if (File.Exists(_pfxPath))
        {
            try
            {
                var existing = new X509Certificate2(_pfxPath, CertPassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
                if (existing.NotAfter > DateTime.UtcNow.AddDays(30))
                {
                    _cachedCert = existing;
                    AppLogger.Instance.Info("Security", $"Loaded existing TLS certificate: {existing.Thumbprint}");
                    return existing;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Warning("Security", $"Existing certificate could not be loaded, generating fresh one: {ex.Message}");
            }
        }

        var newCert = GenerateSelfSignedCertificate(dnsNames, ipAddresses);
        _cachedCert = newCert;
        return newCert;
    }

    private X509Certificate2 GenerateSelfSignedCertificate(string[] dnsNames, string[] ipAddresses)
    {
        AppLogger.Instance.Info("Security", "Generating new self-signed TLS certificate for local network...");

        using var rsa = RSA.Create(2048);
        var subject = new X500DistinguishedName("CN=Print-Ofo-Server, O=Print Ofo, OU=Print Services");
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Basic Constraints: End-entity
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

        // Key Usage: Digital Signature and Key Encipherment
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

        // Enhanced Key Usage: Server Authentication
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.1") }, false)); // Server Auth

        // Subject Alternative Names (SAN)
        var sanBuilder = new SubjectAlternativeNameBuilder();
        foreach (var dns in dnsNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(dns))
            {
                sanBuilder.AddDnsName(dns);
            }
        }

        foreach (var ipStr in ipAddresses.Distinct())
        {
            if (IPAddress.TryParse(ipStr, out var ip))
            {
                sanBuilder.AddIpAddress(ip);
            }
        }

        // Always include localhost and loopback
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);

        request.CertificateExtensions.Add(sanBuilder.Build());

        // Valid for 3 years
        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = DateTimeOffset.UtcNow.AddYears(3);

        using var generatedCert = request.CreateSelfSigned(notBefore, notAfter);

        // Export PFX
        byte[] pfxBytes = generatedCert.Export(X509ContentType.Pfx, CertPassword);
        File.WriteAllBytes(_pfxPath, pfxBytes);

        // Export CER (DER)
        byte[] cerBytes = generatedCert.Export(X509ContentType.Cert);
        File.WriteAllBytes(_cerPath, cerBytes);

        // Export PEM
        var pemBuilder = new StringBuilder();
        pemBuilder.AppendLine("-----BEGIN CERTIFICATE-----");
        pemBuilder.AppendLine(Convert.ToBase64String(cerBytes, Base64FormattingOptions.InsertLineBreaks));
        pemBuilder.AppendLine("-----END CERTIFICATE-----");
        File.WriteAllText(_pemPath, pemBuilder.ToString());

        var certWithKey = new X509Certificate2(pfxBytes, CertPassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
        AppLogger.Instance.Info("Security", $"TLS certificate generated successfully. Thumbprint: {certWithKey.Thumbprint}");
        return certWithKey;
    }
}
