using System;
using System.Collections.Generic;
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

    private readonly IDisposable _eventSubscription;

    private readonly Timer _updateInstantTimer;
    private readonly Timer _updateConfigurationTimer;
    private readonly Timer _updateIsPausedTimer;

    private IDisposable? _enableAfterDelayRegistration;
    private IDisposable? _pendingSaveRegistration;
    private bool _isPausedForLockedScreen;
    private readonly PhrasePicker<CycleStage> _statusPhrasePicker = new();
    private ColorConfiguration? _configurationSmoothingSource;
    private ColorConfiguration? _configurationSmoothingTarget;

    private CycleCurve? _cycleCurve;
    private CycleCurveKey? _cycleCurveKey;

    public DashboardViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        GammaService gammaService,
        HotKeyService hotKeyService,
        ExternalApplicationService externalApplicationService
    )
    {
        _settingsService = settingsService;
        LocalizationManager = localizationManager;
        _gammaService = gammaService;
        _hotKeyService = hotKeyService;
        _externalApplicationService = externalApplicationService;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    public partial bool IsCyclePreviewEnabled { get; set; }

    public bool IsActive => IsEnabled && !IsPaused || IsCyclePreviewEnabled;

    // Everything that depends on the time of day or on the settings is refreshed on every tick
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SolarTimes))]
    [NotifyPropertyChangedFor(nameof(Schedule))]
    [NotifyPropertyChangedFor(nameof(Phase))]
    [NotifyPropertyChangedFor(nameof(ActiveStageTemperature))]
    [NotifyPropertyChangedFor(nameof(MinimumSliderTemperature))]
    [NotifyPropertyChangedFor(nameof(MaximumSliderTemperature))]
    [NotifyPropertyChangedFor(nameof(ActiveStageTemperatureTooltip))]
    [NotifyPropertyChangedFor(nameof(TargetConfiguration))]
    [NotifyPropertyChangedFor(nameof(StatusSentence))]
    [NotifyPropertyChangedFor(nameof(SolarTimingText))]
    [NotifyPropertyChangedFor(nameof(ActivePhaseCaption))]
    [NotifyPropertyChangedFor(nameof(LocationText))]
    [NotifyPropertyChangedFor(nameof(CycleCurve))]
    [NotifyPropertyChangedFor(nameof(DayProgress))]
    [NotifyPropertyChangedFor(nameof(TrayStatusText))]
    [NotifyPropertyChangedFor(nameof(StatusIndicator))]
    public partial DateTimeOffset Instant { get; set; } = DateTimeOffset.Now;

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
    [NotifyPropertyChangedFor(nameof(ActivePhaseCaption))]
    [NotifyPropertyChangedFor(nameof(TrayStatusText))]
    public partial ColorConfiguration CurrentConfiguration { get; set; } =
        ColorConfiguration.Default;

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

    public ColorConfiguration TargetConfiguration =>
        IsActive ? GetCycleConfiguration(SolarTimes, Instant)
        : _settingsService.IsDefaultToDayConfigurationEnabled ? _settingsService.DayConfiguration
        : ColorConfiguration.Default;

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

    private IReadOnlyList<string> GetStagePhrases(CycleStage stage) =>
        SplitPhrases(
            stage switch
            {
                CycleStage.Day => LocalizationManager.StatusDayPhrases,
                CycleStage.Evening => LocalizationManager.StatusEveningPhrases,
                _ => LocalizationManager.StatusNightPhrases,
            }
        );

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
                return LocalizationManager.StatusPausedText;

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
            LocalizationManager.ActivePhaseCaption,
            GetPhaseName(Phase),
            CurrentConfiguration.Temperature.ToString("F0", CultureInfo.CurrentCulture),
            CurrentConfiguration.Brightness.ToString("P0", CultureInfo.CurrentCulture)
        );

    // e.g. "Evening · 5000 K · 100%", as shown at the top of the tray menu
    public string TrayStatusText =>
        (
            !IsEnabled ? LocalizationManager.PausedStateLabel
            : IsPaused ? LocalizationManager.AutoPausedStateLabel
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
        var isSmooth =
            !IsCyclePreviewEnabled
            && CurrentConfiguration != TargetConfiguration
            && _settingsService.IsConfigurationSmoothingEnabled
            && _settingsService.ConfigurationSmoothingMaxDuration.TotalSeconds >= 0.1;

        if (isSmooth)
        {
            // Check if the target configuration has changed since the last transition started
            if (
                _configurationSmoothingTarget != TargetConfiguration
                || _configurationSmoothingSource is null
            )
            {
                _configurationSmoothingSource = CurrentConfiguration;
                _configurationSmoothingTarget = TargetConfiguration;
            }

            var brightnessDelta = Math.Abs(
                _configurationSmoothingTarget.Value.Brightness
                    - _configurationSmoothingSource.Value.Brightness
            );

            var brightnessStep = Math.Max(
                brightnessDelta
                    / _settingsService.ConfigurationSmoothingMaxDuration.TotalSeconds
                    * _updateConfigurationTimer.Interval.TotalSeconds,
                0.08
            );

            var temperatureDelta = Math.Abs(
                _configurationSmoothingTarget.Value.Temperature
                    - _configurationSmoothingSource.Value.Temperature
            );

            var temperatureStep = Math.Max(
                temperatureDelta
                    / _settingsService.ConfigurationSmoothingMaxDuration.TotalSeconds
                    * _updateConfigurationTimer.Interval.TotalSeconds,
                30
            );

            CurrentConfiguration = CurrentConfiguration.StepTo(
                TargetConfiguration,
                temperatureStep,
                brightnessStep
            );
        }
        else
        {
            CurrentConfiguration = TargetConfiguration;
            _configurationSmoothingSource = null;
            _configurationSmoothingTarget = null;
        }

        _gammaService.SetGamma(CurrentConfiguration);
    }

    private void UpdateIsPaused()
    {
        // Pause while the screen is locked
        if (_settingsService.IsPauseWhenScreenLockedEnabled && InputDesktop.IsLocked())
        {
            IsPaused = true;
            _isPausedForLockedScreen = true;
            return;
        }

        // Resume right after unlocking. The checks below may not change the paused state
        // on their own (e.g. if the desktop or taskbar is focused right after unlocking).
        if (_isPausedForLockedScreen)
        {
            _isPausedForLockedScreen = false;
            IsPaused = false;
        }

        // Pause on fullscreen applications
        if (
            _settingsService.IsPauseWhenFullScreenEnabled
            && _externalApplicationService.IsForegroundApplicationFullScreen()
        )
        {
            IsPaused = true;
            return;
        }

        // Ignored or unidentifiable foreground applications should not affect whitelist pausing.
        // This prevents brief flashes when switching from a fullscreen/whitelisted application
        // to an ignored application (e.g., taskbar), and back.
        // https://github.com/Tyrrrz/LightBulb/issues/451
        var foregroundApplication = _externalApplicationService.TryGetForegroundApplication();
        if (foregroundApplication is null)
            return;

        // Pause on whitelisted applications
        if (
            _settingsService.IsApplicationWhitelistEnabled
            && _settingsService.WhitelistedApplications is not null
            && _settingsService.WhitelistedApplications.Contains(foregroundApplication)
        )
        {
            IsPaused = true;
            return;
        }

        IsPaused = false;
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
