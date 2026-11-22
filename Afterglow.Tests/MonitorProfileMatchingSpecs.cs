using System;
using System.IO;
using System.Linq;
using Afterglow.Core;
using Afterglow.Services;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class MonitorProfileMatchingSpecs : IDisposable
{
    private const string Guid = "{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}";

    // Real-world device paths: the same TV before and after the display adapter was re-enumerated
    // (e.g. a graphics driver update), which changes the middle part of the instance path
    private const string TvBefore = @"\\?\DISPLAY#SAM75B2#5&2f42420&0&UID33024#" + Guid;
    private const string TvAfter = @"\\?\DISPLAY#SAM75B2#5&2f42420&1&UID33024#" + Guid;
    private const string MonitorBefore = @"\\?\DISPLAY#HPN3A38#5&2f42420&0&UID33025#" + Guid;
    private const string MonitorAfter = @"\\?\DISPLAY#HPN3A38#5&2f42420&1&UID33025#" + Guid;

    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + System.Guid.NewGuid().ToString("N")
    );

    public MonitorProfileMatchingSpecs() => Directory.CreateDirectory(_directoryPath);

    private static MonitorProfile Profile(string devicePath, int temperatureOffset = -500) =>
        new()
        {
            DevicePath = devicePath,
            LastKnownName = "Display",
            TemperatureOffset = temperatureOffset,
        };

    [Fact]
    public void I_keep_display_preferences_when_the_display_adapter_is_re_enumerated()
    {
        // Arrange
        var profiles = new[] { Profile(TvBefore, -1500), Profile(MonitorBefore, 300) };

        // Act
        var rekeyed = MonitorProfileMatcher.TryRekey([MonitorAfter, TvAfter], profiles);

        // Assert
        rekeyed.Should().NotBeNull();
        rekeyed!.Should().HaveCount(2);
        rekeyed.Single(p => p.DevicePath == TvAfter).TemperatureOffset.Should().Be(-1500);
        rekeyed.Single(p => p.DevicePath == MonitorAfter).TemperatureOffset.Should().Be(300);
    }

    [Fact]
    public void I_keep_display_preferences_when_a_display_is_moved_to_another_connector()
    {
        // Arrange
        var profiles = new[] { Profile(TvBefore, -1500) };
        var tvOnAnotherPort = @"\\?\DISPLAY#SAM75B2#5&2f42420&1&UID4352#" + Guid;

        // Act
        var rekeyed = MonitorProfileMatcher.TryRekey([MonitorAfter, tvOnAnotherPort], profiles);

        // Assert
        rekeyed.Should().ContainSingle().Which.DevicePath.Should().Be(tvOnAnotherPort);
        rekeyed!.Single().TemperatureOffset.Should().Be(-1500);
    }

    [Fact]
    public void I_keep_preferences_of_two_identical_displays_apart_by_their_connectors()
    {
        // Arrange
        var leftBefore = @"\\?\DISPLAY#DEL4321#4&aaa&0&UID100#" + Guid;
        var rightBefore = @"\\?\DISPLAY#DEL4321#4&aaa&0&UID200#" + Guid;
        var leftAfter = @"\\?\DISPLAY#DEL4321#4&bbb&1&UID100#" + Guid;
        var rightAfter = @"\\?\DISPLAY#DEL4321#4&bbb&1&UID200#" + Guid;

        var profiles = new[] { Profile(leftBefore, -100), Profile(rightBefore, 200) };

        // Act
        var rekeyed = MonitorProfileMatcher.TryRekey([rightAfter, leftAfter], profiles);

        // Assert
        rekeyed!.Single(p => p.DevicePath == leftAfter).TemperatureOffset.Should().Be(-100);
        rekeyed!.Single(p => p.DevicePath == rightAfter).TemperatureOffset.Should().Be(200);
    }

    [Fact]
    public void I_do_not_get_preferences_mixed_up_between_identical_displays_on_new_connectors()
    {
        // Arrange
        // Both displays of the same model moved to connectors that no profile knows about,
        // so there's no way to tell which one is which
        var profiles = new[]
        {
            Profile(@"\\?\DISPLAY#DEL4321#4&aaa&0&UID100#" + Guid, -100),
            Profile(@"\\?\DISPLAY#DEL4321#4&aaa&0&UID200#" + Guid, 200),
        };

        // Act
        var rekeyed = MonitorProfileMatcher.TryRekey(
            [
                @"\\?\DISPLAY#DEL4321#4&bbb&1&UID300#" + Guid,
                @"\\?\DISPLAY#DEL4321#4&bbb&1&UID400#" + Guid,
            ],
            profiles
        );

        // Assert
        rekeyed.Should().BeNull();
    }

    [Fact]
    public void I_see_outdated_duplicates_of_a_display_dropped_when_its_preferences_are_kept()
    {
        // Arrange
        // The same TV was saved under two older paths; the most recent one (last) wins
        var older = @"\\?\DISPLAY#SAM75B2#5&2f42420&2&UID33024#" + Guid;
        var profiles = new[] { Profile(older, -200), Profile(TvBefore, -1500) };

        // Act
        var rekeyed = MonitorProfileMatcher.TryRekey([TvAfter], profiles);

        // Assert
        rekeyed.Should().ContainSingle();
        rekeyed!.Single().DevicePath.Should().Be(TvAfter);
        rekeyed.Single().TemperatureOffset.Should().Be(-1500);
    }

    [Fact]
    public void I_see_no_changes_when_every_display_already_has_matching_preferences()
    {
        // Arrange
        // Device paths are compared case-insensitively
        var profiles = new[] { Profile(TvAfter.ToUpperInvariant()), Profile(MonitorAfter) };

        // Act
        var rekeyed = MonitorProfileMatcher.TryRekey([TvAfter, MonitorAfter], profiles);

        // Assert
        rekeyed.Should().BeNull();
    }

    [Fact]
    public void I_keep_preferences_of_disconnected_displays_untouched()
    {
        // Arrange
        var unplugged = @"\\?\DISPLAY#RTK0012#5&2f42420&0&UID33024#" + Guid;
        var profiles = new[] { Profile(unplugged, 400), Profile(TvBefore, -1500) };

        // Act
        var rekeyed = MonitorProfileMatcher.TryRekey([TvAfter], profiles);

        // Assert
        rekeyed.Should().HaveCount(2);
        rekeyed!
            .Should()
            .ContainSingle(p => p.DevicePath == unplugged && p.TemperatureOffset == 400);
        rekeyed.Should().ContainSingle(p => p.DevicePath == TvAfter);
    }

    [Fact]
    public void I_get_displays_without_a_device_path_matched_only_exactly()
    {
        // Arrange
        // Displays that report no device path are identified by their GDI device name instead
        var profiles = new[] { Profile(@"\\.\DISPLAY2", 100) };

        // Act
        var matches = MonitorProfileMatcher.Match([@"\\.\DISPLAY3", @"\\.\DISPLAY2"], profiles);

        // Assert
        matches.Should().ContainSingle().Which.Key.Should().Be(@"\\.\DISPLAY2");
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData(@"\\?\DISPLAY##")]
    [InlineData(@"\\?\USB#VID_1234#5&1&2#" + Guid)]
    public void I_get_no_failures_or_false_matches_for_malformed_device_paths(string devicePath)
    {
        // Arrange
        var profiles = new[] { Profile(TvBefore), Profile(devicePath) };

        // Act
        var rekeyed = MonitorProfileMatcher.TryRekey([devicePath, MonitorAfter], profiles);

        // Assert
        (rekeyed ?? profiles)
            .Should()
            .Contain(p => p.DevicePath == TvBefore);
    }

    [Fact]
    public void I_keep_display_preferences_from_a_settings_file_after_the_display_adapter_is_re_enumerated()
    {
        // Arrange
        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(
            filePath,
            """
            {
              "MonitorProfiles": [
                { "DevicePath": "\\\\?\\DISPLAY#SAM75B2#5&2f42420&0&UID33024#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}", "LastKnownName": "S90D", "TemperatureOffset": -2000 }
              ]
            }
            """
        );

        var settings = new SettingsService(filePath);
        settings.Load();

        // Act
        var isChanged = settings.ReconcileMonitorProfiles([MonitorAfter, TvAfter]);

        // Assert
        isChanged.Should().BeTrue();
        settings.TryGetMonitorProfile(TvBefore).Should().BeNull();

        var profile = settings.TryGetMonitorProfile(TvAfter);
        profile.Should().NotBeNull();
        profile!.TemperatureOffset.Should().Be(-2000);
        profile.LastKnownName.Should().Be("S90D");
        profile.IsEnabled.Should().BeTrue();

        // Nothing left to do
        settings.ReconcileMonitorProfiles([MonitorAfter, TvAfter]).Should().BeFalse();
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
