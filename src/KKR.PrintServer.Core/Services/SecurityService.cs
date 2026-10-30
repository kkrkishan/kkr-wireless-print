using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Common;
using KKR.PrintServer.Core.Models;
using System.Text.Json;

namespace KKR.PrintServer.Core.Services;

public class SecurityService : ISecurityService
{
    private readonly object _pinLock = new();
    private string _currentPin = string.Empty;
    private string _previousPin = string.Empty;
    private DateTime _pinExpiresAtUtc;
    private DateTime _previousPinExpiresAtUtc;
    private readonly int _pinExpiryMinutes;
    private readonly string _spoolTempDirectory;
    private readonly string _clientsFilePath;

    private readonly ConcurrentDictionary<string, PairedClient> _clientsById = new();
    private readonly ConcurrentDictionary<string, string> _tokenHashToClientId = new();
    private readonly ConcurrentDictionary<string, (Guid JobId, DateTime TimestampUtc)> _idempotencyCache = new();

    // Magic byte signatures
    private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegMagic1 = { 0xFF, 0xD8, 0xFF };

    public string CurrentPin
    {
        get
        {
            lock (_pinLock)
            {
                if (string.IsNullOrEmpty(_currentPin) || DateTime.UtcNow > _pinExpiresAtUtc)
                {
                    GenerateNewPinInternal();
                }
                return _currentPin;
            }
        }
    }

    public DateTime PinExpiresAtUtc
    {
        get
        {
            lock (_pinLock)
            {
                if (string.IsNullOrEmpty(_currentPin) || DateTime.UtcNow > _pinExpiresAtUtc)
                {
                    GenerateNewPinInternal();
                }
                return _pinExpiresAtUtc;
            }
        }
    }

    public SecurityService(string spoolTempDirectory, int pinExpiryMinutes = 15)
    {
        _spoolTempDirectory = spoolTempDirectory;
        _pinExpiryMinutes = pinExpiryMinutes;

        if (!Directory.Exists(_spoolTempDirectory))
        {
            Directory.CreateDirectory(_spoolTempDirectory);
        }

        _clientsFilePath = Path.Combine(AppEnvironment.GetDataDirectory(), "paired_clients.json");
        LoadPairedClients();
        GenerateNewPin();
    }

    public string GenerateNewPin()
    {
        lock (_pinLock)
        {
            return GenerateNewPinInternal();
        }
    }

    private string GenerateNewPinInternal()
    {
        if (!string.IsNullOrEmpty(_currentPin))
        {
            _previousPin = _currentPin;
            _previousPinExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        }

        int num = RandomNumberGenerator.GetInt32(100000, 1000000);
        _currentPin = num.ToString("D6");
        _pinExpiresAtUtc = DateTime.UtcNow.AddMinutes(_pinExpiryMinutes);
        AppLogger.Instance.Security("Auth", $"New pairing PIN generated: {_currentPin} (Valid until {_pinExpiresAtUtc.ToLocalTime():HH:mm:ss})");
        return _currentPin;
    }

    public PairResponse AuthenticatePairing(string pin, string deviceName, string clientType, string clientIp)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            AppLogger.Instance.Security("Auth", $"Pairing failed: Empty PIN submitted from {clientIp}");
            return new PairResponse { Success = false, ErrorMessage = "PIN cannot be empty." };
        }

        string cleanPin = pin.Trim();

        lock (_pinLock)
        {
            bool isCurrentMatch = !string.IsNullOrEmpty(_currentPin) &&
                                  DateTime.UtcNow <= _pinExpiresAtUtc &&
                                  IsPinEqual(_currentPin, cleanPin);

            bool isPreviousMatch = !string.IsNullOrEmpty(_previousPin) &&
                                   DateTime.UtcNow <= _previousPinExpiresAtUtc &&
                                   IsPinEqual(_previousPin, cleanPin);

            if (!isCurrentMatch && !isPreviousMatch)
            {
                AppLogger.Instance.Security("Auth", $"Pairing failed: Invalid PIN submitted '{cleanPin}' (Current: '{_currentPin}', Previous: '{_previousPin}') from {clientIp} ({deviceName})");
                return new PairResponse { Success = false, ErrorMessage = "Invalid or expired PIN. Check current PIN on server." };
            }
        }

        // Generate high-entropy bearer token
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        string tokenHash = ComputeHash(token);

        var client = new PairedClient
        {
            ClientId = Guid.NewGuid().ToString("N"),
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Unknown Device" : deviceName.Trim(),
            ClientType = string.IsNullOrWhiteSpace(clientType) ? "Android" : clientType.Trim(),
            TokenHash = tokenHash,
            PairedAtUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
            LastKnownIp = clientIp,
            IsRevoked = false
        };

        _clientsById[client.ClientId] = client;
        _tokenHashToClientId[tokenHash] = client.ClientId;

        AppLogger.Instance.Security("Auth", $"Client successfully paired: {client.DeviceName} ({client.ClientId}) from {clientIp}");

        return new PairResponse
        {
            Success = true,
            Token = token,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(90),
            ClientId = client.ClientId,
            ServerVersion = "1.0.0"
        };
    }

    public bool ValidateToken(string? bearerToken, out PairedClient? client)
    {
        client = null;
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return false;
        }

        string cleanToken = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearerToken.Substring(7).Trim()
            : bearerToken.Trim();

        if (string.IsNullOrWhiteSpace(cleanToken))
        {
            return false;
        }

        string hash = ComputeHash(cleanToken);
        if (_tokenHashToClientId.TryGetValue(hash, out var clientId) && _clientsById.TryGetValue(clientId, out var pairedClient))
        {
            if (pairedClient.IsRevoked)
            {
                return false;
            }

            pairedClient.LastSeenUtc = DateTime.UtcNow;
            client = pairedClient;
            return true;
        }

        return false;
    }

    public void RevokeClient(string clientId)
    {
        if (_clientsById.TryGetValue(clientId, out var client))
        {
            client.IsRevoked = true;
            _tokenHashToClientId.TryRemove(client.TokenHash, out _);
            AppLogger.Instance.Security("Auth", $"Client revoked: {client.DeviceName} ({clientId})");
        }
    }





    private void LoadPairedClients()
    {
        try
        {
            if (File.Exists(_clientsFilePath))
            {
                string json = File.ReadAllText(_clientsFilePath, Encoding.UTF8);
                var clients = JsonSerializer.Deserialize<List<PairedClient>>(json);
                if (clients != null)
                {
                    foreach (var c in clients)
                    {
                        if (!c.IsRevoked)
                        {
                            _clientsById[c.ClientId] = c;
                            if (!string.IsNullOrEmpty(c.TokenHash))
                            {
                                _tokenHashToClientId[c.TokenHash] = c.ClientId;
                            }
                        }
                    }
                    AppLogger.Instance.Info("Auth", $"Loaded {_clientsById.Count} previously paired client(s) from persistent storage.");
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warning("Auth", $"Failed to load paired clients: {ex.Message}");
        }
    }

    private void SavePairedClients()
    {
        try
        {
            string dir = Path.GetDirectoryName(_clientsFilePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var list = _clientsById.Values.ToList();
            string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_clientsFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warning("Auth", $"Failed to save paired clients: {ex.Message}");
        }
    }

    public IReadOnlyList<PairedClient> GetPairedClients() => _clientsById.Values.ToList();

    public bool IsDuplicateRequest(string? idempotencyKey, out Guid existingJobId)
    {
        existingJobId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return false;
        }

        // Clean old items (> 15 minutes)
        var cutoff = DateTime.UtcNow.AddMinutes(-15);
        foreach (var kvp in _idempotencyCache)
        {
            if (kvp.Value.TimestampUtc < cutoff)
            {
                _idempotencyCache.TryRemove(kvp.Key, out _);
            }
        }

        if (_idempotencyCache.TryGetValue(idempotencyKey.Trim(), out var entry))
        {
            existingJobId = entry.JobId;
            AppLogger.Instance.Warning("Deduplication", $"Duplicate print request detected with idempotency key '{idempotencyKey}'. Existing JobId: {existingJobId}");
            return true;
        }

        return false;
    }

    public void RegisterIdempotency(string? idempotencyKey, Guid jobId)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _idempotencyCache[idempotencyKey.Trim()] = (jobId, DateTime.UtcNow);
        }
    }

    public ValidationResult ValidateUploadedDocument(Stream stream, string fileName, long length, long maxAllowedBytes)
    {
        if (length <= 0)
        {
            return new ValidationResult(false, "Uploaded file is empty (0 bytes).", PrintDocumentType.Unknown);
        }

        if (length > maxAllowedBytes)
        {
            double mb = Math.Round((double)maxAllowedBytes / (1024 * 1024), 1);
            return new ValidationResult(false, $"File size exceeds the server limit of {mb} MB.", PrintDocumentType.Unknown);
        }

        if (!stream.CanRead)
        {
            return new ValidationResult(false, "Cannot read file stream.", PrintDocumentType.Unknown);
        }

        // Read up to first 16 bytes for magic number verification
        byte[] header = new byte[16];
        long originalPosition = stream.CanSeek ? stream.Position : 0;
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        int bytesRead = stream.Read(header, 0, header.Length);
        if (stream.CanSeek)
        {
            stream.Position = originalPosition;
        }

        if (bytesRead < 4)
        {
            return new ValidationResult(false, "File header is too short to determine format.", PrintDocumentType.Unknown);
        }

        // Check PDF
        if (StartsWith(header, PdfMagic))
        {
            return new ValidationResult(true, null, PrintDocumentType.Pdf);
        }

        // Check PNG
        if (StartsWith(header, PngMagic))
        {
            return new ValidationResult(true, null, PrintDocumentType.Png);
        }

        // Check JPEG
        if (StartsWith(header, JpegMagic1))
        {
            return new ValidationResult(true, null, PrintDocumentType.Jpeg);
        }

        // Fallback to extension check if magic bytes slightly deviate, but warn
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext == ".pdf")
        {
            return new ValidationResult(false, "File has .pdf extension but invalid PDF magic header signature.", PrintDocumentType.Unknown);
        }
        if (ext is ".jpg" or ".jpeg")
        {
            return new ValidationResult(false, "File has JPEG extension but invalid image header signature.", PrintDocumentType.Unknown);
        }
        if (ext == ".png")
        {
            return new ValidationResult(false, "File has .png extension but invalid PNG header signature.", PrintDocumentType.Unknown);
        }

        return new ValidationResult(false, $"Unsupported file format '{ext}'. Only PDF, JPG, JPEG, and PNG are allowed.", PrintDocumentType.Unknown);
    }

    public string CreateSafeTempFile(string extension)
    {
        if (!extension.StartsWith("."))
        {
            extension = "." + extension;
        }

        string safeName = $"spool_{Guid.NewGuid():N}{extension}";
        return Path.Combine(_spoolTempDirectory, safeName);
    }

    public void DeleteSafeTempFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                AppLogger.Instance.Info("Storage", $"Purged temporary spool file: {Path.GetFileName(filePath)}");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warning("Storage", $"Could not delete temporary file {filePath}: {ex.Message}");
        }
    }

    private static bool StartsWith(byte[] source, byte[] prefix)
    {
        if (source.Length < prefix.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (source[i] != prefix[i]) return false;
        }
        return true;
    }

    private static string ComputeHash(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static bool IsPinEqual(string expected, string actual)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
