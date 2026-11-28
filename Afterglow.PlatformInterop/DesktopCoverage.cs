using System;
using System.Collections.Generic;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

public static class DesktopCoverage
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020;
    private const long WsExLayered = 0x00080000;
    private const int DwmwaExtendedFrameBounds = 9;

    /// <summary>
    /// The bounds of the displays whose desktop can't be seen at all, as a window covers all of it
    /// other than the taskbar (e.g. a maximized one, or one in fullscreen).
    /// </summary>
    /// <remarks>
    /// Meant to be cheap enough to call every few seconds: a single pass over the windows that
    /// stops once every display is covered, ruling most windows out on what's quickest to look up
    /// (visible, not minimized, big enough) before anything else. A window that may be see-through
    /// (layered, like overlays and some custom-drawn windows) doesn't count, so a display is only
    /// ever taken as covered when it surely is.
    /// </remarks>
    public static IReadOnlyList<Rect> GetCoveredMonitorBounds()
    {
        var monitors = new List<(Rect Bounds, Rect WorkArea)>();
        foreach (var monitor in Monitor.GetAll())
        {
            if (monitor.TryGetBounds() is { } bounds && monitor.TryGetWorkArea() is { } workArea)
                monitors.Add((bounds, workArea));
        }

        var covered = new List<Rect>();
        if (monitors.Count == 0)
            return covered;

        NativeMethods.EnumWindows(
            (handle, _) =>
            {
                if (
                    handle == 0
                    || !NativeMethods.IsWindowVisible(handle)
                    || NativeMethods.IsIconic(handle)
                    || !NativeMethods.GetWindowRect(handle, out var outerRect)
                )
                    return true;

                foreach (var (bounds, workArea) in monitors)
                {
                    // (the outer rectangle includes invisible borders, so it's only good for
                    // ruling a window out: one smaller than the work area can't cover it)
                    if (covered.Contains(bounds) || !Contains(outerRect, workArea))
                        continue;

                    if (IsOpaqueCover(handle, workArea))
                        covered.Add(bounds);
                }

                // (done once every display is covered)
                return covered.Count < monitors.Count;
            },
            0
        );

        return covered;
    }

    private static bool Contains(Rect outer, Rect inner) =>
        outer.Left <= inner.Left
        && outer.Top <= inner.Top
        && outer.Right >= inner.Right
        && outer.Bottom >= inner.Bottom;

    // Only looked up for the few windows that are big enough
    private static bool IsOpaqueCover(nint handle, Rect workArea)
    {
        var exStyle =
            IntPtr.Size == 8
                ? (long)NativeMethods.GetWindowLongPtr(handle, GwlExStyle)
                : NativeMethods.GetWindowLong32(handle, GwlExStyle);
        if ((exStyle & (WsExTransparent | WsExLayered)) != 0)
            return false;

        var window = new Window(handle);

        // The desktop itself, the taskbar and the like, and windows that are hidden by Windows
        // (e.g. on another virtual desktop)
        if (window.IsSystemWindow() || window.IsCloaked())
            return false;

        // What's actually drawn, without the invisible borders
        try
        {
            if (
                NativeMethods.DwmGetWindowRectAttribute(
                    handle,
                    DwmwaExtendedFrameBounds,
                    out var frame,
                    System.Runtime.InteropServices.Marshal.SizeOf<Rect>()
                ) == 0
            )
                return Contains(frame, workArea);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { }

        return false;
    }
}
