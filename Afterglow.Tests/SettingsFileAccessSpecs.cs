using System;
using System.IO;
using System.Threading;
using Afterglow.Core;
using Afterglow.Services;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

// Only the file is ever written here (TrySaveToFile), never the registry (Save)
public class SettingsFileAccessSpecs : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + Guid.NewGuid().ToString("N")
    );

    private const string Json = """
        {
          "IsFirstTimeExperienceEnabled": false,
          "NightConfiguration": { "Temperature": 3100, "Brightness": 0.7 }
        }
        """;

    public SettingsFileAccessSpecs() => Directory.CreateDirectory(_directoryPath);

    private string FilePath => Path.Combine(_directoryPath, "Settings.json");

    [Fact]
    public void I_can_load_a_settings_file_that_is_briefly_locked_by_another_process()
    {
        // Arrange
        // E.g. a cloud sync client or an antivirus scan right after signing in
        File.WriteAllText(FilePath, Json);
        var lockStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None);

        // Released on a dedicated thread rather than a thread pool timer: other tests running in
        // parallel can keep the thread pool busy, delaying the release past the last retry
        var releaseThread = new Thread(() =>
        {
            Thread.Sleep(SettingsService.FileAccessRetryDelay * 1.5);
            lockStream.Dispose();
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal,
        };
        releaseThread.Start();

        var settings = new SettingsService(FilePath);

        // Act
        var wasLoaded = settings.Load();
        releaseThread.Join();

        // Assert
        wasLoaded.Should().BeTrue();
        settings.NightConfiguration.Should().Be(new ColorConfiguration(3100, 0.7));
        File.Exists(FilePath + ".bak").Should().BeFalse();
    }

    [Fact]
    public void I_do_not_lose_my_settings_when_the_file_stays_locked_while_the_app_starts()
    {
        // Arrange
        File.WriteAllText(FilePath, Json);
        var settings = new SettingsService(FilePath);

        bool wasLoaded;
        using (new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            // Act
            wasLoaded = settings.Load();
        }

        // Assert
        // Defaults are used for this session, but the file isn't treated as corrupt...
        wasLoaded.Should().BeFalse();
        settings.NightConfiguration.Should().NotBe(new ColorConfiguration(3100, 0.7));
        settings.IsFirstTimeExperienceEnabled.Should().BeFalse();
        File.Exists(FilePath + ".bak").Should().BeFalse();

        // ...and isn't overwritten with the defaults later on
        settings.TrySaveToFile().Should().BeFalse();
        File.ReadAllText(FilePath).Should().Be(Json);
    }

    [Fact]
    public void I_do_not_get_a_crash_when_the_settings_file_is_read_only()
    {
        // Arrange
        File.WriteAllText(FilePath, Json);
        File.SetAttributes(FilePath, FileAttributes.ReadOnly);

        try
        {
            var settings = new SettingsService(FilePath);
            settings.Load().Should().BeTrue();
            settings.NightConfiguration = new ColorConfiguration(2000, 0.5);

            // Act
            var isSaved = settings.TrySaveToFile();

            // Assert
            isSaved.Should().BeFalse();
            File.ReadAllText(FilePath).Should().Be(Json);
        }
        finally
        {
            File.SetAttributes(FilePath, FileAttributes.Normal);
        }
    }

    [Fact]
    public void I_can_save_settings_and_load_them_back()
    {
        // Arrange
        File.WriteAllText(FilePath, Json);
        var settings = new SettingsService(FilePath);
        settings.Load();
        settings.NightConfiguration = new ColorConfiguration(2000, 0.5);
        settings.IsPerceptualMonitorOffsetEnabled = false;

        // Act
        var isSaved = settings.TrySaveToFile();

        var reloaded = new SettingsService(FilePath);
        reloaded.Load();

        // Assert
        isSaved.Should().BeTrue();
        reloaded.NightConfiguration.Should().Be(new ColorConfiguration(2000, 0.5));
        reloaded.IsPerceptualMonitorOffsetEnabled.Should().BeFalse();

        // Derived values are not written to the file
        File.ReadAllText(FilePath).Should().NotContain("\"MonitorOffsetScaling\"");
    }

    public void Dispose()
    {
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
