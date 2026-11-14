using System.Collections.Generic;

namespace LightBulb.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> EnglishLocalization =
        new Dictionary<string, string>
        {
            // Dashboard
            [nameof(SunsetLabel)] = "Sunset",
            [nameof(SunriseLabel)] = "Sunrise",
            [nameof(BedtimeLabel)] = "Bedtime",
            [nameof(DayStateLabel)] = "Day",
            [nameof(EveningStateLabel)] = "Evening",
            [nameof(NightStateLabel)] = "Night",
            [nameof(SunriseTransitionPhaseLabel)] = "Sunrise transition",
            [nameof(SunsetTransitionPhaseLabel)] = "Sunset transition",
            [nameof(NightTransitionPhaseLabel)] = "Bedtime transition",
            // Status sentence: {0} is the temperature, {1} the brightness (in percent), {2} the target stage
            [nameof(StatusDayScheduleText)] =
                "Daytime schedule active at {0} K and {1}% brightness.",
            [nameof(StatusEveningScheduleText)] =
                "Evening schedule active at {0} K and {1}% brightness.",
            [nameof(StatusNightScheduleText)] =
                "Night schedule active at {0} K and {1}% brightness.",
            [nameof(StatusWarmingText)] = "Warming to {0} K {2}.",
            [nameof(StatusCoolingText)] = "Cooling to {0} K {2}.",
            [nameof(StatusDimmingText)] = "Dimming to {1}% {2}.",
            [nameof(StatusBrighteningText)] = "Brightening to {1}% {2}.",
            [nameof(StatusWarmingDimmingText)] = "Warming to {0} K and dimming to {1}% {2}.",
            [nameof(StatusWarmingBrighteningText)] =
                "Warming to {0} K and brightening to {1}% {2}.",
            [nameof(StatusCoolingDimmingText)] = "Cooling to {0} K and dimming to {1}% {2}.",
            [nameof(StatusCoolingBrighteningText)] =
                "Cooling to {0} K and brightening to {1}% {2}.",
            [nameof(StatusForDayText)] = "for day",
            [nameof(StatusForEveningText)] = "for evening",
            [nameof(StatusForNightText)] = "for night",
            [nameof(StatusManualOverrideText)] =
                "Manual override active at {0} K and {1}% brightness.",
            [nameof(StatusPausedRemoteStreamingText)] =
                "Paused for remote streaming with native color restored.",
            [nameof(StatusPausedFullScreenText)] =
                "Paused while a fullscreen application is active.",
            [nameof(StatusPausedScreenLockedText)] = "Paused while the screen is locked.",
            [nameof(StatusPausedManuallyText)] =
                "Paused manually with automatic schedule suspended.",
            [nameof(StatusPausedAutomaticallyText)] =
                "Paused automatically with native color restored.",
            [nameof(StatusPreviewText)] = "Previewing the next 24 hours…",
            [nameof(PausedRemoteStreamingLabel)] = "Paused (Remote streaming)",
            [nameof(PausedScreenLockedLabel)] = "Paused (Screen locked)",
            [nameof(PausedFullScreenLabel)] = "Paused (Fullscreen app)",
            [nameof(PausedWhitelistLabel)] = "Paused (Whitelisted app)",
            [nameof(PausedCaption)] = "{0} · {1} K ({2} brightness)",
            [nameof(SolarEventPastText)] = "{0}: {1} ago",
            [nameof(SolarEventFutureText)] = "{0}: in {1}",
            [nameof(SolarEventNowText)] = "{0}: now",
            [nameof(DurationMinuteText)] = "1 minute",
            [nameof(DurationMinutesText)] = "{0} minutes",
            [nameof(DurationHourText)] = "1 hour",
            [nameof(DurationHoursText)] = "{0} hours",
            [nameof(ActivePhaseCaption)] = "Active phase: {0} · {1} K ({2} brightness)",
            [nameof(ActiveStageTemperatureTooltip)] =
                "Drag to adjust the color temperature of the current stage ({0})",
            [nameof(ManualScheduleText)] = "Manual schedule",
            [nameof(LocationNotSetText)] = "Location: not set",
            [nameof(LocationCoordinatesText)] = "Location: {0}",
            [nameof(OpenLocationSettingsTooltip)] = "Change location settings",
            [nameof(OffsetTooltipHeader)] =
                "Current temperature and brightness values are adjusted by an offset:",
            [nameof(TemperatureOffsetLabel)] = "Temperature offset:",
            [nameof(BrightnessOffsetLabel)] = "Brightness offset:",
            [nameof(ClickToResetLabel)] = "Click to reset",
            [nameof(OffsetLabel)] = "offset",
            // Main window
            [nameof(ToggleLightBulbTooltip)] = "Toggle LightBulb on/off",
            [nameof(HideToTrayTooltip)] = "Hide LightBulb to the system tray",
            [nameof(PreviewText)] = "PREVIEW",
            [nameof(StopPreviewTooltip)] = "Stop preview",
            [nameof(StartPreviewTooltip)] = "Preview 24-hour cycle",
            [nameof(OpenSettingsTooltip)] = "Open settings",
            // Settings dialog
            [nameof(ResetTooltip)] = "Reset all settings to their defaults",
            // Settings tabs
            // Advanced settings tab
            [nameof(LanguageLabel)] = "Language",
            [nameof(LanguageTooltip)] = "Preferred user interface language",
            [nameof(StartWithWindowsLabel)] = "Start with Windows",
            [nameof(StartWithWindowsTooltip)] = "Launch LightBulb at Windows startup",
            [nameof(DefaultToDayConfigLabel)] = "Default to day-time configuration",
            [nameof(DefaultToDayConfigTooltip)] =
                "When LightBulb is disabled or paused, restore the configured day-time temperature and brightness instead of the default monitor gamma",
            [nameof(PauseWhenFullscreenLabel)] = "Pause when fullscreen",
            [nameof(PauseWhenFullscreenTooltip)] =
                "Pause LightBulb when any fullscreen window is in the foreground",
            [nameof(GammaSmoothingLabel)] = "Gamma smoothing",
            [nameof(GammaSmoothingTooltip)] =
                "Transition slowly when enabling or disabling LightBulb to give time for eyes to adjust",
            [nameof(GammaPollingLabel)] = "Gamma polling",
            [nameof(GammaPollingTooltip)] =
                "Force-refresh monitor gamma at regular intervals to prevent other programs from overriding it",
            // General settings tab
            [nameof(DayTemperatureTooltip)] = "Color temperature during the day",
            [nameof(NightTemperatureTooltip)] = "Color temperature during the night",
            [nameof(DayBrightnessTooltip)] = """
                Brightness during the day

                Note that this brightness setting applies to the color gamma, not to the actual brightness of the monitor.
                If your computer is already capable of auto-adjusting screen brightness based on lighting conditions (common for laptops), then it's recommended to disable LightBulb's brightness control by keeping both brightness settings at 100%.
                """,
            [nameof(NightBrightnessTooltip)] = """
                Brightness during the night

                Note that this brightness setting applies to the color gamma, not to the actual brightness of the monitor.
                If your computer is already capable of auto-adjusting screen brightness based on lighting conditions (common for laptops), then it's recommended to disable LightBulb's brightness control by keeping both brightness settings at 100%.
                """,
            [nameof(TransitionDurationTooltip)] =
                "Duration of time it takes to switch between day-time and night-time configurations",
            [nameof(TransitionOffsetTooltip)] =
                "Offset that specifies how early or late the transition starts, relative to the sunrise and sunset",
            [nameof(EveningStageTooltip)] =
                "Use an intermediate evening configuration from sunset until the night start time, instead of switching straight to the night-time configuration at sunset",
            [nameof(EveningTemperatureTooltip)] =
                "Color temperature between the sunset and the night start time",
            [nameof(EveningBrightnessTooltip)] = """
                Brightness between the sunset and the night start time

                Note that this brightness setting applies to the color gamma, not to the actual brightness of the monitor.
                """,
            [nameof(NightStartTimeTooltip)] = """
                Time when the transition from the evening configuration to the night-time configuration begins (adjusted by the transition offset, the same way as the sunset)

                If the night starts before the sunset transition has finished, the evening stage is skipped for that day.
                """,
            // Location settings tab
            [nameof(ManualLabel)] = "Manual",
            [nameof(ManualTooltip)] = "Configure sunrise and sunset manually",
            [nameof(LocationBasedLabel)] = "Location-based",
            [nameof(LocationBasedTooltip)] =
                "Configure your location and use it to automatically calculate the sunrise and sunset times",
            [nameof(YourLocationLabel)] = "Your location:",
            [nameof(AutoDetectLocationTooltip)] =
                "Try to detect the location automatically based on your IP address",
            [nameof(LocationQueryTooltip)] = """
                Specify your location using geographic coordinates or a search query

                Examples of valid inputs:
                **41.25, -120.9762**
                **41.25°N, 120.9762°W**
                **New York, USA**
                **Germany**
                """,
            [nameof(SetLocationTooltip)] = "Set location",
            [nameof(LocationErrorText)] = "Error resolving location, try again",
            // Hot key settings tab
            [nameof(ToggleLightBulbHotkeyLabel)] = "Toggle LightBulb",
            [nameof(ToggleLightBulbHotkeyTooltip)] = "Global hotkey to toggle LightBulb on/off",
            [nameof(ToggleWindowLabel)] = "Toggle window",
            [nameof(ToggleWindowHotkeyTooltip)] =
                "Global hotkey to show/hide LightBulb's main window",
            [nameof(IncreaseTemperatureOffsetLabel)] = "Temperature offset ↑",
            [nameof(IncreaseTemperatureOffsetTooltip)] =
                "Global hotkey to increase the current temperature offset",
            [nameof(DecreaseTemperatureOffsetLabel)] = "Temperature offset ↓",
            [nameof(DecreaseTemperatureOffsetTooltip)] =
                "Global hotkey to decrease the current temperature offset",
            [nameof(IncreaseBrightnessOffsetLabel)] = "Brightness offset ↑",
            [nameof(IncreaseBrightnessOffsetTooltip)] =
                "Global hotkey to increase the current brightness offset",
            [nameof(DecreaseBrightnessOffsetLabel)] = "Brightness offset ↓",
            [nameof(DecreaseBrightnessOffsetTooltip)] =
                "Global hotkey to decrease the current brightness offset",
            [nameof(ResetOffsetLabel)] = "Reset offset",
            [nameof(ResetOffsetHotkeyTooltip)] =
                "Global hotkey to reset the current temperature and brightness offsets",
            // Application whitelist settings tab
            [nameof(AppWhitelistLabel)] = "Application whitelist",
            [nameof(RefreshAppsTooltip)] = "Refresh running applications",
            [nameof(PauseForWhitelistedTooltip)] =
                "Pause LightBulb when one of the selected applications is in the foreground",
            // Tray icon context menu
            [nameof(TraySettingsMenuItem)] = "Settings",
            [nameof(TrayDisableUntilSunriseMenuItem)] = "Until sunrise",
            [nameof(TrayExitMenuItem)] = "Exit",
            [nameof(TrayTooltipDisabled)] = "Disabled",
            // Dialog messages
            [nameof(CloseButton)] = "CLOSE",
            [nameof(UnstableBuildTitle)] = "Unstable build warning",
            [nameof(UnstableBuildMessage)] = """
                You're using a development build of {0}. These builds are not thoroughly tested and may contain bugs.

                Auto-updates are disabled for development builds. If you want to switch to a stable release, please download it manually.
                """,
            [nameof(SeeReleasesButton)] = "SEE RELEASES",
            [nameof(LimitedGammaRangeTitle)] = "Limited gamma range",
            [nameof(LimitedGammaRangeMessage)] = """
                {0} has detected that extended gamma range controls are not enabled on this system.
                This may cause some color configurations to not work correctly.

                Press FIX to unlock the gamma range. Administrator privileges may be required.
                """,
            [nameof(FixButton)] = "FIX",
            [nameof(WelcomeTitle)] = "Welcome!",
            [nameof(WelcomeMessage)] = """
                Thank you for installing {0}!
                To get the most personalized experience, please set your preferred solar configuration.

                Press OK to open settings.
                """,
            [nameof(OkButton)] = "OK",
            // Settings page
            [nameof(BackTooltip)] = "Back to the dashboard (changes are saved)",
            [nameof(CycleTabName)] = "Cycle & Colors",
            [nameof(ScheduleTabName)] = "Schedule & Location",
            [nameof(RulesTabName)] = "Rules & Whitelist",
            [nameof(ControlsTabName)] = "Controls & App",
            // Cycle & colors
            [nameof(DayCardSubtitle)] = "Sunrise to sunset",
            [nameof(EveningCardSubtitle)] = "Sunset to bedtime",
            [nameof(NightCardSubtitle)] = "Bedtime to sunrise",
            [nameof(TemperatureLabel)] = "Temperature",
            [nameof(BrightnessLabel)] = "Brightness",
            [nameof(BedtimeNightStartLabel)] = "Bedtime (night start)",
            [nameof(TransitionsCardTitle)] = "Transitions",
            [nameof(DurationLabel)] = "Duration",
            [nameof(TransitionOffsetShortLabel)] = "Offset",
            // Schedule & location
            [nameof(LocationCardTitle)] = "Location & Coordinates",
            [nameof(LocationCardSubtitle)] =
                "Used to calculate sunrise and sunset. Search for a place, enter coordinates, or detect it automatically.",
            [nameof(LocationInUseText)] = "Using {0}, {1}",
            [nameof(SolarScheduleCardTitle)] = "Solar Schedule",
            [nameof(SolarScheduleCardSubtitle)] =
                "Calculate sunrise and sunset from your location, or set them manually.",
            [nameof(OffLabel)] = "Off",
            // Rules & whitelist
            [nameof(AutoPauseCardTitle)] = "Smart Auto-Pause",
            [nameof(AutoPauseCardSubtitle)] =
                "Temporarily restore normal colors in these situations.",
            [nameof(PauseWhenScreenLockedLabel)] = "Pause when the screen is locked",
            [nameof(PauseWhenScreenLockedTooltip)] =
                "Pause LightBulb while the workstation is locked",
            [nameof(WhitelistCardSubtitle)] =
                "Pause LightBulb while any of the selected programs is in the foreground.",
            [nameof(PerMonitorFullscreenPauseLabel)] = "Pause only the monitor running fullscreen",
            [nameof(PerMonitorFullscreenPauseTooltip)] =
                "When a fullscreen application is in the foreground, restore normal colors only on the display that runs it, and keep the other displays on the cycle",
            [nameof(StreamingAutoPauseLabel)] = "Pause during remote streaming (Sunshine / RDP)",
            [nameof(StreamingAutoPauseTooltip)] =
                "Restore normal colors on all displays while this computer is being streamed with Sunshine or accessed with Remote Desktop",
            [nameof(StreamingPortsLabel)] = "Streaming TCP ports",
            [nameof(StreamingPortsTooltip)] =
                "Remote Desktop is detected automatically, on any port. Sunshine and other streaming apps are detected by an active connection on one of these local TCP ports, separated by commas (e.g. 48010 for Sunshine, or 48010, 27036 to also include Steam Remote Play).",
            [nameof(DisplaysCardTitle)] = "Connected Displays",
            [nameof(DisplaysCardSubtitle)] =
                "Exclude displays or fine-tune their warmth individually. Displays are recognized again when reconnected.",
            [nameof(IdentifyButton)] = "IDENTIFY",
            [nameof(IdentifyTooltip)] = "Show each display's number on its screen",
            [nameof(OffsetScalingLabel)] = "Offset scaling",
            [nameof(OffsetScalingTooltip)] =
                "How each display's temperature offset is applied as the cycle moves from day to night",
            [nameof(PerceptualOffsetScalingOption)] = "Mireds (Perceptual)",
            [nameof(FixedOffsetScalingOption)] = "Kelvin (Fixed)",
            [nameof(PerceptualOffsetScalingDescription)] =
                "Temperature offsets are exact at your day temperature and scale perceptually toward evening and night, so different panel types stay visually matched.",
            [nameof(FixedOffsetScalingDescription)] =
                "Temperature offsets are applied as the same fixed number of Kelvin at all times, without day-to-night scaling.",
            [nameof(NoDisplaysText)] = "No displays detected.",
            [nameof(PrimaryBadge)] = "PRIMARY",
            [nameof(DisplayEnabledTooltip)] =
                "Apply LightBulb to this display. When excluded, the display is kept at neutral colors.",
            [nameof(MonitorTemperatureOffsetLabel)] = "Temperature offset",
            [nameof(MonitorTemperatureOffsetTooltip)] =
                "Shifts the color temperature on this display only. With perceptual scaling, this is the shift at your day temperature.",
            [nameof(MonitorBrightnessOffsetLabel)] = "Brightness offset",
            [nameof(MonitorBrightnessOffsetTooltip)] =
                "Added to the current brightness on this display only",
            [nameof(ResetOffsetsTooltip)] = "Reset offsets",
            [nameof(ReferenceDisplayNote)] =
                "Reference display · Follows main Cycle & Colors schedule",
            [nameof(EffectiveNowText)] = "Effective now: {0} K · {1}",
            [nameof(ExcludedNeutralText)] = "Excluded · Neutral 6600 K",
            [nameof(DisconnectedDisplaysLabel)] = "Saved disconnected displays ({0})",
            [nameof(ForgetButton)] = "FORGET",
            [nameof(ForgetDisplayTooltip)] = "Remove the saved preferences for this display",
            // Controls & app
            [nameof(HotkeysCardTitle)] = "Global Hotkeys",
            [nameof(HotkeysCardSubtitle)] =
                "Click a field and press a key combination to assign it.",
            [nameof(SystemPreferencesCardTitle)] = "System Preferences",
            [nameof(StartMinimizedLabel)] = "Start minimized",
            [nameof(StartMinimizedTooltip)] =
                "Hide LightBulb to the system tray when it starts, even when launched manually",
            [nameof(ExtendedGammaRangeLabel)] = "Extended gamma range",
            [nameof(ExtendedGammaRangeTooltip)] =
                "Unlock the full gamma range in Windows, which allows warmer color temperatures and lower brightness. Changing this requires administrator privileges.",
            [nameof(GammaPollingIntervalLabel)] = "Refresh every",
            [nameof(GammaPollingIntervalTooltip)] =
                "How often gamma is force-refreshed when gamma polling is enabled",
            [nameof(SecondsValueText)] = "{0} s",
            // Tray menu
            [nameof(TrayOpenMenuItem)] = "Open LightBulb",
            [nameof(TrayPauseMenuItem)] = "Pause",
            [nameof(TrayResumeMenuItem)] = "Resume",
            [nameof(TrayPreviewMenuItem)] = "Preview 24h cycle",
            [nameof(PauseFor15MinutesChip)] = "15 min",
            [nameof(PauseFor1HourChip)] = "1 hr",
            [nameof(PauseFor3HoursChip)] = "3 hrs",
            [nameof(PausedStateLabel)] = "Paused",
        };
}
