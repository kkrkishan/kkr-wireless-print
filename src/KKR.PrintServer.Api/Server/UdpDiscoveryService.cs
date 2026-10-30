using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KKR.PrintServer.Core.Logging;
using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Api.Server;

public class UdpDiscoveryService : IDisposable
{
    private const int DiscoveryPort = 8089;
    private readonly ServerConfig _config;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public UdpDiscoveryService(ServerConfig config)
    {
        _config = config;
    }

    public void Start(string currentIp)
    {
        if (_isRunning) return;

        try
        {
            _cts = new CancellationTokenSource();
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
            _isRunning = true;

            Task.Run(() => ListenLoopAsync(currentIp, _cts.Token));
            Task.Run(() => BroadcastBeaconLoopAsync(currentIp, _cts.Token));

            AppLogger.Instance.Info("Discovery", $"UDP Local Auto-Discovery service running on port {DiscoveryPort}.");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warning("Discovery", $"Could not start UDP discovery service on port {DiscoveryPort}: {ex.Message}");
        }
    }

    private async Task ListenLoopAsync(string currentIp, CancellationToken token)
    {
        while (!token.IsCancellationRequested && _udpClient != null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(token);
                string message = Encoding.UTF8.GetString(result.Buffer);

                if (message.Contains("PRINT_OFO_DISCOVER"))
                {
                    var responseJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        serverName = "Print Ofo Server",
                        ip = currentIp,
                        httpPort = _config.HttpPort,
                        httpsPort = _config.HttpsPort,
                        machineName = Environment.MachineName,
                        version = "1.1.0"
                    });

                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    await _udpClient.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                    AppLogger.Instance.Info("Discovery", $"Responded to discovery probe from {result.RemoteEndPoint.Address}");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!_isRunning) break;
                AppLogger.Instance.Warning("Discovery", $"Error in UDP discovery listener: {ex.Message}");
                await Task.Delay(1000, token);
            }
        }
    }

    private async Task BroadcastBeaconLoopAsync(string currentIp, CancellationToken token)
    {
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
        while (!token.IsCancellationRequested && _udpClient != null)
        {
            try
            {
                var beaconJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    type = "PRINT_OFO_BEACON",
                    serverName = "Print Ofo Server",
                    ip = currentIp,
                    httpPort = _config.HttpPort,
                    httpsPort = _config.HttpsPort,
                    machineName = Environment.MachineName
                });

                byte[] beaconBytes = Encoding.UTF8.GetBytes(beaconJson);
                await _udpClient.SendAsync(beaconBytes, beaconBytes.Length, broadcastEndpoint);
            }
            catch
            {
                // Subnet or interface may not route broadcasts; ignore non-fatal drops
            }

            try
            {
                await Task.Delay(5000, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _udpClient?.Close();
        _udpClient?.Dispose();
        _udpClient = null;
    }

    public void Dispose()
    {
        Stop();
    }
}
