import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/server_connection.dart';
import '../models/print_job.dart';

class StorageService {
  static const String _keyServerConnection = 'kkr_server_connection';
  static const String _keyRecentJobs = 'kkr_recent_jobs';
  static const String _keySelectedPrinter = 'kkr_selected_printer';

  static final StorageService _instance = StorageService._internal();
  factory StorageService() => _instance;
  StorageService._internal();

  SharedPreferences? _prefs;

  Future<void> init() async {
    _prefs ??= await SharedPreferences.getInstance();
  }

  Future<void> saveServerConnection(ServerConnection connection) async {
    await init();
    final jsonStr = jsonEncode(connection.toJson());
    await _prefs!.setString(_keyServerConnection, jsonStr);
  }

  Future<ServerConnection?> loadServerConnection() async {
    await init();
    final jsonStr = _prefs!.getString(_keyServerConnection);
    if (jsonStr == null || jsonStr.isEmpty) return null;
    try {
      final map = jsonDecode(jsonStr) as Map<String, dynamic>;
      return ServerConnection.fromJson(map);
    } catch (_) {
      return null;
    }
  }

  Future<void> clearServerConnection() async {
    await init();
    await _prefs!.remove(_keyServerConnection);
  }

  Future<void> saveSelectedPrinter(String printerName) async {
    await init();
    await _prefs!.setString(_keySelectedPrinter, printerName);
  }

  Future<String?> loadSelectedPrinter() async {
    await init();
    return _prefs!.getString(_keySelectedPrinter);
  }

  Future<void> saveRecentJobs(List<PrintJob> jobs) async {
    await init();
    final list = jobs.take(30).map((j) => j.toJson()).toList();
    await _prefs!.setString(_keyRecentJobs, jsonEncode(list));
  }

  Future<List<PrintJob>> loadRecentJobs() async {
    await init();
    final jsonStr = _prefs!.getString(_keyRecentJobs);
    if (jsonStr == null || jsonStr.isEmpty) return [];
    try {
      final list = jsonDecode(jsonStr) as List<dynamic>;
      return list
          .map((item) => PrintJob.fromJson(Map<String, dynamic>.from(item)))
          .toList();
    } catch (_) {
      return [];
    }
  }

  Future<void> addRecentJob(PrintJob job) async {
    final jobs = await loadRecentJobs();
    final index = jobs.indexWhere((j) => j.jobId == job.jobId);
    if (index >= 0) {
      jobs[index] = job;
    } else {
      jobs.insert(0, job);
    }
    await saveRecentJobs(jobs);
  }

  Future<void> updateJobStatus(String jobId, JobState status,
      {String? statusMessage, String? errorMessage}) async {
    final jobs = await loadRecentJobs();
    final index = jobs.indexWhere((j) => j.jobId == jobId);
    if (index >= 0) {
      final existing = jobs[index];
      jobs[index] = PrintJob(
        jobId: existing.jobId,
        status: status,
        statusMessage: statusMessage ?? existing.statusMessage,
        printerName: existing.printerName,
        documentName: existing.documentName,
        documentType: existing.documentType,
        fileSizeBytes: existing.fileSizeBytes,
        pageCount: existing.pageCount,
        createdAt: existing.createdAt,
        startedAt: existing.startedAt,
        completedAt: status.isTerminal ? DateTime.now() : existing.completedAt,
        errorMessage: errorMessage ?? existing.errorMessage,
        settingsSummary: existing.settingsSummary,
        isOwnedByRequester: existing.isOwnedByRequester,
      );
      await saveRecentJobs(jobs);
    }
  }
  static const String _keyDismissDesktopPromo = 'dismiss_desktop_promo';

  Future<void> saveDismissDesktopPromo(bool dismiss) async {
    await init();
    await _prefs!.setBool(_keyDismissDesktopPromo, dismiss);
  }

  Future<bool> loadDismissDesktopPromo() async {
    await init();
    return _prefs!.getBool(_keyDismissDesktopPromo) ?? false;
  }
}