using System;
using System.IO;

namespace KKR.PrintServer.Core.Common;

/// <summary>
/// Resolves safe application data paths ensuring clean separation between
/// read-only application binaries (e.g. C:\Program Files\Print Ofo)
/// and user/runtime data (logs, certs, temp spool, crashes).
/// In portable mode, keeps data in the application directory.
/// </summary>
public static class AppEnvironment
{
    private static readonly object _lock = new();
    private static string? _dataDirectory;

    /// <summary>
    /// Gets the application writable data directory.
    /// When installed in Program Files, returns %LocalAppData%\Print_Ofo.
    /// When running portable (or in dev folder with write permissions), returns AppContext.BaseDirectory.
    /// </summary>
    public static string GetDataDirectory()
    {
        if (_dataDirectory != null) return _dataDirectory;

        lock (_lock)
        {
            if (_dataDirectory != null) return _dataDirectory;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 1. If explicit portable marker exists, always run in portable mode
            string portableMarker = Path.Combine(baseDir, "portable.dat");
            if (File.Exists(portableMarker))
            {
                _dataDirectory = baseDir;
                return _dataDirectory;
            }

            // 2. Check if running from Program Files or Program Files (x86)
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            bool isInProgramFiles = (!string.IsNullOrEmpty(progFiles) && baseDir.StartsWith(progFiles, StringComparison.OrdinalIgnoreCase)) ||
                                    (!string.IsNullOrEmpty(progFilesX86) && baseDir.StartsWith(progFilesX86, StringComparison.OrdinalIgnoreCase));

            if (isInProgramFiles)
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dataDir = Path.Combine(localAppData, "Print_Ofo");
                EnsureDirectory(dataDir);
                _dataDirectory = dataDir;
                return _dataDirectory;
            }

            // 3. Test if baseDir is writable
            try
            {
                string testFile = Path.Combine(baseDir, $".write_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                // Base directory is writable and not in Program Files -> portable/in-place mode
                _dataDirectory = baseDir;
                return _dataDirectory;
            }
            catch
            {
                // Not writable -> fallback to LocalAppData
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dataDir = Path.Combine(localAppData, "Print_Ofo");
                EnsureDirectory(dataDir);
                _dataDirectory = dataDir;
                return _dataDirectory;
            }
        }
    }

    public static string LogsDirectory
    {
        get
        {
            string dir = Path.Combine(GetDataDirectory(), "logs");
            EnsureDirectory(dir);
            return dir;
        }
    }

    public static string CertsDirectory
    {
        get
        {
            string dir = Path.Combine(GetDataDirectory(), "certs");
            EnsureDirectory(dir);
            return dir;
        }
    }

    public static string SpoolDirectory
    {
        get
        {
            string dir = Path.Combine(GetDataDirectory(), "temp_spool");
            EnsureDirectory(dir);
            return dir;
        }
    }

    public static string CrashLogPath => Path.Combine(GetDataDirectory(), "crash.txt");

    public static bool IsPortableMode()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return string.Equals(GetDataDirectory(), baseDir, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch { }
    }
}
