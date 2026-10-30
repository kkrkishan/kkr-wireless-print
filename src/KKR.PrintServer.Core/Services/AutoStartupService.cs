using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using KKR.PrintServer.Core.Logging;

namespace KKR.PrintServer.Core.Services;

/// <summary>
/// Manages automatic startup of Print Ofo Server on Windows user logon
/// via the HKCU\Software\Microsoft\Windows\CurrentVersion\Run registry key.
/// </summary>
public static class AutoStartupService
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryName = "PrintOfoServer";

    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            return key?.GetValue(AppRegistryName) != null;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warning("Startup", $"Failed to check autostart registry key: {ex.Message}");
            return false;
        }
    }

    public static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintOfo.Server.exe");
                }

                if (File.Exists(exePath))
                {
                    key.SetValue(AppRegistryName, $"\"{exePath}\" --autostart");
                    AppLogger.Instance.Info("Startup", $"Auto-startup enabled in registry: {exePath}");
                }
            }
            else
            {
                key.DeleteValue(AppRegistryName, false);
                AppLogger.Instance.Info("Startup", "Auto-startup disabled in registry.");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warning("Startup", $"Failed to set autostart registry key: {ex.Message}");
        }
    }
}
