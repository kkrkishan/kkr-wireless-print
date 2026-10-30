import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:share_plus/share_plus.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../services/storage_service.dart';
import '../theme/app_theme.dart';

class DesktopCompanionDialog extends StatefulWidget {
  final bool autoOpened;
  final VoidCallback? onConnectPressed;

  const DesktopCompanionDialog({
    super.key,
    this.autoOpened = false,
    this.onConnectPressed,
  });

  static Future<void> show(
    BuildContext context, {
    bool autoOpened = false,
    VoidCallback? onConnectPressed,
  }) {
    return showDialog(
      context: context,
      barrierDismissible: true,
      builder: (ctx) => DesktopCompanionDialog(
        autoOpened: autoOpened,
        onConnectPressed: onConnectPressed,
      ),
    );
  }

  @override
  State<DesktopCompanionDialog> createState() => _DesktopCompanionDialogState();
}

class _DesktopCompanionDialogState extends State<DesktopCompanionDialog> {
  bool _dontShowAgain = false;
  static const String _storeWebUrl =
      'https://apps.microsoft.com/search?query=Print+Ofo';
  static const String _storeProtocolUrl =
      'ms-windows-store://search/?query=Print%20Ofo';
  static const String _directInstallerUrl =
      'https://github.com/kkrkishan/kkr-wireless-print/releases/download/v1.1.0/Print_Ofo_Setup.exe';

  @override
  void initState() {
    super.initState();
    _loadPreference();
  }

  Future<void> _loadPreference() async {
    final dismissed = await StorageService().loadDismissDesktopPromo();
    if (mounted) {
      setState(() => _dontShowAgain = dismissed);
    }
  }

  Future<void> _onCheckboxChanged(bool? val) async {
    final checked = val ?? false;
    setState(() => _dontShowAgain = checked);
    await StorageService().saveDismissDesktopPromo(checked);
  }

  Future<void> _openMicrosoftStore() async {
    try {
      final storeProtocolUri = Uri.parse(_storeProtocolUrl);
      if (await canLaunchUrl(storeProtocolUri)) {
        await launchUrl(storeProtocolUri,
            mode: LaunchMode.externalApplication);
        return;
      }
    } catch (_) {}

    try {
      final webUri = Uri.parse(_storeWebUrl);
      await launchUrl(webUri, mode: LaunchMode.externalApplication);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Could not open browser: $e'),
          backgroundColor: AppTheme.dangerRed,
        ),
      );
    }
  }

  Future<void> _shareToPc() async {
    const text = 'Print Ofo — Windows Print Server Companion\n'
        'Download from Microsoft Store to print from Android to USB printers:\n'
        '$_storeWebUrl\n\n'
        'Direct Windows Setup (.exe):\n'
        '$_directInstallerUrl';

    await Share.share(text, subject: 'Print Ofo for Windows');
  }

  Future<void> _copyLink() async {
    await Clipboard.setData(const ClipboardData(text: _storeWebUrl));
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Microsoft Store link copied to clipboard!'),
        backgroundColor: AppTheme.successGreen,
        duration: Duration(seconds: 2),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: AppTheme.cardDark,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: AppTheme.primaryBlue, width: 1.5),
      ),
      child: SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Header
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(10),
                    decoration: BoxDecoration(
                      color: AppTheme.primaryBlue.withValues(alpha: 0.2),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(
                      Icons.desktop_windows_rounded,
                      color: AppTheme.lightBlue,
                      size: 26,
                    ),
                  ),
                  const SizedBox(width: 12),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Print Ofo for Windows',
                          style: TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.bold,
                            color: AppTheme.textLight,
                          ),
                        ),
                        SizedBox(height: 2),
                        Text(
                          'Available on Microsoft Store',
                          style: TextStyle(
                            fontSize: 12,
                            color: AppTheme.accentCyan,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close, color: AppTheme.textMuted),
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
              const SizedBox(height: 16),

              // Description
              const Text(
                'Print Ofo connects to your Windows laptop or PC to send documents and photos directly to your connected USB printers (such as Canon, Epson, HP, etc.).',
                style: TextStyle(
                  fontSize: 13,
                  color: AppTheme.textMuted,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 14),

              // Instructions Card
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppTheme.bgDark,
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: AppTheme.cardBorderDark),
                ),
                child: const Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'QUICK 3-STEP SETUP:',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 0.8,
                        color: AppTheme.lightBlue,
                      ),
                    ),
                    SizedBox(height: 8),
                    Text(
                      '1. Download & run Print Ofo on your Windows PC.\n'
                      '2. Connect phone & laptop to same Wi-Fi.\n'
                      '3. Scan the QR code on your PC screen!',
                      style: TextStyle(
                        fontSize: 12,
                        color: AppTheme.textLight,
                        height: 1.45,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),

              // Main Actions: Microsoft Store & Share to PC
              ElevatedButton.icon(
                icon: const Icon(Icons.open_in_new, size: 18),
                label: const Text('Get from Microsoft Store'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.primaryBlue,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 12),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                onPressed: _openMicrosoftStore,
              ),
              const SizedBox(height: 10),

              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.share, size: 16),
                      label: const Text('Share to PC'),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 10),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10),
                        ),
                      ),
                      onPressed: _shareToPc,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton.icon(
                      icon: const Icon(Icons.copy, size: 16),
                      label: const Text('Copy Link'),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 10),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10),
                        ),
                      ),
                      onPressed: _copyLink,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Checkbox: Don't show again
              Row(
                children: [
                  SizedBox(
                    width: 24,
                    height: 24,
                    child: Checkbox(
                      value: _dontShowAgain,
                      onChanged: _onCheckboxChanged,
                      activeColor: AppTheme.primaryBlue,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: GestureDetector(
                      onTap: () => _onCheckboxChanged(!_dontShowAgain),
                      child: const Text(
                        "Don't show this popup on startup",
                        style: TextStyle(
                          fontSize: 11,
                          color: AppTheme.textMuted,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),

              // Bottom Dismiss / Proceed Button
              TextButton(
                onPressed: () {
                  Navigator.of(context).pop();
                  if (widget.onConnectPressed != null) {
                    widget.onConnectPressed!();
                  }
                },
                child: Text(
                  widget.autoOpened
                      ? 'I already have it running → Connect'
                      : 'Close',
                  style: const TextStyle(
                    fontSize: 13,
                    color: AppTheme.lightBlue,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
