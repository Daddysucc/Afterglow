using System;
using System.IO;
using Afterglow.Core;
using Afterglow.Services;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

// Settings are only loaded here, never saved (saving also writes to the registry)
public class SettingsMigrationSpecs : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + Guid.NewGuid().ToString("N")
    );

    public SettingsMigrationSpecs() => Directory.CreateDirectory(_directoryPath);

    private string FilePath => Path.Combine(_directoryPath, "Settings.json");

    private SettingsService Load(string json, out bool wasLoaded)
    {
        File.WriteAllText(FilePath, json);

        var settings = new SettingsService(FilePath);
        wasLoaded = settings.Load();

        return settings;
    }

    // Settings file as saved by the original (upstream) LightBulb, before any of the newer settings
    // existed. It also contains settings that have since been removed (theme, auto-update, etc).
    private const string UpstreamSettingsJson = """
        {
          "IsFirstTimeExperienceEnabled": false,
          "IsUkraineSupportMessageEnabled": false,
          "MinimumTemperature": 500,
          "MaximumTemperature": 20000,
          "MinimumBrightness": 0.1,
          "MaximumBrightness": 1,
          "DayConfiguration": { "Temperature": 6500, "Brightness": 1 },
          "NightConfiguration": { "Temperature": 3400, "Brightness": 0.8 },
          "ConfigurationTransitionDuration": "01:30:00",
          "ConfigurationTransitionOffset": 0.5,
          "ConfigurationSmoothingMaxDuration": "00:00:05",
          "IsManualSunriseSunsetEnabled": false,
          "ManualSunriseTime": "06:45:00",
          "ManualSunsetTime": "19:15:00",
          "Location": { "Latitude": 50.45, "Longitude": 30.52 },
          "Theme": "Dark",
          "Language": "Ukrainian",
          "IsAutoUpdateEnabled": true,
          "IsDefaultToDayConfigurationEnabled": true,
          "IsConfigurationSmoothingEnabled": true,
          "IsPauseWhenFullScreenEnabled": true,
          "IsGammaPollingEnabled": false,
          "IsApplicationWhitelistEnabled": true,
          "WhitelistedApplications": [
            { "ExecutableFilePath": "C:\\Program Files\\Some Game\\game.exe" }
          ],
          "ToggleHotKey": { "Key": "L", "Modifiers": "Control, Alt" },
          "FocusWindowHotKey": { "Key": "None", "Modifiers": "None" },
          "IncreaseTemperatureOffsetHotKey": { "Key": "None", "Modifiers": "None" },
          "DecreaseTemperatureOffsetHotKey": { "Key": "None", "Modifiers": "None" },
          "IncreaseBrightnessOffsetHotKey": { "Key": "None", "Modifiers": "None" },
          "DecreaseBrightnessOffsetHotKey": { "Key": "None", "Modifiers": "None" },
          "ResetConfigurationOffsetHotKey": { "Key": "None", "Modifiers": "None" }
        }
        """;

    [Fact]
    public void I_can_load_an_upstream_settings_file_and_keep_its_values()
    {
        // Act
        var settings = Load(UpstreamSettingsJson, out var wasLoaded);

        // Assert
        wasLoaded.Should().BeTrue();
        settings.IsFirstTimeExperienceEnabled.Should().BeFalse();
        settings.DayConfiguration.Should().Be(new ColorConfiguration(6500, 1));
        settings.NightConfiguration.Should().Be(new ColorConfiguration(3400, 0.8));
        settings.ConfigurationTransitionDuration.Should().Be(TimeSpan.FromMinutes(90));
        settings.ConfigurationTransitionOffset.Should().Be(0.5);
        settings.IsManualSunriseSunsetEnabled.Should().BeFalse();
        settings.ManualSunrise.Should().Be(new TimeOnly(06, 45));
        settings.Location.Should().Be(new GeoLocation(50.45, 30.52));
        settings.IsDefaultToDayConfigurationEnabled.Should().BeTrue();
        settings.IsPauseWhenFullScreenEnabled.Should().BeTrue();
        settings.WhitelistedApplications.Should().ContainSingle();
        settings.ToggleHotKey.Key.Should().Be(Avalonia.Input.PhysicalKey.L);
    }

    [Fact]
    public void I_can_load_an_upstream_settings_file_and_get_safe_defaults_for_newer_settings()
    {
        // Act
        var settings = Load(UpstreamSettingsJson, out _);

        // Assert
        settings.IsEveningConfigurationEnabled.Should().BeTrue();
        settings.NightStartTime.Should().Be(new TimeOnly(22, 00));
        settings.MonitorProfiles.Should().BeNullOrEmpty();
        settings.IsPerMonitorFullscreenPauseEnabled.Should().BeTrue();
        settings.IsStreamingAutoPauseEnabled.Should().BeFalse();
        settings.StreamingPorts.Should().Be("48010");
        settings.IsPauseWhenScreenLockedEnabled.Should().BeFalse();
        settings.IsStartMinimizedEnabled.Should().BeFalse();
        settings.GammaPollingInterval.Should().Be(TimeSpan.FromSeconds(1));

        // The default evening must fit between the user's own day and night values
        settings.EveningConfiguration.Temperature.Should().BeInRange(3400, 6500);
        settings.EveningConfiguration.Brightness.Should().BeInRange(0.8, 1);
    }

    [Fact]
    public void I_can_load_a_settings_file_with_out_of_range_values_and_get_them_corrected()
    {
        // Act
        var settings = Load(
            """
            {
              "DayConfiguration": { "Temperature": 50000, "Brightness": 3 },
              "NightConfiguration": { "Temperature": 100, "Brightness": 0 },
              "ConfigurationTransitionDuration": "05:00:00",
              "ConfigurationTransitionOffset": 1.7,
              "ConfigurationSmoothingMaxDuration": "-00:00:05",
              "GammaPollingInterval": "00:00:00",
              "StreamingPorts": "abc, 48010, 99999, 48010"
            }
            """,
            out var wasLoaded
        );

        // Assert
        wasLoaded.Should().BeTrue();
        settings.DayConfiguration.Should().Be(new ColorConfiguration(20_000, 1));
        settings.NightConfiguration.Should().Be(new ColorConfiguration(500, 0.1));
        settings.ConfigurationTransitionDuration.Should().Be(TimeSpan.FromHours(3));
        settings.ConfigurationTransitionOffset.Should().Be(1);
        settings.ConfigurationSmoothingMaxDuration.Should().Be(TimeSpan.Zero);
        settings.GammaPollingInterval.Should().Be(TimeSpan.FromSeconds(0.5));
        settings.StreamingPorts.Should().Be("48010");
    }

    [Fact]
    public void I_can_load_a_settings_file_with_invalid_display_profiles_without_losing_other_settings()
    {
        // Act
        var settings = Load(
            """
            {
              "NightConfiguration": { "Temperature": 3000, "Brightness": 0.75 },
              "MonitorProfiles": [
                { "LastKnownName": "No device path", "TemperatureOffset": 100 },
                { "DevicePath": "", "LastKnownName": "Empty device path" },
                { "DevicePath": "\\\\?\\DISPLAY#A#1", "LastKnownName": "First", "TemperatureOffset": 5000, "BrightnessOffset": -2 },
                { "DevicePath": "\\\\?\\display#a#1", "LastKnownName": "Duplicate", "TemperatureOffset": 137 },
                { "DevicePath": "\\\\?\\DISPLAY#B#2", "LastKnownName": "Second", "IsEnabled": false }
              ]
            }
            """,
            out var wasLoaded
        );

        // Assert
        wasLoaded.Should().BeTrue();
        settings.NightConfiguration.Should().Be(new ColorConfiguration(3000, 0.75));

        settings.MonitorProfiles.Should().HaveCount(2);

        // Duplicates keep the last entry, and offsets are kept within the supported range
        var first = settings.TryGetMonitorProfile(@"\\?\DISPLAY#A#1");
        first.Should().NotBeNull();
        first!.LastKnownName.Should().Be("Duplicate");
        first.TemperatureOffset.Should().Be(125);

        settings.TryGetMonitorProfile(@"\\?\DISPLAY#B#2")!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void I_can_load_a_settings_file_from_before_offset_scaling_existed_and_get_perceptual_scaling()
    {
        // Act
        // Saved by an earlier version: offsets in 50 K steps of up to 1000 K, and no scaling setting
        var settings = Load(
            """
            {
              "IsFirstTimeExperienceEnabled": false,
              "DayConfiguration": { "Temperature": 6600, "Brightness": 1 },
              "MonitorProfiles": [
                { "DevicePath": "\\\\?\\DISPLAY#A#1", "LastKnownName": "TV", "TemperatureOffset": 1000, "BrightnessOffset": -0.1 },
                { "DevicePath": "\\\\?\\DISPLAY#B#2", "LastKnownName": "Monitor", "TemperatureOffset": -350 }
              ]
            }
            """,
            out var wasLoaded
        );

        // Assert
        wasLoaded.Should().BeTrue();
        settings.IsPerceptualMonitorOffsetEnabled.Should().BeTrue();
        settings.MonitorOffsetScaling.Should().Be(MonitorOffsetScaling.Perceptual);

        // Existing offsets are kept exactly as they were
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#A#1")!.TemperatureOffset.Should().Be(1000);
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#B#2")!.TemperatureOffset.Should().Be(-350);

        // Profiles that don't say otherwise are enabled
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#A#1")!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void I_can_load_a_settings_file_with_fixed_offset_scaling()
    {
        // Act
        var settings = Load("""{ "IsPerceptualMonitorOffsetEnabled": false }""", out var wasLoaded);

        // Assert
        wasLoaded.Should().BeTrue();
        settings.IsPerceptualMonitorOffsetEnabled.Should().BeFalse();
        settings.MonitorOffsetScaling.Should().Be(MonitorOffsetScaling.Fixed);
    }

    [Fact]
    public void I_can_load_display_offsets_beyond_2000_kelvin_and_get_them_limited()
    {
        // Act
        var settings = Load(
            """
            {
              "MonitorProfiles": [
                { "DevicePath": "\\\\?\\DISPLAY#A#1", "TemperatureOffset": 3000 },
                { "DevicePath": "\\\\?\\DISPLAY#B#2", "TemperatureOffset": -2600 },
                { "DevicePath": "\\\\?\\DISPLAY#C#3", "TemperatureOffset": 1975 }
              ]
            }
            """,
            out _
        );

        // Assert
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#A#1")!.TemperatureOffset.Should().Be(2000);
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#B#2")!.TemperatureOffset.Should().Be(-2000);
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#C#3")!.TemperatureOffset.Should().Be(1975);
    }

    [Fact]
    public void I_can_switch_offset_scaling_back_and_forth_without_changing_saved_offsets()
    {
        // Arrange
        var settings = Load(
            """
            {
              "MonitorProfiles": [
                { "DevicePath": "\\\\?\\DISPLAY#A#1", "TemperatureOffset": -1975, "BrightnessOffset": 0.07 }
              ]
            }
            """,
            out _
        );

        var original = settings.TryGetMonitorProfile(@"\\?\DISPLAY#A#1");

        // Act
        settings.IsPerceptualMonitorOffsetEnabled = false;
        settings.IsPerceptualMonitorOffsetEnabled = true;
        settings.IsPerceptualMonitorOffsetEnabled = false;

        // Assert
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#A#1").Should().Be(original);
    }

    [Theory]
    [InlineData("""{ "DayConfiguration": { "Temperature": 6500, """)] // truncated
    [InlineData("""{ "DayConfiguration": { "Temperature": "warm" } }""")] // wrong type
    [InlineData("""{ "Language": "Klingon" }""")] // unknown value
    [InlineData("not json at all")]
    public void I_can_load_a_corrupted_settings_file_and_get_defaults_with_a_backup(string json)
    {
        // Act
        var settings = Load(json, out var wasLoaded);

        // Assert
        wasLoaded.Should().BeFalse();
        settings.DayConfiguration.Should().Be(new ColorConfiguration(6600, 1));

        // Someone with a corrupted file isn't a new user, so there's no first-time setup
        settings.IsFirstTimeExperienceEnabled.Should().BeFalse();

        File.Exists(FilePath + ".bak").Should().BeTrue();
        File.ReadAllText(FilePath + ".bak").Should().Be(json);
    }

    [Theory]
    // An unknown value, e.g. one added by a newer version
    [InlineData("""{ "Language": "Klingon", "NightStartTime": "23:30:00" }""")]
    // A value of the wrong type, e.g. edited by hand
    [InlineData("""{ "IsConfigurationSmoothingEnabled": "yes", "NightStartTime": "23:30:00" }""")]
    // A setting that another version stores differently
    [InlineData("""{ "GrayscaleSchedule": { "Mode": "Night" }, "NightStartTime": "23:30:00" }""")]
    // A missing value
    [InlineData("""{ "DayConfiguration": null, "NightStartTime": "23:30:00" }""")]
    public void I_keep_my_other_settings_when_one_of_them_cannot_be_read(string json)
    {
        // Act
        var settings = Load(json, out var wasLoaded);

        // Assert
        wasLoaded.Should().BeTrue();
        settings.NightStartTime.Should().Be(new TimeOnly(23, 30));

        // The setting that couldn't be read is back to its default
        settings.Language.Should().Be(Afterglow.Localization.Language.System);
        settings.IsConfigurationSmoothingEnabled.Should().BeTrue();
        settings.GrayscaleSchedule.Should().Be(GrayscaleSchedule.Off);
        settings.DayConfiguration.Should().Be(new ColorConfiguration(6600, 1));

        // The original file is kept for reference
        File.ReadAllText(FilePath + ".bak").Should().Be(json);
    }

    [Fact]
    public void I_keep_my_other_settings_when_an_upstream_settings_file_has_one_unreadable_value()
    {
        // Arrange
        var json = UpstreamSettingsJson.Replace(
            "\"ConfigurationTransitionDuration\": \"01:30:00\"",
            "\"ConfigurationTransitionDuration\": \"an hour and a half\""
        );

        // Act
        var settings = Load(json, out var wasLoaded);

        // Assert
        wasLoaded.Should().BeTrue();
        settings.ConfigurationTransitionDuration.Should().Be(TimeSpan.FromMinutes(40));
        settings.NightConfiguration.Should().Be(new ColorConfiguration(3400, 0.8));
        settings.Location.Should().Be(new GeoLocation(50.45, 30.52));
        settings.WhitelistedApplications.Should().ContainSingle();
    }

    [Fact]
    public void I_keep_my_other_display_profiles_when_one_of_them_cannot_be_read()
    {
        // Act
        var settings = Load(
            """
            {
              "MonitorProfiles": [
                { "DevicePath": "\\\\?\\DISPLAY#A#1", "LastKnownName": "First", "TemperatureOffset": 100 },
                { "DevicePath": "\\\\?\\DISPLAY#B#2", "TemperatureOffset": "warmer" },
                "not a profile",
                { "DevicePath": "\\\\?\\DISPLAY#C#3", "LastKnownName": "Third", "IsEnabled": false }
              ],
              "WhitelistedWindowTitles": ["Movie", 42, "Game"]
            }
            """,
            out var wasLoaded
        );

        // Assert
        wasLoaded.Should().BeTrue();
        settings.MonitorProfiles.Should().HaveCount(2);
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#A#1")!.TemperatureOffset.Should().Be(100);
        settings.TryGetMonitorProfile(@"\\?\DISPLAY#C#3")!.IsEnabled.Should().BeFalse();
        settings.WhitelistedWindowTitles.Should().Equal("Movie", "Game");
    }

    [Theory]
    [InlineData("""{ "StreamingPorts": null }""")]
    [InlineData("""{ "WhitelistedWindowTitles": null, "WhitelistedApplications": null }""")]
    [InlineData("""{ "MonitorProfiles": [null] }""")]
    [InlineData("""{ "Location": null }""")]
    public void I_do_not_get_a_crash_when_a_setting_is_empty(string json)
    {
        // Act
        var settings = Load(json, out _);

        // Assert
        settings.StreamingPorts.Should().NotBeNull();
    }

    [Fact]
    public void I_can_load_settings_when_the_file_does_not_exist_yet()
    {
        // Act
        var settings = new SettingsService(FilePath);
        var wasLoaded = settings.Load();

        // Assert
        wasLoaded.Should().BeFalse();
        settings.IsFirstTimeExperienceEnabled.Should().BeTrue();
        File.Exists(FilePath + ".bak").Should().BeFalse();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directoryPath, true);
        }
        catch
        {
            // Not important
        }
    }
}
