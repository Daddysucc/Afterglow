using System;
using System.Collections.Generic;
using System.IO;
using Afterglow.Core;
using Afterglow.Models;
using Afterglow.PlatformInterop;

namespace Afterglow.Services;

public class ExternalApplicationService
{
    // Applications that we don't want to show to the user
    private readonly HashSet<string> _ignoredApplicationNames = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "explorer",
        // Windows shell experience host (hosts the task switcher, notification center, etc.)
        "ShellExperienceHost",
        // Windows start menu experience host
        "StartMenuExperienceHost",
    };

    private static string? TryGetExecutableFilePath(Window? window)
    {
        using var process = window?.TryGetProcess();
        return process?.TryGetExecutableFilePath();
    }

    private bool IsIgnored(string executableFilePath)
    {
        var executableFileName = Path.GetFileNameWithoutExtension(executableFilePath);

        return string.IsNullOrWhiteSpace(executableFileName)
            || _ignoredApplicationNames.Contains(executableFileName);
    }

    public IEnumerable<ExternalApplication> GetAllRunningApplications()
    {
        foreach (var window in Window.GetAll())
        {
            using var _ = window;

            if (!window.IsVisible() || window.IsCloaked() || window.IsSystemWindow())
                continue;

            var executableFilePath = TryGetExecutableFilePath(window);
            if (string.IsNullOrWhiteSpace(executableFilePath) || IsIgnored(executableFilePath))
                continue;

            yield return new ExternalApplication(executableFilePath);
        }
    }

    // The window in focus, as checked against the whitelist
    private sealed class WhitelistWindow(Window window) : IWhitelistWindow
    {
        public string? TryGetTitle() => window.TryGetTitle();

        // Normalized the same way as the whitelisted applications, so that they can be compared
        public string? TryGetExecutableFilePath() =>
            ExternalApplicationService.TryGetExecutableFilePath(window) is { } path
                ? new ExternalApplication(path).ExecutableFilePath
                : null;

        public string? TryGetMonitor() => window.TryGetNearestMonitorDeviceName();
    }

    /// <summary>
    /// Gets the window in focus to check against the whitelist (windows behind it never count),
    /// or <c>null</c> if it can't be judged: there's none, or it's the desktop, the taskbar, or
    /// another part of the shell, or one of this app's own windows.
    /// </summary>
    public IWhitelistWindow? TryGetForegroundWhitelistWindow()
    {
        var window = Window.TryGetForeground();
        if (window is null)
            return null;

        // This app's own windows never count (e.g. a keyword that's part of its title)
        if (window.GetProcessId() == Environment.ProcessId || window.IsSystemWindow())
        {
            window.Dispose();
            return null;
        }

        return new WhitelistWindow(window);
    }

    /// <summary>
    /// Checks whether the foreground window is a fullscreen application, and gets the GDI device
    /// name (e.g. <c>\\.\DISPLAY2</c>) of the display it's on, or <c>null</c> if it isn't.
    /// Returns <c>false</c> if the foreground window can't be judged: there's none, or it's part
    /// of the shell (e.g. the taskbar), in which case the previous state should be kept, so that
    /// switching between a fullscreen application and the taskbar doesn't cause flashes.
    /// https://github.com/Tyrrrz/LightBulb/issues/451
    /// Applications that can't be identified are still judged: games protected by anti-cheat,
    /// for example, don't let their executable be looked up, but can still be fullscreen.
    /// </summary>
    public bool TryGetForegroundFullScreenMonitor(out string? fullScreenMonitor)
    {
        fullScreenMonitor = null;

        using var window = Window.TryGetForeground();
        if (window is null || window.IsSystemWindow())
            return false;

        var executableFilePath = TryGetExecutableFilePath(window);
        if (!string.IsNullOrWhiteSpace(executableFilePath) && IsIgnored(executableFilePath))
            return false;

        // Hidden, minimized, and cloaked windows (e.g. suspended UWP apps, or windows on other
        // virtual desktops) may still report fullscreen bounds, but aren't actually on screen
        if (!window.IsVisible() || window.IsMinimized() || window.IsCloaked())
            return true;

        fullScreenMonitor = window.IsFullScreen() ? window.TryGetMonitorDeviceName() : null;
        return true;
    }

    /// <summary>
    /// Checks whether the computer is being streamed or accessed remotely: Remote Desktop is
    /// detected on any port, other streaming hosts (e.g. Sunshine) by an established connection
    /// on one of the given local TCP ports.
    /// </summary>
    public bool IsRemoteStreamingActive(IReadOnlyCollection<int> ports) =>
        RemoteSession.IsStreamingActive(ports);
}
