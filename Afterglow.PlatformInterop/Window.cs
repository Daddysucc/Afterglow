using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

// Window handles aren't owned by this process, so there's nothing to release (and no finalizer, which
// would make each of the several windows looked up every second more costly to collect)
public partial class Window(nint handle) : IDisposable
{
    public Process? TryGetProcess()
    {
        _ = NativeMethods.GetWindowThreadProcessId(Handle, out var processId);
        if (processId == 0)
        {
            Debug.WriteLine(
                $"Failed to retrieve process ID for window #{Handle}. "
                    + $"Error {Marshal.GetLastWin32Error()}."
            );

            return null;
        }

        // UWP apps are hosted by ApplicationFrameHost.exe, so the window's process is not
        // the actual app process. Enumerate child windows to find the real UWP app process.
        if (
            string.Equals(
                TryGetClassName(),
                "ApplicationFrameWindow",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            Process? uwpProcess = null;

            NativeMethods.EnumChildWindows(
                Handle,
                (hWnd, lParam) =>
                {
                    _ = NativeMethods.GetWindowThreadProcessId(hWnd, out var childProcessId);
                    if (childProcessId != 0 && childProcessId != processId)
                    {
                        uwpProcess = Process.TryGet((int)childProcessId);
                        if (uwpProcess is not null)
                            return false;
                    }

                    return true;
                },
                0
            );

            if (uwpProcess is not null)
                return uwpProcess;
        }

        return Process.TryGet((int)processId);
    }

    public Monitor? TryGetMonitor()
    {
        var monitorHandle = NativeMethods.MonitorFromWindow(Handle, 0);
        if (monitorHandle == 0)
        {
            Debug.WriteLine(
                $"Failed to retrieve monitor for window #{Handle}. "
                    + $"Error {Marshal.GetLastWin32Error()}."
            );

            return null;
        }

        return new Monitor(monitorHandle);
    }

    // GDI device name (e.g. \\.\DISPLAY1) of the monitor that hosts most of this window
    public string? TryGetMonitorDeviceName() => TryGetMonitor()?.TryGetDeviceName();

    // Same as above, but falls back to the nearest monitor for windows that are off screen
    public string? TryGetNearestMonitorDeviceName()
    {
        const uint monitorDefaultToNearest = 2;

        var monitorHandle = NativeMethods.MonitorFromWindow(Handle, monitorDefaultToNearest);
        return monitorHandle != 0 ? new Monitor(monitorHandle).TryGetDeviceName() : null;
    }

    // ID of the process that created the window. Cheap, unlike opening the process.
    public int GetProcessId()
    {
        _ = NativeMethods.GetWindowThreadProcessId(Handle, out var processId);
        return (int)processId;
    }

    // The window's title, or null if it has none
    public string? TryGetTitle()
    {
        var length = NativeMethods.GetWindowTextLength(Handle);
        if (length <= 0)
            return null;

        var buffer = new StringBuilder(length + 1);
        return NativeMethods.GetWindowText(Handle, buffer, buffer.Capacity) > 0
            ? buffer.ToString()
            : null;
    }

    public Rect? TryGetRect() => NativeMethods.GetWindowRect(Handle, out var rect) ? rect : null;

    public Rect? TryGetClientRect() =>
        NativeMethods.GetClientRect(Handle, out var rect) ? rect : null;

    public string? TryGetClassName()
    {
        var buffer = new StringBuilder(256);

        return NativeMethods.GetClassName(Handle, buffer, buffer.Capacity) >= 0
            ? buffer.ToString()
            : null;
    }

    public bool IsSystemWindow()
    {
        var className = TryGetClassName();

        return string.Equals(className, "Progman", StringComparison.OrdinalIgnoreCase)
            || string.Equals(className, "WorkerW", StringComparison.OrdinalIgnoreCase)
            || string.Equals(className, "Shell_TrayWnd", StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                className,
                "Shell_SecondaryTrayWnd",
                StringComparison.OrdinalIgnoreCase
            )
            || string.Equals(className, "ImmersiveLauncher", StringComparison.OrdinalIgnoreCase)
            || string.Equals(className, "ImmersiveSwitchList", StringComparison.OrdinalIgnoreCase)
            || string.Equals(className, "MultitaskingViewFrame", StringComparison.OrdinalIgnoreCase)
            || string.Equals(className, "ForegroundStaging", StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                className,
                "ApplicationManager_DesktopShellWindow",
                StringComparison.OrdinalIgnoreCase
            )
            || string.Equals(
                className,
                "XamlExplorerHostIslandWindow",
                StringComparison.OrdinalIgnoreCase
            );
    }

    public bool IsVisible() => NativeMethods.IsWindowVisible(Handle);

    public bool IsMinimized() => NativeMethods.IsIconic(Handle);

    // Cloaked windows are "visible" but not actually shown, e.g. suspended UWP app frames,
    // or windows on other virtual desktops
    public bool IsCloaked()
    {
        const int dwmwaCloaked = 14;

        try
        {
            return NativeMethods.DwmGetWindowAttribute(
                    Handle,
                    dwmwaCloaked,
                    out var cloaked,
                    sizeof(int)
                ) == 0
                && cloaked != 0;
        }
        // The desktop window manager API may be missing on stripped-down editions of Windows
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    public bool IsFullScreen()
    {
        if (TryGetMonitor() is not { } monitor)
            return false;

        if (monitor.TryGetBounds() is not { } monitorRect)
            return false;

        if (TryGetRect() is not { } windowRect)
            return false;

        // Calculate the absolute window client rect (not relative to the window)
        var windowClientRect = TryGetClientRect() ?? Rect.Empty;

        var absoluteWindowClientRect = new Rect(
            windowRect.Left + windowClientRect.Left,
            windowRect.Top + windowClientRect.Top,
            windowRect.Left + windowClientRect.Right,
            windowRect.Top + windowClientRect.Bottom
        );

        return absoluteWindowClientRect.Left <= monitorRect.Left
            && absoluteWindowClientRect.Top <= monitorRect.Top
            && absoluteWindowClientRect.Right >= monitorRect.Right
            && absoluteWindowClientRect.Bottom >= monitorRect.Bottom;
    }

    public nint Handle { get; } = handle;

    public void Dispose() { }
}

public partial class Window
{
    public static Window? TryGetForeground()
    {
        var handle = NativeMethods.GetForegroundWindow();
        if (handle == 0)
        {
            Debug.WriteLine(
                "Failed to retrieve foreground window. " + $"Error {Marshal.GetLastWin32Error()}."
            );

            return null;
        }

        return new Window(handle);
    }

    public static IReadOnlyList<Window> GetAll()
    {
        var result = new List<Window>();

        if (
            !NativeMethods.EnumWindows(
                (hWnd, _) =>
                {
                    if (hWnd != 0)
                    {
                        var window = new Window(hWnd);
                        result.Add(window);
                    }

                    return true;
                },
                0
            )
        )
        {
            Debug.WriteLine(
                "Failed to enumerate windows. " + $"Error {Marshal.GetLastWin32Error()}."
            );
        }

        return result;
    }
}

public partial class Window
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020;
    private const long WsExToolWindow = 0x00000080;
    private const long WsExLayered = 0x00080000;
    private const long WsExNoActivate = 0x08000000;
    private const uint LwaAlpha = 0x2;

    /// <summary>
    /// Turns the window into an overlay that never takes focus, doesn't show up in Alt+Tab,
    /// and lets mouse input pass through to whatever is underneath.
    /// </summary>
    public static void MakeOverlay(nint handle)
    {
        var exStyle =
            IntPtr.Size == 8
                ? (long)NativeMethods.GetWindowLongPtr(handle, GwlExStyle)
                : NativeMethods.GetWindowLong32(handle, GwlExStyle);

        // Mouse input only passes through transparent windows that are also layered
        exStyle |= WsExTransparent | WsExLayered | WsExToolWindow | WsExNoActivate;

        if (IntPtr.Size == 8)
            NativeMethods.SetWindowLongPtr(handle, GwlExStyle, (nint)exStyle);
        else
            NativeMethods.SetWindowLong32(handle, GwlExStyle, (int)exStyle);

        // A layered window stays invisible until its layering attributes are set,
        // so make it fully opaque
        if (!NativeMethods.SetLayeredWindowAttributes(handle, 0, 255, LwaAlpha))
        {
            Debug.WriteLine(
                $"Failed to set layered window attributes on window #{handle}. "
                    + $"Error {Marshal.GetLastWin32Error()}."
            );
        }
    }
}
