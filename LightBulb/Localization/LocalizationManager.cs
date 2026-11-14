using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using LightBulb.Services;
using PowerKit;
using PowerKit.Extensions;

namespace LightBulb.Localization;

public partial class LocalizationManager : ObservableObject, IDisposable
{
    private readonly IDisposable _eventSubscription;

    public LocalizationManager(SettingsService settingsService)
    {
        _eventSubscription = Disposable.Merge(
            settingsService.WatchProperty(o => o.Language, v => Language = v, true),
            this.WatchProperty(
                o => o.Language,
                _ =>
                {
                    foreach (var propertyName in EnglishLocalization.Keys)
                        OnPropertyChanged(propertyName);
                }
            )
        );
    }

    [ObservableProperty]
    public partial Language Language { get; set; } = Language.System;

    private string Get([CallerMemberName] string? key = null) =>
        string.IsNullOrWhiteSpace(key) ? string.Empty : Resolve(GetLocalization(Language), key);

    // Translations for the given language (or, for the system language, the one that matches the
    // current UI culture). Keys that a translation doesn't have fall back to English.
    internal static IReadOnlyDictionary<string, string> GetLocalization(Language language) =>
        language switch
        {
            Language.System =>
                CultureInfo.CurrentUICulture.ThreeLetterISOLanguageName.ToLowerInvariant() switch
                {
                    "ukr" => UkrainianLocalization,
                    "deu" => GermanLocalization,
                    "fra" => FrenchLocalization,
                    "spa" => SpanishLocalization,
                    "zho"
                        when CultureInfo
                            .CurrentUICulture.GetSelfAndParents()
                            .Any(c =>
                                string.Equals(c.Name, "zh-Hans", StringComparison.OrdinalIgnoreCase)
                            ) => ChineseSimplifiedLocalization,
                    "zho"
                        when CultureInfo
                            .CurrentUICulture.GetSelfAndParents()
                            .Any(c =>
                                string.Equals(c.Name, "zh-Hant", StringComparison.OrdinalIgnoreCase)
                            ) => ChineseTraditionalLocalization,
                    _ => EnglishLocalization,
                },
            Language.Ukrainian => UkrainianLocalization,
            Language.German => GermanLocalization,
            Language.French => FrenchLocalization,
            Language.Spanish => SpanishLocalization,
            Language.ChineseSimplified => ChineseSimplifiedLocalization,
            Language.ChineseTraditional => ChineseTraditionalLocalization,
            _ => EnglishLocalization,
        };

    internal static IReadOnlyDictionary<string, string> EnglishStrings => EnglishLocalization;

    // Falls back to English for keys that are missing (or blank) in the given translation,
    // so that a partially translated language never shows empty labels
    internal static string Resolve(IReadOnlyDictionary<string, string> localization, string key)
    {
        if (localization.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;

        if (EnglishLocalization.TryGetValue(key, out value))
            return value;

        return $"Missing localization for '{key}'";
    }

    public void Dispose() => _eventSubscription.Dispose();
}

public partial class LocalizationManager
{
    // ---- Dashboard ----

    public string SunsetLabel => Get();
    public string SunriseLabel => Get();
    public string BedtimeLabel => Get();
    public string DayStateLabel => Get();
    public string EveningStateLabel => Get();
    public string NightStateLabel => Get();
    public string SunriseTransitionPhaseLabel => Get();
    public string SunsetTransitionPhaseLabel => Get();
    public string NightTransitionPhaseLabel => Get();
    public string StatusDayScheduleText => Get();
    public string StatusEveningScheduleText => Get();
    public string StatusNightScheduleText => Get();
    public string StatusWarmingText => Get();
    public string StatusCoolingText => Get();
    public string StatusDimmingText => Get();
    public string StatusBrighteningText => Get();
    public string StatusWarmingDimmingText => Get();
    public string StatusWarmingBrighteningText => Get();
    public string StatusCoolingDimmingText => Get();
    public string StatusCoolingBrighteningText => Get();
    public string StatusForDayText => Get();
    public string StatusForEveningText => Get();
    public string StatusForNightText => Get();
    public string StatusManualOverrideText => Get();
    public string StatusPausedRemoteStreamingText => Get();
    public string StatusPausedFullScreenText => Get();
    public string StatusPausedScreenLockedText => Get();
    public string StatusPausedManuallyText => Get();
    public string StatusPausedAutomaticallyText => Get();
    public string StatusPreviewText => Get();
    public string PausedRemoteStreamingLabel => Get();
    public string PausedScreenLockedLabel => Get();
    public string PausedFullScreenLabel => Get();
    public string PausedWhitelistLabel => Get();
    public string PausedCaption => Get();
    public string SolarEventPastText => Get();
    public string SolarEventFutureText => Get();
    public string SolarEventNowText => Get();
    public string DurationMinuteText => Get();
    public string DurationMinutesText => Get();
    public string DurationHourText => Get();
    public string DurationHoursText => Get();
    public string ActivePhaseCaption => Get();
    public string ActiveStageTemperatureTooltip => Get();
    public string ManualScheduleText => Get();
    public string LocationNotSetText => Get();
    public string LocationCoordinatesText => Get();
    public string OpenLocationSettingsTooltip => Get();
    public string OffsetTooltipHeader => Get();
    public string TemperatureOffsetLabel => Get();
    public string BrightnessOffsetLabel => Get();
    public string ClickToResetLabel => Get();
    public string OffsetLabel => Get();

    // ---- Main window ----

    public string ToggleLightBulbTooltip => Get();
    public string HideToTrayTooltip => Get();
    public string PreviewText => Get();
    public string StopPreviewTooltip => Get();
    public string StartPreviewTooltip => Get();
    public string OpenSettingsTooltip => Get();

    // ---- Settings dialog ----

    public string ResetTooltip => Get();

    // ---- Settings tabs ----

    // ---- Advanced settings tab ----

    public string LanguageLabel => Get();
    public string LanguageTooltip => Get();
    public string StartWithWindowsLabel => Get();
    public string StartWithWindowsTooltip => Get();
    public string DefaultToDayConfigLabel => Get();
    public string DefaultToDayConfigTooltip => Get();
    public string PauseWhenFullscreenLabel => Get();
    public string PauseWhenFullscreenTooltip => Get();
    public string GammaSmoothingLabel => Get();
    public string GammaSmoothingTooltip => Get();
    public string GammaPollingLabel => Get();
    public string GammaPollingTooltip => Get();

    // ---- General settings tab ----

    public string DayTemperatureTooltip => Get();
    public string NightTemperatureTooltip => Get();
    public string DayBrightnessTooltip => Get();
    public string NightBrightnessTooltip => Get();
    public string TransitionDurationTooltip => Get();
    public string TransitionOffsetTooltip => Get();
    public string EveningStageTooltip => Get();
    public string EveningTemperatureTooltip => Get();
    public string EveningBrightnessTooltip => Get();
    public string NightStartTimeTooltip => Get();

    // ---- Location settings tab ----

    public string ManualLabel => Get();
    public string ManualTooltip => Get();
    public string LocationBasedLabel => Get();
    public string LocationBasedTooltip => Get();
    public string YourLocationLabel => Get();
    public string AutoDetectLocationTooltip => Get();
    public string LocationQueryTooltip => Get();
    public string SetLocationTooltip => Get();
    public string LocationErrorText => Get();

    // ---- Hot key settings tab ----

    public string ToggleLightBulbHotkeyLabel => Get();
    public string ToggleLightBulbHotkeyTooltip => Get();
    public string ToggleWindowLabel => Get();
    public string ToggleWindowHotkeyTooltip => Get();
    public string IncreaseTemperatureOffsetLabel => Get();
    public string IncreaseTemperatureOffsetTooltip => Get();
    public string DecreaseTemperatureOffsetLabel => Get();
    public string DecreaseTemperatureOffsetTooltip => Get();
    public string IncreaseBrightnessOffsetLabel => Get();
    public string IncreaseBrightnessOffsetTooltip => Get();
    public string DecreaseBrightnessOffsetLabel => Get();
    public string DecreaseBrightnessOffsetTooltip => Get();
    public string ResetOffsetLabel => Get();
    public string ResetOffsetHotkeyTooltip => Get();

    // ---- Application whitelist settings tab ----

    public string AppWhitelistLabel => Get();
    public string RefreshAppsTooltip => Get();
    public string PauseForWhitelistedTooltip => Get();

    // ---- Tray icon context menu ----

    public string TraySettingsMenuItem => Get();
    public string TrayDisableUntilSunriseMenuItem => Get();
    public string TrayExitMenuItem => Get();

    public string TrayTooltipDisabled => Get();

    // ---- Dialog messages ----

    public string CloseButton => Get();
    public string UnstableBuildTitle => Get();
    public string UnstableBuildMessage => Get();
    public string SeeReleasesButton => Get();
    public string LimitedGammaRangeTitle => Get();
    public string LimitedGammaRangeMessage => Get();
    public string FixButton => Get();
    public string WelcomeTitle => Get();
    public string WelcomeMessage => Get();
    public string OkButton => Get();

    // ---- Settings page ----

    public string BackTooltip => Get();
    public string CycleTabName => Get();
    public string ScheduleTabName => Get();
    public string RulesTabName => Get();
    public string ControlsTabName => Get();

    // Cycle & colors
    public string DayCardSubtitle => Get();
    public string EveningCardSubtitle => Get();
    public string NightCardSubtitle => Get();
    public string TemperatureLabel => Get();
    public string BrightnessLabel => Get();
    public string BedtimeNightStartLabel => Get();
    public string TransitionsCardTitle => Get();
    public string DurationLabel => Get();
    public string TransitionOffsetShortLabel => Get();

    // Schedule & location
    public string LocationCardTitle => Get();
    public string LocationCardSubtitle => Get();
    public string LocationInUseText => Get();
    public string SolarScheduleCardTitle => Get();
    public string SolarScheduleCardSubtitle => Get();
    public string OffLabel => Get();

    // Rules & whitelist
    public string AutoPauseCardTitle => Get();
    public string AutoPauseCardSubtitle => Get();
    public string PauseWhenScreenLockedLabel => Get();
    public string PauseWhenScreenLockedTooltip => Get();
    public string WhitelistCardSubtitle => Get();
    public string PerMonitorFullscreenPauseLabel => Get();
    public string PerMonitorFullscreenPauseTooltip => Get();
    public string StreamingAutoPauseLabel => Get();
    public string StreamingAutoPauseTooltip => Get();
    public string StreamingPortsLabel => Get();
    public string StreamingPortsTooltip => Get();
    public string DisplaysCardTitle => Get();
    public string DisplaysCardSubtitle => Get();
    public string IdentifyButton => Get();
    public string IdentifyTooltip => Get();
    public string OffsetScalingLabel => Get();
    public string OffsetScalingTooltip => Get();
    public string PerceptualOffsetScalingOption => Get();
    public string FixedOffsetScalingOption => Get();
    public string PerceptualOffsetScalingDescription => Get();
    public string FixedOffsetScalingDescription => Get();
    public string NoDisplaysText => Get();
    public string PrimaryBadge => Get();
    public string DisplayEnabledTooltip => Get();
    public string MonitorTemperatureOffsetLabel => Get();
    public string MonitorTemperatureOffsetTooltip => Get();
    public string MonitorBrightnessOffsetLabel => Get();
    public string MonitorBrightnessOffsetTooltip => Get();
    public string ResetOffsetsTooltip => Get();
    public string ReferenceDisplayNote => Get();
    public string EffectiveNowText => Get();
    public string ExcludedNeutralText => Get();
    public string DisconnectedDisplaysLabel => Get();
    public string ForgetButton => Get();
    public string ForgetDisplayTooltip => Get();

    // Controls & app
    public string HotkeysCardTitle => Get();
    public string HotkeysCardSubtitle => Get();
    public string SystemPreferencesCardTitle => Get();
    public string StartMinimizedLabel => Get();
    public string StartMinimizedTooltip => Get();
    public string ExtendedGammaRangeLabel => Get();
    public string ExtendedGammaRangeTooltip => Get();
    public string GammaPollingIntervalLabel => Get();
    public string GammaPollingIntervalTooltip => Get();
    public string SecondsValueText => Get();

    // ---- Tray menu ----

    public string TrayOpenMenuItem => Get();
    public string TrayPauseMenuItem => Get();
    public string TrayResumeMenuItem => Get();
    public string TrayPreviewMenuItem => Get();
    public string PauseFor15MinutesChip => Get();
    public string PauseFor1HourChip => Get();
    public string PauseFor3HoursChip => Get();
    public string PausedStateLabel => Get();
}
