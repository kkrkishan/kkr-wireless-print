class PrintOptions {
  int copies;
  String? pageRange;
  String orientation;
  String? paperSize;
  String colorMode;
  String scalingMode;
  bool centerOnPage;

  PrintOptions({
    this.copies = 1,
    this.pageRange,
    this.orientation = 'Auto',
    this.paperSize = 'A4',
    this.colorMode = 'Auto',
    this.scalingMode = 'FitPage',
    this.centerOnPage = true,
  });

  PrintOptions copyWith({
    int? copies,
    String? pageRange,
    String? orientation,
    String? paperSize,
    String? colorMode,
    String? scalingMode,
    bool? centerOnPage,
  }) {
    return PrintOptions(
      copies: copies ?? this.copies,
      pageRange: pageRange ?? this.pageRange,
      orientation: orientation ?? this.orientation,
      paperSize: paperSize ?? this.paperSize,
      colorMode: colorMode ?? this.colorMode,
      scalingMode: scalingMode ?? this.scalingMode,
      centerOnPage: centerOnPage ?? this.centerOnPage,
    );
  }

  Map<String, dynamic> toFormDataMap(String printerName, String idempotencyKey) {
    return {
      'printerName': printerName,
      'copies': copies.toString(),
      if (pageRange != null && pageRange!.trim().isNotEmpty)
        'pageRange': pageRange!.trim(),
      'orientation': orientation,
      if (paperSize != null && paperSize!.trim().isNotEmpty)
        'paperSize': paperSize!.trim(),
      'colorMode': colorMode,
      'scalingMode': scalingMode,
      'centerOnPage': centerOnPage.toString(),
      'idempotencyKey': idempotencyKey,
    };
  }
}
