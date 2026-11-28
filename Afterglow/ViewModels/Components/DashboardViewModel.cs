using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Core.Utils.Extensions;
using Afterglow.Framework;
using Afterglow.Localization;
using Afterglow.Models;
using Afterglow.PlatformInterop;
using Afterglow.Services;
using Afterglow.Utils;
using Afterglow.Utils.Extensions;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PowerKit;
using PowerKit.Extensions;

namespace Afterglow.ViewModels.Components;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly GammaService _gammaService;
    private readonly GrayscaleService _grayscaleService;
    private readonly HotKeyService _hotKeyService;
    private readonly ExternalApplicationService _externalApplicationService;
    private readonly MonitorService _monitorService;
    private readonly UpdateService _updateService;

    private readonly IDisposable _eventSubscription;

    // A single timer drives all updates. It ticks quickly (20 times a second) whenever anything
    // may change or can be seen, and slowly once everything has settled while only the tray icon
    // is showing, so that the app barely wakes up the CPU most of the time.
    private static readonly TimeSpan FastUpdateInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan SlowUpdateInterval = TimeSpan.FromSeconds(1);

    // Pausing conditions (e.g. a fullscreen application) are checked about once a second
    private static readonly TimeSpan IsPausedUpdateInterval = TimeSpan.FromSeconds(1);

    private readonly Timer _updateTimer;
    private readonly System.Threading.Lock _updateRateLock = new();
    private bool _isUpdatingFast = true;
    private DateTimeOffset _lastIsPausedUpdateTimestamp = DateTimeOffset.MinValue;

    // When a temporary pause ends. Checked against the clock rather than with a delayed timer,
    // because timers don't count the time the computer spends asleep, which would stretch a pause
    // (e.g. "until sunrise", with the laptop shut overnight) well past its end.
    private DateTimeOffset? _enableAt;
    private IDisposable? _pendingSaveRegistration;

    // Smoothing of the overall configuration, and of each display (by GDI device name)
    private readonly ConfigurationSmoother _globalSmoother = new(ColorConfiguration.Default);
    private readonly Dictionary<string, ConfigurationSmoother> _monitorSmoothers = new(
        StringComparer.OrdinalIgnoreCase
    );

    // Last known states, kept while an unidentifiable application (e.g. the taskbar) is in focus
    private string? _fullScreenMonitor;

    // (not until the window shows it, which it may never do, if the app starts hidden)
    private bool _isDashboardVisible;

    private readonly GrayscaleController _grayscaleController = new();
    private bool _isGrayscaleWanted;
    private DateTimeOffset? _grayscaleSnoozedUntil;
    private GrayscaleSchedule? _lastGrayscaleSchedule;

    private CycleCurve? _cycleCurve;
    private CycleCurveKey? _cycleCurveKey;

    public DashboardViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        GammaService gammaService,
        GrayscaleService grayscaleService,
        HotKeyService hotKeyService,
        ExternalApplicationService externalApplicationService,
        MonitorService monitorService,
        UpdateService updateService,
        HueService hueService
    )
    {
        Hue = hueService;
        _settingsService = settingsService;
        LocalizationManager = localizationManager;
        _gammaService = gammaService;
        _grayscaleService = grayscaleService;
        _hotKeyService = hotKeyService;
        _externalApplicationService = externalApplicationService;
        _monitorService = monitorService;
        _updateService = updateService;

        _eventSubscription = Disposable.Merge(
            // The tray says how many lights would be turned on
            hueService.WatchProperty(
                o => o.TurnOnLightCount,
                _ => OnPropertyChanged(nameof(TurnOnLightsText))
            ),
            // Invalidate the cached local time zone when the system clock or time zone changes
            // https://github.com/Tyrrrz/LightBulb/issues/465
            SystemEvent.Register(
                SystemEvent.Ids.TimeChanged,
                () =>
                {
                    TimeZoneInfo.ClearCachedData();
                    RequestFastUpdates();
                }
            ),
            SystemEvent.Register(
                SystemEvent.Ids.SettingsChanged,
                () =>
                {
                    TimeZoneInfo.ClearCachedData();
                    RequestFastUpdates();
                }
            ),
            this.WatchProperty(
                o => o.IsEnabled,
                v =>
                {
                    if (v)
                    {
                        // Cancel any active temporary pause
                        _enableAt = null;

                        // Invalidate device contexts
                        _gammaService.InvalidateDeviceContexts();
                    }

                    // Grayscale is suspended while paused manually, and restored when resumed
                    UpdateGrayscale();
                }
            ),
            // A new grayscale schedule replaces any temporary disable
            // (only an actual change: saving the settings notifies of every property at once)
            settingsService.WatchProperty(
                o => o.GrayscaleSchedule,
                v =>
                {
                    if (v == _lastGrayscaleSchedule)
                        return;

                    _lastGrayscaleSchedule = v;
                    _grayscaleSnoozedUntil = null;
                }
            ),
            // Apply grayscale settings right away, rather than on the next update
            settingsService.WatchProperties(
                [o => o.GrayscaleSchedule, o => o.IsGrayscaleTemperatureShiftEnabled],
                () =>
                {
                    UpdateGrayscale();
                    OnPropertyChanged(nameof(TargetConfiguration));
                    OnPropertyChanged(nameof(StatusSentence));
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
                    OnPropertyChanged(nameof(WallpaperStatusText));
                    OnPropertyChanged(nameof(ActiveStageTemperatureTooltip));
                    OnPropertyChanged(nameof(LocationText));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(TrayStatusText));
                    OnPropertyChanged(nameof(TurnOnLightsText));
                    OnPropertyChanged(nameof(UpdateAvailableTooltip));
                    OnPropertyChanged(nameof(UpdateButtonText));
                }
            ),
            updateService.WatchProperties(
                [o => o.AvailableVersion, o => o.InstallState, o => o.DownloadProgress],
                () =>
                {
                    OnPropertyChanged(nameof(IsUpdateAvailable));
                    OnPropertyChanged(nameof(UpdateAvailableTooltip));
                    OnPropertyChanged(nameof(UpdateButtonText));
                    InstallUpdateCommand.NotifyCanExecuteChanged();
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
                    o => o.ToggleGrayscaleHotKey,
                    o => o.ToggleLightsPauseHotKey,
                ],
                RegisterHotKeys
            )
        );

        _updateTimer = new Timer(FastUpdateInterval, () => Dispatcher.UIThread.Post(Update));

        // Anything that may change the colors switches back to fast updates right away
        _settingsService.PropertyChanged += OnExternalChange;
        _gammaService.Invalidated += RequestFastUpdates;
        _monitorService.MonitorsChanged += OnExternalChange;
    }

    private void OnExternalChange(object? sender, EventArgs args) => RequestFastUpdates();

    // Properties of this view model that change what the displays should be set to, or whether
    // anything is shown (a change of any of them calls for fast updates)
    private static readonly HashSet<string> FastUpdatePropertyNames =
    [
        nameof(IsEnabled),
        nameof(IsPaused),
        nameof(PausedMonitors),
        nameof(IsCyclePreviewEnabled),
        nameof(TemperatureOffset),
        nameof(BrightnessOffset),
        nameof(IsGrayscaleActive),
        nameof(IsDashboardVisible),
        nameof(IsTrayMenuOpen),
    ];

    protected override void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);

        if (
            string.IsNullOrEmpty(args.PropertyName)
            || FastUpdatePropertyNames.Contains(args.PropertyName)
        )
        {
            RequestFastUpdates();
        }
    }

    // Switches to fast updates (if not already), with the next one coming right away.
    // May be called from any thread.
    private void RequestFastUpdates()
    {
        using (_updateRateLock.EnterScope())
        {
            if (_isUpdatingFast)
                return;

            _isUpdatingFast = true;
            _updateTimer.ChangeInterval(FastUpdateInterval, TimeSpan.Zero);
        }
    }

    private void RequestSlowUpdates()
    {
        using (_updateRateLock.EnterScope())
        {
            if (!_isUpdatingFast)
                return;

            _isUpdatingFast = false;
            _updateTimer.ChangeInterval(SlowUpdateInterval, SlowUpdateInterval);
        }
    }

    // Whether updates can slow down without anything behaving differently: nothing is changing
    // faster than slow updates keep up with (no smoothing in progress, and no transition that's
    // quick enough for each slow update to make a bigger step than the gamma is written in),
    // nothing is shown that would update visibly, and gamma doesn't need frequent attention
    internal static bool CanUpdateSlowly(
        bool isSettled,
        bool isChangingQuickly,
        bool isCyclePreviewEnabled,
        bool isAnythingShown,
        bool isGammaPolledQuickly,
        bool doesGammaNeedFrequentUpdates
    ) =>
        isSettled
        && !isChangingQuickly
        && !isCyclePreviewEnabled
        && !isAnythingShown
        && !isGammaPolledQuickly
        && !doesGammaNeedFrequentUpdates;

    // Whether the colors move on by more than half a gamma write's step from one slow update to
    // the next (e.g. in a transition of a few minutes). Long transitions move so slowly that
    // slow updates make the same steps as fast ones would (e.g. 2,600 K over 3 hours is a
    // fifteenth of a step a second), so they don't need fast updates.
    internal static bool IsChangingFasterThanSlowUpdates(
        ColorConfiguration now,
        ColorConfiguration oneSlowUpdateLater
    ) =>
        Math.Abs(oneSlowUpdateLater.Temperature - now.Temperature)
            > GammaWriteTracker.TemperatureThreshold / 2
        || Math.Abs(oneSlowUpdateLater.Brightness - now.Brightness)
            > GammaWriteTracker.BrightnessThreshold / 2;

    // Whether gamma is polled more often than slow updates come
    internal static bool IsPolledFasterThanSlowUpdates(bool isEnabled, TimeSpan interval) =>
        isEnabled && interval < SlowUpdateInterval;

    private void Update()
    {
        // Pausing conditions are checked about once a second, regardless of the update rate
        // (allowing for the timer's own imprecision, so a slow update always includes it)
        var now = DateTimeOffset.Now;
        if (
            (now - _lastIsPausedUpdateTimestamp).Duration()
            >= IsPausedUpdateInterval - FastUpdateInterval
        )
        {
            _lastIsPausedUpdateTimestamp = now;
            UpdateIsPaused();
        }

        UpdateInstant();
        var isSettled = UpdateConfiguration();

        if (
            CanUpdateSlowly(
                isSettled,
                Phase.IsTransitioning
                    && IsChangingFasterThanSlowUpdates(
                        GetCycleConfiguration(SolarTimes, Instant),
                        GetCycleConfiguration(SolarTimes, Instant + SlowUpdateInterval)
                    ),
                IsCyclePreviewEnabled,
                IsDashboardVisible || IsTrayMenuOpen,
                IsPolledFasterThanSlowUpdates(
                    _settingsService.IsGammaPollingEnabled,
                    _settingsService.GammaPollingInterval
                ),
                _gammaService.NeedsFrequentUpdates
            )
        )
        {
            RequestSlowUpdates();
        }
        else
        {
            RequestFastUpdates();
        }
    }

    // Set by the app while the tray menu is open, as it shows the current state
    [ObservableProperty]
    public partial bool IsTrayMenuOpen { get; set; }

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

    // GDI device names of the displays that are paused on their own (e.g. one running a fullscreen
    // application, and others showing whitelisted windows), while the rest stay on the cycle
    [ObservableProperty]
    public partial IReadOnlyCollection<string>? PausedMonitors { get; set; }

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

    // How far apart the times of day are when stepping through them (16 a day)
    private static readonly TimeSpan PreviewTimeStep = TimeSpan.FromMinutes(90);

    // The time of day held by stepping through the day (a debugging aid for the dynamic
    // wallpaper, which also sets the screen and lights to it), rather than playing the preview
    private DateTimeOffset? _steppedInstant;

    // When the last step was taken: stepping through the day stops on its own after a while
    // without one, so that a forgotten preview doesn't hold the screen, the lights and the
    // wallpaper at another time of day
    private DateTimeOffset _lastSteppedAt = DateTimeOffset.MinValue;
    private static readonly TimeSpan SteppingTimeout = TimeSpan.FromMinutes(10);

    // Whether a time of day is held by stepping through the day
    public bool IsSteppingThroughDay => IsCyclePreviewEnabled && _steppedInstant is not null;

    // e.g. "Previewing 16:30"
    public string SteppedTimeText =>
        _steppedInstant is { } instant
            ? string.Format(
                LocalizationManager.DynamicWallpaperPreviewingText,
                instant.ToString("t")
            )
            : "";

    // How high the sun is (in degrees, negative below the horizon): the real sun where the user
    // is, when that's what sunrise and sunset follow, and otherwise a smooth arc between the times
    // entered by hand (as the sky should follow those), for the location's latitude if it's known
    public double SunElevation =>
        _settingsService
            is { IsManualSunriseSunsetEnabled: false, Location: { IsValid: true } location }
            ? DynamicWallpaper.GetSunElevation(location, Instant)
            : DynamicWallpaper.GetSunElevation(
                SolarTimes,
                Instant,
                _settingsService.Location?.Latitude
            );

    // Moves on to the next time of day (starting from the next step after now), holding the
    // screen, the lights and the dynamic wallpaper there until stopped
    [RelayCommand]
    private void StepThroughDay()
    {
        var from = _steppedInstant ?? DateTimeOffset.Now;
        var dayStart = from.ResetTimeOfDay();
        var steps = Math.Floor((from - dayStart) / PreviewTimeStep) + 1;

        _steppedInstant = dayStart + PreviewTimeStep * steps;
        _lastSteppedAt = DateTimeOffset.Now;
        IsCyclePreviewEnabled = true;

        OnPropertyChanged(nameof(IsSteppingThroughDay));
        OnPropertyChanged(nameof(SteppedTimeText));
        RequestFastUpdates();
    }

    [RelayCommand]
    private void StopSteppingThroughDay() => IsCyclePreviewEnabled = false;

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
        new(nameof(WallpaperStatusText)),
        new(nameof(WallpaperStatusIcon)),
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
            NotifyDashboardVisualsChanged();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOffsetEnabled))]
    [NotifyPropertyChangedFor(nameof(TargetConfiguration))]
    [NotifyPropertyChangedFor(nameof(CycleCurve))]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    public partial double TemperatureOffset { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOffsetEnabled))]
    [NotifyPropertyChangedFor(nameof(TargetConfiguration))]
    [NotifyPropertyChangedFor(nameof(CycleCurve))]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
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
            ? _solarTimesCache.Get(location, Instant)
            : new SolarTimes(_settingsService.ManualSunrise, _settingsService.ManualSunset);

    // Read several times on every update, but only changes with the location and the date
    private readonly SolarTimesCache _solarTimesCache = new();

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

    // While grayscale is on, the color temperature either tints it, or is replaced by a neutral
    // white point (keeping the brightness), depending on the settings
    public ColorConfiguration TargetConfiguration =>
        IsActive
            ? GrayscaleController.GetConfiguration(
                GetCycleConfiguration(SolarTimes, Instant),
                IsGrayscaleActive,
                _settingsService.IsGrayscaleTemperatureShiftEnabled
            )
            : PausedConfiguration;

    // What room lights follow: the cycle as the screen shows it (including the preview and any
    // adjustments), without grayscale, which only makes sense on a screen
    public ColorConfiguration LightConfiguration => GetCycleConfiguration(SolarTimes, Instant);

    // Whether the screen (or any display) is paused on its own, e.g. for a fullscreen application
    public bool IsScreenPausedAutomatically =>
        !IsCyclePreviewEnabled && (IsPaused || PausedMonitors is { Count: > 0 });

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

    // Plain-language statement of what Afterglow is doing right now
    // (e.g. "Warming to 4500 K and dimming to 90% for evening.")
    public string StatusSentence =>
        StatusSentenceBuilder.Build(
            new StatusSentenceContext(
                IsCyclePreviewEnabled,
                IsEnabled,
                IsPaused ? PauseReason : PauseReason.None,
                IsOffsetEnabled,
                TargetConfiguration,
                Phase,
                Schedule.IsEveningIncluded,
                _settingsService.DayConfiguration,
                _settingsService.EveningConfiguration,
                _settingsService.NightConfiguration,
                IsGrayscaleActive,
                _settingsService.IsGrayscaleTemperatureShiftEnabled,
                _settingsService.IsDefaultToDayConfigurationEnabled,
                _grayscaleSnoozedUntil - DateTimeOffset.Now,
                IsGrayscaleHeldUntilScheduleChange
            ),
            LocalizationManager
        );

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
    /// <summary>
    /// Where what the dynamic wallpaper shows comes from (set once the services are up).
    /// </summary>
    public Func<WallpaperStatus?>? WallpaperStatusSource { get; set; }

    // e.g. "Partly cloudy → Light rain", next to an icon of where it's going (null while the
    // wallpaper is off, so that nothing shows)
    public string? WallpaperStatusText =>
        WallpaperStatusSource?.Invoke() switch
        {
            null => null,
            { IsWaiting: true } => LocalizationManager.WallpaperWaitingText,
            { From: { } from } status =>
                $"{LocalizationManager.GetSkyKindName(from)} → {LocalizationManager.GetSkyKindName(status.To)}",
            { } status => LocalizationManager.GetSkyKindName(status.To),
        };

    // (the sun or the moon, for a clear or partly cloudy sky, as it's day or night)
    public MaterialIconKind WallpaperStatusIcon =>
        WallpaperStatusSource?.Invoke() switch
        {
            null or { IsWaiting: true } => MaterialIconKind.Wallpaper,
            { } status => (status.To, SunElevation > 0) switch
            {
                (SkyKind.Clear, true) => MaterialIconKind.WeatherSunny,
                (SkyKind.Clear, false) => MaterialIconKind.WeatherNight,
                (SkyKind.HighClouds or SkyKind.PartlyCloudy, true) =>
                    MaterialIconKind.WeatherPartlyCloudy,
                (SkyKind.HighClouds or SkyKind.PartlyCloudy, false) =>
                    MaterialIconKind.WeatherNightPartlyCloudy,
                (SkyKind.MostlyCloudy or SkyKind.Overcast, _) => MaterialIconKind.WeatherCloudy,
                (SkyKind.Fog, _) => MaterialIconKind.WeatherFog,
                (SkyKind.Haze, _) => MaterialIconKind.WeatherHazy,
                (SkyKind.Dust, _) => MaterialIconKind.WeatherDust,
                (SkyKind.LightRain, _) => MaterialIconKind.WeatherPartlyRainy,
                (SkyKind.Rain, _) => MaterialIconKind.WeatherRainy,
                (SkyKind.HeavyRain, _) => MaterialIconKind.WeatherPouring,
                (SkyKind.Snow, _) => MaterialIconKind.WeatherSnowy,
                (SkyKind.HeavySnow, _) => MaterialIconKind.WeatherSnowyHeavy,
                (SkyKind.Thunderstorm, _) => MaterialIconKind.WeatherLightningRainy,
                _ => MaterialIconKind.Wallpaper,
            },
        };

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
        Program.DisplayName
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

    // For when the app is crashing or being terminated: writes pending changes to the settings
    // file only (without touching the registry or notifying the UI), so it's safe from any thread
    internal void TryFlushPendingSaveToFile()
    {
        if (_pendingSaveRegistration is null)
            return;

        _settingsService.TrySaveToFile();
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
                App.ToggleMainWindow
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

        if (_settingsService.ToggleGrayscaleHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(_settingsService.ToggleGrayscaleHotKey, ToggleGrayscale);
        }

        if (_settingsService.ToggleLightsPauseHotKey != HotKey.None)
        {
            _hotKeyService.RegisterHotKey(
                _settingsService.ToggleLightsPauseHotKey,
                ToggleLightsPause
            );
        }
    }

    private void UpdateInstant()
    {
        // If in cycle preview mode, advance quickly until the full cycle has been reached
        if (IsCyclePreviewEnabled && _steppedInstant is { } steppedInstant)
        {
            if ((DateTimeOffset.Now - _lastSteppedAt).Duration() >= SteppingTimeout)
            {
                // (and back to the time of day right away)
                IsCyclePreviewEnabled = false;
                Instant = DateTimeOffset.Now;
            }
            else
            {
                Instant = steppedInstant;
            }
        }
        else if (IsCyclePreviewEnabled)
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

    // Returns whether everything has reached its target (i.e. no smoothing is in progress)
    private bool UpdateConfiguration()
    {
        var isSmooth = !IsCyclePreviewEnabled && _settingsService.IsConfigurationSmoothingEnabled;
        var maxDuration = _settingsService.ConfigurationSmoothingMaxDuration;

        // Smoothing only ever progresses during fast updates (updates only slow down once it's
        // done), so it always steps at that rate
        var interval = FastUpdateInterval;

        var targetConfiguration = TargetConfiguration;

        // Overall configuration, as shown on the dashboard and in the tray
        CurrentConfiguration = _globalSmoother.Step(
            targetConfiguration,
            isSmooth,
            maxDuration,
            interval
        );

        var isSettled = CurrentConfiguration == targetConfiguration;

        // Each display is resolved (and smoothed) separately, to account for its own preferences
        var context = new MonitorColorContext(
            targetConfiguration,
            PausedConfiguration,
            !IsActive,
            _settingsService.MinimumTemperature,
            _settingsService.MaximumTemperature,
            _settingsService.MinimumBrightness,
            _settingsService.MaximumBrightness,
            _settingsService.MonitorOffsetScaling,
            _settingsService.DayConfiguration.Temperature
        );

        // The cycle preview overrides pauses, including display-specific ones
        var pausedMonitors = IsCyclePreviewEnabled ? null : PausedMonitors;
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
                pausedMonitors?.Contains(gdiDeviceName, StringComparer.OrdinalIgnoreCase) == true,
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

            if (current != target)
                isSettled = false;

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

        return isSettled;
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

    private static bool AreSameMonitors(
        IReadOnlyCollection<string>? first,
        IReadOnlyCollection<string>? second
    ) =>
        (first?.Count ?? 0) == (second?.Count ?? 0)
        && (
            first is null
            || second is null
            || first.All(m => second.Contains(m, StringComparer.OrdinalIgnoreCase))
        );

    private readonly ForegroundWhitelist _foregroundWhitelist = new();

    // Whitelist rules are only rebuilt when the settings they come from change
    private WhitelistRules? _whitelistRules;
    private (
        bool,
        IReadOnlyList<ExternalApplication>?,
        bool,
        IReadOnlyList<string>?
    ) _whitelistRulesSource;

    private WhitelistRules GetWhitelistRules()
    {
        var source = (
            _settingsService.IsWhitelistedApplicationPauseEnabled,
            _settingsService.WhitelistedApplications,
            _settingsService.IsWhitelistedWindowTitlePauseEnabled,
            _settingsService.WhitelistedWindowTitles
        );

        if (_whitelistRules is null || source != _whitelistRulesSource)
        {
            _whitelistRules = WhitelistRules.Create(
                source.Item1,
                source.Item2?.Select(a => a.ExecutableFilePath),
                source.Item3,
                source.Item4
            );

            _whitelistRulesSource = source;
        }

        return _whitelistRules;
    }

    // Whether a temporary pause that ends at the given time is over
    internal static bool IsTemporaryPauseOver(DateTimeOffset? enableAt, DateTimeOffset now) =>
        enableAt is { } at && now >= at;

    private void UpdateIsPaused()
    {
        // Also catches up right after waking from sleep
        if (IsTemporaryPauseOver(_enableAt, DateTimeOffset.Now))
        {
            _enableAt = null;
            IsEnabled = true;
        }

        var settings = new AutoPauseSettings(
            _settingsService.IsStreamingAutoPauseEnabled,
            _settingsService.IsPauseWhenScreenLockedEnabled,
            _settingsService.IsPauseWhenFullScreenEnabled,
            _settingsService.IsPerMonitorFullscreenPauseEnabled,
            _settingsService.IsApplicationWhitelistEnabled,
            _settingsService.IsPerMonitorWhitelistPauseEnabled
        );

        // These are only checked when the corresponding setting is enabled, to avoid needless work
        var isRemoteStreamingActive =
            settings.IsStreamingAutoPauseEnabled
            && _externalApplicationService.IsRemoteStreamingActive(GetStreamingPorts());

        var isScreenLocked = settings.IsPauseWhenScreenLockedEnabled && InputDesktop.IsLocked();

        // The taskbar and other parts of the shell don't affect the fullscreen state, which
        // prevents brief flashes when switching from a fullscreen application to them and back.
        // Only looked up when needed, since this opens the foreground application's process.
        if (
            settings.IsForegroundApplicationNeeded
            && _externalApplicationService.TryGetForegroundFullScreenMonitor(
                out var fullScreenMonitor
            )
        )
        {
            _fullScreenMonitor = fullScreenMonitor;
        }

        // Only the window in focus counts. It isn't looked up at all when there's nothing to match.
        var whitelistRules = settings.IsApplicationWhitelistEnabled
            ? GetWhitelistRules()
            : WhitelistRules.None;

        var whitelist = whitelistRules.IsEmpty
            ? _foregroundWhitelist.Reset()
            : _foregroundWhitelist.Update(
                whitelistRules,
                _externalApplicationService.TryGetForegroundWhitelistWindow()
            );

        var state = AutoPause.Evaluate(
            new AutoPauseConditions(
                isRemoteStreamingActive,
                isScreenLocked,
                settings.IsPauseWhenFullScreenEnabled ? _fullScreenMonitor : null,
                whitelist.IsMatched,
                whitelist.Monitor,
                _monitorService.Monitors.Select(m => m.GdiDeviceName).ToArray()
            ),
            settings
        );

        PauseReason = state.Reason;
        IsPaused = state.IsGloballyPaused;

        // Only replaced when it changes, as it's checked on every update
        if (!AreSameMonitors(PausedMonitors, state.PausedMonitors))
            PausedMonitors = state.PausedMonitors;

        // Also follows the grayscale schedule as the cycle moves between stages
        UpdateGrayscale();
    }

    // Whether the screen is grayscale right now
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TargetConfiguration))]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    public partial bool IsGrayscaleActive { get; private set; }

    // Whether grayscale is on (or would be, if Afterglow weren't paused), as shown by its toggles
    public bool IsGrayscaleEnabled
    {
        get => _isGrayscaleWanted;
        set
        {
            if (value != _isGrayscaleWanted)
                ToggleGrayscale();
        }
    }

    private bool IsGrayscaleScheduled =>
        GrayscaleController.IsScheduled(_settingsService.GrayscaleSchedule, Phase);

    private void UpdateGrayscale()
    {
        var isScheduled = IsGrayscaleScheduled;

        // A manual choice only lasts until the schedule next turns grayscale on or off
        var manualOverride = _grayscaleController.UpdateOverride(
            isScheduled,
            _settingsService.GrayscaleOverride
        );

        if (manualOverride != _settingsService.GrayscaleOverride)
        {
            _settingsService.GrayscaleOverride = manualOverride;
            QueueSave();
        }

        var isWanted = GrayscaleController.IsWanted(isScheduled, manualOverride);
        if (isWanted != _isGrayscaleWanted)
        {
            _isGrayscaleWanted = isWanted;
            OnPropertyChanged(nameof(IsGrayscaleEnabled));
        }

        // A temporary disable ends by itself once its time is up
        if (!GrayscaleController.IsSnoozed(_grayscaleSnoozedUntil, DateTimeOffset.Now))
            _grayscaleSnoozedUntil = null;

        // Disabled until the next schedule change, which is a manual override that the
        // schedule clears by itself
        IsGrayscaleHeldUntilScheduleChange = isScheduled && manualOverride == false;
        IsGrayscaleSnoozed =
            _grayscaleSnoozedUntil is not null || IsGrayscaleHeldUntilScheduleChange;

        // Suspended while paused, and while any display is paused on its own (for a fullscreen
        // or whitelisted window), as grayscale can't be limited to the other displays
        IsGrayscaleActive = GrayscaleController.IsApplied(
            isWanted && _grayscaleSnoozedUntil is null,
            IsActive,
            !IsCyclePreviewEnabled && PausedMonitors is { Count: > 0 }
        );

        _grayscaleService.SetEnabled(IsGrayscaleActive);
    }

    // Grayscale is temporarily disabled (for a while, or until the next schedule change)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    public partial bool IsGrayscaleSnoozed { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    public partial bool IsGrayscaleHeldUntilScheduleChange { get; private set; }

    [RelayCommand]
    private void SnoozeGrayscale(TimeSpan duration)
    {
        _grayscaleSnoozedUntil = DateTimeOffset.Now + duration;
        UpdateGrayscale();
    }

    [RelayCommand]
    private void SnoozeGrayscaleUntilSunrise() => SnoozeGrayscale(GetTimeUntilSunrise());

    // Hue lights, which can be paused from the tray (going back to how they were meanwhile),
    // without affecting the screens
    public HueService Hue { get; }

    [RelayCommand]
    private void PauseLights(TimeSpan duration) => Hue.Pause(duration);

    [RelayCommand]
    private void PauseLightsUntilSunrise() => Hue.Pause(GetTimeUntilSunrise());

    // e.g. "Turn on lights (6)"
    public string TurnOnLightsText =>
        string.Format(
            CultureInfo.CurrentCulture,
            LocalizationManager.TrayTurnOnLightsMenuItem,
            Hue.TurnOnLightCount
        );

    // Turns on the lights in the chosen rooms (from the tray)
    [RelayCommand]
    private void TurnOnLights() => Hue.TurnOnLights();

    // The lights row in the tray menu resumes them while they're paused, and pauses them (until
    // resumed) otherwise
    [RelayCommand]
    private void ToggleLightsPause()
    {
        // (also from a hotkey, which does nothing while Hue control is off or not connected)
        if (!Hue.IsAvailable)
            return;

        if (Hue.IsPausedByUser)
            Hue.Resume();
        else
            Hue.Pause();
    }

    // The grayscale row in the tray menu resumes grayscale while it's temporarily disabled,
    // and toggles it otherwise (like the pause row does for Afterglow itself)
    [RelayCommand]
    private void ToggleOrResumeGrayscale()
    {
        if (IsGrayscaleSnoozed)
            ResumeGrayscale();
        else
            ToggleGrayscale();
    }

    private void ResumeGrayscale()
    {
        _grayscaleSnoozedUntil = null;

        if (IsGrayscaleHeldUntilScheduleChange)
        {
            _settingsService.GrayscaleOverride = null;
            QueueSave();
        }

        UpdateGrayscale();
    }

    [RelayCommand]
    private void ToggleGrayscale()
    {
        var isScheduled = IsGrayscaleScheduled;

        // Bring the schedule state up to date first, so that the toggle isn't mistaken for
        // something that happened before a schedule change
        var manualOverride = _grayscaleController.UpdateOverride(
            isScheduled,
            _settingsService.GrayscaleOverride
        );

        _settingsService.GrayscaleOverride = GrayscaleController.Toggle(
            isScheduled,
            manualOverride
        );

        // Toggling replaces any temporary disable
        _grayscaleSnoozedUntil = null;

        QueueSave();
        UpdateGrayscale();
    }

    partial void OnIsCyclePreviewEnabledChanged(bool value)
    {
        if (!value && _steppedInstant is not null)
        {
            _steppedInstant = null;
            OnPropertyChanged(nameof(IsSteppingThroughDay));
            OnPropertyChanged(nameof(SteppedTimeText));
        }

        UpdateGrayscale();
    }

    private bool _isInitialized;

    // Started once, when the app starts (whether or not the window is shown), rather than each
    // time the window is created
    public override Task InitializeAsync()
    {
        if (_isInitialized)
            return Task.CompletedTask;

        _isInitialized = true;

        _updateTimer.Start();
        _updateService.Start();

        // Hack: feign property changes to refresh the tray icon
        OnAllPropertiesChanged();

        return Task.CompletedTask;
    }

    // ---- Updates ----

    public bool IsUpdateAvailable => _updateService.AvailableVersion is not null;

    public string UpdateAvailableTooltip =>
        _updateService.AvailableVersion is { } version
            ? UpdateText.GetAction(LocalizationManager, _updateService.InstallState, version)
            : "";

    // e.g. "Update available", or the progress while updating
    public string UpdateButtonText =>
        UpdateText.GetButton(
            LocalizationManager,
            _updateService.InstallState,
            _updateService.DownloadProgress
        );

    private bool CanInstallUpdate() => !UpdateText.IsBusy(_updateService.InstallState);

    // Downloads and installs the update, and restarts (or opens the download page, if that failed)
    [RelayCommand(CanExecute = nameof(CanInstallUpdate))]
    private async Task InstallUpdateAsync() => await _updateService.UpdateAsync();

    [RelayCommand]
    private void DisableTemporarily(TimeSpan duration)
    {
        IsEnabled = false;
        _enableAt = DateTimeOffset.Now + duration;
    }

    private TimeSpan GetTimeUntilSunrise()
    {
        var now = DateTimeOffset.Now;
        return SolarTimes.Sunrise.NextAfter(now) - now;
    }

    [RelayCommand]
    private void DisableUntilSunrise() => DisableTemporarily(GetTimeUntilSunrise());

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

            _settingsService.PropertyChanged -= OnExternalChange;
            _gammaService.Invalidated -= RequestFastUpdates;
            _monitorService.MonitorsChanged -= OnExternalChange;

            _updateTimer.Dispose();
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
