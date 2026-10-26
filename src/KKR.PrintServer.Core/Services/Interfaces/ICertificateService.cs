using System.Security.Cryptography.X509Certificates;

namespace KKR.PrintServer.Core.Services;

public interface ICertificateService
{
    X509Certificate2 GetOrCreateServerCertificate(string[] dnsNames, string[] ipAddresses);
    string GetCertificateThumbprint();
    string GetCertificatePemPath();
    string GetCertificateCerPath();
}
