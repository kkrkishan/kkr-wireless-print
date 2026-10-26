import 'package:flutter_test/flutter_test.dart';
import 'package:kkr_wireless_print/models/server_connection.dart';
import 'package:kkr_wireless_print/models/printer_info.dart';
import 'package:kkr_wireless_print/models/print_job.dart';
import 'package:kkr_wireless_print/models/print_options.dart';

void main() {
  group('ServerConnection Model Tests', () {
    test('Serializes and deserializes correctly', () {
      final conn = ServerConnection(
        ip: '10.134.68.221',
        httpPort: 8080,
        httpsPort: 8443,
        pin: '123456',
        thumbprint: 'AABBCCDDEEFF',
        token: 'test-jwt-token',
        serverName: 'KKR Wireless Print Server',
      );

      final json = conn.toJson();
      expect(json['ip'], '10.134.68.221');
      expect(json['httpPort'], 8080);
      expect(json['httpsPort'], 8443);
      expect(json['pin'], '123456');

      final restored = ServerConnection.fromJson(json);
      expect(restored.ip, '10.134.68.221');
      expect(restored.httpPort, 8080);
      expect(restored.token, 'test-jwt-token');
      expect(restored.baseUrlHttp, 'http://10.134.68.221:8080');
      expect(restored.baseUrlHttps, 'https://10.134.68.221:8443');
    });
  });

  group('PrinterInfo Model Tests', () {
    test('Parses Epson color printer JSON correctly', () {
      final json = {
        'name': 'EPSON L3210 Series',
        'port': 'USB001',
        'driverName': 'EPSON L3210 Series',
        'isDefault': true,
        'isOnline': true,
        'status': 'Ready',
        'capabilities': {
          'supportedPaperSizes': ['Letter', 'A4', 'Legal', '4x6'],
          'supportsColor': true,
          'canDuplex': false,
          'maxCopies': 99,
        }
      };

      final printer = PrinterInfo.fromJson(json);
      expect(printer.name, 'EPSON L3210 Series');
      expect(printer.port, 'USB001');
      expect(printer.isDefault, true);
      expect(printer.isOnline, true);
      expect(printer.capabilities.supportsColor, true);
      expect(printer.capabilities.supportedPaperSizes, contains('A4'));
      expect(printer.capabilities.maxCopies, 99);
    });

    test('Parses Canon monochrome printer JSON correctly', () {
      final json = {
        'name': 'Canon MF4800 Series UFRII LT',
        'port': 'USB004',
        'driverName': 'Canon MF4800 Series UFRII LT',
        'isDefault': false,
        'isOnline': true,
        'status': 'Ready',
        'capabilities': {
          'supportedPaperSizes': ['Letter', 'A4', 'Legal'],
          'supportsColor': false,
          'canDuplex': false,
          'maxCopies': 99,
        }
      };

      final printer = PrinterInfo.fromJson(json);
      expect(printer.name, 'Canon MF4800 Series UFRII LT');
      expect(printer.capabilities.supportsColor, false);
      expect(printer.capabilities.canDuplex, false);
    });
  });

  group('PrintJob & JobState Tests', () {
    test('Parses job status transitions', () {
      expect(JobState.fromString('Accepted'), JobState.accepted);
      expect(JobState.fromString('Queued'), JobState.queued);
      expect(JobState.fromString('Processing'), JobState.processing);
      expect(JobState.fromString('Completed'), JobState.completed);
      expect(JobState.fromString('Failed'), JobState.failed);
      expect(JobState.fromString('Cancelled'), JobState.cancelled);
      expect(JobState.fromString('canceled'), JobState.cancelled);

      expect(JobState.completed.isTerminal, true);
      expect(JobState.failed.isTerminal, true);
      expect(JobState.cancelled.isTerminal, true);
      expect(JobState.processing.isTerminal, false);
    });

    test('Formats file size properly', () {
      final job = PrintJob(
        jobId: '1111-2222',
        status: JobState.accepted,
        printerName: 'EPSON L3210 Series',
        documentName: 'test.pdf',
        fileSizeBytes: 2048 * 1024, // 2MB
        createdAt: DateTime.now(),
      );

      expect(job.formattedFileSize, '2.0 MB');
    });
  });

  group('PrintOptions Tests', () {
    test('Converts options to API form map', () {
      final options = PrintOptions(
        copies: 3,
        pageRange: '1-2',
        orientation: 'Landscape',
        paperSize: 'A4',
        colorMode: 'Grayscale',
        scalingMode: 'FitPage',
        centerOnPage: true,
      );

      final map = options.toFormDataMap('EPSON L3210 Series', 'key-123');
      expect(map['printerName'], 'EPSON L3210 Series');
      expect(map['copies'], '3');
      expect(map['pageRange'], '1-2');
      expect(map['orientation'], 'Landscape');
      expect(map['paperSize'], 'A4');
      expect(map['colorMode'], 'Grayscale');
      expect(map['scalingMode'], 'FitPage');
      expect(map['centerOnPage'], 'true');
      expect(map['idempotencyKey'], 'key-123');
    });
  });
}
