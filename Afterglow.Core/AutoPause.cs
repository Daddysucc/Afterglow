using System;
using System.Collections.Generic;
using System.Linq;

namespace Afterglow.Core;

public enum PauseReason
{
    None,
    RemoteStreaming,
    ScreenLocked,
    FullScreen,
    Whitelist,
}

/// <summary>
/// Conditions observed on the system.
/// </summary>
/// <param name="FullScreenMonitor">Identifier of the display running a fullscreen foreground application, if any.</param>
/// <param name="IsWhitelistMatched">Whether the window in focus belongs to a whitelisted application, or has a whitelisted title.</param>
/// <param name="WhitelistedMonitor">Identifier of the display that shows the whitelisted window, if known.</param>
/// <param name="ConnectedMonitors">Identifiers of all connected displays, to tell when all of them are paused.</param>
public readonly record struct AutoPauseConditions(
    bool IsRemoteStreamingActive,
    bool IsScreenLocked,
    string? FullScreenMonitor,
    bool IsWhitelistMatched,
    string? WhitelistedMonitor = null,
    IReadOnlyCollection<string>? ConnectedMonitors = null
);

public readonly record struct AutoPauseSettings(
    bool IsStreamingAutoPauseEnabled,
    bool IsPauseWhenScreenLockedEnabled,
    bool IsPauseWhenFullScreenEnabled,
    bool IsPerMonitorFullScreenPauseEnabled,
    bool IsApplicationWhitelistEnabled,
    bool IsPerMonitorWhitelistPauseEnabled = false
)
{
    /// <summary>
    /// Whether the foreground application needs to be looked up at all (which involves opening
    /// its process), as only the fullscreen check depends on it. The whitelist looks at the
    /// foreground window on its own, opening its process only when the title doesn't match.
    /// </summary>
    public bool IsForegroundApplicationNeeded => IsPauseWhenFullScreenEnabled;
}

/// <param name="Reason">Why all displays are paused, if they are.</param>
/// <param name="PausedMonitors">Displays that are paused on their own, while the others are not.</param>
public readonly record struct AutoPauseState(
    PauseReason Reason,
    IReadOnlyCollection<string>? PausedMonitors = null
)
{
    public bool IsGloballyPaused => Reason != PauseReason.None;

    // Whether any display is paused, on its own or along with all the others
    public bool IsAnyMonitorPaused => IsGloballyPaused || PausedMonitors is { Count: > 0 };

    public bool IsMonitorPaused(string monitor) =>
        IsGloballyPaused
        || PausedMonitors?.Contains(monitor, StringComparer.OrdinalIgnoreCase) == true;

    public static AutoPauseState None { get; } = new(PauseReason.None);
}

public static class AutoPause
{
    /// <summary>
    /// Determines which displays should be paused. Global reasons take precedence, in order:
    /// remote streaming, locked screen, fullscreen application (unless paused per display),
    /// whitelisted window in focus (unless paused per display). Otherwise, the display running a
    /// fullscreen application and the display showing the whitelisted window are paused on their
    /// own, both at once. When that adds up to every connected display, all displays are paused.
    /// </summary>
    public static AutoPauseState Evaluate(
        AutoPauseConditions conditions,
        AutoPauseSettings settings
    )
    {
        if (settings.IsStreamingAutoPauseEnabled && conditions.IsRemoteStreamingActive)
            return new AutoPauseState(PauseReason.RemoteStreaming);

        if (settings.IsPauseWhenScreenLockedEnabled && conditions.IsScreenLocked)
            return new AutoPauseState(PauseReason.ScreenLocked);

        var isFullScreen =
            settings.IsPauseWhenFullScreenEnabled && conditions.FullScreenMonitor is not null;

        if (isFullScreen && !settings.IsPerMonitorFullScreenPauseEnabled)
            return new AutoPauseState(PauseReason.FullScreen);

        var isWhitelisted = settings.IsApplicationWhitelistEnabled && conditions.IsWhitelistMatched;

        // Without knowing which display shows the whitelisted window, all of them are paused
        if (
            isWhitelisted
            && (
                !settings.IsPerMonitorWhitelistPauseEnabled || conditions.WhitelistedMonitor is null
            )
        )
        {
            return new AutoPauseState(PauseReason.Whitelist);
        }

        var pausedMonitors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (isFullScreen)
            pausedMonitors.Add(conditions.FullScreenMonitor!);

        if (isWhitelisted)
            pausedMonitors.Add(conditions.WhitelistedMonitor!);

        if (pausedMonitors.Count == 0)
            return AutoPauseState.None;

        // Every display is paused anyway
        if (
            conditions.ConnectedMonitors is { Count: > 0 } connectedMonitors
            && connectedMonitors.All(pausedMonitors.Contains)
        )
        {
            return new AutoPauseState(
                isFullScreen ? PauseReason.FullScreen : PauseReason.Whitelist
            );
        }

        return new AutoPauseState(PauseReason.None, pausedMonitors);
    }
}
