using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using LightBulb.PlatformInterop.Internal;

namespace LightBulb.PlatformInterop;

/// <summary>
/// Snapshot of a connected display.
/// </summary>
/// <param name="Handle">Monitor handle (HMONITOR). Only valid until the display configuration changes.</param>
/// <param name="GdiDeviceName">GDI device name, e.g. <c>\\.\DISPLAY1</c>. Used to create device contexts.</param>
/// <param name="FriendlyName">Monitor name, preferably from its EDID.</param>
/// <param name="DevicePath">Persistent hardware identifier, which survives reconnects and reboots.</param>
/// <param name="Bounds">Bounds in physical pixels, in virtual screen coordinates.</param>
/// <param name="IsPrimary">Whether this is the primary display.</param>
public record DisplayMonitor(
    nint Handle,
    string GdiDeviceName,
    string FriendlyName,
    string DevicePath,
    Rect Bounds,
    bool IsPrimary
);

public static class DisplayMonitors
{
    private const uint QdcOnlyActivePaths = 0x2;
    private const uint DisplayConfigDeviceInfoGetSourceName = 1;
    private const uint DisplayConfigDeviceInfoGetTargetName = 2;
    private const uint EddGetDeviceInterfaceName = 0x1;
    private const uint MonitorInfoPrimary = 0x1;
    private const int ErrorInsufficientBuffer = 122;

    // Maps GDI device names (\\.\DISPLAYn) to the EDID name and device path of the attached monitor
    private static Dictionary<
        string,
        (string FriendlyName, string DevicePath)
    > QueryDisplayTargets()
    {
        var result = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);

        DisplayConfigPathInfo[] paths;
        DisplayConfigModeInfo[] modes;
        uint pathCount,
            modeCount;
        int error;

        // The configuration may change between the two calls, in which case the buffers are too small
        var attempts = 0;
        do
        {
            error = NativeMethods.GetDisplayConfigBufferSizes(
                QdcOnlyActivePaths,
                out pathCount,
                out modeCount
            );

            if (error != 0)
            {
                Debug.WriteLine($"Failed to get display config buffer sizes. Error {error}.");
                return result;
            }

            paths = new DisplayConfigPathInfo[pathCount];
            modes = new DisplayConfigModeInfo[modeCount];

            error = NativeMethods.QueryDisplayConfig(
                QdcOnlyActivePaths,
                ref pathCount,
                paths,
                ref modeCount,
                modes,
                0
            );
        } while (error == ErrorInsufficientBuffer && ++attempts < 3);

        if (error != 0)
        {
            Debug.WriteLine($"Failed to query display config. Error {error}.");
            return result;
        }

        foreach (var path in paths.Take((int)pathCount))
        {
            var source = new DisplayConfigSourceDeviceName
            {
                Header = new DisplayConfigDeviceInfoHeader
                {
                    Type = DisplayConfigDeviceInfoGetSourceName,
                    Size = (uint)Marshal.SizeOf<DisplayConfigSourceDeviceName>(),
                    AdapterId = path.SourceInfo.AdapterId,
                    Id = path.SourceInfo.Id,
                },
                ViewGdiDeviceName = "",
            };

            if (NativeMethods.DisplayConfigGetDeviceInfo(ref source) != 0)
                continue;

            var target = new DisplayConfigTargetDeviceName
            {
                Header = new DisplayConfigDeviceInfoHeader
                {
                    Type = DisplayConfigDeviceInfoGetTargetName,
                    Size = (uint)Marshal.SizeOf<DisplayConfigTargetDeviceName>(),
                    AdapterId = path.TargetInfo.AdapterId,
                    Id = path.TargetInfo.Id,
                },
                MonitorFriendlyDeviceName = "",
                MonitorDevicePath = "",
            };

            if (NativeMethods.DisplayConfigGetDeviceInfo(ref target) != 0)
                continue;

            // In duplicate (clone) mode, several targets share a source; the first one wins
            result.TryAdd(
                source.ViewGdiDeviceName,
                (target.MonitorFriendlyDeviceName, target.MonitorDevicePath)
            );
        }

        return result;
    }

    // Fallback for when the display configuration API doesn't provide a name (e.g. no EDID)
    private static (string? Name, string? DevicePath) TryGetFromDisplayDevices(string gdiDeviceName)
    {
        var device = new DisplayDevice { Size = Marshal.SizeOf<DisplayDevice>() };

        // Index 0 is the first monitor attached to this display adapter output
        if (
            !NativeMethods.EnumDisplayDevices(
                gdiDeviceName,
                0,
                ref device,
                EddGetDeviceInterfaceName
            )
        )
            return (null, null);

        return (
            string.IsNullOrWhiteSpace(device.DeviceString) ? null : device.DeviceString,
            string.IsNullOrWhiteSpace(device.DeviceId) ? null : device.DeviceId
        );
    }

    /// <summary>
    /// Gets the bounds, the work area (excluding the taskbar and docked toolbars), and the DPI scale
    /// factor of the display that contains the given point (or the nearest one).
    /// All coordinates are in physical screen pixels.
    /// </summary>
    public static (Rect Bounds, Rect WorkArea, double Scale) GetWorkAreaAt(int x, int y)
    {
        const uint monitorDefaultToNearest = 2;

        var monitorHandle = NativeMethods.MonitorFromPoint(
            new NativePoint(x, y),
            monitorDefaultToNearest
        );

        var monitorInfo = new MonitorInfoEx();
        if (monitorHandle == 0 || !NativeMethods.GetMonitorInfo(monitorHandle, ref monitorInfo))
            return (Rect.Empty, Rect.Empty, 1);

        return (monitorInfo.Monitor, monitorInfo.WorkArea, TryGetScale(monitorHandle) ?? 1);
    }

    // Effective DPI scale of a display (e.g. 1.5 at 150%)
    private static double? TryGetScale(nint monitorHandle)
    {
        const int mdtEffectiveDpi = 0;

        try
        {
            return
                NativeMethods.GetDpiForMonitor(monitorHandle, mdtEffectiveDpi, out var dpiX, out _)
                    == 0
                && dpiX > 0
                ? dpiX / 96.0
                : null;
        }
        // Per-monitor DPI isn't available on older and stripped-down editions of Windows
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all connected displays that are part of the desktop.
    /// </summary>
    public static IReadOnlyList<DisplayMonitor> GetAll()
    {
        var targets = QueryDisplayTargets();
        var result = new List<DisplayMonitor>();

        foreach (var monitor in Monitor.GetAll())
        {
            var monitorInfo = new MonitorInfoEx();
            if (!NativeMethods.GetMonitorInfo(monitor.Handle, ref monitorInfo))
            {
                Debug.WriteLine(
                    $"Failed to retrieve info for monitor #{monitor.Handle}. "
                        + $"Error {Marshal.GetLastWin32Error()}."
                );

                continue;
            }

            var gdiDeviceName = monitorInfo.DeviceName;
            if (string.IsNullOrWhiteSpace(gdiDeviceName))
                continue;

            targets.TryGetValue(gdiDeviceName, out var target);

            var friendlyName = target.FriendlyName;
            var devicePath = target.DevicePath;

            if (string.IsNullOrWhiteSpace(friendlyName) || string.IsNullOrWhiteSpace(devicePath))
            {
                var (fallbackName, fallbackDevicePath) = TryGetFromDisplayDevices(gdiDeviceName);
                if (string.IsNullOrWhiteSpace(friendlyName))
                    friendlyName = fallbackName;
                if (string.IsNullOrWhiteSpace(devicePath))
                    devicePath = fallbackDevicePath;
            }

            result.Add(
                new DisplayMonitor(
                    monitor.Handle,
                    gdiDeviceName,
                    string.IsNullOrWhiteSpace(friendlyName) ? gdiDeviceName : friendlyName,
                    // Not persistent, but better than nothing
                    string.IsNullOrWhiteSpace(devicePath)
                        ? gdiDeviceName
                        : devicePath,
                    monitorInfo.Monitor,
                    (monitorInfo.Flags & MonitorInfoPrimary) != 0
                )
            );
        }

        return result;
    }
}
