using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightBulb.Core;
using LightBulb.Localization;
using LightBulb.Models;
using LightBulb.Services;
using PowerKit;
using PowerKit.Extensions;

namespace LightBulb.ViewModels.Components.Settings;

public partial class RulesSettingsTabViewModel : SettingsTabViewModelBase
{
    private readonly ExternalApplicationService _externalApplicationService;
    private readonly MonitorService _monitorService;
    private readonly DashboardViewModel _dashboard;
    private readonly IDisposable _eventSubscription;

    public RulesSettingsTabViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        ExternalApplicationService externalApplicationService,
        MonitorService monitorService,
        DashboardViewModel dashboard
    )
        : base(settingsService, localizationManager, 2)
    {
        _externalApplicationService = externalApplicationService;
        _monitorService = monitorService;
        _dashboard = dashboard;

        RebuildMonitors();
        RebuildDisconnectedMonitors();

        _monitorService.MonitorsChanged += MonitorService_OnMonitorsChanged;

        _eventSubscription = Disposable.Merge(
            this.WatchProperty(
                o => o.IsApplicationWhitelistEnabled,
                _ => RefreshApplicationsCommand.NotifyCanExecuteChanged()
            ),
            this.WatchProperty(
                o => o.IsWhitelistedApplicationPauseEnabled,
                _ => RefreshApplicationsCommand.NotifyCanExecuteChanged()
            ),
            // Display items are kept (not re-created) when settings change, so that
            // sliders that are being dragged don't get replaced; they're refreshed instead
            settingsService.WatchAllProperties(() =>
            {
                foreach (var monitor in Monitors)
                    monitor.Refresh();

                RebuildDisconnectedMonitors();
            }),
            dashboard.WatchProperty(o => o.MonitorConfigurations, UpdateEffectiveConfigurations),
            Disposable.Create(() =>
                _monitorService.MonitorsChanged -= MonitorService_OnMonitorsChanged
            )
        );
    }

    public override string DisplayName => LocalizationManager.RulesTabName;

    // ---- Smart auto-pause ----

    public bool IsPauseWhenFullScreenEnabled
    {
        get => SettingsService.IsPauseWhenFullScreenEnabled;
        set => SettingsService.IsPauseWhenFullScreenEnabled = value;
    }

    public bool IsPerMonitorFullscreenPauseEnabled
    {
        get => SettingsService.IsPerMonitorFullscreenPauseEnabled;
        set => SettingsService.IsPerMonitorFullscreenPauseEnabled = value;
    }

    public bool IsStreamingAutoPauseEnabled
    {
        get => SettingsService.IsStreamingAutoPauseEnabled;
        set => SettingsService.IsStreamingAutoPauseEnabled = value;
    }

    private string? _rejectedStreamingPorts;

    // Comma-separated local TCP ports, stored in the canonical form (invalid entries are dropped)
    public string StreamingPorts
    {
        get => _rejectedStreamingPorts ?? SettingsService.StreamingPorts;
        set
        {
            SettingsService.StreamingPorts = StreamingPortList.Normalize(value);

            if (string.Equals(SettingsService.StreamingPorts, value, StringComparison.Ordinal))
                return;

            // The input was normalized. If the stored value didn't change as a result, bindings
            // wouldn't refresh and the input would keep showing what was typed. So briefly report
            // the typed value and then the actual one, which makes the input show the normalized value.
            _rejectedStreamingPorts = value;
            OnPropertyChanged();

            Dispatcher.UIThread.Post(() =>
            {
                _rejectedStreamingPorts = null;
                OnPropertyChanged(nameof(StreamingPorts));
            });
        }
    }

    public bool IsPauseWhenScreenLockedEnabled
    {
        get => SettingsService.IsPauseWhenScreenLockedEnabled;
        set => SettingsService.IsPauseWhenScreenLockedEnabled = value;
    }

    public bool IsDefaultToDayConfigurationEnabled
    {
        get => SettingsService.IsDefaultToDayConfigurationEnabled;
        set => SettingsService.IsDefaultToDayConfigurationEnabled = value;
    }

    // ---- Displays ----

    [ObservableProperty]
    public partial IReadOnlyList<MonitorItemViewModel> Monitors { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDisconnectedMonitors))]
    [NotifyPropertyChangedFor(nameof(DisconnectedMonitorsHeader))]
    public partial IReadOnlyList<DisconnectedMonitorItemViewModel> DisconnectedMonitors { get; set; } =
    [];

    public bool HasMonitors => Monitors.Count > 0;

    // Index into the offset scaling selector: Mireds (perceptual) first, then Kelvin (fixed)
    public int MonitorOffsetScalingIndex
    {
        get => SettingsService.IsPerceptualMonitorOffsetEnabled ? 0 : 1;
        set
        {
            if (value is not (0 or 1))
                return;

            SettingsService.IsPerceptualMonitorOffsetEnabled = value == 0;
        }
    }

    public string MonitorOffsetScalingDescription =>
        SettingsService.IsPerceptualMonitorOffsetEnabled
            ? LocalizationManager.PerceptualOffsetScalingDescription
            : LocalizationManager.FixedOffsetScalingDescription;

    public bool HasDisconnectedMonitors => DisconnectedMonitors.Count > 0;

    public string DisconnectedMonitorsHeader =>
        string.Format(LocalizationManager.DisconnectedDisplaysLabel, DisconnectedMonitors.Count);

    private void MonitorService_OnMonitorsChanged(object? sender, EventArgs args)
    {
        RebuildMonitors();
        RebuildDisconnectedMonitors();
    }

    private void RebuildMonitors()
    {
        var monitors = _monitorService.Monitors;

        // Displays that share the same name get a number, e.g. "Display (1)" and "Display (2)"
        var duplicateNames = monitors
            .GroupBy(m => m.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var duplicateCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Monitors = monitors
            .Select(
                (monitor, i) =>
                {
                    var name = monitor.FriendlyName;
                    if (duplicateNames.Contains(name))
                    {
                        var index = duplicateCounters.GetValueOrDefault(name) + 1;
                        duplicateCounters[name] = index;
                        name = $"{name} ({index})";
                    }

                    var item = new MonitorItemViewModel(
                        SettingsService,
                        LocalizationManager,
                        monitor,
                        i + 1,
                        name
                    );

                    item.UpdateEffectiveConfiguration(
                        _dashboard.TryGetMonitorConfiguration(monitor.GdiDeviceName)
                    );

                    return item;
                }
            )
            .ToArray();

        OnPropertyChanged(nameof(HasMonitors));
    }

    private void RebuildDisconnectedMonitors()
    {
        var connectedPaths = _monitorService
            .Monitors.Select(m => m.DevicePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var disconnectedProfiles = (SettingsService.MonitorProfiles ?? [])
            .Where(p => !connectedPaths.Contains(p.DevicePath))
            .ToArray();

        // Avoid re-creating the list (and collapsing its expander) when nothing has changed
        if (
            disconnectedProfiles
                .Select(p => p.DevicePath)
                .SequenceEqual(DisconnectedMonitors.Select(m => m.DevicePath))
        )
        {
            return;
        }

        DisconnectedMonitors = disconnectedProfiles
            .Select(p => new DisconnectedMonitorItemViewModel(SettingsService, p))
            .ToArray();
    }

    private void UpdateEffectiveConfigurations(
        IReadOnlyDictionary<string, LightBulb.Core.ColorConfiguration> configurations
    )
    {
        foreach (var monitor in Monitors)
        {
            monitor.UpdateEffectiveConfiguration(
                configurations.TryGetValue(monitor.Monitor.GdiDeviceName, out var configuration)
                    ? configuration
                    : null
            );
        }
    }

    // ---- Application whitelist ----

    public bool IsApplicationWhitelistEnabled
    {
        get => SettingsService.IsApplicationWhitelistEnabled;
        set => SettingsService.IsApplicationWhitelistEnabled = value;
    }

    [ObservableProperty]
    public partial IReadOnlyList<ExternalApplication>? Applications { get; set; }

    public IReadOnlyList<ExternalApplication>? WhitelistedApplications
    {
        get => SettingsService.WhitelistedApplications;
        set => SettingsService.WhitelistedApplications = value;
    }

    public bool IsWhitelistedApplicationPauseEnabled
    {
        get => SettingsService.IsWhitelistedApplicationPauseEnabled;
        set => SettingsService.IsWhitelistedApplicationPauseEnabled = value;
    }

    public bool IsWhitelistedWindowTitlePauseEnabled
    {
        get => SettingsService.IsWhitelistedWindowTitlePauseEnabled;
        set => SettingsService.IsWhitelistedWindowTitlePauseEnabled = value;
    }

    public bool IsPerMonitorWhitelistPauseEnabled
    {
        get => SettingsService.IsPerMonitorWhitelistPauseEnabled;
        set => SettingsService.IsPerMonitorWhitelistPauseEnabled = value;
    }

    public IReadOnlyList<string> WhitelistedWindowTitles =>
        SettingsService.WhitelistedWindowTitles ?? [];

    // Keyword being typed, before it's added to the list
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddWindowTitleCommand))]
    public partial string? NewWindowTitle { get; set; }

    private bool CanAddWindowTitle() => !string.IsNullOrWhiteSpace(NewWindowTitle);

    [RelayCommand(CanExecute = nameof(CanAddWindowTitle))]
    private void AddWindowTitle()
    {
        SettingsService.WhitelistedWindowTitles = WindowTitleKeywords.Normalize([
            .. WhitelistedWindowTitles,
            NewWindowTitle,
        ]);

        NewWindowTitle = "";
    }

    [RelayCommand]
    private void RemoveWindowTitle(string keyword) =>
        SettingsService.WhitelistedWindowTitles = [
            .. WhitelistedWindowTitles.Where(k =>
                !string.Equals(k, keyword, StringComparison.OrdinalIgnoreCase)
            ),
        ];

    private bool CanRefreshApplications() =>
        IsApplicationWhitelistEnabled && IsWhitelistedApplicationPauseEnabled;

    [RelayCommand(CanExecute = nameof(CanRefreshApplications))]
    private void RefreshApplications()
    {
        var applications = new HashSet<ExternalApplication>();

        // Add previously whitelisted applications
        // (this has to be done first to preserve references in selected applications)
        foreach (var application in WhitelistedApplications ?? [])
            applications.Add(application);

        // Add all running applications
        foreach (var application in _externalApplicationService.GetAllRunningApplications())
            applications.Add(application);

        Applications = applications.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _eventSubscription.Dispose();

        base.Dispose(disposing);
    }
}
