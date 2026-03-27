using System;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace AyanoTimer.Services;

[SupportedOSPlatform("windows")]

public static class AutoStartService
{
    private const string AppName = "AyanoTimer";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? "";
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Registry access may fail on some corporate PCs
        }
    }
}
