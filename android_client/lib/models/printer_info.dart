class PrinterCapabilities {
  final List<String> supportedPaperSizes;
  final bool supportsColor;
  final bool canDuplex;
  final int maxCopies;

  const PrinterCapabilities({
    this.supportedPaperSizes = const ['Letter', 'A4'],
    this.supportsColor = false,
    this.canDuplex = false,
    this.maxCopies = 99,
  });

  factory PrinterCapabilities.fromJson(Map<String, dynamic>? json) {
    if (json == null) return const PrinterCapabilities();
    return PrinterCapabilities(
      supportedPaperSizes: (json['supportedPaperSizes'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          ['Letter', 'A4'],
      supportsColor: json['supportsColor'] as bool? ?? false,
      canDuplex: json['canDuplex'] as bool? ?? false,
      maxCopies: json['maxCopies'] as int? ?? 99,
    );
  }

  Map<String, dynamic> toJson() => {
        'supportedPaperSizes': supportedPaperSizes,
        'supportsColor': supportsColor,
        'canDuplex': canDuplex,
        'maxCopies': maxCopies,
      };
}

class PrinterInfo {
  final String name;
  final String port;
  final String driverName;
  final bool isDefault;
  final bool isOnline;
  final String status;
  final PrinterCapabilities capabilities;

  const PrinterInfo({
    required this.name,
    this.port = 'USB',
    this.driverName = '',
    this.isDefault = false,
    this.isOnline = true,
    this.status = 'Ready',
    this.capabilities = const PrinterCapabilities(),
  });

  factory PrinterInfo.fromJson(Map<String, dynamic> json) {
    return PrinterInfo(
      name: json['name'] as String? ?? 'Unknown Printer',
      port: json['port'] as String? ?? 'USB',
      driverName: json['driverName'] as String? ?? '',
      isDefault: json['isDefault'] as bool? ?? false,
      isOnline: json['isOnline'] as bool? ?? true,
      status: json['status'] as String? ?? 'Ready',
      capabilities: PrinterCapabilities.fromJson(
        json['capabilities'] as Map<String, dynamic>?,
      ),
    );
  }

  Map<String, dynamic> toJson() => {
        'name': name,
        'port': port,
        'driverName': driverName,
        'isDefault': isDefault,
        'isOnline': isOnline,
        'status': status,
        'capabilities': capabilities.toJson(),
      };
}
