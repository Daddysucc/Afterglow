using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Afterglow.Core.Utils;

namespace Afterglow.Core;

/// <summary>
/// A file attached to a release. The SHA-256 digest (as hex) is the one GitHub calculated when the
/// file was uploaded, if it's known.
/// </summary>
public sealed record ReleaseAsset(string Name, Uri DownloadUrl, long Size, string? Sha256)
{
    // GitHub reports digests as e.g. "sha256:9f86d0..." (64 hex digits)
    public static string? TryParseSha256Digest(string? digest)
    {
        const string prefix = "sha256:";

        if (
            digest is null
            || !digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || digest.Length != prefix.Length + 64
        )
        {
            return null;
        }

        var hex = digest[prefix.Length..];
        foreach (var c in hex)
        {
            if (!char.IsAsciiHexDigit(c))
                return null;
        }

        return hex.ToLowerInvariant();
    }
}

/// <summary>
/// A newer release, with the files that can be downloaded from it.
/// </summary>
/// <param name="Version">The release's version.</param>
/// <param name="Assets">The files that can be downloaded.</param>
/// <param name="Notes">What changed, as written for the release (Markdown), if anything.</param>
public sealed record Release(
    Version Version,
    IReadOnlyList<ReleaseAsset> Assets,
    string? Notes = null
);

/// <summary>
/// How often to look for a newer release on GitHub, when that's turned on.
/// </summary>
public enum UpdateCheckFrequency
{
    Daily,
    Weekly,
    EveryTwoWeeks,
}

/// <summary>
/// Finds out whether a newer version has been released on GitHub, and which files it has.
/// </summary>
public static class UpdateCheck
{
    // How long to wait after the last successful check before checking again
    public static TimeSpan GetInterval(UpdateCheckFrequency frequency) =>
        frequency switch
        {
            UpdateCheckFrequency.Daily => TimeSpan.FromDays(1),
            UpdateCheckFrequency.EveryTwoWeeks => TimeSpan.FromDays(14),
            _ => TimeSpan.FromDays(7),
        };

    // Checked against the clock (which is remembered between launches), so that time spent asleep
    // or closed counts, and a clock that was set back doesn't postpone the next check
    public static bool IsDue(
        DateTimeOffset? lastCheckedAt,
        DateTimeOffset now,
        TimeSpan interval
    ) => lastCheckedAt is not { } at || now - at >= interval || now < at;

    /// <summary>
    /// Whether the last check (possibly before the app was restarted) found a version that's newer
    /// than this one. It's looked up again right away, so that it's offered again without waiting
    /// for the next scheduled check.
    /// </summary>
    public static bool IsKnownUpdatePending(string? knownVersion, Version currentVersion) =>
        TryParseVersion(knownVersion) is { } version && version > Normalize(currentVersion);

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

    private static string? TryGetString(JsonElement json, string propertyName) =>
        json.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// Gets a GitHub release (as returned by its API) with its downloadable files, if it's a
    /// published, stable release that is newer than the current version. Files that can't be
    /// downloaded safely (e.g. without a link or a size) are left out.
    /// </summary>
    public static Release? TryGetNewerRelease(JsonElement release, Version currentVersion)
    {
        if (TryGetNewerVersion(release, currentVersion) is not { } version)
            return null;

        var assets = new List<ReleaseAsset>();

        if (
            release.TryGetProperty("assets", out var assetsJson)
            && assetsJson.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var assetJson in assetsJson.EnumerateArray())
            {
                if (assetJson.ValueKind != JsonValueKind.Object)
                    continue;

                var name = TryGetString(assetJson, "name");
                var url = TryGetString(assetJson, "browser_download_url");

                if (
                    string.IsNullOrWhiteSpace(name)
                    || !Uri.TryCreate(url, UriKind.Absolute, out var downloadUrl)
                    || !assetJson.TryGetProperty("size", out var sizeJson)
                    || !sizeJson.TryGetInt64(out var size)
                    || size <= 0
                )
                {
                    continue;
                }

                assets.Add(
                    new ReleaseAsset(
                        name,
                        downloadUrl,
                        size,
                        ReleaseAsset.TryParseSha256Digest(TryGetString(assetJson, "digest"))
                    )
                );
            }
        }

        var notes = TryGetString(release, "body");

        return new Release(version, assets, string.IsNullOrWhiteSpace(notes) ? null : notes);
    }

    /// <summary>
    /// Gets the latest release from the GitHub API (e.g.
    /// <c>https://api.github.com/repos/owner/name/releases/latest</c>), if it's newer than the
    /// current version. Throws if the release can't be retrieved (e.g. there's no connection).
    /// </summary>
    public static async Task<Release?> GetNewerReleaseAsync(
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

        return TryGetNewerRelease(json.RootElement, currentVersion);
    }

    /// <summary>
    /// Gets the version of the latest release from the GitHub API, if it's newer than the
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
