enum JobState {
  accepted,
  queued,
  processing,
  completed,
  failed,
  cancelled,
  unknown;

  static JobState fromString(String? val) {
    if (val == null) return JobState.unknown;
    switch (val.toLowerCase()) {
      case 'accepted':
        return JobState.accepted;
      case 'queued':
        return JobState.queued;
      case 'processing':
        return JobState.processing;
      case 'completed':
        return JobState.completed;
      case 'failed':
        return JobState.failed;
      case 'cancelled':
      case 'canceled':
        return JobState.cancelled;
      default:
        return JobState.unknown;
    }
  }

  String get label {
    switch (this) {
      case JobState.accepted:
        return 'Accepted';
      case JobState.queued:
        return 'Queued';
      case JobState.processing:
        return 'Processing';
      case JobState.completed:
        return 'Completed';
      case JobState.failed:
        return 'Failed';
      case JobState.cancelled:
        return 'Cancelled';
      case JobState.unknown:
        return 'Pending';
    }
  }

  bool get isTerminal =>
      this == JobState.completed ||
      this == JobState.failed ||
      this == JobState.cancelled;

  bool get isSuccess => this == JobState.completed;
}

class PrintJob {
  final String jobId;
  final JobState status;
  final String? statusMessage;
  final String printerName;
  final String documentName;
  final String documentType;
  final int fileSizeBytes;
  final int pageCount;
  final DateTime createdAt;
  final DateTime? startedAt;
  final DateTime? completedAt;
  final String? errorMessage;
  final String? settingsSummary;
  final bool isOwnedByRequester;

  const PrintJob({
    required this.jobId,
    required this.status,
    this.statusMessage,
    required this.printerName,
    required this.documentName,
    this.documentType = 'Pdf',
    this.fileSizeBytes = 0,
    this.pageCount = 1,
    required this.createdAt,
    this.startedAt,
    this.completedAt,
    this.errorMessage,
    this.settingsSummary,
    this.isOwnedByRequester = true,
  });

  factory PrintJob.fromJson(Map<String, dynamic> json) {
    return PrintJob(
      jobId: json['jobId'] as String? ?? '',
      status: JobState.fromString(json['status'] as String?),
      statusMessage: json['statusMessage'] as String?,
      printerName: json['printerName'] as String? ?? 'Printer',
      documentName: json['documentName'] as String? ?? 'Document',
      documentType: json['documentType'] as String? ?? 'Pdf',
      fileSizeBytes: json['fileSizeBytes'] as int? ?? 0,
      pageCount: json['pageCount'] as int? ?? 1,
      createdAt: json['createdAtUtc'] != null
          ? DateTime.tryParse(json['createdAtUtc'] as String)?.toLocal() ??
              DateTime.now()
          : DateTime.now(),
      startedAt: json['startedAtUtc'] != null
          ? DateTime.tryParse(json['startedAtUtc'] as String)?.toLocal()
          : null,
      completedAt: json['completedAtUtc'] != null
          ? DateTime.tryParse(json['completedAtUtc'] as String)?.toLocal()
          : null,
      errorMessage: json['errorMessage'] as String?,
      settingsSummary: json['settingsSummary'] as String?,
      isOwnedByRequester: json['isOwnedByRequester'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() => {
        'jobId': jobId,
        'status': status.name,
        'statusMessage': statusMessage,
        'printerName': printerName,
        'documentName': documentName,
        'documentType': documentType,
        'fileSizeBytes': fileSizeBytes,
        'pageCount': pageCount,
        'createdAtUtc': createdAt.toIso8601String(),
        'startedAtUtc': startedAt?.toIso8601String(),
        'completedAtUtc': completedAt?.toIso8601String(),
        'errorMessage': errorMessage,
        'settingsSummary': settingsSummary,
        'isOwnedByRequester': isOwnedByRequester,
      };

  String get formattedFileSize {
    if (fileSizeBytes < 1024) return '$fileSizeBytes B';
    if (fileSizeBytes < 1024 * 1024) {
      return '${(fileSizeBytes / 1024).toStringAsFixed(1)} KB';
    }
    return '${(fileSizeBytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }
}
