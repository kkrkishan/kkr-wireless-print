using System.Runtime.InteropServices;

namespace KKR.PrintServer.Core.Services;

public static class NativePrintSpooler
{
    public const int JOB_CONTROL_PAUSE = 1;
    public const int JOB_CONTROL_RESUME = 2;
    public const int JOB_CONTROL_CANCEL = 3;
    public const int JOB_CONTROL_RESTART = 4;
    public const int JOB_CONTROL_DELETE = 5;

    public const int PRINTER_STATUS_PAUSED = 0x00000001;
    public const int PRINTER_STATUS_ERROR = 0x00000002;
    public const int PRINTER_STATUS_PENDING_DELETION = 0x00000004;
    public const int PRINTER_STATUS_PAPER_JAM = 0x00000008;
    public const int PRINTER_STATUS_PAPER_OUT = 0x00000010;
    public const int PRINTER_STATUS_MANUAL_FEED = 0x00000020;
    public const int PRINTER_STATUS_PAPER_PROBLEM = 0x00000040;
    public const int PRINTER_STATUS_OFFLINE = 0x00000080;
    public const int PRINTER_STATUS_IO_ACTIVE = 0x00000100;
    public const int PRINTER_STATUS_BUSY = 0x00000200;
    public const int PRINTER_STATUS_PRINTING = 0x00000400;
    public const int PRINTER_STATUS_OUTPUT_BIN_FULL = 0x00000800;
    public const int PRINTER_STATUS_NOT_AVAILABLE = 0x00001000;
    public const int PRINTER_STATUS_WAITING = 0x00002000;
    public const int PRINTER_STATUS_PROCESSING = 0x00004000;
    public const int PRINTER_STATUS_INITIALIZING = 0x00008000;
    public const int PRINTER_STATUS_WARMING_UP = 0x00010000;
    public const int PRINTER_STATUS_TONER_LOW = 0x00020000;
    public const int PRINTER_STATUS_NO_TONER = 0x00040000;
    public const int PRINTER_STATUS_PAGE_PUNT = 0x00080000;
    public const int PRINTER_STATUS_USER_INTERVENTION = 0x00100000;
    public const int PRINTER_STATUS_OUT_OF_MEMORY = 0x00200000;
    public const int PRINTER_STATUS_DOOR_OPEN = 0x00400000;
    public const int PRINTER_STATUS_SERVER_UNKNOWN = 0x00800000;
    public const int PRINTER_STATUS_POWER_SAVE = 0x01000000;

    public const int JOB_STATUS_PAUSED = 0x00000001;
    public const int JOB_STATUS_ERROR = 0x00000002;
    public const int JOB_STATUS_DELETING = 0x00000004;
    public const int JOB_STATUS_SPOOLING = 0x00000008;
    public const int JOB_STATUS_PRINTING = 0x00000010;
    public const int JOB_STATUS_OFFLINE = 0x00000020;
    public const int JOB_STATUS_PAPEROUT = 0x00000040;
    public const int JOB_STATUS_PRINTED = 0x00000080;
    public const int JOB_STATUS_DELETED = 0x00000100;
    public const int JOB_STATUS_BLOCKED_DEVQ = 0x00000200;
    public const int JOB_STATUS_USER_INTERVENTION = 0x00000400;
    public const int JOB_STATUS_RESTART = 0x00000800;

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool SetJob(IntPtr hPrinter, int jobId, int level, IntPtr pJob, int command);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool GetJob(IntPtr hPrinter, int jobId, int level, IntPtr pJob, int cbBuf, out int pcbNeeded);

    public static bool CancelSpoolerJob(string printerName, int jobId, out string? errorMessage)
    {
        errorMessage = null;
        if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
        {
            int err = Marshal.GetLastWin32Error();
            errorMessage = $"Cannot open printer '{printerName}'. Win32 Error: {err}";
            return false;
        }

        try
        {
            if (!SetJob(hPrinter, jobId, 0, IntPtr.Zero, JOB_CONTROL_CANCEL))
            {
                int err = Marshal.GetLastWin32Error();
                errorMessage = $"SetJob cancel failed for Job #{jobId}. Win32 Error: {err}";
                return false;
            }
            return true;
        }
        finally
        {
            ClosePrinter(hPrinter);
        }
    }
}
