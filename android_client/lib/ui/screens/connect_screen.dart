import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../../models/server_connection.dart';
import '../../services/api_service.dart';
import '../../services/storage_service.dart';
import '../theme/app_theme.dart';
import '../widgets/desktop_companion_dialog.dart';
import '../widgets/desktop_companion_dialog.dart';

class ConnectScreen extends StatefulWidget {
  final ServerConnection? initialConnection;

  const ConnectScreen({super.key, this.initialConnection});

  @override
  State<ConnectScreen> createState() => _ConnectScreenState();
}

class _ConnectScreenState extends State<ConnectScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final MobileScannerController _scannerController = MobileScannerController(
    detectionSpeed: DetectionSpeed.normal,
    facing: CameraFacing.back,
    torchEnabled: false,
  );

  final _formKey = GlobalKey<FormState>();
  final _ipController = TextEditingController();
  final _httpPortController = TextEditingController(text: '8080');
  final _httpsPortController = TextEditingController(text: '8443');
  final _pinController = TextEditingController();

  bool _isLoading = false;
  String? _errorMessage;
  String? _statusText;
  bool _isScanningHandled = false;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);

    if (widget.initialConnection != null) {
      _ipController.text = widget.initialConnection!.ip;
      _httpPortController.text = widget.initialConnection!.httpPort.toString();
      _httpsPortController.text =
          widget.initialConnection!.httpsPort.toString();
      _pinController.text = widget.initialConnection!.pin;
    } else {
      _ipController.text = '10.92.81.221';
    }
  }

  @override
  void dispose() {
    _tabController.dispose();
    _scannerController.dispose();
    _ipController.dispose();
    _httpPortController.dispose();
    _httpsPortController.dispose();
    _pinController.dispose();
    super.dispose();
  }

  Future<void> _handleQrDetected(BarcodeCapture capture) async {
    if (_isScanningHandled || _isLoading) return;

    final barcodes = capture.barcodes;
    if (barcodes.isEmpty) return;

    final rawValue = barcodes.first.rawValue;
    if (rawValue == null || rawValue.isEmpty) return;

    setState(() {
      _isScanningHandled = true;
      _isLoading = true;
      _errorMessage = null;
      _statusText = 'QR code detected. Connecting to server...';
    });

    try {
      String ip = '';
      int httpPort = 8080;
      int httpsPort = 8443;
      String pin = '';
      String? thumbprint;

      // Try parsing JSON format
      if (rawValue.trim().startsWith('{')) {
        final map = jsonDecode(rawValue) as Map<String, dynamic>;
        ip = (map['server'] ?? map['ip'] ?? '').toString();
        httpPort = int.tryParse(map['httpPort']?.toString() ?? '8080') ?? 8080;
        httpsPort =
            int.tryParse(map['httpsPort']?.toString() ?? '8443') ?? 8443;
        pin = (map['pin'] ?? '').toString();
        thumbprint = map['thumbprint']?.toString();
      } else {
        // Fallback: raw text as IP:Port:PIN or URL
        final parts = rawValue.split(':');
        if (parts.isNotEmpty) ip = parts[0];
        if (parts.length > 1) httpPort = int.tryParse(parts[1]) ?? 8080;
        if (parts.length > 2) pin = parts[2];
      }

      if (ip.isEmpty) {
        throw const FormatException('Invalid QR code: missing IP address.');
      }

      await _executePairing(
        ip: ip,
        httpPort: httpPort,
        httpsPort: httpsPort,
        pin: pin,
        thumbprint: thumbprint,
      );
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _errorMessage = 'Could not parse QR code: $e';
          _isScanningHandled = false;
        });
      }
    }
  }

  Future<void> _handleManualConnect() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isLoading = true;
      _errorMessage = null;
      _statusText = 'Verifying Windows Print Server...';
    });

    final ip = _ipController.text.trim();
    final httpPort = int.tryParse(_httpPortController.text.trim()) ?? 8080;
    final httpsPort = int.tryParse(_httpsPortController.text.trim()) ?? 8443;
    final pin = _pinController.text.trim();

    await _executePairing(
      ip: ip,
      httpPort: httpPort,
      httpsPort: httpsPort,
      pin: pin,
    );
  }

  Future<void> _executePairing({
    required String ip,
    required int httpPort,
    required int httpsPort,
    required String pin,
    String? thumbprint,
  }) async {
    setState(() {
      _statusText = 'Pinging $ip on local Wi-Fi...';
    });

    final api = ApiService();

    // 1. Discover server
    final discoverResult = await api.testAndDiscover(
      ip: ip,
      httpPort: httpPort,
      httpsPort: httpsPort,
    );

    if (!discoverResult.success) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _errorMessage = discoverResult.errorMessage;
          _isScanningHandled = false;
        });
      }
      return;
    }

    final serverData = discoverResult.data ?? {};
    final serverName = serverData['serverName'] as String? ?? 'Windows Server';
    final machineName = serverData['machineName'] as String?;
    final discoveredThumbprint =
        serverData['httpsThumbprint'] as String? ?? thumbprint;

    // 2. Authenticate with PIN
    setState(() {
      _statusText = 'Authenticating with PIN...';
    });

    final pairResult = await api.pair(
      ip: ip,
      httpPort: httpPort,
      httpsPort: httpsPort,
      pin: pin,
      deviceName: 'Android Mobile App',
    );

    if (!pairResult.success) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _errorMessage = pairResult.errorMessage;
        });
        // Delay before allowing re-scan to prevent instant frame loop
        Future.delayed(const Duration(seconds: 2), () {
          if (mounted) {
            setState(() {
              _isScanningHandled = false;
            });
          }
        });
      }
      return;
    }

    final token = pairResult.data?['token'] as String?;

    // 3. Persist paired connection
    final connection = ServerConnection(
      ip: ip,
      httpPort: httpPort,
      httpsPort: httpsPort,
      pin: pin,
      thumbprint: discoveredThumbprint,
      token: token,
      serverName: serverName,
      machineName: machineName,
      activeBaseUrl: api.activeBaseUrl,
      lastConnectedAt: DateTime.now(),
      isConnected: true,
    );

    await StorageService().saveServerConnection(connection);
    api.configure(connection);

    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Connected & paired with $serverName ($ip)'),
          backgroundColor: AppTheme.successGreen,
        ),
      );
      Navigator.of(context).pop(true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Connect to Computer'),
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: AppTheme.primaryBlue,
          tabs: const [
            Tab(icon: Icon(Icons.qr_code_scanner), text: 'Scan QR Code'),
            Tab(icon: Icon(Icons.tune), text: 'Manual Setup'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _buildQrTab(),
          _buildManualTab(),
        ],
      ),
    );
  }

  Widget _buildQrTab() {
    return Column(
      children: [
        Container(
          padding: const EdgeInsets.all(16),
          color: AppTheme.cardDark,
          child: const Row(
            children: [
              Icon(Icons.info_outline, color: AppTheme.accentCyan, size: 20),
              SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Point camera at the QR code displayed on the Windows Print Ofo Server dashboard.',
                  style: TextStyle(color: AppTheme.textMuted, fontSize: 13),
                ),
              ),
            ],
          ),
        ),
        Expanded(
          child: Stack(
            alignment: Alignment.center,
            children: [
              MobileScanner(
                controller: _scannerController,
                onDetect: _handleQrDetected,
              ),
              // Target reticle
              Container(
                width: 240,
                height: 240,
                decoration: BoxDecoration(
                  border: Border.all(color: AppTheme.accentCyan, width: 2.5),
                  borderRadius: BorderRadius.circular(16),
                ),
              ),
              if (_isLoading)
                Container(
                  color: Colors.black54,
                  child: Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const CircularProgressIndicator(color: Colors.white),
                        const SizedBox(height: 16),
                        Text(
                          _statusText ?? 'Connecting...',
                          style: const TextStyle(
                              color: Colors.white, fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
        ),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
          color: AppTheme.cardDark,
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
            children: [
              IconButton(
                icon: const Icon(Icons.flash_on, color: Colors.white),
                onPressed: () => _scannerController.toggleTorch(),
                tooltip: 'Toggle Flashlight',
              ),
              IconButton(
                icon: const Icon(Icons.flip_camera_android, color: Colors.white),
                onPressed: () => _scannerController.switchCamera(),
                tooltip: 'Switch Camera',
              ),
            ],
          ),
        ),
        if (_errorMessage != null)
          Container(
            padding: const EdgeInsets.all(12),
            color: AppTheme.dangerRed.withValues(alpha: 0.15),
            child: Row(
              children: [
                const Icon(Icons.error_outline,
                    color: AppTheme.dangerRed, size: 20),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _errorMessage!,
                    style: const TextStyle(
                        color: AppTheme.dangerRed, fontSize: 12),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }

  Widget _buildManualTab() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Enter Windows Server Details',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: AppTheme.textLight,
              ),
            ),
            const SizedBox(height: 6),
            const Text(
              'Find these details on the Print Ofo dashboard on your Windows laptop.',
              style: TextStyle(fontSize: 13, color: AppTheme.textMuted),
            ),
            const SizedBox(height: 20),
            TextFormField(
              controller: _ipController,
              keyboardType: TextInputType.datetime,
              decoration: const InputDecoration(
                labelText: 'Windows Laptop IPv4 Address',
                hintText: 'e.g. 10.134.68.221 or 192.168.1.100',
                prefixIcon: Icon(Icons.laptop, color: AppTheme.lightBlue),
              ),
              validator: (val) {
                if (val == null || val.trim().isEmpty) {
                  return 'IP address is required';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: TextFormField(
                    controller: _httpPortController,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'HTTP Port',
                      hintText: '8080',
                      prefixIcon:
                          Icon(Icons.numbers, color: AppTheme.textMuted),
                    ),
                    validator: (val) {
                      if (val == null || val.trim().isEmpty) {
                        return 'Port required';
                      }
                      return null;
                    },
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: TextFormField(
                    controller: _httpsPortController,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'HTTPS Port',
                      hintText: '8443',
                      prefixIcon: Icon(Icons.lock, color: AppTheme.textMuted),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _pinController,
              keyboardType: TextInputType.number,
              maxLength: 6,
              style: const TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.bold,
                letterSpacing: 4,
              ),
              decoration: const InputDecoration(
                labelText: 'Pairing PIN',
                hintText: '123456',
                prefixIcon: Icon(Icons.pin, color: AppTheme.warningAmber),
                counterText: '',
              ),
              validator: (val) {
                if (val == null || val.trim().length != 6) {
                  return 'Enter the 6-digit PIN from Windows dashboard';
                }
                return null;
              },
            ),
            const SizedBox(height: 24),
            if (_errorMessage != null)
              Container(
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: AppTheme.dangerRed.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: AppTheme.dangerRed.withValues(alpha: 0.4)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.error_outline,
                        color: AppTheme.dangerRed, size: 20),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        _errorMessage!,
                        style: const TextStyle(
                            color: AppTheme.dangerRed, fontSize: 13),
                      ),
                    ),
                  ],
                ),
              ),
            ElevatedButton(
              onPressed: _isLoading ? null : _handleManualConnect,
              child: _isLoading
                  ? Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const SizedBox(
                          width: 18,
                          height: 18,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Text(_statusText ?? 'Connecting...'),
                      ],
                    )
                  : const Text('Test Connection & Pair'),
            ),
            const SizedBox(height: 20),
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: AppTheme.cardDark,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: AppTheme.cardBorderDark),
              ),
              child: const Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Troubleshooting Tips:',
                    style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                        color: AppTheme.textLight),
                  ),
                  SizedBox(height: 6),
                  Text(
                    '• Ensure phone and laptop are on the same Wi-Fi router / mobile hotspot.\n'
                    '• On Windows, check that Private network firewall rule is active.\n'
                    '• Make sure the Print Ofo Server is running on the laptop.\n'
                    '• Some public/guest Wi-Fi isolate devices (client isolation). Use a home network or phone hotspot.',
                    style: TextStyle(fontSize: 11, color: AppTheme.textMuted, height: 1.4),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
