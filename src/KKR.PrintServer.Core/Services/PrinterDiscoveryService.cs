using System.Drawing.Printing;
using System.Printing;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public class PrinterDiscoveryService : IPrinterDiscoveryService
{
    private readonly object _lock = new();
    private List<PrinterInfo> _cachedPrinters = new();
    private DateTime _lastRefreshedUtc = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    public IReadOnlyList<PrinterInfo> GetPrinters(bool refresh = false)
    {
        lock (_lock)
        {
            if (!refresh && _cachedPrinters.Any() && DateTime.UtcNow - _lastRefreshedUtc < CacheDuration)
            {
                return _cachedPrinters.ToList();
            }

            _cachedPrinters = QueryPrintersInternal();
            _lastRefreshedUtc = DateTime.UtcNow;
            return _cachedPrinters.ToList();
        }
    }

    public PrinterInfo? GetPrinter(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var printers = GetPrinters(refresh: false);
        return printers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsPrinterReady(string name, out string? reasonIfNotReady)
    {
        reasonIfNotReady = null;
        var info = GetPrinter(name);
        if (info == null)
        {
            reasonIfNotReady = $"Printer '{name}' is not installed on this system.";
            return false;
        }

        if (!info.IsOnline)
        {
            reasonIfNotReady = $"Printer '{name}' is currently offline. Ensure USB cable is connected and powered on.";
            return false;
        }

        if (info.IsOutOfPaper)
        {
            reasonIfNotReady = $"Printer '{name}' reports out of paper.";
            return false;
        }

        if (info.IsPaperJammed)
        {
            reasonIfNotReady = $"Printer '{name}' has a paper jam.";
            return false;
        }

        if (info.IsInError)
        {
            reasonIfNotReady = $"Printer '{name}' is reporting a driver error ({info.StatusDescription}).";
            return false;
        }

        return true;
    }

    private List<PrinterInfo> QueryPrintersInternal()
    {
        var result = new List<PrinterInfo>();
        var queueDict = new Dictionary<string, PrintQueue>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var printServer = new LocalPrintServer();
            var printQueues = printServer.GetPrintQueues(new[]
            {
                EnumeratedPrintQueueTypes.Local,
                EnumeratedPrintQueueTypes.Connections
            });

            foreach (var pq in printQueues)
            {
                queueDict[pq.Name] = pq;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warning("Discovery", $"Failed to query LocalPrintServer print queues: {ex.Message}");
        }

        foreach (string printerName in PrinterSettings.InstalledPrinters)
        {
            try
            {
                var settings = new PrinterSettings { PrinterName = printerName };
                if (!settings.IsValid)
                {
                    continue;
                }

                queueDict.TryGetValue(printerName, out var queue);

                bool isOffline = queue?.IsOffline ?? false;
                bool isBusy = queue?.IsBusy ?? false;
                bool isInError = queue?.IsInError ?? false;
                bool isOutOfPaper = queue?.HasPaperProblem ?? false;
                bool isPaperJammed = queue?.IsPaperJammed ?? false;
                int jobCount = queue?.NumberOfJobs ?? 0;

                string statusDesc = BuildStatusDescription(queue, isOffline, isInError, isOutOfPaper, isPaperJammed, isBusy);

                var caps = ExtractCapabilities(settings);

                var printerInfo = new PrinterInfo
                {
                    Name = printerName,
                    DriverName = queue?.QueueDriver?.Name ?? "Unknown Driver",
                    PortName = queue?.QueuePort?.Name ?? "USB / Local Port",
                    IsDefault = settings.IsDefaultPrinter,
                    IsOnline = !isOffline,
                    IsBusy = isBusy,
                    IsInError = isInError,
                    IsOutOfPaper = isOutOfPaper,
                    IsPaperJammed = isPaperJammed,
                    StatusDescription = statusDesc,
                    QueuedJobCount = jobCount,
                    Capabilities = caps
                };

                result.Add(printerInfo);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.Error("Discovery", $"Error inspecting printer '{printerName}': {ex.Message}");
            }
        }

        return result;
    }

    private static string BuildStatusDescription(PrintQueue? queue, bool isOffline, bool isInError, bool isOutOfPaper, bool isPaperJammed, bool isBusy)
    {
        if (isPaperJammed) return "Paper Jam";
        if (isOutOfPaper) return "Out of Paper";
        if (isOffline) return "Offline";
        if (isInError) return "Error";
        if (isBusy) return "Busy";

        if (queue != null)
        {
            if (queue.IsPrinting) return "Printing";
            if (queue.IsWaiting) return "Waiting";
            if (queue.IsWarmingUp) return "Warming Up";
            if (queue.IsProcessing) return "Processing";
            if (queue.IsManualFeedRequired) return "Manual Feed Required";
            if (queue.IsDoorOpened) return "Door Open";
            if (queue.IsTonerLow) return "Toner / Ink Low";
            if (queue.QueueStatus != PrintQueueStatus.None)
            {
                return queue.QueueStatus.ToString();
            }
        }

        return "Ready";
    }

    private static PrinterCapabilities ExtractCapabilities(PrinterSettings settings)
    {
        var paperSizes = new List<PaperSizeInfo>();
        try
        {
            foreach (PaperSize ps in settings.PaperSizes)
            {
                paperSizes.Add(new PaperSizeInfo
                {
                    Name = ps.PaperName,
                    WidthHundredthsOfInch = ps.Width,
                    HeightHundredthsOfInch = ps.Height
                });
            }
        }
        catch
        {
            // Fallback standard sizes
            paperSizes.Add(new PaperSizeInfo { Name = "A4", WidthHundredthsOfInch = 827, HeightHundredthsOfInch = 1169 });
            paperSizes.Add(new PaperSizeInfo { Name = "Letter", WidthHundredthsOfInch = 850, HeightHundredthsOfInch = 1100 });
        }

        var resolutions = new List<string>();
        try
        {
            foreach (PrinterResolution pr in settings.PrinterResolutions)
            {
                resolutions.Add(pr.ToString());
            }
        }
        catch
        {
            // Ignore
        }

        return new PrinterCapabilities
        {
            SupportedPaperSizes = paperSizes,
            CanDuplex = settings.CanDuplex,
            SupportsColor = settings.SupportsColor,
            SupportedResolutions = resolutions,
            MaxCopies = settings.MaximumCopies > 0 ? settings.MaximumCopies : 999
        };
    }
}
