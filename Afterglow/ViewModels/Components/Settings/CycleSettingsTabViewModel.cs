using System;
using System.Collections.Generic;
using System.Linq;
using Afterglow.Core;
using Afterglow.Localization;
using Afterglow.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using PowerKit;
using PowerKit.Extensions;

namespace Afterglow.ViewModels.Components.Settings;

// Day, evening, and night values are kept in order (day >= evening >= night) by the settings
// service, so changing one of them may push the others along with it.
public partial class CycleSettingsTabViewModel : SettingsTabViewModelBase
{
    private readonly MonitorService? _monitorService;
    private readonly DashboardViewModel? _dashboard;
    private readonly IDisposable _eventSubscription;

    // (the displays are only listed with the services that know them, e.g. not in tests)
    public CycleSettingsTabViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        MonitorService? monitorService = null,
        DashboardViewModel? dashboard = null
    )
        : base(settingsService, localizationManager, 0)
    {
        _monitorService = monitorService;
        _dashboard = dashboard;

        RebuildMonitors();
        RebuildDisconnectedMonitors();

        if (_monitorService is not null)
            _monitorService.MonitorsChanged += MonitorService_OnMonitorsChanged;

        _eventSubscription = Disposable.Merge(
            // Display items are kept (not re-created) when settings change, so that
            // sliders that are being dragged don't get replaced; they're refreshed instead
            settingsService.WatchAllProperties(() =>
            {
                foreach (var monitor in Monitors)
                    monitor.Refresh();

                RebuildDisconnectedMonitors();
            }),
            dashboard?.WatchProperty(o => o.MonitorConfigurations, UpdateEffectiveConfigurations)
                ?? Disposable.Null,
            Disposable.Create(() =>
            {
                if (_monitorService is not null)
                    _monitorService.MonitorsChanged -= MonitorService_OnMonitorsChanged;
            })
        );
    }

    public override string DisplayName => LocalizationManager.CycleTabName;

    protected override void ResetTabSettings(SettingsService defaults)
    {
        SettingsService.DayConfiguration = defaults.DayConfiguration;
        SettingsService.EveningConfiguration = defaults.EveningConfiguration;
        SettingsService.NightConfiguration = defaults.NightConfiguration;
        SettingsService.IsEveningConfigurationEnabled = defaults.IsEveningConfigurationEnabled;
        SettingsService.NightStartTime = defaults.NightStartTime;
        SettingsService.ConfigurationTransitionDuration = defaults.ConfigurationTransitionDuration;
        SettingsService.ConfigurationTransitionOffset = defaults.ConfigurationTransitionOffset;
        SettingsService.GrayscaleSchedule = defaults.GrayscaleSchedule;
        SettingsService.GrayscaleOverride = defaults.GrayscaleOverride;
        SettingsService.IsGrayscaleTemperatureShiftEnabled =
            defaults.IsGrayscaleTemperatureShiftEnabled;
        SettingsService.IsDefaultToDayConfigurationEnabled =
            defaults.IsDefaultToDayConfigurationEnabled;
        SettingsService.IsConfigurationSmoothingEnabled = defaults.IsConfigurationSmoothingEnabled;
        SettingsService.IsPerceptualMonitorOffsetEnabled =
            defaults.IsPerceptualMonitorOffsetEnabled;
        SettingsService.MonitorProfiles = defaults.MonitorProfiles;
    }

    // What the screen shows while Afterglow is off or paused: the day's colors, or none
    public bool IsDefaultToDayConfigurationEnabled
    {
        get => SettingsService.IsDefaultToDayConfigurationEnabled;
        set => SettingsService.IsDefaultToDayConfigurationEnabled = value;
    }

    // Whether turning Afterglow on and off fades slowly
    public bool IsConfigurationSmoothingEnabled
    {
        get => SettingsService.IsConfigurationSmoothingEnabled;
        set => SettingsService.IsConfigurationSmoothingEnabled = value;
    }

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumDayTemperature => Math.Max(6600, DayTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumDayTemperature => Math.Min(2500, DayTemperature);

    public double DayTemperature
    {
        get => SettingsService.DayConfiguration.Temperature;
        set => SettingsService.SetStageTemperature(CycleStage.Day, value);
    }

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumEveningTemperature => Math.Max(6600, EveningTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumEveningTemperature => Math.Min(2500, EveningTemperature);

    public double EveningTemperature
    {
        get => SettingsService.EveningConfiguration.Temperature;
        set => SettingsService.SetStageTemperature(CycleStage.Evening, value);
    }

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumNightTemperature => Math.Max(6600, NightTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumNightTemperature => Math.Min(2500, NightTemperature);

    public double NightTemperature
    {
        get => SettingsService.NightConfiguration.Temperature;
        set => SettingsService.SetStageTemperature(CycleStage.Night, value);
    }

    public double DayBrightness
    {
        get => SettingsService.DayConfiguration.Brightness;
        set => SettingsService.SetStageBrightness(CycleStage.Day, value);
    }

    public double EveningBrightness
    {
        get => SettingsService.EveningConfiguration.Brightness;
        set => SettingsService.SetStageBrightness(CycleStage.Evening, value);
    }

    public double NightBrightness
    {
        get => SettingsService.NightConfiguration.Brightness;
        set => SettingsService.SetStageBrightness(CycleStage.Night, value);
    }

    private TimeSpan? _rejectedTransitionDuration;

    // Limited to 3 hours by the settings service
    public TimeSpan ConfigurationTransitionDuration
    {
        get => _rejectedTransitionDuration ?? SettingsService.ConfigurationTransitionDuration;
        set
        {
            SettingsService.ConfigurationTransitionDuration = value;

            if (SettingsService.ConfigurationTransitionDuration == value)
                return;

            // The value was out of range and got clamped. If the stored value didn't change as
            // a result (e.g. it was already at the maximum), bindings wouldn't refresh and the
            // input would keep showing what was typed. So briefly report the typed value and then
            // the actual one, which makes the input snap back to it.
            _rejectedTransitionDuration = value;
            OnPropertyChanged();

            Dispatcher.UIThread.Post(() =>
            {
                _rejectedTransitionDuration = null;
                OnPropertyChanged(nameof(ConfigurationTransitionDuration));
            });
        }
    }

    public double ConfigurationTransitionOffset
    {
        get => SettingsService.ConfigurationTransitionOffset;
        set => SettingsService.ConfigurationTransitionOffset = value.Clamp(0, 1);
    }

    public bool IsEveningConfigurationEnabled
    {
        get => SettingsService.IsEveningConfigurationEnabled;
        set => SettingsService.IsEveningConfigurationEnabled = value;
    }

    public TimeOnly NightStartTime
    {
        get => SettingsService.NightStartTime;
        set => SettingsService.NightStartTime = value;
    }

    // Index into the grayscale schedule selector, in the order of the schedule's values
    public int GrayscaleScheduleIndex
    {
        get => (int)SettingsService.GrayscaleSchedule;
        set
        {
            if (!Enum.IsDefined((GrayscaleSchedule)value))
                return;

            // A new schedule applies right away, replacing any earlier manual choice
            if ((GrayscaleSchedule)value != SettingsService.GrayscaleSchedule)
                SettingsService.GrayscaleOverride = null;

            SettingsService.GrayscaleSchedule = (GrayscaleSchedule)value;
        }
    }

    public bool IsGrayscaleTemperatureShiftEnabled
    {
        get => SettingsService.IsGrayscaleTemperatureShiftEnabled;
        set => SettingsService.IsGrayscaleTemperatureShiftEnabled = value;
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
        var monitors = _monitorService?.Monitors ?? [];

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
                        _dashboard?.TryGetMonitorConfiguration(monitor.GdiDeviceName)
                    );

                    return item;
                }
            )
            .ToArray();

        OnPropertyChanged(nameof(HasMonitors));
    }

    private void RebuildDisconnectedMonitors()
    {
        var connectedPaths = (_monitorService?.Monitors ?? [])
            .Select(m => m.DevicePath)
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
        IReadOnlyDictionary<string, Afterglow.Core.ColorConfiguration> configurations
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _eventSubscription.Dispose();

        base.Dispose(disposing);
    }
}
