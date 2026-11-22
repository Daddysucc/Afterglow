using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace Afterglow.Utils;

/// <summary>
/// Carries over what a copy from before the rename (when the app, its settings folder, and its
/// startup entry were all named "LightBulb") left behind, so that updating doesn't lose them.
/// </summary>
internal static class LegacyMigration
{
    private const string RunKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Run(StartOptions options)
    {
        if (options.LegacySettingsPath is not null)
            TryCopySettingsFile(options.LegacySettingsPath, options.SettingsPath);

        // Auto-start is never enabled in debug builds (see SettingsService.Save)
#if !DEBUG
        TryMoveAutoStartEntry(
            RunKeyName,
            Program.LegacyName,
            Program.Name,
            Services.SettingsService.AutoStartCommand,
            Program.ExecutableFilePath
        );
#endif
    }

    /// <summary>
    /// Copies the legacy settings file to the current location, unless there are settings there
    /// already. The legacy file is kept, so that going back to an older version still works.
    /// </summary>
    internal static bool TryCopySettingsFile(string legacyFilePath, string filePath)
    {
        try
        {
            if (File.Exists(filePath) || !File.Exists(legacyFilePath))
                return false;

            if (Path.GetDirectoryName(filePath) is { Length: > 0 } dirPath)
                Directory.CreateDirectory(dirPath);

            File.Copy(legacyFilePath, filePath, false);
            return true;
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            Debug.WriteLine($"Failed to carry over legacy settings: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Replaces the legacy startup entry with the current one, if the legacy entry launched this
    /// copy of the app, a replaced one (in the same folder), or one that no longer exists. An
    /// entry that launches a different, existing program (e.g. the original LightBulb installed
    /// elsewhere) is left alone.
    /// </summary>
    internal static bool TryMoveAutoStartEntry(
        string runKeyName,
        string legacyEntryName,
        string entryName,
        string command,
        string executableFilePath
    )
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(
                RegistryHive.CurrentUser,
                RegistryView.Registry64
            );
            using var key = baseKey.OpenSubKey(runKeyName, true);

            if (key?.GetValue(legacyEntryName) is not string legacyCommand)
                return false;

            if (!IsLaunchingThisApp(legacyCommand, executableFilePath))
                return false;

            if (key.GetValue(entryName) is null)
                key.SetValue(entryName, command);

            key.DeleteValue(legacyEntryName, false);
            return true;
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            Debug.WriteLine($"Failed to carry over the legacy startup entry: {ex}");
            return false;
        }
    }

    internal static bool IsLaunchingThisApp(string command, string executableFilePath)
    {
        // Startup entries are written as: "<executable path>" <arguments>
        var trimmed = command.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '"')
            return false;

        var closingQuoteIndex = trimmed.IndexOf('"', 1);
        if (closingQuoteIndex < 0)
            return false;

        try
        {
            var targetFilePath = Path.GetFullPath(trimmed[1..closingQuoteIndex]);
            var thisFilePath = Path.GetFullPath(executableFilePath);

            return string.Equals(targetFilePath, thisFilePath, StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    Path.GetDirectoryName(targetFilePath),
                    Path.GetDirectoryName(thisFilePath),
                    StringComparison.OrdinalIgnoreCase
                )
                || !File.Exists(targetFilePath);
        }
        catch (Exception ex)
            when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
