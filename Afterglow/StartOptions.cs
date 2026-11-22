using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Afterglow.Utils;
using PowerKit.Extensions;

namespace Afterglow;

public partial class StartOptions
{
    public required bool IsInitiallyHidden { get; init; }

    public required string SettingsPath { get; init; }

    // Where a copy from before the rename kept its settings, if it would have kept them in the
    // same place as this one (only the app data folder's name has changed)
    public string? LegacySettingsPath { get; init; }

    // Whether the app is running from somewhere files don't stay for long (e.g. Downloads)
    public required bool IsRunningFromTemporaryLocation { get; init; }

    // The process of the version that this one has just replaced (when a portable copy updates
    // itself), which has to close before this one starts
    public int? PreviousVersionProcessId { get; private set; }
}

public partial class StartOptions
{
    public static string IsInitiallyHiddenArgument { get; } = "--start-hidden";

    // Followed by the process ID of the version that has just been replaced
    public static string PreviousVersionArgument { get; } = "--replacing";

    public static StartOptions Current { get; } =
        Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());

    public static StartOptions Parse(IReadOnlyList<string> commandLineArgs)
    {
        var options = ParseLocation(commandLineArgs);

        for (var i = 0; i < commandLineArgs.Count - 1; i++)
        {
            if (
                string.Equals(
                    commandLineArgs[i],
                    PreviousVersionArgument,
                    StringComparison.OrdinalIgnoreCase
                )
                && int.TryParse(commandLineArgs[i + 1], out var processId)
                && processId > 0
            )
            {
                options.PreviousVersionProcessId = processId;
            }
        }

        return options;
    }

    private static StartOptions ParseLocation(IReadOnlyList<string> commandLineArgs)
    {
        var isInitiallyHidden = commandLineArgs.Contains(
            IsInitiallyHiddenArgument,
            StringComparer.OrdinalIgnoreCase
        );

        var isRunningFromTemporaryLocation = AppLocation.IsTemporary(Program.ExecutableDirPath);

        // Provided environment override
        if (
            Environment.GetEnvironmentVariable("AFTERGLOW_SETTINGS_PATH") is { } path
            && !string.IsNullOrWhiteSpace(path)
        )
        {
            return new()
            {
                IsInitiallyHidden = isInitiallyHidden,
                IsRunningFromTemporaryLocation = isRunningFromTemporaryLocation,
                SettingsPath =
                    Path.EndsInDirectorySeparator(path) || Directory.Exists(path)
                        // It's a directory path
                        ? Path.Combine(path, "Settings.json")
                        // It's a file path
                        : path,
            };
        }

        // Installed, or cannot keep the settings in the program directory. Files in temporary
        // locations are soon gone, so the settings are kept in the user's profile instead (where
        // an installed copy also keeps them).
        if (
            File.Exists(Path.Combine(Program.ExecutableDirPath, ".installed"))
            || isRunningFromTemporaryLocation
            || !Directory.CheckWriteAccess(Program.ExecutableDirPath)
        )
        {
            var appDataDirPath = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData
            );

            return new()
            {
                IsInitiallyHidden = isInitiallyHidden,
                IsRunningFromTemporaryLocation = isRunningFromTemporaryLocation,
                SettingsPath = Path.Combine(appDataDirPath, Program.Name, "Settings.json"),
                LegacySettingsPath = Path.Combine(
                    appDataDirPath,
                    Program.LegacyName,
                    "Settings.json"
                ),
            };
        }

        // Can write to the program directory
        return new()
        {
            IsInitiallyHidden = isInitiallyHidden,
            IsRunningFromTemporaryLocation = isRunningFromTemporaryLocation,
            SettingsPath = Path.Combine(Program.ExecutableDirPath, "Settings.json"),
        };
    }
}
