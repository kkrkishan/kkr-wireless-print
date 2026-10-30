using System.Net;
using System.Net.NetworkInformation;
using KKR.PrintServer.Api.Endpoints;
using KKR.PrintServer.Api.Middleware;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KKR.PrintServer.Api.Server;

public class PrintApiHost
{
    private readonly ServerConfig _config;
    private readonly ISecurityService _securityService;
    private readonly ICertificateService _certificateService;
    private readonly IPrinterDiscoveryService _printerService;
    private readonly IJobQueueService _queueService;

    private WebApplication? _app;
    private CancellationTokenSource? _hostCts;
    private UdpDiscoveryService? _udpDiscovery;

    public bool IsRunning => _app != null;
    public string? HttpUrl { get; private set; }
    public string? HttpsUrl { get; private set; }

    public PrintApiHost(
        ServerConfig config,
        ISecurityService securityService,
        ICertificateService certificateService,
        IPrinterDiscoveryService printerService,
        IJobQueueService queueService)
    {
        _config = config;
        _securityService = securityService;
        _certificateService = certificateService;
        _printerService = printerService;
        _queueService = queueService;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return;
        }

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        // Register routing and core singletons into DI
        builder.Services.AddRouting();
        builder.Services.AddSingleton(_config);
        builder.Services.AddSingleton(_securityService);
        builder.Services.AddSingleton(_certificateService);
        builder.Services.AddSingleton(_printerService);
        builder.Services.AddSingleton(_queueService);

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        // Configure Kestrel
        builder.WebHost.UseKestrel(kestrelOptions =>
        {
            IPAddress bindIp = IPAddress.Any;
            if (!string.IsNullOrWhiteSpace(_config.ListenIpAddress) &&
                IPAddress.TryParse(_config.ListenIpAddress, out var parsedIp))
            {
                bindIp = parsedIp;
            }

            if (_config.EnableHttpFallback)
            {
                kestrelOptions.Listen(bindIp, _config.HttpPort);
            }

            if (_config.EnableHttps)
            {
                var cert = _certificateService.GetOrCreateServerCertificate(
                    new[] { "localhost", Environment.MachineName },
                    new[] { _config.ListenIpAddress, "127.0.0.1" });

                kestrelOptions.Listen(bindIp, _config.HttpsPort, listenOptions =>
                {
                    listenOptions.UseHttps(cert);
                });
            }

            kestrelOptions.Limits.MaxRequestBodySize = _config.MaxUploadSizeBytes;
        });

        _app = builder.Build();

        _app.UseCors();
        _app.UseMiddleware<TokenAuthenticationMiddleware>();

        // Map Endpoints
        _app.MapDiscoveryEndpoints();
        _app.MapAuthEndpoints();
        _app.MapPrinterEndpoints();
        _app.MapJobEndpoints(_config);

        _hostCts = new CancellationTokenSource();
        await _app.StartAsync(_hostCts.Token);

        HttpUrl = _config.EnableHttpFallback ? $"http://{GetDisplayIp()}:{_config.HttpPort}" : null;
        HttpsUrl = _config.EnableHttps ? $"https://{GetDisplayIp()}:{_config.HttpsPort}" : null;

        try
        {
            _udpDiscovery = new UdpDiscoveryService(_config);
            _udpDiscovery.Start(GetDisplayIp());
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warning("Discovery", $"UDP Auto-Discovery could not be started: {ex.Message}");
        }

        AppLogger.Instance.Info("Host", $"Print Server API started. HTTP: {HttpUrl ?? "Disabled"}, HTTPS: {HttpsUrl ?? "Disabled"}");
    }

    public async Task StopAsync()
    {
        if (_udpDiscovery != null)
        {
            _udpDiscovery.Dispose();
            _udpDiscovery = null;
        }

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
        if (_hostCts != null)
        {
            _hostCts.Dispose();
            _hostCts = null;
        }
        AppLogger.Instance.Info("Host", "Print Server API stopped.");
    }

    private string GetDisplayIp()
    {
        if (_config.ListenIpAddress != "0.0.0.0" && !string.IsNullOrWhiteSpace(_config.ListenIpAddress))
        {
            return _config.ListenIpAddress;
        }

        try
        {
            var validInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(ni =>
                {
                    var name = ni.Name.ToLowerInvariant();
                    var desc = ni.Description.ToLowerInvariant();
                    return !name.Contains("vethernet") &&
                           !name.Contains("virtual") &&
                           !name.Contains("wsl") &&
                           !name.Contains("docker") &&
                           !desc.Contains("virtual") &&
                           !desc.Contains("hyper-v") &&
                           !desc.Contains("wsl") &&
                           !desc.Contains("vmware") &&
                           !desc.Contains("virtualbox") &&
                           !desc.Contains("pseudo");
                })
                .ToList();

            // 1. Wi-Fi first
            var wifi = validInterfaces.FirstOrDefault(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);
            if (wifi != null)
            {
                var addr = wifi.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                         !IPAddress.IsLoopback(a.Address) &&
                                         !a.Address.ToString().StartsWith("169.254."));
                if (addr != null) return addr.Address.ToString();
            }

            // 2. Physical Ethernet
            var eth = validInterfaces.FirstOrDefault(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
            if (eth != null)
            {
                var addr = eth.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                         !IPAddress.IsLoopback(a.Address) &&
                                         !a.Address.ToString().StartsWith("169.254."));
                if (addr != null) return addr.Address.ToString();
            }

            var fallback = Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                     !IPAddress.IsLoopback(a) &&
                                     !a.ToString().StartsWith("169.254.") &&
                                     !a.ToString().StartsWith("172.31."));
            if (fallback != null)
            {
                return fallback.ToString();
            }
        }
        catch
        {
            // Ignore
        }

        return "127.0.0.1";
    }
}
