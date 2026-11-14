using System;
using System.Collections.Generic;
using LightBulb.Core;

namespace LightBulb.Utils;

/// <summary>
/// Decides when gamma needs to be written to each display: only when the configuration has changed
/// noticeably since the last successful write, and, after a failed write, again shortly afterwards
/// (rather than on every tick, or never).
/// </summary>
public class GammaWriteTracker
{
    // Writing too often causes stuttering, and smaller changes aren't visible anyway
    public const double TemperatureThreshold = 15;
    public const double BrightnessThreshold = 0.01;

    // Displays may reject gamma ramps for a while (e.g. right after waking up), or permanently
    // (e.g. some virtual displays), so failed writes are retried, but not on every tick
    public static TimeSpan RetryDelay { get; } = TimeSpan.FromSeconds(1);

    private readonly Dictionary<string, ColorConfiguration> _lastConfigurations = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly Dictionary<string, DateTimeOffset> _lastFailureTimestamps = new(
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>
    /// Whether gamma should be written to the given display.
    /// </summary>
    /// <param name="deviceName">GDI device name of the display.</param>
    /// <param name="configuration">Configuration to write.</param>
    /// <param name="isStale">Whether gamma may have been changed from the outside, so it must be written regardless.</param>
    /// <param name="instant">Current time.</param>
    public bool ShouldWrite(
        string deviceName,
        ColorConfiguration configuration,
        bool isStale,
        DateTimeOffset instant
    )
    {
        if (isStale)
            return true;

        // The last write failed, so try again once the retry delay has passed
        if (_lastFailureTimestamps.TryGetValue(deviceName, out var lastFailureTimestamp))
            return (instant - lastFailureTimestamp).Duration() >= RetryDelay;

        // Nothing to compare to
        if (!_lastConfigurations.TryGetValue(deviceName, out var lastConfiguration))
            return true;

        return Math.Abs(configuration.Temperature - lastConfiguration.Temperature)
                > TemperatureThreshold
            || Math.Abs(configuration.Brightness - lastConfiguration.Brightness)
                > BrightnessThreshold;
    }

    public void RecordSuccess(string deviceName, ColorConfiguration configuration)
    {
        _lastConfigurations[deviceName] = configuration;
        _lastFailureTimestamps.Remove(deviceName);
    }

    public void RecordFailure(string deviceName, DateTimeOffset instant)
    {
        // What the display shows now is unknown
        _lastConfigurations.Remove(deviceName);
        _lastFailureTimestamps[deviceName] = instant;
    }

    /// <summary>
    /// Forgets everything, so that gamma is written to all displays on the next update
    /// (e.g. after the set of displays has changed).
    /// </summary>
    public void Reset()
    {
        _lastConfigurations.Clear();
        _lastFailureTimestamps.Clear();
    }
}
