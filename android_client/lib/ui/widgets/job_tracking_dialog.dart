import 'dart:async';
import 'dart:io';
import 'package:flutter/material.dart';
import '../../models/printer_info.dart';
import '../../models/print_options.dart';
import '../../models/print_job.dart';
import '../../services/api_service.dart';
import '../../services/storage_service.dart';
import '../theme/app_theme.dart';

class JobTrackingDialog extends StatefulWidget {
  final File file;
  final PrinterInfo printer;
  final PrintOptions options;

  const JobTrackingDialog({
    super.key,
    required this.file,
    required this.printer,
    required this.options,
  });

  @override
  State<JobTrackingDialog> createState() => _JobTrackingDialogState();
}

class _JobTrackingDialogState extends State<JobTrackingDialog> {
  double _uploadProgress = 0.0;
  bool _isUploading = true;
  String? _jobId;
  JobState _currentState = JobState.accepted;
  String? _statusMessage;
  String? _errorMessage;
  Timer? _pollingTimer;
  bool _isCancelling = false;

  @override
  void initState() {
    super.initState();
    _startSubmission();
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  Future<void> _startSubmission() async {
    setState(() {
      _isUploading = true;
      _uploadProgress = 0.0;
      _statusMessage = 'Uploading document to Windows server...';
    });

    final api = ApiService();
    final result = await api.submitPrintJob(
      file: widget.file,
      printerName: widget.printer.name,
      options: widget.options,
      onSendProgress: (sent, total) {
        if (total > 0 && mounted) {
          setState(() {
            _uploadProgress = sent / total;
          });
        }
      },
    );

    if (!mounted) return;

    if (!result.success || result.data == null) {
      setState(() {
        _isUploading = false;
        _currentState = JobState.failed;
        _errorMessage = result.errorMessage ?? 'Upload failed.';
      });
      return;
    }

    final job = result.data!;
    setState(() {
      _isUploading = false;
      _jobId = job.jobId;
      _currentState = job.status;
      _statusMessage = job.statusMessage ?? 'Accepted by Windows spooler.';
    });

    // Save initial job to local history
    await StorageService().addRecentJob(job);

    // Start polling status
    _startPolling(job.jobId);
  }

  void _startPolling(String jobId) {
    _pollingTimer?.cancel();
    _pollingTimer = Timer.periodic(const Duration(milliseconds: 1500), (t) async {
      final res = await ApiService().getJobStatus(jobId);
      if (!mounted) {
        t.cancel();
        return;
      }

      if (res.success && res.data != null) {
        final job = res.data!;
        setState(() {
          _currentState = job.status;
          _statusMessage = job.statusMessage;
          _errorMessage = job.errorMessage;
        });

        // Update local storage
        await StorageService().updateJobStatus(
          job.jobId,
          job.status,
          statusMessage: job.statusMessage,
          errorMessage: job.errorMessage,
        );

        if (job.status.isTerminal) {
          t.cancel();
        }
      }
    });
  }

  Future<void> _cancelJob() async {
    if (_jobId == null || _isCancelling) return;

    setState(() {
      _isCancelling = true;
      _statusMessage = 'Requesting cancellation...';
    });

    final res = await ApiService().cancelJob(_jobId!);
    if (mounted) {
      setState(() => _isCancelling = false);
      if (res.success) {
        setState(() {
          _currentState = JobState.cancelled;
          _statusMessage = 'Job cancelled by user.';
        });
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(res.errorMessage ?? 'Could not cancel job.'),
            backgroundColor: AppTheme.dangerRed,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final fileName = widget.file.path.split(Platform.pathSeparator).last;

    return AlertDialog(
      backgroundColor: AppTheme.cardDark,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: const BorderSide(color: AppTheme.cardBorderDark),
      ),
      title: Row(
        children: [
          _buildStatusIcon(),
          const SizedBox(width: 12),
          const Expanded(
            child: Text(
              'Print Job Progress',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
      content: SizedBox(
        width: double.maxFinite,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              fileName,
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
              'Printer: ${widget.printer.name}',
              style: const TextStyle(color: AppTheme.textMuted, fontSize: 12),
            ),
            if (_jobId != null) ...[
              const SizedBox(height: 4),
              Text(
                'Job ID: ${_jobId!.substring(0, 8)}...',
                style: const TextStyle(
                  color: AppTheme.accentCyan,
                  fontSize: 11,
                  fontFamily: 'monospace',
                ),
              ),
            ],
            const SizedBox(height: 20),

            // Progress bar
            if (_isUploading) ...[
              LinearProgressIndicator(
                value: _uploadProgress,
                backgroundColor: AppTheme.bgDark,
                color: AppTheme.primaryBlue,
                minHeight: 8,
                borderRadius: BorderRadius.circular(4),
              ),
              const SizedBox(height: 8),
              Text(
                'Uploading: ${(_uploadProgress * 100).toInt()}%',
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 12, color: AppTheme.lightBlue),
              ),
            ] else ...[
              _buildTimeline(),
            ],

            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppTheme.bgDark,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: AppTheme.cardBorderDark),
              ),
              child: Text(
                _errorMessage != null
                    ? '⚠️ Error: $_errorMessage'
                    : (_statusMessage ?? 'Processing...'),
                style: TextStyle(
                  fontSize: 12,
                  color: _errorMessage != null
                      ? AppTheme.dangerRed
                      : AppTheme.textLight,
                ),
              ),
            ),
          ],
        ),
      ),
      actions: [
        if (!_currentState.isTerminal && _jobId != null)
          OutlinedButton(
            onPressed: _isCancelling ? null : _cancelJob,
            style: OutlinedButton.styleFrom(
              foregroundColor: AppTheme.dangerRed,
              side: const BorderSide(color: AppTheme.dangerRed),
            ),
            child: _isCancelling
                ? const SizedBox(
                    width: 14,
                    height: 14,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Text('Cancel Job'),
          ),
        ElevatedButton(
          onPressed: _isUploading && !_currentState.isTerminal
              ? null
              : () => Navigator.of(context).pop(),
          child: Text(_currentState.isTerminal ? 'Done' : 'Background'),
        ),
      ],
    );
  }

  Widget _buildStatusIcon() {
    if (_isUploading) {
      return const SizedBox(
        width: 24,
        height: 24,
        child: CircularProgressIndicator(strokeWidth: 2.5),
      );
    }
    switch (_currentState) {
      case JobState.completed:
        return const Icon(Icons.check_circle,
            color: AppTheme.successGreen, size: 28);
      case JobState.failed:
        return const Icon(Icons.error, color: AppTheme.dangerRed, size: 28);
      case JobState.cancelled:
        return const Icon(Icons.cancel, color: AppTheme.warningAmber, size: 28);
      case JobState.processing:
        return const SizedBox(
          width: 24,
          height: 24,
          child: CircularProgressIndicator(
            strokeWidth: 2.5,
            color: AppTheme.lightBlue,
          ),
        );
      default:
        return const Icon(Icons.hourglass_top,
            color: AppTheme.warningAmber, size: 26);
    }
  }

  Widget _buildTimeline() {
    final steps = [
      {'label': 'Accepted', 'done': true},
      {
        'label': 'Queued',
        'done': _currentState != JobState.accepted,
      },
      {
        'label': 'Spooling',
        'done': _currentState == JobState.processing ||
            _currentState == JobState.completed,
      },
      {
        'label': 'Completed',
        'done': _currentState == JobState.completed,
      },
    ];

    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: steps.map((s) {
        final isDone = s['done'] as bool;
        final label = s['label'] as String;

        return Expanded(
          child: Column(
            children: [
              Container(
                width: 20,
                height: 20,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: isDone ? AppTheme.primaryBlue : AppTheme.bgDark,
                  border: Border.all(
                    color: isDone ? AppTheme.primaryBlue : AppTheme.cardBorderDark,
                    width: 2,
                  ),
                ),
                child: isDone
                    ? const Icon(Icons.check, size: 12, color: Colors.white)
                    : null,
              ),
              const SizedBox(height: 4),
              Text(
                label,
                style: TextStyle(
                  fontSize: 10,
                  color: isDone ? AppTheme.textLight : AppTheme.textMuted,
                  fontWeight: isDone ? FontWeight.bold : FontWeight.normal,
                ),
              ),
            ],
          ),
        );
      }).toList(),
    );
  }
}
