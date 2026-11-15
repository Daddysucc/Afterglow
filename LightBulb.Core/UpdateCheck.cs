using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LightBulb.Core.Utils;

namespace LightBulb.Core;

/// <summary>
/// Finds out whether a newer version has been released on GitHub. Only the version is looked up:
/// nothing is downloaded or installed.
/// </summary>
public static class UpdateCheck
{
    // How long to wait after the last successful check before checking again
    public static TimeSpan Interval { get; } = TimeSpan.FromHours(12);

    // Checked against the clock, so that time spent asleep counts
    public static bool IsDue(DateTimeOffset? lastCheckedAt, DateTimeOffset now) =>
        lastCheckedAt is not { } at || now - at >= Interval || now < at;

    /// <summary>
    /// Gets the version from a release tag, e.g. "v3.1.0" or "3.1", or <c>null</c> if it isn't one.
    /// Versions are compared by their major, minor, and patch numbers.
    /// </summary>
    public static Version? TryParseVersion(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        var value = tag.Trim().TrimStart('v', 'V');

        // Pre-release and build labels (e.g. "3.1.0-beta") don't take part in the comparison
        var labelIndex = value.IndexOfAny(['-', '+']);
        if (labelIndex >= 0)
            value = value[..labelIndex];

        return Version.TryParse(value, out var version) ? Normalize(version) : null;
    }

    private static Version Normalize(Version version) =>
        new(version.Major, Math.Max(version.Minor, 0), Math.Max(version.Build, 0));

    /// <summary>
    /// Gets the version of a GitHub release (as returned by its API), if it's a published, stable
    /// release that is newer than the current version.
    /// </summary>
    public static Version? TryGetNewerVersion(JsonElement release, Version currentVersion)
    {
        if (release.ValueKind != JsonValueKind.Object)
            return null;

        if (IsTrue(release, "draft") || IsTrue(release, "prerelease"))
            return null;

        var tag =
            release.TryGetProperty("tag_name", out var tagJson)
            && tagJson.ValueKind == JsonValueKind.String
                ? tagJson.GetString()
                : null;

        return TryParseVersion(tag) is { } version && version > Normalize(currentVersion)
            ? version
            : null;
    }

    private static bool IsTrue(JsonElement json, string propertyName) =>
        json.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>
    /// Gets the version of the latest release from the GitHub API (e.g.
    /// <c>https://api.github.com/repos/owner/name/releases/latest</c>), if it's newer than the
    /// current version. Throws if the release can't be retrieved (e.g. there's no connection).
    /// </summary>
    public static async Task<Version?> GetNewerVersionAsync(
        string latestReleaseApiUrl,
        Version currentVersion,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await Http.Client.GetAsync(latestReleaseApiUrl, cancellationToken);

        // Nothing has been released yet
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken
        );

        return TryGetNewerVersion(json.RootElement, currentVersion);
    }
}
