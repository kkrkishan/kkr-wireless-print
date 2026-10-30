import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:uuid/uuid.dart';
import '../models/server_connection.dart';
import '../models/printer_info.dart';
import '../models/print_job.dart';
import '../models/print_options.dart';

class ApiResult<T> {
  final bool success;
  final T? data;
  final String? errorMessage;
  final int? statusCode;

  const ApiResult.success(this.data)
      : success = true,
        errorMessage = null,
        statusCode = 200;

  const ApiResult.failure(this.errorMessage, {this.statusCode})
      : success = false,
        data = null;
}

class ApiService {
  static final ApiService _instance = ApiService._internal();
  factory ApiService() => _instance;

  late Dio _dio;
  ServerConnection? _currentConnection;
  String? _authToken;
  String _activeBaseUrl = '';

  ApiService._internal() {
    _dio = Dio(BaseOptions(
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 25),
      sendTimeout: const Duration(seconds: 60),
    ));

    // Trust self-signed certificates on local network (for HTTPS)
    _dio.httpClientAdapter = IOHttpClientAdapter(
      createHttpClient: () {
        final client = HttpClient();
        client.badCertificateCallback =
            (X509Certificate cert, String host, int port) => true;
        return client;
      },
    );
  }

  void configure(ServerConnection connection) {
    _currentConnection = connection;
    _authToken = connection.token;
    _activeBaseUrl = connection.effectiveBaseUrl;

    _dio.options.headers['Authorization'] =
        _authToken != null ? 'Bearer $_authToken' : null;
  }

  void setAuthToken(String token) {
    _authToken = token;
    _dio.options.headers['Authorization'] = 'Bearer $token';
  }

  ServerConnection? get currentConnection => _currentConnection;
  bool get isAuthenticated => _authToken != null && _authToken!.isNotEmpty;
  String get activeBaseUrl => _activeBaseUrl;

  /// Discover Print Ofo Windows Server on local Wi-Fi via UDP broadcast (port 8089)
  Future<Map<String, dynamic>?> discoverServer({
    Duration timeout = const Duration(seconds: 3),
  }) async {
    RawDatagramSocket? socket;
    try {
      socket = await RawDatagramSocket.bind(InternetAddress.anyIPv4, 0);
      socket.broadcastEnabled = true;

      final completer = Completer<Map<String, dynamic>?>();
      final probeBytes = utf8.encode('PRINT_OFO_DISCOVER');

      // Send to broadcast address
      socket.send(probeBytes, InternetAddress('255.255.255.255'), 8089);

      final timer = Timer(timeout, () {
        if (!completer.isCompleted) {
          completer.complete(null);
        }
      });

      socket.listen((event) {
        if (event == RawSocketEvent.read && socket != null) {
          final datagram = socket.receive();
          if (datagram != null) {
            try {
              final text = utf8.decode(datagram.data);
              final map = jsonDecode(text) as Map<String, dynamic>;
              if (map['serverName'] != null || map['ip'] != null) {
                map['ip'] ??= datagram.address.address;
                timer.cancel();
                if (!completer.isCompleted) {
                  completer.complete(map);
                }
              }
            } catch (_) {}
          }
        }
      });

      final result = await completer.future;
      socket.close();
      return result;
    } catch (_) {
      socket?.close();
      return null;
    }
  }

  /// Auto-reconnect with persistent credentials.
  /// Tries saved IP first; if DHCP changed the laptop IP, scans via UDP discovery,
  /// re-binds the active IP seamlessly, and queries printers.
  Future<ApiResult<List<PrinterInfo>>> autoReconnectAndGetPrinters(
    ServerConnection savedConnection, {
    Function(ServerConnection updated)? onIpUpdated,
  }) async {
    configure(savedConnection);

    // 1. Try saved endpoint first
    var printerRes = await getPrinters();
    if (printerRes.success) {
      return printerRes;
    }

    // 2. If saved endpoint is unreachable, auto-discover laptop over Wi-Fi
    final discovered = await discoverServer();
    if (discovered != null && discovered['ip'] != null) {
      final newIp = discovered['ip'].toString();
      final httpPort = discovered['httpPort'] as int? ?? savedConnection.httpPort;
      final httpsPort =
          discovered['httpsPort'] as int? ?? savedConnection.httpsPort;

      final updatedConnection = savedConnection.copyWith(
        ip: newIp,
        httpPort: httpPort,
        httpsPort: httpsPort,
        isConnected: true,
      );

      configure(updatedConnection);
      printerRes = await getPrinters();
      if (printerRes.success) {
        onIpUpdated?.call(updatedConnection);
        return printerRes;
      }
    }

    return printerRes;
  }

  /// Test connection & discover server metadata
  Future<ApiResult<Map<String, dynamic>>> testAndDiscover({
    required String ip,
    int httpPort = 8080,
    int httpsPort = 8443,
  }) async {
    // Try HTTP first for fastest LAN response, fallback to HTTPS
    final urlsToTry = [
      'http://$ip:$httpPort',
      if (httpsPort > 0) 'https://$ip:$httpsPort',
    ];

    String? lastError;
    for (final base in urlsToTry) {
      try {
        final response = await _dio.get(
          '$base/api/v1/discovery',
          options: Options(
            sendTimeout: const Duration(seconds: 4),
            receiveTimeout: const Duration(seconds: 4),
          ),
        );

        if (response.statusCode == 200 && response.data is Map) {
          _activeBaseUrl = base;
          return ApiResult.success(Map<String, dynamic>.from(response.data));
        }
      } on DioException catch (e) {
        lastError = _formatDioError(e);
      } catch (e) {
        lastError = e.toString();
      }
    }

    return ApiResult.failure(
      lastError ??
          'Unable to reach Windows Print Server. Verify laptop IP ($ip) and Wi-Fi connection.',
    );
  }

  /// Pair with Windows Print Server using 6-digit PIN
  Future<ApiResult<Map<String, dynamic>>> pair({
    required String ip,
    int httpPort = 8080,
    int httpsPort = 8443,
    required String pin,
    String deviceName = 'Android Mobile',
  }) async {
    final urlsToTry = [
      if (_activeBaseUrl.isNotEmpty) _activeBaseUrl,
      'http://$ip:$httpPort',
      if (httpsPort > 0) 'https://$ip:$httpsPort',
    ].toSet().toList();

    String? lastError;
    for (final base in urlsToTry) {
      try {
        final response = await _dio.post(
          '$base/api/v1/auth/pair',
          data: {
            'pin': pin.trim(),
            'deviceName': deviceName,
            'clientType': 'Android',
          },
          options: Options(
            headers: {'Content-Type': 'application/json'},
            sendTimeout: const Duration(seconds: 5),
            receiveTimeout: const Duration(seconds: 8),
          ),
        );

        if (response.statusCode == 200 && response.data is Map) {
          final data = Map<String, dynamic>.from(response.data);
          final token = data['token'] as String?;
          if (token != null) {
            _activeBaseUrl = base;
            setAuthToken(token);
            return ApiResult.success(data);
          }
        }
      } on DioException catch (e) {
        if (e.response?.statusCode == 400) {
          final data = e.response?.data;
          final msg = (data is Map && data['errorMessage'] != null)
              ? data['errorMessage'].toString()
              : (data is Map && data['message'] != null)
                  ? data['message'].toString()
                  : 'Invalid or expired PIN. Please enter the current PIN from the Windows server.';
          return ApiResult.failure(msg, statusCode: 400);
        }
        lastError = _formatDioError(e);
      } catch (e) {
        lastError = e.toString();
      }
    }

    return ApiResult.failure(lastError ?? 'Pairing failed. Server unreachable.');
  }

  /// Get list of installed Windows printers
  Future<ApiResult<List<PrinterInfo>>> getPrinters() async {
    try {
      final response = await _dio.get('$_activeBaseUrl/api/v1/printers');
      if (response.statusCode == 200 && response.data is List) {
        final list = (response.data as List)
            .map((item) => PrinterInfo.fromJson(Map<String, dynamic>.from(item)))
            .toList();
        return ApiResult.success(list);
      }
      return const ApiResult.failure('Failed to parse printer list');
    } on DioException catch (e) {
      return ApiResult.failure(_formatDioError(e),
          statusCode: e.response?.statusCode);
    } catch (e) {
      return ApiResult.failure(e.toString());
    }
  }

  /// Submit a print job with file upload progress
  Future<ApiResult<PrintJob>> submitPrintJob({
    required File file,
    required String printerName,
    required PrintOptions options,
    ProgressCallback? onSendProgress,
    String? idempotencyKey,
  }) async {
    try {
      final key = idempotencyKey ?? const Uuid().v4();
      final fileName = file.path.split(Platform.pathSeparator).last;

      final formMap = options.toFormDataMap(printerName, key);
      formMap['file'] = await MultipartFile.fromFile(
        file.path,
        filename: fileName,
      );

      final formData = FormData.fromMap(formMap);

      final response = await _dio.post(
        '$_activeBaseUrl/api/v1/jobs',
        data: formData,
        options: Options(
          headers: {'X-Idempotency-Key': key},
          receiveTimeout: const Duration(seconds: 40),
        ),
        onSendProgress: onSendProgress,
      );

      if ((response.statusCode == 200 || response.statusCode == 202) &&
          response.data is Map) {
        final data = Map<String, dynamic>.from(response.data);
        return ApiResult.success(PrintJob.fromJson(data));
      }

      return const ApiResult.failure('Server did not accept print submission');
    } on DioException catch (e) {
      if (e.response != null && e.response!.data is Map) {
        final msg = e.response!.data['message'] ?? e.response!.data['error'];
        return ApiResult.failure(msg?.toString() ?? _formatDioError(e),
            statusCode: e.response!.statusCode);
      }
      return ApiResult.failure(_formatDioError(e),
          statusCode: e.response?.statusCode);
    } catch (e) {
      return ApiResult.failure('Print submission failed: $e');
    }
  }

  /// Get status of a print job
  Future<ApiResult<PrintJob>> getJobStatus(String jobId) async {
    try {
      final response = await _dio.get('$_activeBaseUrl/api/v1/jobs/$jobId');
      if (response.statusCode == 200 && response.data is Map) {
        return ApiResult.success(
          PrintJob.fromJson(Map<String, dynamic>.from(response.data)),
        );
      }
      return const ApiResult.failure('Job status not found');
    } on DioException catch (e) {
      return ApiResult.failure(_formatDioError(e),
          statusCode: e.response?.statusCode);
    } catch (e) {
      return ApiResult.failure(e.toString());
    }
  }

  /// Cancel a queued or spooling job
  Future<ApiResult<bool>> cancelJob(String jobId) async {
    try {
      final response =
          await _dio.post('$_activeBaseUrl/api/v1/jobs/$jobId/cancel');
      if (response.statusCode == 200) {
        return const ApiResult.success(true);
      }
      return const ApiResult.failure('Cancellation refused by spooler');
    } on DioException catch (e) {
      if (e.response?.data is Map && e.response?.data['error'] != null) {
        return ApiResult.failure(e.response?.data['error'].toString(),
            statusCode: e.response?.statusCode);
      }
      return ApiResult.failure(_formatDioError(e),
          statusCode: e.response?.statusCode);
    } catch (e) {
      return ApiResult.failure(e.toString());
    }
  }

  /// Get job history from server
  Future<ApiResult<List<PrintJob>>> getJobs() async {
    try {
      final response = await _dio.get('$_activeBaseUrl/api/v1/jobs');
      if (response.statusCode == 200 && response.data is List) {
        final jobs = (response.data as List)
            .map((item) => PrintJob.fromJson(Map<String, dynamic>.from(item)))
            .toList();
        return ApiResult.success(jobs);
      }
      return const ApiResult.failure('Failed to load server jobs');
    } on DioException catch (e) {
      return ApiResult.failure(_formatDioError(e),
          statusCode: e.response?.statusCode);
    } catch (e) {
      return ApiResult.failure(e.toString());
    }
  }

  String _formatDioError(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return 'Connection timed out. Check that your laptop is on and on the same Wi-Fi network.';
      case DioExceptionType.badResponse:
        final code = e.response?.statusCode;
        if (code == 401) {
          return 'Unauthorized: Device pairing has expired or was revoked. Please reconnect.';
        }
        if (code == 403) {
          return 'Forbidden: Client is not authorized for this action.';
        }
        if (code == 404) {
          return 'Requested printer or job not found on Windows server.';
        }
        final msg = e.response?.data is Map
            ? e.response?.data['message'] ?? e.response?.data['error']
            : null;
        return msg?.toString() ?? 'Server error (HTTP $code)';
      case DioExceptionType.connectionError:
        return 'Cannot connect to Windows Print Server. Make sure both devices are connected to the same Wi-Fi, and Windows Firewall permits private network traffic on port 8080/8443.';
      case DioExceptionType.cancel:
        return 'Request cancelled.';
      default:
        return e.message ?? 'Network communication error.';
    }
  }
}
