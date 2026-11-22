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

    [Fact]
    public void I_get_updates_checked_when_they_have_not_been_yet()
    {
        // Act & assert
        UpdateCheck.IsDue(null, Now).Should().BeTrue();
    }

    [Fact]
    public void I_get_updates_checked_again_only_after_a_while()
    {
        // Act & assert
        UpdateCheck.IsDue(Now - TimeSpan.FromHours(1), Now).Should().BeFalse();
        UpdateCheck.IsDue(Now - UpdateCheck.Interval, Now).Should().BeTrue();

        // Time asleep counts, as it's checked against the clock
        UpdateCheck.IsDue(Now - TimeSpan.FromDays(2), Now).Should().BeTrue();
    }

    [Fact]
    public void I_get_updates_checked_again_after_the_clock_was_set_back()
    {
        // Act & assert
        UpdateCheck.IsDue(Now + TimeSpan.FromDays(1), Now).Should().BeTrue();
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
