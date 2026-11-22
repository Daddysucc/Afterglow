using System;
using System.IO;
using System.Text.Json;
using Afterglow.Core;
using Afterglow.Services;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class UpdateCheckSpecs : IDisposable
{
    // As stamped on a released build (assembly versions have four parts)
    private static readonly Version CurrentVersion = new(3, 1, 0, 0);

    private static Version? GetNewerVersion(string releaseJson)
    {
        using var document = JsonDocument.Parse(releaseJson);
        return UpdateCheck.TryGetNewerVersion(document.RootElement, CurrentVersion);
    }

    // ---- Release tags ----

    [Theory]
    [InlineData("3.2.0", "3.2.0")]
    [InlineData("v3.2.0", "3.2.0")]
    [InlineData("V3.2.0", "3.2.0")]
    [InlineData("3.2", "3.2.0")]
    [InlineData(" v3.2.1 ", "3.2.1")]
    [InlineData("3.2.0-beta.2", "3.2.0")]
    [InlineData("3.2.0+build.7", "3.2.0")]
    public void I_can_get_the_version_of_a_release_tag(string tag, string expected)
    {
        // Act & assert
        UpdateCheck.TryParseVersion(tag).Should().Be(Version.Parse(expected));
    }

    [Theory]
    [InlineData("latest")]
    [InlineData("v")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void I_cannot_get_a_version_from_a_tag_that_is_not_one(string? tag)
    {
        // Act & assert
        UpdateCheck.TryParseVersion(tag).Should().BeNull();
    }

    // ---- Releases ----

    [Theory]
    [InlineData("v3.1.1", "3.1.1")]
    [InlineData("3.2.0", "3.2.0")]
    [InlineData("4.0", "4.0.0")]
    public void I_am_told_about_a_newer_release(string tag, string expected)
    {
        // Act
        var version = GetNewerVersion(
            $$"""{ "tag_name": "{{tag}}", "draft": false, "prerelease": false }"""
        );

        // Assert
        version.Should().Be(Version.Parse(expected));
    }

    [Theory]
    [InlineData("3.1.0")]
    [InlineData("v3.1")]
    [InlineData("3.0.9")]
    [InlineData("2.5.0")]
    public void I_am_not_told_about_the_version_i_have_or_an_older_one(string tag)
    {
        // Act & assert
        GetNewerVersion($$"""{ "tag_name": "{{tag}}" }""").Should().BeNull();
    }

    [Theory]
    [InlineData("""{ "tag_name": "9.0.0", "prerelease": true }""")]
    [InlineData("""{ "tag_name": "9.0.0", "draft": true }""")]
    public void I_am_not_told_about_unfinished_releases(string releaseJson)
    {
        // Act & assert
        GetNewerVersion(releaseJson).Should().BeNull();
    }

    [Theory]
    [InlineData("""{ }""")]
    [InlineData("""{ "tag_name": null }""")]
    [InlineData("""{ "tag_name": 9 }""")]
    [InlineData("""{ "tag_name": "nightly" }""")]
    [InlineData("""{ "message": "Not Found" }""")]
    [InlineData("""[ { "tag_name": "9.0.0" } ]""")]
    public void I_am_not_told_about_an_update_from_an_unexpected_response(string releaseJson)
    {
        // Act & assert
        GetNewerVersion(releaseJson).Should().BeNull();
    }

    // ---- Schedule ----

    private static readonly DateTimeOffset Now = new(2026, 11, 14, 20, 00, 00, TimeSpan.Zero);

    private static readonly TimeSpan Week = UpdateCheck.GetInterval(UpdateCheckFrequency.Weekly);

    [Fact]
    public void I_get_updates_checked_when_they_have_not_been_yet()
    {
        // Act & assert
        UpdateCheck.IsDue(null, Now, Week).Should().BeTrue();
    }

    [Theory]
    [InlineData(UpdateCheckFrequency.Daily, 1)]
    [InlineData(UpdateCheckFrequency.Weekly, 7)]
    [InlineData(UpdateCheckFrequency.EveryTwoWeeks, 14)]
    public void I_get_updates_checked_as_often_as_i_chose(UpdateCheckFrequency frequency, int days)
    {
        // Arrange
        var interval = UpdateCheck.GetInterval(frequency);

        // Act & assert
        interval.Should().Be(TimeSpan.FromDays(days));
        UpdateCheck
            .IsDue(Now - interval + TimeSpan.FromMinutes(1), Now, interval)
            .Should()
            .BeFalse();
        UpdateCheck.IsDue(Now - interval, Now, interval).Should().BeTrue();
    }

    [Fact]
    public void I_get_updates_checked_again_only_after_a_while()
    {
        // Act & assert
        UpdateCheck.IsDue(Now - TimeSpan.FromHours(1), Now, Week).Should().BeFalse();
        UpdateCheck.IsDue(Now - TimeSpan.FromDays(3), Now, Week).Should().BeFalse();

        // Time asleep (or with the app closed) counts, as it's checked against the clock
        UpdateCheck.IsDue(Now - TimeSpan.FromDays(10), Now, Week).Should().BeTrue();
    }

    [Fact]
    public void I_get_updates_checked_again_after_the_clock_was_set_back()
    {
        // Act & assert
        UpdateCheck.IsDue(Now + TimeSpan.FromDays(1), Now, Week).Should().BeTrue();
    }

    [Theory]
    [InlineData("3.2.0", true)]
    [InlineData("v4.0", true)]
    [InlineData("3.1.0", false)]
    [InlineData("3.0.1", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("not a version", false)]
    public void I_get_an_update_found_before_a_restart_offered_again_right_away(
        string? knownVersion,
        bool isPending
    )
    {
        // Act & assert
        UpdateCheck.IsKnownUpdatePending(knownVersion, CurrentVersion).Should().Be(isPending);
    }

    // ---- Settings ----

    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + Guid.NewGuid().ToString("N")
    );

    public UpdateCheckSpecs() => Directory.CreateDirectory(_directoryPath);

    // Only loaded, never saved (saving also writes to the registry)
    private SettingsService Load(string json)
    {
        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(filePath, json);

        var settings = new SettingsService(filePath);
        settings.Load();

        return settings;
    }

    [Fact]
    public void I_get_updates_checked_by_default()
    {
        // Act & assert
        Load("{}").IsUpdateCheckEnabled.Should().BeTrue();
    }

    [Fact]
    public void I_keep_updates_turned_off_if_i_turned_them_off_in_the_original_lightbulb()
    {
        // Act & assert
        Load("""{ "IsAutoUpdateEnabled": false }""").IsUpdateCheckEnabled.Should().BeFalse();
    }

    [Fact]
    public void I_get_updates_checked_weekly_by_default()
    {
        // Arrange
        var settings = Load("{}");

        // Assert
        settings.UpdateCheckFrequency.Should().Be(UpdateCheckFrequency.Weekly);
        settings.LastUpdateCheckTime.Should().BeNull();
        settings.AvailableUpdateVersion.Should().BeNull();
    }

    [Fact]
    public void I_keep_my_update_check_frequency_and_when_updates_were_last_checked()
    {
        // Arrange
        var lastCheckTime = new DateTimeOffset(2026, 11, 21, 22, 5, 0, TimeSpan.FromHours(2));

        var settings = Load("{}");
        settings.UpdateCheckFrequency = UpdateCheckFrequency.EveryTwoWeeks;
        settings.LastUpdateCheckTime = lastCheckTime;
        settings.AvailableUpdateVersion = "3.4.0";

        // Act
        settings.TrySaveToFile().Should().BeTrue();

        var reloaded = new SettingsService(Path.Combine(_directoryPath, "Settings.json"));
        reloaded.Load();

        // Assert
        reloaded.UpdateCheckFrequency.Should().Be(UpdateCheckFrequency.EveryTwoWeeks);
        reloaded.LastUpdateCheckTime.Should().Be(lastCheckTime);
        reloaded.AvailableUpdateVersion.Should().Be("3.4.0");
    }

    [Fact]
    public void I_get_updates_checked_weekly_if_the_saved_frequency_is_not_one_i_can_choose()
    {
        // Act
        // E.g. from a newer version, or edited by hand
        var settings = Load(
            """{ "UpdateCheckFrequency": "Hourly", "NightStartTime": "23:30:00" }"""
        );

        // Assert
        settings.UpdateCheckFrequency.Should().Be(UpdateCheckFrequency.Weekly);
        settings.NightStartTime.Should().Be(new TimeOnly(23, 30));
    }

    [Theory]
    [InlineData(0, false, UpdateCheckFrequency.Weekly)]
    [InlineData(1, true, UpdateCheckFrequency.Daily)]
    [InlineData(2, true, UpdateCheckFrequency.Weekly)]
    [InlineData(3, true, UpdateCheckFrequency.EveryTwoWeeks)]
    public void I_can_choose_how_often_updates_are_checked_or_turn_checking_off(
        int index,
        bool isEnabled,
        UpdateCheckFrequency frequency
    )
    {
        // Arrange
        var settings = Load("{}");
        using var localization = new Afterglow.Localization.LocalizationManager(settings);
        using var updateService = new UpdateService(settings);
        using var tab = new Afterglow.ViewModels.Components.Settings.ControlsSettingsTabViewModel(
            settings,
            localization,
            updateService
        );

        // Act
        tab.UpdateCheckFrequencyIndex = index;

        // Assert
        settings.IsUpdateCheckEnabled.Should().Be(isEnabled);
        settings.UpdateCheckFrequency.Should().Be(frequency);
        tab.UpdateCheckFrequencyIndex.Should().Be(index);
    }

    [Fact]
    public void I_can_see_which_version_i_have_and_when_updates_were_last_checked()
    {
        // Arrange
        var settings = Load("{}");
        using var localization = new Afterglow.Localization.LocalizationManager(settings);
        using var updateService = new UpdateService(settings);
        using var tab = new Afterglow.ViewModels.Components.Settings.ControlsSettingsTabViewModel(
            settings,
            localization,
            updateService
        );

        // Act & assert
        tab.VersionText.Should().Contain(Program.VersionString);
        tab.VersionText.Should().Contain(localization.NeverCheckedText);

        settings.LastUpdateCheckTime = new DateTimeOffset(2026, 11, 21, 22, 5, 0, TimeSpan.Zero);
        tab.VersionText.Should().Contain(Program.VersionString);
        tab.VersionText.Should().NotContain(localization.NeverCheckedText);
        tab.VersionText.Should()
            .Contain(
                settings
                    .LastUpdateCheckTime.Value.ToLocalTime()
                    .ToString("g", System.Globalization.CultureInfo.CurrentCulture)
            );
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directoryPath, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
