import 'dart:async';
import 'dart:io';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:receive_sharing_intent/receive_sharing_intent.dart';
import '../../models/printer_info.dart';
import '../../models/server_connection.dart';
import '../../models/print_job.dart';
import '../../services/api_service.dart';
import '../../services/storage_service.dart';
import '../theme/app_theme.dart';
import 'connect_screen.dart';
import 'preview_and_settings_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final ApiService _api = ApiService();
  final StorageService _storage = StorageService();

  ServerConnection? _connection;
  List<PrinterInfo> _printers = [];
  List<PrintJob> _recentJobs = [];
  bool _isLoading = false;
  String? _statusMessage;
  Timer? _refreshTimer;
  StreamSubscription? _intentSub;

  @override
  void initState() {
    super.initState();
    _initApp();
    _listenToSharedFiles();
  }

  @override
  void dispose() {
    _refreshTimer?.cancel();
    _intentSub?.cancel();
    super.dispose();
  }

  Future<void> _initApp() async {
    setState(() => _isLoading = true);
    await _storage.init();
    _connection = await _storage.loadServerConnection();
    _recentJobs = await _storage.loadRecentJobs();

    if (_connection != null && _connection!.token != null) {
      _api.configure(_connection!);
      await _refreshPrinters();
      await _syncRecentJobs();
    }

    setState(() => _isLoading = false);

    // Periodic auto-refresh every 15s if connected
    _refreshTimer = Timer.periodic(const Duration(seconds: 15), (_) {
      if (_connection != null && _api.isAuthenticated) {
        _refreshPrinters(silent: true);
        _syncRecentJobs(silent: true);
      }
    });
  }

  void _listenToSharedFiles() {
    // For sharing files while app is in memory
    _intentSub = ReceiveSharingIntent.instance.getMediaStream().listen((files) {
      if (files.isNotEmpty) {
        _handleIncomingSharedFile(File(files.first.path));
      }
    }, onError: (err) {
      debugPrint('Share intent error: $err');
    });

    // For sharing files when app was closed
    ReceiveSharingIntent.instance.getInitialMedia().then((files) {
      if (files.isNotEmpty) {
        _handleIncomingSharedFile(File(files.first.path));
      }
    });
  }

  Future<void> _handleIncomingSharedFile(File file) async {
    if (!mounted) return;

    // Ensure connection & printers are loaded if coming from cold start
    if (_connection == null || !_api.isAuthenticated) {
      await _storage.init();
      _connection = await _storage.loadServerConnection();
      if (_connection != null && _connection!.token != null) {
        _api.configure(_connection!);
      }
    }

    if (_api.isAuthenticated && _printers.isEmpty) {
      await _refreshPrinters(silent: true);
    }

    await _openPrintFlow(file);
  }

  Future<void> _refreshPrinters({bool silent = false}) async {
    if (!silent) setState(() => _isLoading = true);

    final res = await _api.getPrinters();
    if (!mounted) return;

    if (res.success && res.data != null) {
      setState(() {
        _printers = res.data!;
        _connection = _connection?.copyWith(isConnected: true);
        _statusMessage = null;
      });
    } else {
      if (!silent) {
        setState(() {
          _statusMessage = res.errorMessage;
          _connection = _connection?.copyWith(isConnected: false);
        });
      }
    }

    if (!silent) setState(() => _isLoading = false);
  }

  Future<void> _syncRecentJobs({bool silent = false}) async {
    final serverJobsRes = await _api.getJobs();
    if (serverJobsRes.success && serverJobsRes.data != null) {
      final serverJobs = serverJobsRes.data!;
      // Merge with local recent jobs
      final local = await _storage.loadRecentJobs();
      final map = <String, PrintJob>{};

      for (final j in local) {
        map[j.jobId] = j;
      }
      for (final j in serverJobs) {
        map[j.jobId] = j;
      }

      final combined = map.values.toList()
        ..sort((a, b) => b.createdAt.compareTo(a.createdAt));

      await _storage.saveRecentJobs(combined);
      if (mounted) {
        setState(() => _recentJobs = combined);
      }
    }
  }

  Future<void> _pickDocumentAndPrint() async {
    if (_connection == null || !_api.isAuthenticated) {
      _showConnectPrompt();
      return;
    }

    final result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png'],
    );

    if (result.isNotEmpty && result.first.path != null) {
      final file = File(result.first.path!);
      await _openPrintFlow(file);
    }
  }

  Future<void> _openPrintFlow(File file) async {
    if (_connection == null || !_api.isAuthenticated) {
      await _storage.init();
      _connection = await _storage.loadServerConnection();
      if (_connection != null && _connection!.token != null) {
        _api.configure(_connection!);
        await _refreshPrinters(silent: true);
      }
    }

    if (_connection == null || !_api.isAuthenticated) {
      _showConnectPrompt();
      return;
    }

    if (_printers.isEmpty) {
      await _refreshPrinters(silent: true);
    }

    final savedPrinterName = await _storage.loadSelectedPrinter();
    PrinterInfo? preselected;
    if (_printers.isNotEmpty) {
      preselected = _printers.firstWhere(
        (p) => p.name == savedPrinterName,
        orElse: () => _printers.firstWhere(
          (p) => p.isDefault,
          orElse: () => _printers.first,
        ),
      );
    }

    if (!mounted) return;
    final didPrint = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => PreviewAndSettingsScreen(
          initialFile: file,
          availablePrinters: _printers,
          preselectedPrinter: preselected,
        ),
      ),
    );

    if (didPrint == true) {
      _syncRecentJobs();
    }
  }

  void _showConnectPrompt() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppTheme.cardDark,
        title: const Text('Not Connected'),
        content: const Text(
          'Please connect and pair with the Windows Print Server first.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              _openConnectScreen();
            },
            child: const Text('Connect Now'),
          ),
        ],
      ),
    );
  }

  Future<void> _openConnectScreen() async {
    final result = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => ConnectScreen(initialConnection: _connection),
      ),
    );

    if (result == true) {
      _connection = await _storage.loadServerConnection();
      if (_connection != null) {
        _api.configure(_connection!);
        await _refreshPrinters();
        await _syncRecentJobs();
      }
    }
  }

  Future<void> _disconnectServer() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppTheme.cardDark,
        title: const Text('Disconnect Computer'),
        content: const Text(
          'Do you want to disconnect from this Windows print server?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.dangerRed),
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const Text('Disconnect'),
          ),
        ],
      ),
    );

    if (confirm == true) {
      await _storage.clearServerConnection();
      setState(() {
        _connection = null;
        _printers = [];
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final isConnected = _connection != null && _api.isAuthenticated;

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: AppTheme.primaryBlue.withValues(alpha: 0.2),
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Icon(Icons.print_outlined,
                  color: AppTheme.lightBlue, size: 22),
            ),
            const SizedBox(width: 10),
            const Text('Print Ofo'),
          ],
        ),
        actions: [
          if (isConnected)
            IconButton(
              icon: const Icon(Icons.refresh),
              tooltip: 'Refresh Printers',
              onPressed: _isLoading ? null : () => _refreshPrinters(),
            ),
          IconButton(
            icon: Icon(
              isConnected ? Icons.link_off : Icons.link,
              color: isConnected ? AppTheme.textMuted : AppTheme.accentCyan,
            ),
            tooltip: isConnected ? 'Disconnect' : 'Connect',
            onPressed: isConnected ? _disconnectServer : _openConnectScreen,
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          if (isConnected) {
            await _refreshPrinters();
            await _syncRecentJobs();
          }
        },
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              _buildServerStatusCard(isConnected),
              const SizedBox(height: 16),
              _buildPrintersSection(isConnected),
              const SizedBox(height: 16),
              _buildRecentJobsSection(),
            ],
          ),
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _pickDocumentAndPrint,
        backgroundColor: AppTheme.primaryBlue,
        icon: const Icon(Icons.note_add_outlined, color: Colors.white),
        label: const Text(
          'Print Document',
          style: TextStyle(
            color: Colors.white,
            fontWeight: FontWeight.bold,
            fontSize: 15,
          ),
        ),
      ),
    );
  }

  Widget _buildServerStatusCard(bool isConnected) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 10,
                  height: 10,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: isConnected
                        ? AppTheme.successGreen
                        : AppTheme.dangerRed,
                  ),
                ),
                const SizedBox(width: 8),
                Text(
                  isConnected ? 'CONNECTED TO COMPUTER' : 'NOT CONNECTED',
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1,
                    color: isConnected
                        ? AppTheme.successGreen
                        : AppTheme.dangerRed,
                  ),
                ),
                const Spacer(),
                OutlinedButton(
                  onPressed: _openConnectScreen,
                  style: OutlinedButton.styleFrom(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  ),
                  child: Text(
                    isConnected ? 'Change' : 'Connect',
                    style: const TextStyle(fontSize: 12),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            if (isConnected && _connection != null) ...[
              Text(
                _connection!.serverName ?? 'Windows Print Server',
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppTheme.textLight,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                'LAN Endpoint: ${_connection!.ip}:${_connection!.httpPort} / ${_connection!.httpsPort} (Private Wi-Fi)',
                style: const TextStyle(fontSize: 12, color: AppTheme.textMuted),
              ),
            ] else ...[
              const Text(
                'Connect to your Windows laptop on the same Wi-Fi network to print to USB printers.',
                style: TextStyle(fontSize: 13, color: AppTheme.textMuted),
              ),
              const SizedBox(height: 12),
              ElevatedButton.icon(
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('Connect to Computer'),
                onPressed: _openConnectScreen,
              ),
            ],
            if (_statusMessage != null) ...[
              const SizedBox(height: 10),
              Text(
                _statusMessage!,
                style: const TextStyle(fontSize: 12, color: AppTheme.warningAmber),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildPrintersSection(bool isConnected) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'INSTALLED PRINTERS',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                letterSpacing: 1,
                color: AppTheme.textMuted,
              ),
            ),
            if (isConnected)
              Text(
                '${_printers.length} Available',
                style: const TextStyle(fontSize: 12, color: AppTheme.lightBlue),
              ),
          ],
        ),
        const SizedBox(height: 8),
        if (!isConnected)
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Center(
                child: Column(
                  children: const [
                    Icon(Icons.print_disabled,
                        color: AppTheme.textMuted, size: 36),
                    SizedBox(height: 8),
                    Text(
                      'Printers will appear once connected to Windows server.',
                      style: TextStyle(color: AppTheme.textMuted, fontSize: 13),
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
              ),
            ),
          )
        else if (_printers.isEmpty)
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Center(
                child: _isLoading
                    ? const CircularProgressIndicator()
                    : const Text(
                        'No printers reported by Windows server.',
                        style: TextStyle(color: AppTheme.warningAmber),
                      ),
              ),
            ),
          )
        else
          ..._printers.map((printer) => _buildPrinterCard(printer)),
      ],
    );
  }

  Widget _buildPrinterCard(PrinterInfo printer) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: printer.isOnline
                ? AppTheme.successGreen.withValues(alpha: 0.15)
                : AppTheme.dangerRed.withValues(alpha: 0.15),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Icon(
            Icons.print,
            color: printer.isOnline
                ? AppTheme.successGreen
                : AppTheme.dangerRed,
          ),
        ),
        title: Row(
          children: [
            Expanded(
              child: Text(
                printer.name,
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                  color: AppTheme.textLight,
                ),
              ),
            ),
            if (printer.isDefault)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: AppTheme.primaryBlue.withValues(alpha: 0.2),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: const Text(
                  'Default',
                  style: TextStyle(fontSize: 10, color: AppTheme.lightBlue),
                ),
              ),
          ],
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Text(
              'Port: ${printer.port} • Status: ${printer.status}',
              style: const TextStyle(color: AppTheme.textMuted, fontSize: 12),
            ),
            const SizedBox(height: 4),
            Text(
              '${printer.capabilities.supportsColor ? "Color" : "Monochrome"} • Max ${printer.capabilities.maxCopies} Copies',
              style: const TextStyle(fontSize: 11, color: AppTheme.accentCyan),
            ),
          ],
        ),
        trailing: const Icon(Icons.chevron_right, color: AppTheme.textMuted),
        onTap: () => _pickDocumentAndPrint(),
      ),
    );
  }

  Widget _buildRecentJobsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            const Text(
              'RECENT PRINT JOBS',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                letterSpacing: 1,
                color: AppTheme.textMuted,
              ),
            ),
            if (_recentJobs.isNotEmpty)
              TextButton(
                onPressed: () async {
                  await _storage.saveRecentJobs([]);
                  setState(() => _recentJobs = []);
                },
                child: const Text('Clear', style: TextStyle(fontSize: 11)),
              ),
          ],
        ),
        const SizedBox(height: 8),
        if (_recentJobs.isEmpty)
          Card(
            child: const Padding(
              padding: EdgeInsets.all(24),
              child: Center(
                child: Text(
                  'No recent print jobs yet.',
                  style: TextStyle(color: AppTheme.textMuted, fontSize: 13),
                ),
              ),
            ),
          )
        else
          ..._recentJobs.take(8).map((job) => _buildJobCard(job)),
        const SizedBox(height: 60), // Spacing for FAB
      ],
    );
  }

  Widget _buildJobCard(PrintJob job) {
    Color badgeColor;
    switch (job.status) {
      case JobState.completed:
        badgeColor = AppTheme.successGreen;
        break;
      case JobState.failed:
        badgeColor = AppTheme.dangerRed;
        break;
      case JobState.cancelled:
        badgeColor = AppTheme.warningAmber;
        break;
      default:
        badgeColor = AppTheme.lightBlue;
        break;
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    job.documentName,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                      color: AppTheme.textLight,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: badgeColor.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(6),
                    border: Border.all(color: badgeColor.withValues(alpha: 0.4)),
                  ),
                  child: Text(
                    job.status.label,
                    style: TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      color: badgeColor,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                Text(
                  job.printerName,
                  style:
                      const TextStyle(fontSize: 11, color: AppTheme.textMuted),
                ),
                const Text(' • ',
                    style: TextStyle(color: AppTheme.textMuted, fontSize: 11)),
                Text(
                  job.formattedFileSize,
                  style:
                      const TextStyle(fontSize: 11, color: AppTheme.textMuted),
                ),
                const Spacer(),
                Text(
                  _formatTime(job.createdAt),
                  style:
                      const TextStyle(fontSize: 10, color: AppTheme.textMuted),
                ),
              ],
            ),
            if (job.statusMessage != null && job.statusMessage!.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(
                job.statusMessage!,
                style: TextStyle(
                  fontSize: 11,
                  color: job.status == JobState.failed
                      ? AppTheme.dangerRed
                      : AppTheme.textMuted,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _formatTime(DateTime dt) {
    final now = DateTime.now();
    final diff = now.difference(dt);
    if (diff.inMinutes < 1) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    return '${dt.day}/${dt.month} ${dt.hour}:${dt.minute.toString().padLeft(2, '0')}';
  }
}
