namespace KKR.PrintServer.Core.Logging;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Security
}

public record LogEntry
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public DateTime TimestampLocal => TimestampUtc.ToLocalTime();
    public LogLevel Level { get; init; } = LogLevel.Info;
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Exception { get; init; }

    public override string ToString() =>
        $"[{TimestampLocal:yyyy-MM-dd HH:mm:ss}] [{Level.ToString().ToUpperInvariant()}] [{Category}] {Message}" +
        (string.IsNullOrWhiteSpace(Exception) ? "" : $" Exception: {Exception}");
}

public class AppLogger
{
    private static readonly Lazy<AppLogger> _instance = new(() => new AppLogger());
    public static AppLogger Instance => _instance.Value;

    private readonly object _lock = new();
    private string _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    private readonly List<LogEntry> _recentEntries = new();
    private const int MaxMemoryEntries = 500;

    public event Action<LogEntry>? LogEmitted;

    public void Initialize(string logDirectory)
    {
        lock (_lock)
        {
            _logDirectory = logDirectory;
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }
    }

    public void Log(LogLevel level, string category, string message, Exception? ex = null)
    {
        var entry = new LogEntry
        {
            Level = level,
            Category = category,
            Message = message,
            Exception = ex?.ToString()
        };

        lock (_lock)
        {
            _recentEntries.Add(entry);
            if (_recentEntries.Count > MaxMemoryEntries)
            {
                _recentEntries.RemoveAt(0);
            }

            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                string fileName = $"kkr-print-{DateTime.UtcNow:yyyyMMdd}.log";
                string fullPath = Path.Combine(_logDirectory, fileName);
                File.AppendAllText(fullPath, entry.ToString() + Environment.NewLine);
            }
            catch
            {
                // Never crash the application on log write failure
            }
        }

        try
        {
            LogEmitted?.Invoke(entry);
        }
        catch
        {
            // Ignore subscriber errors
        }
    }

    public void Info(string category, string message) => Log(LogLevel.Info, category, message);
    public void Warning(string category, string message) => Log(LogLevel.Warning, category, message);
    public void Error(string category, string message, Exception? ex = null) => Log(LogLevel.Error, category, message, ex);
    public void Security(string category, string message) => Log(LogLevel.Security, category, message);

    public IReadOnlyList<LogEntry> GetRecentEntries()
    {
        lock (_lock)
        {
            return _recentEntries.ToList();
        }
    }
}
