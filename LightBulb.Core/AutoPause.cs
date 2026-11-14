namespace LightBulb.Core;

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
public readonly record struct AutoPauseConditions(
    bool IsRemoteStreamingActive,
    bool IsScreenLocked,
    string? FullScreenMonitor,
    bool IsWhitelistedApplicationActive
);

public readonly record struct AutoPauseSettings(
    bool IsStreamingAutoPauseEnabled,
    bool IsPauseWhenScreenLockedEnabled,
    bool IsPauseWhenFullScreenEnabled,
    bool IsPerMonitorFullScreenPauseEnabled,
    bool IsApplicationWhitelistEnabled
)
{
    /// <summary>
    /// Whether the foreground application needs to be looked up at all (which involves opening
    /// its process), as only the fullscreen and whitelist checks depend on it.
    /// </summary>
    public bool IsForegroundApplicationNeeded =>
        IsPauseWhenFullScreenEnabled || IsApplicationWhitelistEnabled;
}

/// <param name="Reason">Why all displays are paused, if they are.</param>
/// <param name="PausedMonitor">Display that is paused on its own, while the others are not.</param>
public readonly record struct AutoPauseState(PauseReason Reason, string? PausedMonitor)
{
    public bool IsGloballyPaused => Reason != PauseReason.None;

    public static AutoPauseState None { get; } = new(PauseReason.None, null);
}

public static class AutoPause
{
    /// <summary>
    /// Determines which displays should be paused. Global reasons take precedence, in order:
    /// remote streaming, locked screen, fullscreen application (unless paused per display),
    /// whitelisted application. A display-specific fullscreen pause only applies when nothing
    /// pauses all displays.
    /// </summary>
    public static AutoPauseState Evaluate(
        AutoPauseConditions conditions,
        AutoPauseSettings settings
    )
    {
        if (settings.IsStreamingAutoPauseEnabled && conditions.IsRemoteStreamingActive)
            return new AutoPauseState(PauseReason.RemoteStreaming, null);

        if (settings.IsPauseWhenScreenLockedEnabled && conditions.IsScreenLocked)
            return new AutoPauseState(PauseReason.ScreenLocked, null);

        var isFullScreen =
            settings.IsPauseWhenFullScreenEnabled && conditions.FullScreenMonitor is not null;

        if (isFullScreen && !settings.IsPerMonitorFullScreenPauseEnabled)
            return new AutoPauseState(PauseReason.FullScreen, null);

        if (settings.IsApplicationWhitelistEnabled && conditions.IsWhitelistedApplicationActive)
            return new AutoPauseState(PauseReason.Whitelist, null);

        return isFullScreen
            ? new AutoPauseState(PauseReason.None, conditions.FullScreenMonitor)
            : AutoPauseState.None;
    }
}
