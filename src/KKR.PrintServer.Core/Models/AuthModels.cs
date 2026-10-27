using KKR.PrintServer.Core.Common;

namespace KKR.PrintServer.Core.Models;

public class PairRequest
{
    public string Pin { get; set; } = string.Empty;
    public string DeviceName { get; set; } = "Android Device";
    public string ClientType { get; set; } = "Android";
}

public class PairResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? ClientId { get; set; }
    public string ServerVersion { get; set; } = "1.0.0";
    public string? ErrorMessage { get; set; }
}

public class PairedClient
{
    public string ClientId { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceName { get; set; } = string.Empty;
    public string ClientType { get; set; } = "Android";
    public string TokenHash { get; set; } = string.Empty;
    public DateTime PairedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
    public string? LastKnownIp { get; set; }
}

public class ServerConfig
{
    public string ListenIpAddress { get; set; } = "0.0.0.0";
    public int HttpPort { get; set; } = 8080;
    public int HttpsPort { get; set; } = 8443;
    public bool EnableHttps { get; set; } = true;
    public bool EnableHttpFallback { get; set; } = true;
    public long MaxUploadSizeBytes { get; set; } = 52_428_800; // 50 MB
    public int PinExpiryMinutes { get; set; } = 15;
    public string SpoolTempDirectory { get; set; } = AppEnvironment.SpoolDirectory;
    public string CertDirectory { get; set; } = AppEnvironment.CertsDirectory;
    public string LogDirectory { get; set; } = AppEnvironment.LogsDirectory;
}
