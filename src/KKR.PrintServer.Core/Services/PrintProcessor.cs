using System.Drawing;
using System.Drawing.Printing;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public class PrintProcessor : IPrintProcessor
{
    private readonly IPrinterDiscoveryService _printerDiscovery;
    private readonly ISecurityService _securityService;

    public PrintProcessor(IPrinterDiscoveryService printerDiscovery, ISecurityService securityService)
    {
        _printerDiscovery = printerDiscovery;
        _securityService = securityService;
    }

    public async Task ProcessPrintJobAsync(PrintJobRecord record, CancellationToken cancellationToken = default)
    {
        record.StartedAtUtc = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(record.TempFilePath) || !File.Exists(record.TempFilePath))
        {
            record.Status = JobStatus.Failed;
            record.ErrorMessage = "Print job payload file not found on disk.";
            record.CompletedAtUtc = DateTime.UtcNow;
            AppLogger.Instance.Error("PrintProcessor", $"Job {record.JobId} failed: {record.ErrorMessage}");
            return;
        }

        // Validate printer state
        if (!_printerDiscovery.IsPrinterReady(record.PrinterName, out var notReadyReason))
        {
            record.Status = JobStatus.Failed;
            record.ErrorMessage = notReadyReason;
            record.CompletedAtUtc = DateTime.UtcNow;
            _securityService.DeleteSafeTempFile(record.TempFilePath);
            AppLogger.Instance.Error("PrintProcessor", $"Job {record.JobId} rejected: {notReadyReason}");
            return;
        }

        var printerInfo = _printerDiscovery.GetPrinter(record.PrinterName);

        try
        {
            record.Status = JobStatus.Processing;
            record.StatusMessage = "Rendering and spooling document to Windows print queue...";
            AppLogger.Instance.Info("PrintProcessor", $"Beginning processing job {record.JobId} ({record.DocumentName}) for printer '{record.PrinterName}'");

            if (record.DocumentType == PrintDocumentType.Pdf)
            {
                await PrintPdfDocumentAsync(record, printerInfo, cancellationToken);
            }
            else if (record.DocumentType is PrintDocumentType.Jpeg or PrintDocumentType.Png)
            {
                PrintImageDocument(record, printerInfo, cancellationToken);
            }
            else
            {
                record.Status = JobStatus.Failed;
                record.ErrorMessage = $"Unsupported document format '{record.DocumentType}'.";
                record.CompletedAtUtc = DateTime.UtcNow;
            }
        }
        catch (OperationCanceledException)
        {
            record.Status = JobStatus.Cancelled;
            record.StatusMessage = "Job cancelled by user request.";
            record.CompletedAtUtc = DateTime.UtcNow;
            AppLogger.Instance.Warning("PrintProcessor", $"Job {record.JobId} cancelled.");
        }
        catch (Exception ex)
        {
            record.Status = JobStatus.Failed;
            record.ErrorMessage = $"Windows print error: {ex.Message}";
            record.CompletedAtUtc = DateTime.UtcNow;
            AppLogger.Instance.Error("PrintProcessor", $"Print execution failed for job {record.JobId}", ex);
        }
        finally
        {
            _securityService.DeleteSafeTempFile(record.TempFilePath);
        }
    }

    public Task<PrintJobRecord> PrintTestPageAsync(string printerName, string serverIp, int port, bool httpsEnabled, CancellationToken cancellationToken = default)
    {
        var info = _printerDiscovery.GetPrinter(printerName);
        string tempPath = _securityService.CreateSafeTempFile(".png");

        using (var bitmap = TestPageGenerator.GenerateTestPageImage(printerName, info, serverIp, port, httpsEnabled))
        {
            bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var record = new PrintJobRecord
        {
            JobId = Guid.NewGuid(),
            ClientId = "SYSTEM-CONSOLE",
            ClientDeviceName = "Local Desktop Dashboard",
            ClientIp = "127.0.0.1",
            PrinterName = printerName,
            DocumentName = $"Test_Page_{printerName.Replace(" ", "_")}.png",
            DocumentType = PrintDocumentType.Png,
            FileSizeBytes = new FileInfo(tempPath).Length,
            PageCount = 1,
            Status = JobStatus.Accepted,
            StatusMessage = "Test page queued.",
            TempFilePath = tempPath,
            Request = new PrintJobRequest
            {
                PrinterName = printerName,
                DocumentType = PrintDocumentType.Png,
                Copies = 1,
                ScalingMode = PrintScalingMode.FitPage,
                Orientation = PrintOrientation.Portrait,
                CenterOnPage = true,
                DocumentName = "Test Page"
            }
        };

        return Task.FromResult(record);
    }

    private async Task PrintPdfDocumentAsync(PrintJobRecord record, PrinterInfo? printerInfo, CancellationToken cancellationToken)
    {
        using var pdfRenderer = new PdfRenderer(record.TempFilePath!);
        await pdfRenderer.InitializeAsync();

        int totalDocPages = (int)pdfRenderer.PageCount;
        if (totalDocPages == 0)
        {
            throw new InvalidOperationException("PDF contains 0 renderable pages.");
        }

        var pagesToPrint = PageRangeParser.Parse(record.Request.PageRange, totalDocPages);
        record.PageCount = pagesToPrint.Count;

        using var printDoc = new PrintDocument();
        ConfigurePrintDocumentSettings(printDoc, record, printerInfo);

        // Auto-detect orientation from first page if requested
        if (record.Request.Orientation == PrintOrientation.Auto)
        {
            var firstPageSize = pdfRenderer.GetPageSize((uint)(pagesToPrint[0] - 1));
            printDoc.DefaultPageSettings.Landscape = firstPageSize.Width > firstPageSize.Height;
        }

        int currentSequenceIndex = 0;
        Bitmap? currentBitmap = null;

        printDoc.BeginPrint += (s, e) =>
        {
            currentSequenceIndex = 0;
        };

        printDoc.PrintPage += (s, e) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                e.Cancel = true;
                return;
            }

            int pageNumber = pagesToPrint[currentSequenceIndex];
            uint zeroBasedIndex = (uint)(pageNumber - 1);

            // Render high DPI bitmap for this page synchronously inside the PrintPage event
            currentBitmap = pdfRenderer.RenderPageToBitmapAsync(zeroBasedIndex, 300).GetAwaiter().GetResult();

            var printableBounds = e.MarginBounds;
            // Use printable area if margins are not set or default
            if (printableBounds.Width <= 0 || printableBounds.Height <= 0)
            {
                printableBounds = e.PageBounds;
            }

            ImageRenderer.DrawImageToPrinter(e.Graphics!, printableBounds, currentBitmap, record.Request.ScalingMode, record.Request.CenterOnPage);

            currentBitmap.Dispose();
            currentBitmap = null;

            currentSequenceIndex++;
            e.HasMorePages = currentSequenceIndex < pagesToPrint.Count;
        };

        printDoc.EndPrint += (s, e) =>
        {
            currentBitmap?.Dispose();
        };

        printDoc.Print();

        record.Status = JobStatus.Completed;
        record.StatusMessage = "Spooling completed to Windows Print Spooler. Job accepted by printer queue.";
        record.CompletedAtUtc = DateTime.UtcNow;
        AppLogger.Instance.Info("PrintProcessor", $"Spooling completed successfully for job {record.JobId} ({record.PageCount} pages).");
    }

    private void PrintImageDocument(PrintJobRecord record, PrinterInfo? printerInfo, CancellationToken cancellationToken)
    {
        record.PageCount = 1;

        using var image = ImageRenderer.LoadImageWithExifOrientation(record.TempFilePath!);
        using var printDoc = new PrintDocument();
        ConfigurePrintDocumentSettings(printDoc, record, printerInfo);

        if (record.Request.Orientation == PrintOrientation.Auto)
        {
            printDoc.DefaultPageSettings.Landscape = image.Width > image.Height;
        }

        printDoc.PrintPage += (s, e) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                e.Cancel = true;
                return;
            }

            var printableBounds = e.MarginBounds;
            if (printableBounds.Width <= 0 || printableBounds.Height <= 0)
            {
                printableBounds = e.PageBounds;
            }

            ImageRenderer.DrawImageToPrinter(e.Graphics!, printableBounds, image, record.Request.ScalingMode, record.Request.CenterOnPage);
            e.HasMorePages = false;
        };

        printDoc.Print();

        record.Status = JobStatus.Completed;
        record.StatusMessage = "Spooling completed to Windows Print Spooler. Job accepted by printer queue.";
        record.CompletedAtUtc = DateTime.UtcNow;
        AppLogger.Instance.Info("PrintProcessor", $"Spooling completed successfully for image job {record.JobId}.");
    }

    private static void ConfigurePrintDocumentSettings(PrintDocument printDoc, PrintJobRecord record, PrinterInfo? printerInfo)
    {
        printDoc.PrinterSettings.PrinterName = record.PrinterName;
        printDoc.DocumentName = string.IsNullOrWhiteSpace(record.DocumentName) ? "KKR_Wireless_Print_Job" : record.DocumentName;

        // Copies
        int copies = Math.Clamp(record.Request.Copies, 1, printerInfo?.Capabilities.MaxCopies ?? 99);
        printDoc.PrinterSettings.Copies = (short)copies;

        // Color Mode
        if (record.Request.ColorMode == PrintColorMode.Grayscale)
        {
            printDoc.DefaultPageSettings.Color = false;
        }
        else if (record.Request.ColorMode == PrintColorMode.Color && (printerInfo?.Capabilities.SupportsColor ?? false))
        {
            printDoc.DefaultPageSettings.Color = true;
        }

        // Orientation
        if (record.Request.Orientation == PrintOrientation.Landscape)
        {
            printDoc.DefaultPageSettings.Landscape = true;
        }
        else if (record.Request.Orientation == PrintOrientation.Portrait)
        {
            printDoc.DefaultPageSettings.Landscape = false;
        }

        // Paper Size
        if (!string.IsNullOrWhiteSpace(record.Request.PaperSizeName))
        {
            foreach (PaperSize ps in printDoc.PrinterSettings.PaperSizes)
            {
                if (string.Equals(ps.PaperName, record.Request.PaperSizeName, StringComparison.OrdinalIgnoreCase))
                {
                    printDoc.DefaultPageSettings.PaperSize = ps;
                    break;
                }
            }
        }

        // Reasonable margins: 0.25 inch (25 hundredths of an inch)
        printDoc.DefaultPageSettings.Margins = new Margins(25, 25, 25, 25);
    }
}
