class ServerConnection {
  final String ip;
  final int httpPort;
  final int httpsPort;
  final String pin;
  final String? thumbprint;
  final String? token;
  final String? serverName;
  final String? machineName;
  final String? activeBaseUrl;
  final DateTime? lastConnectedAt;
  final bool isConnected;

  const ServerConnection({
    required this.ip,
    this.httpPort = 8080,
    this.httpsPort = 8443,
    required this.pin,
    this.thumbprint,
    this.token,
    this.serverName,
    this.machineName,
    this.activeBaseUrl,
    this.lastConnectedAt,
    this.isConnected = false,
  });

  String get baseUrlHttp => 'http://$ip:$httpPort';
  String get baseUrlHttps => 'https://$ip:$httpsPort';
  String get effectiveBaseUrl => activeBaseUrl ?? baseUrlHttp;

  ServerConnection copyWith({
    String? ip,
    int? httpPort,
    int? httpsPort,
    String? pin,
    String? thumbprint,
    String? token,
    String? serverName,
    String? machineName,
    String? activeBaseUrl,
    DateTime? lastConnectedAt,
    bool? isConnected,
  }) {
    return ServerConnection(
      ip: ip ?? this.ip,
      httpPort: httpPort ?? this.httpPort,
      httpsPort: httpsPort ?? this.httpsPort,
      pin: pin ?? this.pin,
      thumbprint: thumbprint ?? this.thumbprint,
      token: token ?? this.token,
      serverName: serverName ?? this.serverName,
      machineName: machineName ?? this.machineName,
      activeBaseUrl: activeBaseUrl ?? this.activeBaseUrl,
      lastConnectedAt: lastConnectedAt ?? this.lastConnectedAt,
      isConnected: isConnected ?? this.isConnected,
    );
  }

  Map<String, dynamic> toJson() => {
        'ip': ip,
        'httpPort': httpPort,
        'httpsPort': httpsPort,
        'pin': pin,
        'thumbprint': thumbprint,
        'token': token,
        'serverName': serverName,
        'machineName': machineName,
        'activeBaseUrl': activeBaseUrl,
        'lastConnectedAt': lastConnectedAt?.toIso8601String(),
      };

  factory ServerConnection.fromJson(Map<String, dynamic> json) =>
      ServerConnection(
        ip: json['ip'] as String? ?? '127.0.0.1',
        httpPort: json['httpPort'] as int? ?? 8080,
        httpsPort: json['httpsPort'] as int? ?? 8443,
        pin: json['pin'] as String? ?? '',
        thumbprint: json['thumbprint'] as String?,
        token: json['token'] as String?,
        serverName: json['serverName'] as String?,
        machineName: json['machineName'] as String?,
        activeBaseUrl: json['activeBaseUrl'] as String?,
        lastConnectedAt: json['lastConnectedAt'] != null
            ? DateTime.tryParse(json['lastConnectedAt'] as String)
            : null,
        isConnected: false,
      );
}
