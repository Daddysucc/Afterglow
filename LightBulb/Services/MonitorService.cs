using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Threading;
using LightBulb.PlatformInterop;

namespace LightBulb.Services;

/// <summary>
/// Keeps track of the connected displays. The list is only re-queried when the display
/// configuration changes (e.g. a display is plugged in, unplugged, or rearranged).
/// </summary>
public class MonitorService : IDisposable
{
    private readonly IDisposable _eventSubscription;
    private IReadOnlyList<DisplayMonitor>? _monitors;

    public MonitorService() =>
        // WM_DISPLAYCHANGE, which is also what SystemEvents.DisplaySettingsChanged is based on
        _eventSubscription = SystemEvent.Register(
            SystemEvent.Ids.DisplayChanged,
            () => Dispatcher.UIThread.Post(Refresh)
        );

    // Queried lazily, so that it happens after the UI framework has made the process DPI-aware
    // (otherwise the display bounds would be scaled instead of in physical pixels)
    public IReadOnlyList<DisplayMonitor> Monitors => _monitors ??= Query();

    /// <summary>
    /// Occurs on the UI thread when the list of displays has been refreshed.
    /// </summary>
    public event EventHandler? MonitorsChanged;

    private static IReadOnlyList<DisplayMonitor> Query()
    {
        try
        {
            // Order the displays from left to right (and top to bottom), as they're arranged
            return DisplayMonitors
                .GetAll()
                .OrderBy(m => m.Bounds.Left)
                .ThenBy(m => m.Bounds.Top)
                .ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to query displays: {ex}");
            return [];
        }
    }

    public void Refresh()
    {
        _monitors = Query();
        MonitorsChanged?.Invoke(this, EventArgs.Empty);
    }

    public DisplayMonitor? TryGetByGdiDeviceName(string gdiDeviceName) =>
        Monitors.FirstOrDefault(m =>
            string.Equals(m.GdiDeviceName, gdiDeviceName, StringComparison.OrdinalIgnoreCase)
        );

    public void Dispose() => _eventSubscription.Dispose();
}
