namespace KKR.PrintServer.Core.Models;

public class PrintJobRequest
{
    public string PrinterName { get; set; } = string.Empty;
    public PrintDocumentType DocumentType { get; set; } = PrintDocumentType.Unknown;
    public int Copies { get; set; } = 1;
    public string? PageRange { get; set; }
    public PrintOrientation Orientation { get; set; } = PrintOrientation.Auto;
    public string? PaperSizeName { get; set; }
    public PrintColorMode ColorMode { get; set; } = PrintColorMode.Auto;
    public PrintScalingMode ScalingMode { get; set; } = PrintScalingMode.FitPage;
    public bool CenterOnPage { get; set; } = true;
    public string? IdempotencyKey { get; set; }
    public string? ClientId { get; set; }
    public string? ClientDeviceName { get; set; }
    public string? SourceIp { get; set; }
    public string DocumentName { get; set; } = "Document";
}

public class PrintJobRecord
{
    public Guid JobId { get; set; } = Guid.NewGuid();
    public int? WindowsSpoolerJobId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientDeviceName { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public PrintDocumentType DocumentType { get; set; } = PrintDocumentType.Unknown;
    public long FileSizeBytes { get; set; }
    public int PageCount { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Accepted;
    public string? StatusMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public string SettingsSummary { get; set; } = string.Empty;
    public PrintJobRequest Request { get; set; } = new();
    public string? TempFilePath { get; set; }
}
