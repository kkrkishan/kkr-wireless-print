using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public interface IPrintProcessor
{
    Task ProcessPrintJobAsync(PrintJobRecord record, CancellationToken cancellationToken = default);
    Task<PrintJobRecord> PrintTestPageAsync(string printerName, string serverIp, int port, bool httpsEnabled, CancellationToken cancellationToken = default);
}
