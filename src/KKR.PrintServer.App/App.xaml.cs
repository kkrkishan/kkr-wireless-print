using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using KKR.PrintServer.Api.Server;
using KKR.PrintServer.App.ViewModels;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;

namespace KKR.PrintServer.App;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "Local\\KKR_Wireless_Print_Server_SingleInstance";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_RESTORE = 9;

    private MainViewModel? _mainViewModel;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Another instance is already running; activate its window and exit
            try
            {
                var current = System.Diagnostics.Process.GetCurrentProcess();
                foreach (var process in System.Diagnostics.Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(process.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
            }
            catch { }

            Shutdown();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            string err = args.ExceptionObject?.ToString() ?? "Unknown AppDomain error";
            try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), err); } catch { }
            MessageBox.Show(err, "Fatal Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            string err = args.Exception?.ToString() ?? "Unknown Dispatcher error";
            try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), err); } catch { }
            MessageBox.Show(err, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            string baseDir = AppContext.BaseDirectory;
            string logsDir = Path.Combine(baseDir, "logs");
            string certsDir = Path.Combine(baseDir, "certs");
            string spoolDir = Path.Combine(baseDir, "temp_spool");

            AppLogger.Instance.Initialize(logsDir);
            AppLogger.Instance.Info("App", "KKR Wireless Print Server starting up...");

            var config = new ServerConfig
            {
                ListenIpAddress = "0.0.0.0", // Listen on all network interfaces
                HttpPort = 8080,
                HttpsPort = 8443,
                EnableHttps = true,
                EnableHttpFallback = true,
                CertDirectory = certsDir,
                LogDirectory = logsDir,
                SpoolTempDirectory = spoolDir
            };

            var certService = new CertificateService(certsDir);
            var securityService = new SecurityService(spoolDir);
            var discoveryService = new PrinterDiscoveryService();
            var printProcessor = new PrintProcessor(discoveryService, securityService);
            var queueService = new JobQueueService(printProcessor);

            var apiHost = new PrintApiHost(config, securityService, certService, discoveryService, queueService);

            _mainViewModel = new MainViewModel(config, discoveryService, securityService, certService, printProcessor, queueService, apiHost);

            var mainWindow = new MainWindow { DataContext = _mainViewModel };
            MainWindow = mainWindow;
            mainWindow.Show();

            // Auto-start API server on launch
            await _mainViewModel.StartServerAsync();
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), ex.ToString()); } catch { }
            MessageBox.Show(ex.ToString(), "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_mainViewModel != null)
        {
            await _mainViewModel.StopServerAsync();
        }
        AppLogger.Instance.Info("App", "KKR Wireless Print Server shut down cleanly.");

        if (_mutex != null)
        {
            try { _mutex.ReleaseMutex(); } catch { }
            _mutex.Dispose();
            _mutex = null;
        }

        base.OnExit(e);
    }
}
