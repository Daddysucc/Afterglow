using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightBulb.Core;
using LightBulb.Core.Utils.Extensions;
using LightBulb.Framework;
using LightBulb.Localization;
using LightBulb.Models;
using LightBulb.PlatformInterop;
using LightBulb.Services;
using LightBulb.Utils;
using LightBulb.Utils.Extensions;
using PowerKit;
using PowerKit.Extensions;

namespace LightBulb.ViewModels.Components;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly GammaService _gammaService;
    private readonly HotKeyService _hotKeyService;
    private readonly ExternalApplicationService _externalApplicationService;
    private readonly MonitorService _monitorService;

    private readonly IDisposable _eventSubscription;

    private readonly Timer _updateInstantTimer;
    private readonly Timer _updateConfigurationTimer;
    private readonly Timer _updateIsPausedTimer;

    private IDisposable? _enableAfterDelayRegistration;
    private IDisposable? _pendingSaveRegistration;
    private readonly PhrasePicker<CycleStage> _statusPhrasePicker = new();
    private readonly Dictionary<
        CycleStage,
        (string Source, IReadOnlyList<string> Phrases)
    > _stagePhrasesCache = new();

    // Smoothing of the overall configuration, and of each display (by GDI device name)
    private readonly ConfigurationSmoother _globalSmoother = new(ColorConfiguration.Default);
    private readonly Dictionary<string, ConfigurationSmoother> _monitorSmoothers = new(
        StringComparer.OrdinalIgnoreCase
    );

    // Last known states, kept while an unidentifiable application (e.g. the taskbar) is in focus
    private string? _fullScreenMonitor;
    private bool _isWhitelistedApplicationActive;

    private bool _isDashboardVisible = true;
    private long _lastHiddenStatusMinute = long.MinValue;

    private CycleCurve? _cycleCurve;
    private CycleCurveKey? _cycleCurveKey;

    public DashboardViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        GammaService gammaService,
        HotKeyService hotKeyService,
        ExternalApplicationService externalApplicationService,
        MonitorService monitorService
    )
    {
        _settingsService = settingsService;
        LocalizationManager = localizationManager;
        _gammaService = gammaService;
        _hotKeyService = hotKeyService;
        _externalApplicationService = externalApplicationService;
        _monitorService = monitorService;

        _eventSubscription = Disposable.Merge(
            // Invalidate the cached local time zone when the system clock or time zone changes
            // https://github.com/Tyrrrz/LightBulb/issues/465
            SystemEvent.Register(SystemEvent.Ids.TimeChanged, TimeZoneInfo.ClearCachedData),
            SystemEvent.Register(SystemEvent.Ids.SettingsChanged, TimeZoneInfo.ClearCachedData),
            this.WatchProperty(
                o => o.IsEnabled,
                v =>
                {
                    if (v)
                    {
                        // Cancel any activate 'disable temporarily' timers
                        _enableAfterDelayRegistration?.Dispose();

                        // Invalidate device contexts
                        _gammaService.InvalidateDeviceContexts();
                    }
                }
            ),
            // Refresh localized text when the language changes
            localizationManager.WatchProperty(
                o => o.Language,
                _ =>
                {
                    OnPropertyChanged(nameof(StatusSentence));
                    OnPropertyChanged(nameof(SolarTimingText));
                    OnPropertyChanged(nameof(ActivePhaseCaption));
                    OnPropertyChanged(nameof(ActiveStageTemperatureTooltip));
                    OnPropertyChanged(nameof(LocationText));
                    OnPropertyChanged(nameof(StatusText));
                }
            ),
            // Re-register hotkeys when they get updated
            settingsService.WatchProperties(
                [
                    o => o.ToggleHotKey,
                    o => o.ToggleWindowHotKey,
                    o => o.IncreaseTemperatureOffsetHotKey,
                    o => o.DecreaseTemperatureOffsetHotKey,
                    o => o.IncreaseBrightnessOffsetHotKey,
                    o => o.DecreaseBrightnessOffsetHotKey,
                    o => o.ResetConfigurationOffsetHotKey,
                ],
                RegisterHotKeys
            )
        );

        _updateConfigurationTimer = new Timer(
            TimeSpan.FromMilliseconds(50),
            () => Dispatcher.UIThread.Post(UpdateConfiguration)
        );
        _updateInstantTimer = new Timer(
            TimeSpan.FromMilliseconds(50),
            () => Dispatcher.UIThread.Post(UpdateInstant)
        );
        _updateIsPausedTimer = new Timer(
            TimeSpan.FromSeconds(1),
            () => Dispatcher.UIThread.Post(UpdateIsPaused)
        );
    }

    public LocalizationManager LocalizationManager { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    [NotifyPropertyChangedFor(nameof(TrayStatusText))]
    [NotifyPropertyChangedFor(nameof(StatusIndicator))]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    [NotifyPropertyChangedFor(nameof(TrayStatusText))]
    [NotifyPropertyChangedFor(nameof(StatusIndicator))]
    public partial bool IsPaused { get; set; }

    // Why all displays are paused automatically, if they are
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    [NotifyPropertyChangedFor(nameof(ActivePhaseCaption))]
    [NotifyPropertyChangedFor(nameof(TrayStatusText))]
    public partial PauseReason PauseReason { get; set; }

    // GDI device name of the display that is paused on its own (running a fullscreen application)
    [ObservableProperty]
    public partial string? PausedMonitor { get; set; }

    // Current configuration of each display, by GDI device name
    [ObservableProperty]
    public partial IReadOnlyDictionary<
        string,
        ColorConfiguration
    > MonitorConfigurations { get; set; } = new Dictionary<string, ColorConfiguration>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    public partial bool IsCyclePreviewEnabled { get; set; }

    public bool IsActive => IsEnabled && !IsPaused || IsCyclePreviewEnabled;

    // Everything that depends on the time of day or on the settings is refreshed on every tick.
    // Dashboard-only visuals are refreshed separately, and only while the dashboard can be seen.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetConfiguration))]
    [NotifyPropertyChangedFor(nameof(TrayStatusText))]
    [NotifyPropertyChangedFor(nameof(StatusIndicator))]
    public partial DateTimeOffset Instant { get; set; } = DateTimeOffset.Now;

    // Properties that are only shown on the dashboard and change with the time of day
    private static readonly PropertyChangedEventArgs[] DashboardVisualPropertyArgs =
    [
        new(nameof(SolarTimes)),
        new(nameof(Schedule)),
        new(nameof(Phase)),
        new(nameof(ActiveStageTemperature)),
        new(nameof(MinimumSliderTemperature)),
        new(nameof(MaximumSliderTemperature)),
        new(nameof(ActiveStageTemperatureTooltip)),
        new(nameof(StatusSentence)),
        new(nameof(SolarTimingText)),
        new(nameof(ActivePhaseCaption)),
        new(nameof(LocationText)),
        new(nameof(CycleCurve)),
        new(nameof(DayProgress)),
    ];

    private static readonly PropertyChangedEventArgs ActivePhaseCaptionArgs = new(
        nameof(ActivePhaseCaption)
    );

    // Set by the main window: false while it's hidden or minimized
    public bool IsDashboardVisible
    {
        get => _isDashboardVisible;
        set
        {
            if (!SetProperty(ref _isDashboardVisible, value))
                return;

            // Catch up on everything that was skipped while the dashboard couldn't be seen
            if (value)
                NotifyDashboardVisualsChanged();
        }
    }

    private void NotifyDashboardVisualsChanged()
    {
        foreach (var args in DashboardVisualPropertyArgs)
            OnPropertyChanged(args);
    }

    partial void OnInstantChanged(DateTimeOffset value)
    {
        if (IsDashboardVisible)
        {
            NotifyDashboardVisualsChanged();
            return;
        }

        // While hidden, still look at the status sentence once a minute, so that its phrase
        // picker sees every stage change and picks phrases the same way as when it's visible
        var minute = value.ToUnixTimeSeconds() / 60;
        if (minute == _lastHiddenStatusMinute)
            return;

        _lastHiddenStatusMinute = minute;
        _ = StatusSentence;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOffsetEnabled))]
    [NotifyPropertyChangedFor(nameof(TargetConfiguration))]
    [NotifyPropertyChangedFor(nameof(CycleCurve))]
    public partial double TemperatureOffset { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOffsetEnabled))]
    [NotifyPropertyChangedFor(nameof(TargetConfiguration))]
    [NotifyPropertyChangedFor(nameof(CycleCurve))]
    public partial double BrightnessOffset { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(TrayStatusText))]
    public partial ColorConfiguration CurrentConfiguration { get; set; } =
        ColorConfiguration.Default;

    // Changes on every tick while smoothing or transitioning, but is only shown on the dashboard
    partial void OnCurrentConfigurationChanged(ColorConfiguration value)
    {
        if (IsDashboardVisible)
            OnPropertyChanged(ActivePhaseCaptionArgs);
    }

    public SolarTimes SolarTimes =>
        _settingsService is { IsManualSunriseSunsetEnabled: false, Location: { } location }
            ? SolarTimes.Calculate(location, Instant)
            : new SolarTimes(_settingsService.ManualSunrise, _settingsService.ManualSunset);

    // Null when the evening stage is disabled, which results in the classic two-stage cycle
    private TimeOnly? EffectiveNightStartTime =>
        _settingsService.IsEveningConfigurationEnabled ? _settingsService.NightStartTime : null;

    // Effective transition times, with any overlaps between transitions already resolved
    public CycleSchedule Schedule =>
        Cycle.GetSchedule(
            SolarTimes,
            _settingsService.ConfigurationTransitionDuration,
            _settingsService.ConfigurationTransitionOffset,
            EffectiveNightStartTime
        );

    public CyclePhase Phase =>
        Cycle.GetPhase(
            SolarTimes,
            _settingsService.ConfigurationTransitionDuration,
            _settingsService.ConfigurationTransitionOffset,
            EffectiveNightStartTime,
            Instant
        );

    // Temperature of the stage that is currently active (or being transitioned into).
    // This is what the dashboard slider adjusts.
    public double ActiveStageTemperature
    {
        get => _settingsService.GetStageConfiguration(Phase.Stage).Temperature;
        set
        {
            _settingsService.SetStageTemperature(Phase.Stage, value);
            QueueSave();

            OnPropertyChanged();
            OnPropertyChanged(nameof(MinimumSliderTemperature));
            OnPropertyChanged(nameof(MaximumSliderTemperature));
            OnPropertyChanged(nameof(TargetConfiguration));
            OnPropertyChanged(nameof(CycleCurve));
        }
    }

    // These values are used for slider bounds, but they're not actual restrictions
    public double MinimumSliderTemperature => Math.Min(1900, ActiveStageTemperature);

    public double MaximumSliderTemperature => Math.Max(6600, ActiveStageTemperature);

    public string ActiveStageTemperatureTooltip =>
        string.Format(LocalizationManager.ActiveStageTemperatureTooltip, GetStageName(Phase.Stage));

    public bool IsOffsetEnabled => Math.Abs(TemperatureOffset) + Math.Abs(BrightnessOffset) >= 0.01;

    private ColorConfiguration GetCycleConfiguration(
        SolarTimes solarTimes,
        DateTimeOffset instant
    ) =>
        (
            _settingsService.IsEveningConfigurationEnabled
                ? Cycle.InterpolateConfiguration(
                    solarTimes,
                    _settingsService.DayConfiguration,
                    _settingsService.EveningConfiguration,
                    _settingsService.NightConfiguration,
                    _settingsService.NightStartTime,
                    _settingsService.ConfigurationTransitionDuration,
                    _settingsService.ConfigurationTransitionOffset,
                    instant
                )
                : Cycle.InterpolateConfiguration(
                    solarTimes,
                    _settingsService.DayConfiguration,
                    _settingsService.NightConfiguration,
                    _settingsService.ConfigurationTransitionDuration,
                    _settingsService.ConfigurationTransitionOffset,
                    instant
                )
        )
            .WithOffset(TemperatureOffset, BrightnessOffset)
            .Clamp(
                _settingsService.MinimumTemperature,
                _settingsService.MaximumTemperature,
                _settingsService.MinimumBrightness,
                _settingsService.MaximumBrightness
            );

    // What displays are set to while paused
    private ColorConfiguration PausedConfiguration =>
        _settingsService.IsDefaultToDayConfigurationEnabled
            ? _settingsService.DayConfiguration
            : ColorConfiguration.Default;

    public ColorConfiguration TargetConfiguration =>
        IsActive ? GetCycleConfiguration(SolarTimes, Instant) : PausedConfiguration;

    // e.g. "Paused (Remote streaming)"
    private string PausedLabel =>
        PauseReason switch
        {
            PauseReason.RemoteStreaming => LocalizationManager.PausedRemoteStreamingLabel,
            PauseReason.ScreenLocked => LocalizationManager.PausedScreenLockedLabel,
            PauseReason.FullScreen => LocalizationManager.PausedFullScreenLabel,
            PauseReason.Whitelist => LocalizationManager.PausedWhitelistLabel,
            _ => LocalizationManager.PausedStateLabel,
        };

    // Gets the current configuration of a display, by its GDI device name
    public ColorConfiguration? TryGetMonitorConfiguration(string gdiDeviceName) =>
        MonitorConfigurations.TryGetValue(gdiDeviceName, out var configuration)
            ? configuration
            : null;

    // Fraction of the day that has passed, rounded down to the minute to avoid redundant redraws
    public double DayProgress => Math.Floor(Instant.TimeOfDay.TotalMinutes) / (24 * 60);

    // Configuration over the whole current day, as plotted on the dashboard graph.
    // Only recalculated when something that affects it changes.
    public CycleCurve CycleCurve
    {
        get
        {
            var dayStart = Instant.ResetTimeOfDay();
            var solarTimes = SolarTimes;

            var key = new CycleCurveKey(
                dayStart,
                solarTimes,
                _settingsService.DayConfiguration,
                _settingsService.EveningConfiguration,
                _settingsService.NightConfiguration,
                EffectiveNightStartTime,
                _settingsService.ConfigurationTransitionDuration,
                _settingsService.ConfigurationTransitionOffset,
                TemperatureOffset,
                BrightnessOffset
            );

            if (_cycleCurve is not null && _cycleCurveKey == key)
                return _cycleCurve;

            // One sample every 2 minutes, including both 00:00 and 24:00
            const int sampleCount = 24 * 30 + 1;

            var samples = new ColorConfiguration[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var instant = dayStart + TimeSpan.FromDays((double)i / (sampleCount - 1));
                samples[i] = GetCycleConfiguration(solarTimes, instant);
            }

            _cycleCurve = new CycleCurve(samples, CycleCurve.GetSegments(Schedule));
            _cycleCurveKey = key;

            return _cycleCurve;
        }
    }

    private string GetStageName(CycleStage stage) =>
        stage switch
        {
            CycleStage.Day => LocalizationManager.DayStateLabel,
            CycleStage.Evening => LocalizationManager.EveningStateLabel,
            _ => LocalizationManager.NightStateLabel,
        };

    private string GetPhaseName(CyclePhase phase) =>
        phase switch
        {
            { IsTransitioning: false } => GetStageName(phase.Stage),
            { Stage: CycleStage.Day } => LocalizationManager.SunriseTransitionPhaseLabel,
            { Stage: CycleStage.Evening } => LocalizationManager.SunsetTransitionPhaseLabel,
            _ => Schedule.IsEveningIncluded
                ? LocalizationManager.NightTransitionPhaseLabel
                : LocalizationManager.SunsetTransitionPhaseLabel,
        };

    // Each phrase pool is a localized string with one phrase per line
    private static IReadOnlyList<string> SplitPhrases(string phrases) =>
        phrases.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

    // Split once per language, rather than on every refresh. The cache is keyed by the source
    // string, so a different language (i.e. a different string) is split again automatically.
    private IReadOnlyList<string> GetStagePhrases(CycleStage stage)
    {
        var source = stage switch
        {
            CycleStage.Day => LocalizationManager.StatusDayPhrases,
            CycleStage.Evening => LocalizationManager.StatusEveningPhrases,
            _ => LocalizationManager.StatusNightPhrases,
        };

        if (
            _stagePhrasesCache.TryGetValue(stage, out var cached)
            && string.Equals(cached.Source, source, StringComparison.Ordinal)
        )
        {
            return cached.Phrases;
        }

        var phrases = SplitPhrases(source);
        _stagePhrasesCache[stage] = (source, phrases);

        return phrases;
    }

    // Plain-language description of what's going on right now.
    // A new random phrase is only picked when the stage changes, not on every refresh.
    public string StatusSentence
    {
        get
        {
            if (IsCyclePreviewEnabled)
                return LocalizationManager.StatusPreviewText;

            if (!IsEnabled)
            {
                return _settingsService.IsDefaultToDayConfigurationEnabled
                    ? LocalizationManager.StatusDisabledDayConfigText
                    : LocalizationManager.StatusDisabledText;
            }

            if (IsPaused)
            {
                return PauseReason == PauseReason.RemoteStreaming
                    ? LocalizationManager.StatusRemoteStreamingPausedText
                    : LocalizationManager.StatusPausedText;
            }

            return Phase switch
            {
                { IsTransitioning: true, Stage: CycleStage.Day } =>
                    LocalizationManager.StatusTransitionToDayText,
                { IsTransitioning: true, Stage: CycleStage.Evening } =>
                    LocalizationManager.StatusTransitionToEveningText,
                { IsTransitioning: true } => LocalizationManager.StatusTransitionToNightText,
                var phase => _statusPhrasePicker.Get(phase.Stage, GetStagePhrases(phase.Stage)),
            };
        }
    }

    private string FormatRelativeTime(string eventName, TimeSpan delta, bool isPast)
    {
        var minutes = (int)Math.Round(delta.Duration().TotalMinutes);
        if (minutes < 1)
            return string.Format(LocalizationManager.SolarEventNowText, eventName);

        var hours = (int)Math.Round(delta.Duration().TotalHours);

        var duration =
            minutes < 60
                ? minutes == 1
                    ? LocalizationManager.DurationMinuteText
                    : string.Format(LocalizationManager.DurationMinutesText, minutes)
                : hours == 1
                    ? LocalizationManager.DurationHourText
                    : string.Format(LocalizationManager.DurationHoursText, hours);

        return string.Format(
            isPast
                ? LocalizationManager.SolarEventPastText
                : LocalizationManager.SolarEventFutureText,
            eventName,
            duration
        );
    }

    // When the most recent solar event happened, and when the next one is coming,
    // e.g. "Sunrise: 4 hours ago, Sunset: in 5 hours"
    public string SolarTimingText
    {
        get
        {
            var instant = Instant;
            var solarTimes = SolarTimes;

            var events = new List<(string Name, TimeOnly Time)>
            {
                (LocalizationManager.SunriseLabel, solarTimes.Sunrise),
                (LocalizationManager.SunsetLabel, solarTimes.Sunset),
            };

            if (_settingsService.IsEveningConfigurationEnabled)
                events.Add((LocalizationManager.BedtimeLabel, _settingsService.NightStartTime));

            var previous = events.MaxBy(e => e.Time.PreviousBefore(instant));
            var next = events.MinBy(e => e.Time.NextAfter(instant));

            return FormatRelativeTime(
                    previous.Name,
                    instant - previous.Time.PreviousBefore(instant),
                    true
                )
                + ", "
                + FormatRelativeTime(next.Name, next.Time.NextAfter(instant) - instant, false);
        }
    }

    // e.g. "Active phase: Evening · 4500 K (95% brightness)"
    public string ActivePhaseCaption =>
        string.Format(
            IsPaused && !IsCyclePreviewEnabled
                ? LocalizationManager.PausedCaption
                : LocalizationManager.ActivePhaseCaption,
            IsPaused && !IsCyclePreviewEnabled ? PausedLabel : GetPhaseName(Phase),
            CurrentConfiguration.Temperature.ToString("F0", CultureInfo.CurrentCulture),
            CurrentConfiguration.Brightness.ToString("P0", CultureInfo.CurrentCulture)
        );

    // e.g. "Evening · 5000 K · 100%", as shown at the top of the tray menu
    public string TrayStatusText =>
        (
            !IsEnabled ? LocalizationManager.PausedStateLabel
            : IsPaused ? PausedLabel
            : GetPhaseName(Phase)
        )
        + " · "
        + CurrentConfiguration.Temperature.ToString("F0", CultureInfo.CurrentCulture)
        + " K · "
        + CurrentConfiguration.Brightness.ToString("P0", CultureInfo.CurrentCulture);

    public StatusIndicatorKind StatusIndicator =>
        !IsEnabled || IsPaused
            ? StatusIndicatorKind.Paused
            : Phase switch
            {
                { IsTransitioning: true } => StatusIndicatorKind.Transition,
                { Stage: CycleStage.Day } => StatusIndicatorKind.Day,
                { Stage: CycleStage.Evening } => StatusIndicatorKind.Evening,
                _ => StatusIndicatorKind.Night,
            };

    public string LocationText
    {
        get
        {
            if (_settingsService.IsManualSunriseSunsetEnabled)
                return LocalizationManager.ManualScheduleText;

            if (_settingsService.Location is not { } location)
                return LocalizationManager.LocationNotSetText;

            var latitude = string.Create(
                CultureInfo.InvariantCulture,
                $"{Math.Abs(location.Latitude):F1}°{(location.Latitude >= 0 ? "N" : "S")}"
            );

            var longitude = string.Create(
                CultureInfo.InvariantCulture,
                $"{Math.Abs(location.Longitude):F1}°{(location.Longitude >= 0 ? "E" : "W")}"
            );

            return string.Format(
                LocalizationManager.LocationCoordinatesText,
                latitude + ", " + longitude
            );
        }
    }

    public string StatusText =>
        Program.Name
        + Environment.NewLine
        + (
            IsActive
                ? CurrentConfiguration.Temperature.ToString("F0")
                    + " / "
                    + CurrentConfiguration.Brightness.ToString("P0")
                : LocalizationManager.TrayTooltipDisabled
        );

    // Settings changed from the dashboard are saved shortly after the last change,
    // so that dragging the slider doesn't write the settings file on every step
    private void QueueSave()
    {
        _pendingSaveRegistration?.Dispose();
        _pendingSaveRegistration = Timer.QueueDelayedAction(
            TimeSpan.FromSeconds(1),
            () => Dispatcher.UIThread.Post(FlushPendingSave)
        );
    }

    public void FlushPendingSave()
    {
        if (_pendingSaveRegistration is null)
            return;

        _pendingSaveRegistration.Dispose();
        _pendingSaveRegistration = null;

        _settingsService.Save();
    }

    private void RegisterHotKeys()
    {
        _hotKeyService.UnregisterAllHotKeys();

        if (_settingsService.ToggleHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(
                _settingsService.ToggleHotKey,
                () => IsEnabled = !IsEnabled
            );
        }

        if (_settingsService.ToggleWindowHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(
                _settingsService.ToggleWindowHotKey,
                () => Application.Current?.ApplicationLifetime?.TryGetMainWindow()?.Toggle()
            );
        }

        if (_settingsService.IncreaseTemperatureOffsetHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(
                _settingsService.IncreaseTemperatureOffsetHotKey,
                () =>
                {
                    TemperatureOffset += Math.Min(
                        100,
                        _settingsService.MaximumTemperature - TargetConfiguration.Temperature
                    );
                }
            );
        }

        if (_settingsService.DecreaseTemperatureOffsetHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(
                _settingsService.DecreaseTemperatureOffsetHotKey,
                () =>
                {
                    TemperatureOffset += Math.Max(
                        -100,
                        _settingsService.MinimumTemperature - TargetConfiguration.Temperature
                    );
                }
            );
        }

        if (_settingsService.IncreaseBrightnessOffsetHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(
                _settingsService.IncreaseBrightnessOffsetHotKey,
                () =>
                {
                    BrightnessOffset += Math.Min(
                        0.05,
                        _settingsService.MaximumBrightness - TargetConfiguration.Brightness
                    );
                }
            );
        }

        if (_settingsService.DecreaseBrightnessOffsetHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(
                _settingsService.DecreaseBrightnessOffsetHotKey,
                () =>
                {
                    BrightnessOffset += Math.Max(
                        -0.05,
                        _settingsService.MinimumBrightness - TargetConfiguration.Brightness
                    );
                }
            );
        }

        if (_settingsService.ResetConfigurationOffsetHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(
                _settingsService.ResetConfigurationOffsetHotKey,
                ResetConfigurationOffset
            );
        }
    }

    private void UpdateInstant()
    {
        // If in cycle preview mode, advance quickly until the full cycle has been reached
        if (IsCyclePreviewEnabled)
        {
            // Cycle is supposed to end 1 full day past the current real time
            var targetInstant = DateTimeOffset.Now + TimeSpan.FromDays(1);

            Instant = Instant.StepTo(targetInstant, TimeSpan.FromMinutes(5));
            if (Instant >= targetInstant)
                IsCyclePreviewEnabled = false;
        }
        // Otherwise, synchronize the instant with the system clock
        else
        {
            Instant = DateTimeOffset.Now;
        }
    }

    private void UpdateConfiguration()
    {
        var isSmooth = !IsCyclePreviewEnabled && _settingsService.IsConfigurationSmoothingEnabled;
        var maxDuration = _settingsService.ConfigurationSmoothingMaxDuration;
        var interval = _updateConfigurationTimer.Interval;

        var targetConfiguration = TargetConfiguration;

        // Overall configuration, as shown on the dashboard and in the tray
        CurrentConfiguration = _globalSmoother.Step(
            targetConfiguration,
            isSmooth,
            maxDuration,
            interval
        );

        // Each display is resolved (and smoothed) separately, to account for its own preferences
        var context = new MonitorColorContext(
            targetConfiguration,
            PausedConfiguration,
            !IsActive,
            _settingsService.MinimumTemperature,
            _settingsService.MaximumTemperature,
            _settingsService.MinimumBrightness,
            _settingsService.MaximumBrightness
        );

        // The cycle preview overrides pauses, including display-specific ones
        var pausedMonitor = IsCyclePreviewEnabled ? null : PausedMonitor;
        var isChanged = false;

        _gammaService.SetGamma(gdiDeviceName =>
        {
            var monitor = _monitorService.TryGetByGdiDeviceName(gdiDeviceName);
            var profile = monitor is not null
                ? _settingsService.TryGetMonitorProfile(monitor.DevicePath)
                : null;

            // The primary display is the reference that the others are calibrated against,
            // so its offsets are ignored
            var target = MonitorColorResolver.Resolve(
                context,
                profile,
                string.Equals(gdiDeviceName, pausedMonitor, StringComparison.OrdinalIgnoreCase),
                monitor?.IsPrimary ?? false
            );

            if (!_monitorSmoothers.TryGetValue(gdiDeviceName, out var smoother))
            {
                smoother = new ConfigurationSmoother(ColorConfiguration.Default);
                _monitorSmoothers[gdiDeviceName] = smoother;
            }

            var previous = smoother.Current;
            var current = smoother.Step(target, isSmooth, maxDuration, interval);

            if (current != previous || !MonitorConfigurations.ContainsKey(gdiDeviceName))
                isChanged = true;

            return current;
        });

        // Publish per-display values (e.g. for the settings page) only when something has changed
        if (isChanged)
        {
            MonitorConfigurations = _monitorSmoothers.ToDictionary(
                p => p.Key,
                p => p.Value.Current,
                StringComparer.OrdinalIgnoreCase
            );
        }
    }

    // Parsed ports, re-parsed only when the setting changes
    private string? _streamingPortsSource;
    private IReadOnlyList<int> _streamingPorts = [];

    private IReadOnlyList<int> GetStreamingPorts()
    {
        if (
            !string.Equals(
                _streamingPortsSource,
                _settingsService.StreamingPorts,
                StringComparison.Ordinal
            )
        )
        {
            _streamingPortsSource = _settingsService.StreamingPorts;
            _streamingPorts = StreamingPortList.Parse(_streamingPortsSource);
        }

        return _streamingPorts;
    }

    private void UpdateIsPaused()
    {
        var settings = new AutoPauseSettings(
            _settingsService.IsStreamingAutoPauseEnabled,
            _settingsService.IsPauseWhenScreenLockedEnabled,
            _settingsService.IsPauseWhenFullScreenEnabled,
            _settingsService.IsPerMonitorFullscreenPauseEnabled,
            _settingsService.IsApplicationWhitelistEnabled
        );

        // These are only checked when the corresponding setting is enabled, to avoid needless work
        var isRemoteStreamingActive =
            settings.IsStreamingAutoPauseEnabled
            && _externalApplicationService.IsRemoteStreamingActive(GetStreamingPorts());

        var isScreenLocked = settings.IsPauseWhenScreenLockedEnabled && InputDesktop.IsLocked();

        // Ignored or unidentifiable foreground applications (e.g. the taskbar) should not affect
        // the fullscreen and whitelist states. This prevents brief flashes when switching from a
        // fullscreen/whitelisted application to an ignored application, and back.
        // https://github.com/Tyrrrz/LightBulb/issues/451
        var foregroundApplication = _externalApplicationService.TryGetForegroundApplication();
        if (foregroundApplication is not null)
        {
            _fullScreenMonitor = settings.IsPauseWhenFullScreenEnabled
                ? _externalApplicationService.TryGetForegroundFullScreenMonitor()
                : null;

            _isWhitelistedApplicationActive =
                settings.IsApplicationWhitelistEnabled
                && _settingsService.WhitelistedApplications is not null
                && _settingsService.WhitelistedApplications.Contains(foregroundApplication);
        }

        var state = AutoPause.Evaluate(
            new AutoPauseConditions(
                isRemoteStreamingActive,
                isScreenLocked,
                settings.IsPauseWhenFullScreenEnabled ? _fullScreenMonitor : null,
                settings.IsApplicationWhitelistEnabled && _isWhitelistedApplicationActive
            ),
            settings
        );

        PauseReason = state.Reason;
        PausedMonitor = state.PausedMonitor;
        IsPaused = state.IsGloballyPaused;
    }

    public override Task InitializeAsync()
    {
        _updateInstantTimer.Start();
        _updateConfigurationTimer.Start();
        _updateIsPausedTimer.Start();

        // Hack: feign property changes to refresh the tray icon
        OnAllPropertiesChanged();

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void DisableTemporarily(TimeSpan duration)
    {
        IsEnabled = false;
        _enableAfterDelayRegistration?.Dispose();
        _enableAfterDelayRegistration = Timer.QueueDelayedAction(
            duration,
            () => Dispatcher.UIThread.Post(() => IsEnabled = true)
        );
    }

    [RelayCommand]
    private void DisableUntilSunrise()
    {
        var now = DateTimeOffset.Now;
        var timeUntilSunrise = SolarTimes.Sunrise.NextAfter(now) - now;
        DisableTemporarily(timeUntilSunrise);
    }

    [RelayCommand]
    private void Toggle() => IsEnabled = !IsEnabled;

    [RelayCommand]
    private void TogglePreview() => IsCyclePreviewEnabled = !IsCyclePreviewEnabled;

    [RelayCommand]
    private void ResetConfigurationOffset()
    {
        TemperatureOffset = 0;
        BrightnessOffset = 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Don't lose changes made on the dashboard right before exiting
            FlushPendingSave();

            _eventSubscription.Dispose();

            _updateInstantTimer.Dispose();
            _updateConfigurationTimer.Dispose();
            _updateIsPausedTimer.Dispose();

            _enableAfterDelayRegistration?.Dispose();
        }

        base.Dispose(disposing);
    }
}

public partial class DashboardViewModel
{
    // Everything that the dashboard graph depends on
    private readonly record struct CycleCurveKey(
        DateTimeOffset DayStart,
        SolarTimes SolarTimes,
        ColorConfiguration DayConfiguration,
        ColorConfiguration EveningConfiguration,
        ColorConfiguration NightConfiguration,
        TimeOnly? NightStartTime,
        TimeSpan TransitionDuration,
        double TransitionOffset,
        double TemperatureOffset,
        double BrightnessOffset
    );
}
