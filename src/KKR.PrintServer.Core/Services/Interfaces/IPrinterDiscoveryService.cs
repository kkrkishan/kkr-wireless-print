using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public interface IPrinterDiscoveryService
{
    IReadOnlyList<PrinterInfo> GetPrinters(bool refresh = false);
    PrinterInfo? GetPrinter(string name);
    bool IsPrinterReady(string name, out string? reasonIfNotReady);
}
