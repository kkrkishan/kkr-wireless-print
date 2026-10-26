import 'dart:io';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:pdfx/pdfx.dart' as pdfx;
import '../../models/printer_info.dart';
import '../../models/print_options.dart';
import '../../services/storage_service.dart';
import '../theme/app_theme.dart';
import '../widgets/job_tracking_dialog.dart';

class PreviewAndSettingsScreen extends StatefulWidget {
  final File initialFile;
  final List<PrinterInfo> availablePrinters;
  final PrinterInfo? preselectedPrinter;

  const PreviewAndSettingsScreen({
    super.key,
    required this.initialFile,
    required this.availablePrinters,
    this.preselectedPrinter,
  });

  @override
  State<PreviewAndSettingsScreen> createState() =>
      _PreviewAndSettingsScreenState();
}

class _PreviewAndSettingsScreenState extends State<PreviewAndSettingsScreen> {
  late File _currentFile;
  late String _fileName;
  late String _extension;
  int _fileSizeBytes = 0;

  // Image metadata
  int? _imageWidth;
  int? _imageHeight;

  // PDF metadata
  int _pdfPageCount = 1;
  pdfx.PdfDocument? _pdfDocument;
  pdfx.PdfPageImage? _pdfFirstPageImage;
  bool _isLoadingPreview = true;

  // Settings
  PrinterInfo? _selectedPrinter;
  final PrintOptions _options = PrintOptions();
  final TextEditingController _pageRangeController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _currentFile = widget.initialFile;
    _inspectAndLoadFile();

    if (widget.availablePrinters.isNotEmpty) {
      _selectedPrinter = widget.preselectedPrinter ??
          widget.availablePrinters.firstWhere(
            (p) => p.isDefault,
            orElse: () => widget.availablePrinters.first,
          );
      _syncPrinterDefaults(_selectedPrinter!);
    }
  }

  @override
  void dispose() {
    _pdfDocument?.close();
    _pageRangeController.dispose();
    super.dispose();
  }

  void _syncPrinterDefaults(PrinterInfo printer) {
    if (printer.capabilities.supportedPaperSizes.isNotEmpty) {
      if (!printer.capabilities.supportedPaperSizes.contains(_options.paperSize)) {
        _options.paperSize = printer.capabilities.supportedPaperSizes.first;
      }
    }
    // If printer only supports monochrome, force Grayscale
    if (!printer.capabilities.supportsColor) {
      _options.colorMode = 'Grayscale';
    }
  }

  Future<void> _inspectAndLoadFile() async {
    setState(() => _isLoadingPreview = true);

    try {
      _fileName = _currentFile.path.split(Platform.pathSeparator).last;
      _extension = _fileName.split('.').last.toLowerCase();
      _fileSizeBytes = await _currentFile.length();

      if (_isPdf) {
        _pdfDocument?.close();
        _pdfDocument = await pdfx.PdfDocument.openFile(_currentFile.path);
        _pdfPageCount = _pdfDocument!.pagesCount;

        // Render first page thumbnail
        final page = await _pdfDocument!.getPage(1);
        _pdfFirstPageImage = await page.render(
          width: page.width * 1.5,
          height: page.height * 1.5,
          format: pdfx.PdfPageImageFormat.jpeg,
        );
        await page.close();
      } else if (_isImage) {
        final bytes = await _currentFile.readAsBytes();
        final decoded = await decodeImageFromList(bytes);
        _imageWidth = decoded.width;
        _imageHeight = decoded.height;
      }
    } catch (e) {
      debugPrint('Error inspecting file: $e');
    } finally {
      if (mounted) setState(() => _isLoadingPreview = false);
    }
  }

  bool get _isPdf => _extension == 'pdf';
  bool get _isImage =>
      _extension == 'jpg' || _extension == 'jpeg' || _extension == 'png';

  Future<void> _replaceFile() async {
    final result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png'],
    );

    if (result.isNotEmpty && result.first.path != null) {
      setState(() {
        _currentFile = File(result.first.path!);
      });
      await _inspectAndLoadFile();
    }
  }

  Future<void> _submitJob() async {
    if (_selectedPrinter == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please select a printer.'),
          backgroundColor: AppTheme.dangerRed,
        ),
      );
      return;
    }

    if (!_selectedPrinter!.isOnline) {
      final proceed = await showDialog<bool>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Printer Offline'),
          content: Text(
            '${_selectedPrinter!.name} reports offline or unavailable status in Windows. Spooler may queue or fail the job. Continue anyway?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Cancel'),
            ),
            ElevatedButton(
              onPressed: () => Navigator.of(ctx).pop(true),
              child: const Text('Continue'),
            ),
          ],
        ),
      );
      if (proceed != true) return;
    }

    _options.pageRange = _pageRangeController.text.trim();

    // Show tracking dialog
    if (!mounted) return;
    await showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => JobTrackingDialog(
        file: _currentFile,
        printer: _selectedPrinter!,
        options: _options,
      ),
    );

    if (mounted) {
      Navigator.of(context).pop(true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Print Preview & Settings'),
        actions: [
          IconButton(
            icon: const Icon(Icons.file_upload_outlined),
            tooltip: 'Replace File',
            onPressed: _replaceFile,
          ),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  _buildPreviewCard(),
                  const SizedBox(height: 16),
                  _buildPrinterSelector(),
                  const SizedBox(height: 16),
                  _buildSettingsCard(),
                ],
              ),
            ),
          ),
          _buildBottomBar(),
        ],
      ),
    );
  }

  Widget _buildPreviewCard() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  _isPdf ? Icons.picture_as_pdf : Icons.image,
                  color: _isPdf ? AppTheme.dangerRed : AppTheme.accentCyan,
                  size: 28,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        _fileName,
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 14,
                          color: AppTheme.textLight,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '${(_fileSizeBytes / 1024).toStringAsFixed(1)} KB'
                        '${_isPdf ? ' • $_pdfPageCount Page${_pdfPageCount > 1 ? 's' : ''}' : ''}'
                        '${_isImage && _imageWidth != null ? ' • ${_imageWidth}x$_imageHeight px' : ''}',
                        style: const TextStyle(
                          color: AppTheme.textMuted,
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
                OutlinedButton(
                  onPressed: _replaceFile,
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  ),
                  child: const Text('Change', style: TextStyle(fontSize: 12)),
                ),
              ],
            ),
            const SizedBox(height: 14),
            // Preview viewport
            Container(
              height: 220,
              width: double.infinity,
              decoration: BoxDecoration(
                color: const Color(0xFF0F172A),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: AppTheme.cardBorderDark),
              ),
              child: _isLoadingPreview
                  ? const Center(
                      child: CircularProgressIndicator(color: AppTheme.primaryBlue),
                    )
                  : _buildRenderedContent(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildRenderedContent() {
    if (_isPdf && _pdfFirstPageImage != null) {
      return Center(
        child: Image.memory(
          _pdfFirstPageImage!.bytes,
          fit: BoxFit.contain,
        ),
      );
    } else if (_isImage) {
      return Center(
        child: Image.file(
          _currentFile,
          fit: BoxFit.contain,
        ),
      );
    }
    return const Center(
      child: Text(
        'Document ready to print',
        style: TextStyle(color: AppTheme.textMuted),
      ),
    );
  }

  Widget _buildPrinterSelector() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'TARGET PRINTER',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: AppTheme.textMuted,
                letterSpacing: 1,
              ),
            ),
            const SizedBox(height: 12),
            if (widget.availablePrinters.isEmpty)
              const Text(
                'No installed Windows printers available.',
                style: TextStyle(color: AppTheme.warningAmber),
              )
            else
              DropdownButtonFormField<PrinterInfo>(
                initialValue: _selectedPrinter,
                decoration: const InputDecoration(
                  prefixIcon: Icon(Icons.print, color: AppTheme.primaryBlue),
                ),
                items: widget.availablePrinters.map((printer) {
                  return DropdownMenuItem<PrinterInfo>(
                    value: printer,
                    child: Row(
                      children: [
                        Container(
                          width: 8,
                          height: 8,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: printer.isOnline
                                ? AppTheme.successGreen
                                : AppTheme.dangerRed,
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            printer.name,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(fontSize: 14),
                          ),
                        ),
                        if (printer.isDefault)
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: AppTheme.primaryBlue.withValues(alpha: 0.2),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: const Text(
                              'Default',
                              style: TextStyle(
                                  fontSize: 10, color: AppTheme.lightBlue),
                            ),
                          ),
                      ],
                    ),
                  );
                }).toList(),
                onChanged: (printer) {
                  if (printer != null) {
                    setState(() {
                      _selectedPrinter = printer;
                      _syncPrinterDefaults(printer);
                    });
                    StorageService().saveSelectedPrinter(printer.name);
                  }
                },
              ),
            if (_selectedPrinter != null) ...[
              const SizedBox(height: 8),
              Wrap(
                spacing: 8,
                runSpacing: 4,
                children: [
                  _badge(
                    'Port: ${_selectedPrinter!.port}',
                    AppTheme.cardBorderDark,
                    AppTheme.textMuted,
                  ),
                  _badge(
                    _selectedPrinter!.capabilities.supportsColor
                        ? 'Color Support'
                        : 'Monochrome Only',
                    _selectedPrinter!.capabilities.supportsColor
                        ? AppTheme.accentCyan.withValues(alpha: 0.2)
                        : Colors.grey.withValues(alpha: 0.2),
                    _selectedPrinter!.capabilities.supportsColor
                        ? AppTheme.accentCyan
                        : AppTheme.textMuted,
                  ),
                  if (_selectedPrinter!.capabilities.canDuplex)
                    _badge(
                      'Duplex',
                      AppTheme.successGreen.withValues(alpha: 0.2),
                      AppTheme.successGreen,
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildSettingsCard() {
    final supportsColor = _selectedPrinter?.capabilities.supportsColor ?? true;
    final paperSizes =
        _selectedPrinter?.capabilities.supportedPaperSizes ?? ['Letter', 'A4'];

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'PRINT SETTINGS',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.bold,
                color: AppTheme.textMuted,
                letterSpacing: 1,
              ),
            ),
            const SizedBox(height: 16),

            // Copies Stepper
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Copies',
                        style: TextStyle(
                            fontSize: 14, fontWeight: FontWeight.w600)),
                    Text('Number of print copies',
                        style:
                            TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                  ],
                ),
                Row(
                  children: [
                    IconButton(
                      icon: const Icon(Icons.remove_circle_outline),
                      color: AppTheme.lightBlue,
                      onPressed: _options.copies > 1
                          ? () => setState(() => _options.copies--)
                          : null,
                    ),
                    Text(
                      '${_options.copies}',
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: AppTheme.textLight,
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.add_circle_outline),
                      color: AppTheme.lightBlue,
                      onPressed: _options.copies <
                              (_selectedPrinter?.capabilities.maxCopies ?? 99)
                          ? () => setState(() => _options.copies++)
                          : null,
                    ),
                  ],
                ),
              ],
            ),
            const Divider(color: AppTheme.cardBorderDark, height: 24),

            // Page Range (for PDF only)
            if (_isPdf) ...[
              const Text('Page Range',
                  style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
              const SizedBox(height: 4),
              TextField(
                controller: _pageRangeController,
                decoration: InputDecoration(
                  hintText: 'e.g. 1-3, 5 (Leave blank for all $_pdfPageCount pages)',
                  prefixIcon:
                      const Icon(Icons.format_list_numbered, color: AppTheme.textMuted),
                ),
              ),
              const Divider(color: AppTheme.cardBorderDark, height: 24),
            ],

            // Paper Size
            const Text('Paper Size',
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            DropdownButtonFormField<String>(
              initialValue: paperSizes.contains(_options.paperSize)
                  ? _options.paperSize
                  : paperSizes.firstOrNull ?? 'A4',
              items: paperSizes
                  .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                  .toList(),
              onChanged: (val) {
                if (val != null) setState(() => _options.paperSize = val);
              },
            ),
            const Divider(color: AppTheme.cardBorderDark, height: 24),

            // Orientation
            const Text('Orientation',
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(value: 'Auto', label: Text('Auto')),
                ButtonSegment(value: 'Portrait', label: Text('Portrait')),
                ButtonSegment(value: 'Landscape', label: Text('Landscape')),
              ],
              selected: {_options.orientation},
              onSelectionChanged: (val) {
                setState(() => _options.orientation = val.first);
              },
            ),
            const Divider(color: AppTheme.cardBorderDark, height: 24),

            // Color Mode
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Color Mode',
                        style: TextStyle(
                            fontSize: 14, fontWeight: FontWeight.w600)),
                    if (!supportsColor)
                      const Text('(Selected printer is monochrome)',
                          style: TextStyle(
                              fontSize: 11, color: AppTheme.warningAmber)),
                  ],
                ),
                SegmentedButton<String>(
                  segments: [
                    ButtonSegment(
                      value: 'Color',
                      label: const Text('Color'),
                      enabled: supportsColor,
                    ),
                    const ButtonSegment(
                      value: 'Grayscale',
                      label: Text('B & W'),
                    ),
                  ],
                  selected: {_options.colorMode},
                  onSelectionChanged: (val) {
                    setState(() => _options.colorMode = val.first);
                  },
                ),
              ],
            ),
            const Divider(color: AppTheme.cardBorderDark, height: 24),

            // Scaling Mode
            const Text('Scaling / Fit',
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(value: 'FitPage', label: Text('Fit Page')),
                ButtonSegment(value: 'FillPage', label: Text('Fill')),
                ButtonSegment(value: 'ActualSize', label: Text('100%')),
              ],
              selected: {_options.scalingMode},
              onSelectionChanged: (val) {
                setState(() => _options.scalingMode = val.first);
              },
            ),
            const SizedBox(height: 12),

            // Center on Page Checkbox
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Center on Page',
                  style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
              subtitle: const Text('Preserves margins and centers content',
                  style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
              value: _options.centerOnPage,
              onChanged: (val) => setState(() => _options.centerOnPage = val),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBottomBar() {
    return Container(
      padding: const EdgeInsets.all(16),
      color: AppTheme.cardDark,
      child: SafeArea(
        top: false,
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    _selectedPrinter?.name ?? 'No printer selected',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                      color: AppTheme.textLight,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  Text(
                    '${_options.copies} Cop${_options.copies > 1 ? 'ies' : 'y'} • ${_options.orientation} • ${_options.paperSize}',
                    style: const TextStyle(
                      fontSize: 11,
                      color: AppTheme.textMuted,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 12),
            ElevatedButton.icon(
              icon: const Icon(Icons.print),
              label: const Text('Print Now'),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppTheme.primaryBlue,
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
              ),
              onPressed: _selectedPrinter != null ? _submitJob : null,
            ),
          ],
        ),
      ),
    );
  }

  Widget _badge(String text, Color bg, Color fg) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Text(
        text,
        style: TextStyle(fontSize: 11, color: fg, fontWeight: FontWeight.w500),
      ),
    );
  }
}
