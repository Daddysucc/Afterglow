using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Threading;
using LightBulb.PlatformInterop;
using PowerKit;
using PowerKit.Extensions;

namespace LightBulb.Services;

/// <summary>
/// Keeps track of the connected displays. The list is only re-queried when the display
/// configuration changes (e.g. a display is plugged in, unplugged, or rearranged).
/// </summary>
public class MonitorService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly IDisposable _eventSubscription;
    private IReadOnlyList<DisplayMonitor>? _monitors;

    public MonitorService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        _eventSubscription = Disposable.Merge(
            // WM_DISPLAYCHANGE, which is also what SystemEvents.DisplaySettingsChanged is based on
            SystemEvent.Register(
                SystemEvent.Ids.DisplayChanged,
                () => Dispatcher.UIThread.Post(Refresh)
            ),
            // Saved profiles may be (re)loaded after the displays have been queried
            settingsService.WatchProperty(
                o => o.MonitorProfiles,
                _ =>
                {
                    if (_monitors is not null)
                        ReconcileProfiles(_monitors);
                }
            )
        );
    }

    // Queried lazily, so that it happens after the UI framework has made the process DPI-aware
    // (otherwise the display bounds would be scaled instead of in physical pixels)
    public IReadOnlyList<DisplayMonitor> Monitors
    {
        get
        {
            if (_monitors is null)
            {
                _monitors = Query();
                ReconcileProfiles(_monitors);
            }

            return _monitors;
        }
    }

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

    // Device paths of displays can change (e.g. after a graphics driver update),
    // so make sure that saved profiles follow the displays they belong to
    private void ReconcileProfiles(IReadOnlyList<DisplayMonitor> monitors)
    {
        if (monitors.Count == 0)
            return;

        _settingsService.ReconcileMonitorProfiles(monitors.Select(m => m.DevicePath).ToArray());
    }

    public void Refresh()
    {
        _monitors = Query();
        ReconcileProfiles(_monitors);

        MonitorsChanged?.Invoke(this, EventArgs.Empty);
    }

    // Called for every display on every update tick, so it avoids allocations
    public DisplayMonitor? TryGetByGdiDeviceName(string gdiDeviceName)
    {
        foreach (var monitor in Monitors)
        {
            if (
                string.Equals(
                    monitor.GdiDeviceName,
                    gdiDeviceName,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return monitor;
            }
        }

        return null;
    }

    public void Dispose() => _eventSubscription.Dispose();
}
