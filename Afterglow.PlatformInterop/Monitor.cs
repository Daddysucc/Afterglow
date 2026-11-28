using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

// Monitor handles don't need to be released, so there's nothing to dispose (and no finalizer)
public partial class Monitor(nint handle) : IDisposable
{
    private MonitorInfoEx? TryGetMonitorInfo()
    {
        var monitorInfo = new MonitorInfoEx();

        if (!NativeMethods.GetMonitorInfo(Handle, ref monitorInfo))
        {
            Debug.WriteLine(
                $"Failed to retrieve info for monitor #{Handle}. "
                    + $"Error {Marshal.GetLastWin32Error()}."
            );

            return null;
        }

        return monitorInfo;
    }

    public Rect? TryGetBounds() => TryGetMonitorInfo()?.Monitor;

    // The display without the taskbar (and anything else docked to its edges)
    public Rect? TryGetWorkArea() => TryGetMonitorInfo()?.WorkArea;

    public string? TryGetDeviceName() => TryGetMonitorInfo()?.DeviceName;

    public DeviceContext? TryCreateDeviceContext()
    {
        var name = TryGetDeviceName();
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return DeviceContext.TryCreate(name);
    }

    public nint Handle { get; } = handle;

    public void Dispose() { }
}

public partial class Monitor
{
    public static IReadOnlyList<Monitor> GetAll()
    {
        var monitors = new List<Monitor>();

        if (
            !NativeMethods.EnumDisplayMonitors(
                0,
                0,
                (hMonitor, _, _, _) =>
                {
                    monitors.Add(new Monitor(hMonitor));
                    return true;
                },
                0
            )
        )
        {
            Debug.WriteLine(
                "Failed to enumerate display monitors. " + $"Error {Marshal.GetLastWin32Error()}."
            );
        }

        return monitors;
    }
}
