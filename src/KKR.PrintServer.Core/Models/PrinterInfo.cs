namespace KKR.PrintServer.Core.Models;

public record PaperSizeInfo
{
    public string Name { get; init; } = string.Empty;
    public int WidthHundredthsOfInch { get; init; }
    public int HeightHundredthsOfInch { get; init; }
    public double WidthMm => Math.Round(WidthHundredthsOfInch * 0.254, 1);
    public double HeightMm => Math.Round(HeightHundredthsOfInch * 0.254, 1);
}

public record PrinterCapabilities
{
    public List<PaperSizeInfo> SupportedPaperSizes { get; init; } = new();
    public bool CanDuplex { get; init; }
    public bool SupportsColor { get; init; }
    public List<string> SupportedResolutions { get; init; } = new();
    public int MaxCopies { get; init; } = 999;
}

public class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string PortName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsOnline { get; set; } = true;
    public bool IsBusy { get; set; }
    public bool IsInError { get; set; }
    public bool IsOutOfPaper { get; set; }
    public bool IsPaperJammed { get; set; }
    public string StatusDescription { get; set; } = "Ready";
    public int QueuedJobCount { get; set; }
    public PrinterCapabilities Capabilities { get; set; } = new();
}
