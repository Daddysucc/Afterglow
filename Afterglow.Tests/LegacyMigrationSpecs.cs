using System;
using System.IO;
using Afterglow.Utils;
using AwesomeAssertions;
using Microsoft.Win32;
using Xunit;

namespace Afterglow.Tests;

public class LegacyMigrationSpecs : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + Guid.NewGuid().ToString("N")
    );

    // A throwaway key in the current user's registry hive, standing in for the startup entries
    private readonly string _runKeyName =
        @"Software\Afterglow.Tests\" + Guid.NewGuid().ToString("N");

    public LegacyMigrationSpecs() => Directory.CreateDirectory(_directoryPath);

    private string LegacySettingsPath => Path.Combine(_directoryPath, "LightBulb", "Settings.json");

    private string SettingsPath => Path.Combine(_directoryPath, "Afterglow", "Settings.json");

    private string ExecutablePath => Path.Combine(_directoryPath, "App", "Afterglow.exe");

    private void SetRunValue(string name, string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_runKeyName);
        key.SetValue(name, value);
    }

    private object? GetRunValue(string name)
    {
        using var key = Registry.CurrentUser.OpenSubKey(_runKeyName);
        return key?.GetValue(name);
    }

    private bool MoveAutoStartEntry() =>
        LegacyMigration.TryMoveAutoStartEntry(
            _runKeyName,
            "LightBulb",
            "Afterglow",
            $"\"{ExecutablePath}\" --start-hidden",
            ExecutablePath
        );

    // ---- Settings ----

    [Fact]
    public void I_keep_my_settings_from_before_the_rename()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(LegacySettingsPath)!);
        File.WriteAllText(LegacySettingsPath, """{ "NightStartTime": "23:00:00" }""");

        // Act
        var isCopied = LegacyMigration.TryCopySettingsFile(LegacySettingsPath, SettingsPath);

        // Assert
        isCopied.Should().BeTrue();
        File.ReadAllText(SettingsPath).Should().Contain("23:00:00");

        // Kept, so that going back to an older version still works
        File.Exists(LegacySettingsPath).Should().BeTrue();
    }

    [Fact]
    public void I_do_not_get_my_current_settings_replaced_by_old_ones()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(LegacySettingsPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(LegacySettingsPath, "old");
        File.WriteAllText(SettingsPath, "current");

        // Act
        var isCopied = LegacyMigration.TryCopySettingsFile(LegacySettingsPath, SettingsPath);

        // Assert
        isCopied.Should().BeFalse();
        File.ReadAllText(SettingsPath).Should().Be("current");
    }

    [Fact]
    public void I_start_with_defaults_when_there_are_no_old_settings()
    {
        // Act
        var isCopied = LegacyMigration.TryCopySettingsFile(LegacySettingsPath, SettingsPath);

        // Assert
        isCopied.Should().BeFalse();
        File.Exists(SettingsPath).Should().BeFalse();
    }

    // ---- Startup entry ----

    [Fact]
    public void I_keep_starting_with_windows_after_an_installation_is_upgraded()
    {
        // Arrange
        // The old executable sat in the same folder, and has been removed by the installer
        var legacyExecutablePath = Path.Combine(_directoryPath, "App", "LightBulb.exe");
        SetRunValue("LightBulb", $"\"{legacyExecutablePath}\" --start-hidden");

        // Act
        var isMoved = MoveAutoStartEntry();

        // Assert
        isMoved.Should().BeTrue();
        GetRunValue("LightBulb").Should().BeNull();
        GetRunValue("Afterglow").Should().Be($"\"{ExecutablePath}\" --start-hidden");
    }

    [Fact]
    public void I_keep_starting_with_windows_when_a_portable_copy_is_replaced_in_place()
    {
        // Arrange
        SetRunValue("LightBulb", $"\"{ExecutablePath}\" --start-hidden");

        // Act
        var isMoved = MoveAutoStartEntry();

        // Assert
        isMoved.Should().BeTrue();
        GetRunValue("LightBulb").Should().BeNull();
        GetRunValue("Afterglow").Should().NotBeNull();
    }

    [Fact]
    public void I_do_not_get_the_startup_entry_of_another_existing_program_taken_over()
    {
        // Arrange
        // E.g. the original LightBulb, kept elsewhere
        var otherExecutablePath = Path.Combine(_directoryPath, "Other", "LightBulb.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(otherExecutablePath)!);
        File.WriteAllText(otherExecutablePath, "");
        SetRunValue("LightBulb", $"\"{otherExecutablePath}\" --start-hidden");

        // Act
        var isMoved = MoveAutoStartEntry();

        // Assert
        isMoved.Should().BeFalse();
        GetRunValue("LightBulb").Should().NotBeNull();
        GetRunValue("Afterglow").Should().BeNull();
    }

    [Fact]
    public void I_do_not_get_a_startup_entry_when_there_was_none()
    {
        // Act
        var isMoved = MoveAutoStartEntry();

        // Assert
        isMoved.Should().BeFalse();
        GetRunValue("Afterglow").Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("C:\\NoQuotes\\LightBulb.exe --start-hidden")]
    [InlineData("\"unterminated")]
    public void I_do_not_get_an_unrecognized_startup_entry_taken_over(string command)
    {
        // Act & assert
        LegacyMigration.IsLaunchingThisApp(command, ExecutablePath).Should().BeFalse();
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_runKeyName, false);
            Registry.CurrentUser.DeleteSubKey(@"Software\Afterglow.Tests", false);
        }
        catch
        {
            // Ignore
        }

        try
        {
            Directory.Delete(_directoryPath, true);
        }
        catch
        {
            // Ignore
        }
    }
}
