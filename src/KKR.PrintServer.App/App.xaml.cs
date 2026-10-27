using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using KKR.PrintServer.Api.Server;
using KKR.PrintServer.App.ViewModels;
using KKR.PrintServer.Core.Common;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;

namespace KKR.PrintServer.App;

public partial class App : Application
{
    private MainViewModel? _mainViewModel;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        // Ensure only this instance runs: terminate any old or orphaned background instances
        try
        {
            var current = System.Diagnostics.Process.GetCurrentProcess();
            var existing = System.Diagnostics.Process.GetProcessesByName(current.ProcessName)
                .Where(p => p.Id != current.Id)
                .ToList();

            foreach (var p in existing)
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(1000);
                }
                catch { }
            }
        }
        catch { }

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            string err = args.ExceptionObject?.ToString() ?? "Unknown AppDomain error";
            try { File.AppendAllText(AppEnvironment.CrashLogPath, err); } catch { }
            MessageBox.Show(err, "Fatal Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            string err = args.Exception?.ToString() ?? "Unknown Dispatcher error";
            try { File.AppendAllText(AppEnvironment.CrashLogPath, err); } catch { }
            MessageBox.Show(err, "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            string dataDir = AppEnvironment.GetDataDirectory();
            string logsDir = AppEnvironment.LogsDirectory;
            string certsDir = AppEnvironment.CertsDirectory;
            string spoolDir = AppEnvironment.SpoolDirectory;

            AppLogger.Instance.Initialize(logsDir);
            AppLogger.Instance.Info("App", $"KKR Wireless Print Server starting up... (Mode: {(AppEnvironment.IsPortableMode() ? "Portable" : "Installed")}, Data: {dataDir})");

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
            try { File.AppendAllText(AppEnvironment.CrashLogPath, ex.ToString()); } catch { }
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
        base.OnExit(e);
    }
}
