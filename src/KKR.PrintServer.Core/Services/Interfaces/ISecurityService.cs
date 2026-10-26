using System.IO;
using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public record ValidationResult(bool IsValid, string? ErrorMessage, PrintDocumentType DetectedType);

public interface ISecurityService
{
    string CurrentPin { get; }
    DateTime PinExpiresAtUtc { get; }
    string GenerateNewPin();
    PairResponse AuthenticatePairing(string pin, string deviceName, string clientType, string clientIp);
    bool ValidateToken(string? bearerToken, out PairedClient? client);
    void RevokeClient(string clientId);
    IReadOnlyList<PairedClient> GetPairedClients();
    bool IsDuplicateRequest(string? idempotencyKey, out Guid existingJobId);
    void RegisterIdempotency(string? idempotencyKey, Guid jobId);
    ValidationResult ValidateUploadedDocument(Stream stream, string fileName, long length, long maxAllowedBytes);
    string CreateSafeTempFile(string extension);
    void DeleteSafeTempFile(string? filePath);
}
