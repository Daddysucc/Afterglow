using System;
using System.Collections.Generic;
using System.Linq;

namespace LightBulb.Core;

/// <summary>
/// Matches saved display profiles to connected displays by device path.
/// </summary>
/// <remarks>
/// Device paths look like <c>\\?\DISPLAY#SAM75B2#5&amp;2f42420&amp;1&amp;UID33024#{...}</c>:
/// the hardware ID (model) of the display, then its instance path, which ends with the ID of the
/// connector it's plugged into. The rest of the instance path describes the display adapter, and
/// Windows changes it whenever the adapter is re-enumerated (e.g. after a driver update, or when
/// switching GPUs on a hybrid laptop), which would otherwise orphan all saved profiles.
/// </remarks>
public static class MonitorProfileMatcher
{
    private readonly record struct DeviceIdentity(string HardwareId, string ConnectorId);

    private static DeviceIdentity? TryGetIdentity(string? devicePath)
    {
        if (string.IsNullOrWhiteSpace(devicePath))
            return null;

        var parts = devicePath.Split('#');
        if (
            parts.Length < 3
            || !parts[0].EndsWith("DISPLAY", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(parts[1])
            || string.IsNullOrWhiteSpace(parts[2])
        )
        {
            return null;
        }

        var instancePath = parts[2];
        var connectorId = instancePath[(instancePath.LastIndexOf('&') + 1)..];

        return new DeviceIdentity(parts[1].ToUpperInvariant(), connectorId.ToUpperInvariant());
    }

    /// <summary>
    /// Finds the saved profile of each connected display: by exact device path first, then the same
    /// model on the same connector, then the same model on any connector (only when it's the only
    /// connected display of that model). Each profile is matched to at most one display, and when
    /// several profiles fit equally well, the last one (the most recently changed) wins.
    /// </summary>
    /// <returns>Matched profiles, keyed by the device path of the connected display.</returns>
    public static IReadOnlyDictionary<string, MonitorProfile> Match(
        IReadOnlyList<string> connectedDevicePaths,
        IReadOnlyList<MonitorProfile> profiles
    )
    {
        var result = new Dictionary<string, MonitorProfile>(StringComparer.OrdinalIgnoreCase);
        var unmatchedProfiles = new List<MonitorProfile>(profiles);

        var connected = connectedDevicePaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        void Assign(string devicePath, MonitorProfile profile)
        {
            result[devicePath] = profile;
            unmatchedProfiles.Remove(profile);
        }

        // Exact device path
        foreach (var devicePath in connected)
        {
            var profile = unmatchedProfiles.LastOrDefault(p =>
                string.Equals(p.DevicePath, devicePath, StringComparison.OrdinalIgnoreCase)
            );

            if (profile is not null)
                Assign(devicePath, profile);
        }

        // Same model on the same connector
        foreach (var devicePath in connected.Where(p => !result.ContainsKey(p)))
        {
            if (TryGetIdentity(devicePath) is not { } identity)
                continue;

            var profile = unmatchedProfiles.LastOrDefault(p =>
                TryGetIdentity(p.DevicePath) == identity
            );
            if (profile is not null)
                Assign(devicePath, profile);
        }

        // Same model on a different connector, as long as there's no ambiguity about which display it is
        var unmatchedByModel = connected
            .Where(p => !result.ContainsKey(p))
            .Select(p => (DevicePath: p, Identity: TryGetIdentity(p)))
            .Where(x => x.Identity is not null)
            .GroupBy(x => x.Identity!.Value.HardwareId)
            .Where(g => g.Count() == 1)
            .Select(g => g.Single());

        foreach (var (devicePath, identity) in unmatchedByModel)
        {
            var profile = unmatchedProfiles.LastOrDefault(p =>
                TryGetIdentity(p.DevicePath)?.HardwareId == identity!.Value.HardwareId
            );

            if (profile is not null)
                Assign(devicePath, profile);
        }

        return result;
    }

    /// <summary>
    /// Updates the device paths of saved profiles that were matched to connected displays under
    /// a different path. Outdated duplicates of a re-keyed profile (the same model on the same
    /// connector, saved under an older path) are dropped, since they're the same display.
    /// </summary>
    /// <returns>The updated profiles, or <c>null</c> if nothing needed to change.</returns>
    public static IReadOnlyList<MonitorProfile>? TryRekey(
        IReadOnlyList<string> connectedDevicePaths,
        IReadOnlyList<MonitorProfile> profiles
    )
    {
        var matches = Match(connectedDevicePaths, profiles);

        var rekeyed = matches
            .Where(m =>
                !string.Equals(m.Value.DevicePath, m.Key, StringComparison.OrdinalIgnoreCase)
            )
            .ToArray();

        if (rekeyed.Length == 0)
            return null;

        var rekeyedProfiles = rekeyed
            .Select(m => m.Value)
            .ToHashSet(ReferenceEqualityComparer.Instance);
        var rekeyedIdentities = rekeyed
            .Select(m => TryGetIdentity(m.Key))
            .Where(i => i is not null)
            .ToHashSet();
        var matchedProfiles = matches.Values.ToHashSet(ReferenceEqualityComparer.Instance);

        var result = new List<MonitorProfile>();
        foreach (var profile in profiles)
        {
            // Superseded by a profile that was re-keyed to the same display
            if (
                !matchedProfiles.Contains(profile)
                && rekeyedIdentities.Contains(TryGetIdentity(profile.DevicePath))
            )
            {
                continue;
            }

            if (rekeyedProfiles.Contains(profile))
            {
                var devicePath = rekeyed.First(m => ReferenceEquals(m.Value, profile)).Key;
                result.Add(profile with { DevicePath = devicePath });
            }
            else
            {
                result.Add(profile);
            }
        }

        return result;
    }
}
