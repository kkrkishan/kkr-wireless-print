using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using QRCoder;
using KKR.PrintServer.Api.Server;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;
using Microsoft.Win32;

namespace KKR.PrintServer.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ServerConfig _config;
    private readonly IPrinterDiscoveryService _printerDiscovery;
    private readonly ISecurityService _securityService;
    private readonly ICertificateService _certificateService;
    private readonly IPrintProcessor _printProcessor;
    private readonly IJobQueueService _jobQueue;
    private readonly PrintApiHost _apiHost;
    private readonly DispatcherTimer _refreshTimer;

    private bool _isServerRunning;
    private string _currentPin = string.Empty;
    private string _pinExpiryText = string.Empty;
    private string _serverIp = "127.0.0.1";
    private string _httpsThumbprint = string.Empty;
    private PrinterItemViewModel? _selectedPrinter;
    private string _statusMessage = "Ready";
    private BitmapSource? _qrCodeImage;
    private string _lastQrPin = string.Empty;
    private string _lastQrIp = string.Empty;

    public MainViewModel(
        ServerConfig config,
        IPrinterDiscoveryService printerDiscovery,
        ISecurityService securityService,
        ICertificateService certificateService,
        IPrintProcessor printProcessor,
        IJobQueueService jobQueue,
        PrintApiHost apiHost)
    {
        _config = config;
        _printerDiscovery = printerDiscovery;
        _securityService = securityService;
        _certificateService = certificateService;
        _printProcessor = printProcessor;
        _jobQueue = jobQueue;
        _apiHost = apiHost;

        Printers = new ObservableCollection<PrinterItemViewModel>();
        Jobs = new ObservableCollection<JobItemViewModel>();
        Logs = new ObservableCollection<LogEntry>();
        PairedClients = new ObservableCollection<PairedClient>();

        // Commands
        ToggleServerCommand = new RelayCommand(async () => await ToggleServerAsync());
        RefreshPrintersCommand = new RelayCommand(RefreshPrinters);
        RegeneratePinCommand = new RelayCommand(RegeneratePin);
        CopyPinCommand = new RelayCommand(CopyPinToClipboard);
        PrintTestPageCommand = new RelayCommand(async () => await PrintTestPageAsync(SelectedPrinter), () => SelectedPrinter != null && SelectedPrinter.IsOnline);
        SubmitLocalFileCommand = new RelayCommand(async () => await SubmitLocalFileAsync(), () => SelectedPrinter != null && SelectedPrinter.IsOnline);
        OpenCertFolderCommand = new RelayCommand(OpenCertFolder);
        OpenLogsFolderCommand = new RelayCommand(OpenLogsFolder);
        ClearLogsCommand = new RelayCommand(ClearLogs);

        // Subscriptions
        _jobQueue.JobUpdated += OnJobUpdated;
        AppLogger.Instance.LogEmitted += OnLogEmitted;

        // Initialize UI State
        DetermineLocalIp();
        _httpsThumbprint = _certificateService.GetCertificateThumbprint();
        UpdatePinState();
        RefreshPrinters();

        // 1-second refresh timer for PIN countdown and periodic printer sync
        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _refreshTimer.Tick += (s, e) => OnTimerTick();
        _refreshTimer.Start();
    }

    public ObservableCollection<PrinterItemViewModel> Printers { get; }
    public ObservableCollection<JobItemViewModel> Jobs { get; }
    public ObservableCollection<LogEntry> Logs { get; }
    public ObservableCollection<PairedClient> PairedClients { get; }

    public bool IsServerRunning
    {
        get => _isServerRunning;
        set
        {
            _isServerRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ServerStatusText));
        }
    }

    public string ServerStatusText => IsServerRunning ? "ACTIVE (ONLINE)" : "STOPPED";

    public string ServerIp
    {
        get => _serverIp;
        set { _serverIp = value; OnPropertyChanged(); }
    }

    public int HttpPort => _config.HttpPort;
    public int HttpsPort => _config.HttpsPort;

    public string HttpsThumbprint
    {
        get => _httpsThumbprint;
        set { _httpsThumbprint = value; OnPropertyChanged(); }
    }

    public string CurrentPin
    {
        get => _currentPin;
        set { _currentPin = value; OnPropertyChanged(); }
    }

    public string PinExpiryText
    {
        get => _pinExpiryText;
        set { _pinExpiryText = value; OnPropertyChanged(); }
    }

    public BitmapSource? QrCodeImage
    {
        get => _qrCodeImage;
        private set { _qrCodeImage = value; OnPropertyChanged(); }
    }

    public PrinterItemViewModel? SelectedPrinter
    {
        get => _selectedPrinter;
        set
        {
            _selectedPrinter = value;
            OnPropertyChanged();
            ((RelayCommand)PrintTestPageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SubmitLocalFileCommand).RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public ICommand ToggleServerCommand { get; }
    public ICommand RefreshPrintersCommand { get; }
    public ICommand RegeneratePinCommand { get; }
    public ICommand CopyPinCommand { get; }
    public ICommand PrintTestPageCommand { get; }
    public ICommand SubmitLocalFileCommand { get; }
    public ICommand OpenCertFolderCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }
    public ICommand ClearLogsCommand { get; }

    public async Task StartServerAsync()
    {
        try
        {
            StatusMessage = "Starting local network API...";
            _jobQueue.Start();
            await _apiHost.StartAsync();
            IsServerRunning = _apiHost.IsRunning;
            StatusMessage = $"Server running on https://{ServerIp}:{HttpsPort}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to start server: {ex.Message}";
            AppLogger.Instance.Error("Host", "Server start error", ex);
        }
    }

    public async Task StopServerAsync()
    {
        try
        {
            StatusMessage = "Stopping server...";
            await _apiHost.StopAsync();
            _jobQueue.Stop();
            IsServerRunning = false;
            StatusMessage = "Server stopped.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error stopping server: {ex.Message}";
            AppLogger.Instance.Error("Host", "Server stop error", ex);
        }
    }

    private async Task ToggleServerAsync()
    {
        if (IsServerRunning)
        {
            await StopServerAsync();
        }
        else
        {
            await StartServerAsync();
        }
    }

    public void RefreshPrinters()
    {
        var discovered = _printerDiscovery.GetPrinters(refresh: true);
        Application.Current?.Dispatcher.Invoke(() =>
        {
            foreach (var p in discovered)
            {
                var existing = Printers.FirstOrDefault(x => string.Equals(x.Name, p.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.UpdateModel(p);
                }
                else
                {
                    Printers.Add(new PrinterItemViewModel(p, async vm => await PrintTestPageAsync(vm)));
                }
            }

            // Remove uninstalled
            var toRemove = Printers.Where(vm => !discovered.Any(d => string.Equals(d.Name, vm.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            foreach (var r in toRemove)
            {
                Printers.Remove(r);
            }

            if (SelectedPrinter == null && Printers.Any())
            {
                SelectedPrinter = Printers.FirstOrDefault(p => p.IsDefault) ?? Printers.First();
            }
        });
    }

    public void RegeneratePin()
    {
        _securityService.GenerateNewPin();
        UpdatePinState();
    }

    private void CopyPinToClipboard()
    {
        try
        {
            Clipboard.SetText(CurrentPin);
            StatusMessage = $"PIN '{CurrentPin}' copied to clipboard.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Clipboard error: {ex.Message}";
        }
    }

    public async Task PrintTestPageAsync(PrinterItemViewModel? printerVm)
    {
        if (printerVm == null) return;

        printerVm.IsPrintingTestPage = true;
        StatusMessage = $"Generating test page for '{printerVm.Name}'...";

        try
        {
            var record = await _printProcessor.PrintTestPageAsync(printerVm.Name, ServerIp, HttpsPort, _config.EnableHttps);
            _jobQueue.EnqueueJob(record);
            StatusMessage = $"Test page queued for '{printerVm.Name}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Test print error: {ex.Message}";
            AppLogger.Instance.Error("UI", $"Test print failed for {printerVm.Name}", ex);
        }
        finally
        {
            printerVm.IsPrintingTestPage = false;
        }
    }

    public async Task SubmitLocalFileAsync()
    {
        if (SelectedPrinter == null) return;

        var dlg = new OpenFileDialog
        {
            Title = $"Select document to print to {SelectedPrinter.Name}",
            Filter = "Supported Documents (*.pdf;*.png;*.jpg;*.jpeg)|*.pdf;*.png;*.jpg;*.jpeg|PDF Documents (*.pdf)|*.pdf|Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            string filePath = dlg.FileName;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            using var fs = File.OpenRead(filePath);
            var validation = _securityService.ValidateUploadedDocument(fs, Path.GetFileName(filePath), fs.Length, _config.MaxUploadSizeBytes);

            if (!validation.IsValid)
            {
                MessageBox.Show(validation.ErrorMessage, "Invalid File", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tempFile = _securityService.CreateSafeTempFile(ext);
            fs.Position = 0;
            using (var dest = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
            {
                await fs.CopyToAsync(dest);
            }

            var record = new PrintJobRecord
            {
                JobId = Guid.NewGuid(),
                ClientId = "LOCAL-DESKTOP",
                ClientDeviceName = Environment.MachineName,
                ClientIp = "127.0.0.1",
                PrinterName = SelectedPrinter.Name,
                DocumentName = Path.GetFileName(filePath),
                DocumentType = validation.DetectedType,
                FileSizeBytes = new FileInfo(filePath).Length,
                PageCount = 1,
                Status = JobStatus.Accepted,
                StatusMessage = "Submitted locally from desktop dashboard.",
                TempFilePath = tempFile,
                Request = new PrintJobRequest
                {
                    PrinterName = SelectedPrinter.Name,
                    DocumentType = validation.DetectedType,
                    Copies = 1,
                    ScalingMode = PrintScalingMode.FitPage,
                    Orientation = PrintOrientation.Auto,
                    CenterOnPage = true,
                    DocumentName = Path.GetFileName(filePath)
                }
            };

            _jobQueue.EnqueueJob(record);
            StatusMessage = $"Enqueued local file '{record.DocumentName}' to '{record.PrinterName}'.";
        }
    }

    private void OpenCertFolder()
    {
        try
        {
            if (Directory.Exists(_config.CertDirectory))
            {
                Process.Start(new ProcessStartInfo { FileName = _config.CertDirectory, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open cert folder: {ex.Message}";
        }
    }

    private void OpenLogsFolder()
    {
        try
        {
            if (Directory.Exists(_config.LogDirectory))
            {
                Process.Start(new ProcessStartInfo { FileName = _config.LogDirectory, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open logs folder: {ex.Message}";
        }
    }

    private void ClearLogs()
    {
        Application.Current?.Dispatcher.Invoke(() => Logs.Clear());
    }

    private void OnTimerTick()
    {
        UpdatePinState();
        UpdateClients();
    }

    private void UpdatePinState()
    {
        CurrentPin = _securityService.CurrentPin;
        var expires = _securityService.PinExpiresAtUtc;
        var remaining = expires - DateTime.UtcNow;

        if (remaining > TimeSpan.Zero)
        {
            PinExpiryText = $"Expires in {remaining.Minutes:D2}m {remaining.Seconds:D2}s";
        }
        else
        {
            PinExpiryText = "Expired - Auto-renewing";
        }

        UpdateQrCode();
    }

    private void UpdateQrCode()
    {
        if (string.IsNullOrEmpty(CurrentPin) || (CurrentPin == _lastQrPin && ServerIp == _lastQrIp && QrCodeImage != null))
        {
            return;
        }

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                server = ServerIp,
                httpPort = HttpPort,
                httpsPort = HttpsPort,
                pin = CurrentPin,
                thumbprint = HttpsThumbprint
            });

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrData);
            byte[] qrBytes = qrCode.GetGraphic(8);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var bitmap = new BitmapImage();
                using var stream = new MemoryStream(qrBytes);
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                QrCodeImage = bitmap;
                _lastQrPin = CurrentPin;
                _lastQrIp = ServerIp;
            });
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error("UI", "Failed to generate pairing QR code", ex);
        }
    }

    private void UpdateClients()
    {
        var clients = _securityService.GetPairedClients();
        Application.Current?.Dispatcher.Invoke(() =>
        {
            foreach (var c in clients)
            {
                if (!PairedClients.Any(x => x.ClientId == c.ClientId))
                {
                    PairedClients.Add(c);
                }
            }
        });
    }

    private void OnJobUpdated(PrintJobRecord record)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var existing = Jobs.FirstOrDefault(j => j.JobId == record.JobId);
            if (existing != null)
            {
                existing.UpdateModel(record);
            }
            else
            {
                Jobs.Insert(0, new JobItemViewModel(record, vm =>
                {
                    _jobQueue.CancelJob(vm.JobId, out _);
                }));
            }
        });
    }

    private void OnLogEmitted(LogEntry entry)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Logs.Insert(0, entry);
            if (Logs.Count > 300)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }
        });
    }

    private void DetermineLocalIp()
    {
        try
        {
            // Prefer active Wi-Fi or Ethernet adapter
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                // Check for Wi-Fi or Ethernet
                if (ni.NetworkInterfaceType is NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.Ethernet)
                {
                    var props = ni.GetIPProperties();
                    var ipv4 = props.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                                             !IPAddress.IsLoopback(a.Address));
                    if (ipv4 != null)
                    {
                        ServerIp = ipv4.Address.ToString();
                        return;
                    }
                }
            }

            var fallback = Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
            if (fallback != null)
            {
                ServerIp = fallback.ToString();
            }
        }
        catch
        {
            ServerIp = "127.0.0.1";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
