using System;
using System.Globalization;
using LightBulb.Core;
using LightBulb.Localization;

namespace LightBulb.Utils;

/// <summary>
/// Everything that the dashboard's status sentence describes.
/// </summary>
/// <param name="IsPreviewing">Whether the 24-hour preview is running.</param>
/// <param name="IsEnabled">Whether LightBulb is enabled (i.e. not paused manually).</param>
/// <param name="PauseReason">Why all displays are paused automatically, if they are.</param>
/// <param name="IsManualOverride">Whether the temperature or brightness is adjusted manually (e.g. with hotkeys).</param>
/// <param name="CurrentConfiguration">Configuration being applied, including manual adjustments.</param>
/// <param name="Phase">Current position within the cycle.</param>
/// <param name="IsEveningIncluded">Whether today's cycle includes the evening stage.</param>
/// <param name="IsGrayscaleActive">Whether the screen is grayscale right now.</param>
/// <param name="IsGrayscaleTemperatureShiftEnabled">Whether the color temperature still applies in grayscale.</param>
/// <param name="IsDefaultToDayConfigurationEnabled">Whether pauses restore the day configuration (rather than native colors).</param>
public readonly record struct StatusSentenceContext(
    bool IsPreviewing,
    bool IsEnabled,
    PauseReason PauseReason,
    bool IsManualOverride,
    ColorConfiguration CurrentConfiguration,
    CyclePhase Phase,
    bool IsEveningIncluded,
    ColorConfiguration DayConfiguration,
    ColorConfiguration EveningConfiguration,
    ColorConfiguration NightConfiguration,
    bool IsGrayscaleActive = false,
    bool IsGrayscaleTemperatureShiftEnabled = true,
    bool IsDefaultToDayConfigurationEnabled = false,
    TimeSpan? GrayscaleSnoozeRemaining = null,
    bool IsGrayscaleHeldUntilScheduleChange = false
);

/// <summary>
/// Builds the plain-language sentence that states what LightBulb is doing right now.
/// </summary>
public static class StatusSentenceBuilder
{
    private enum Trend
    {
        Unchanged,
        Up,
        Down,
    }

    private static string FormatTemperature(double temperature) =>
        temperature.ToString("F0", CultureInfo.CurrentCulture);

    // As a number of percent, so that each language can place the percent sign as it should
    private static string FormatBrightness(double brightness) =>
        (brightness * 100).ToString("F0", CultureInfo.CurrentCulture);

    // Values are compared as they're shown, so that a change too small to see isn't mentioned
    private static Trend GetTrend(double from, double to) =>
        Math.Round(to).CompareTo(Math.Round(from)) switch
        {
            > 0 => Trend.Up,
            < 0 => Trend.Down,
            _ => Trend.Unchanged,
        };

    private static ColorConfiguration GetStageConfiguration(
        in StatusSentenceContext context,
        CycleStage stage
    ) =>
        stage switch
        {
            CycleStage.Day => context.DayConfiguration,
            CycleStage.Evening => context.EveningConfiguration,
            _ => context.NightConfiguration,
        };

    // The stage that the cycle is coming from, when transitioning into the given one
    private static CycleStage GetPreviousStage(CycleStage stage, bool isEveningIncluded) =>
        stage switch
        {
            CycleStage.Day => CycleStage.Night,
            CycleStage.Evening => CycleStage.Day,
            _ => isEveningIncluded ? CycleStage.Evening : CycleStage.Day,
        };

    private static string GetScheduleTemplate(CycleStage stage, LocalizationManager localization) =>
        stage switch
        {
            CycleStage.Day => localization.StatusDayScheduleText,
            CycleStage.Evening => localization.StatusEveningScheduleText,
            _ => localization.StatusNightScheduleText,
        };

    private static string GetTargetPhrase(CycleStage stage, LocalizationManager localization) =>
        stage switch
        {
            CycleStage.Day => localization.StatusForDayText,
            CycleStage.Evening => localization.StatusForEveningText,
            _ => localization.StatusForNightText,
        };

    // Templates use {0} for the temperature, {1} for the brightness, and {2} for the target stage
    private static string? TryGetTransitionTemplate(
        Trend temperature,
        Trend brightness,
        LocalizationManager localization
    ) =>
        (temperature, brightness) switch
        {
            (Trend.Down, Trend.Down) => localization.StatusWarmingDimmingText,
            (Trend.Down, Trend.Up) => localization.StatusWarmingBrighteningText,
            (Trend.Up, Trend.Down) => localization.StatusCoolingDimmingText,
            (Trend.Up, Trend.Up) => localization.StatusCoolingBrighteningText,
            (Trend.Down, Trend.Unchanged) => localization.StatusWarmingText,
            (Trend.Up, Trend.Unchanged) => localization.StatusCoolingText,
            (Trend.Unchanged, Trend.Down) => localization.StatusDimmingText,
            (Trend.Unchanged, Trend.Up) => localization.StatusBrighteningText,
            _ => null,
        };

    private static string Format(
        string template,
        ColorConfiguration configuration,
        string target = ""
    ) =>
        string.Format(
            CultureInfo.CurrentCulture,
            template,
            FormatTemperature(configuration.Temperature),
            FormatBrightness(configuration.Brightness),
            target
        );

    // Time left, rounded up so that it never reads "0 minutes" (e.g. "28 minutes", "1 hour")
    internal static string FormatDuration(TimeSpan duration, LocalizationManager localization)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes));
        if (minutes < 60)
        {
            return minutes == 1
                ? localization.DurationMinuteText
                : string.Format(
                    CultureInfo.CurrentCulture,
                    localization.DurationMinutesText,
                    minutes
                );
        }

        var hours = (int)Math.Round(minutes / 60.0);
        return hours == 1
            ? localization.DurationHourText
            : string.Format(CultureInfo.CurrentCulture, localization.DurationHoursText, hours);
    }

    public static string Build(in StatusSentenceContext context, LocalizationManager localization)
    {
        if (context.IsPreviewing)
            return localization.StatusPreviewText;

        if (!context.IsEnabled)
            return localization.StatusPausedManuallyText;

        // Pauses restore either native colors or, if configured so, the day configuration
        switch (context.PauseReason)
        {
            case PauseReason.RemoteStreaming:
                return context.IsDefaultToDayConfigurationEnabled
                    ? localization.StatusPausedRemoteStreamingDayText
                    : localization.StatusPausedRemoteStreamingText;
            case PauseReason.FullScreen:
                return localization.StatusPausedFullScreenText;
            case PauseReason.ScreenLocked:
                return localization.StatusPausedScreenLockedText;
            case PauseReason.Whitelist:
                return context.IsDefaultToDayConfigurationEnabled
                    ? localization.StatusPausedAutomaticallyDayText
                    : localization.StatusPausedAutomaticallyText;
        }

        // Grayscale is the most visible change, so it's mentioned ahead of manual adjustments
        // (whose values are included) and the schedule
        if (context.IsGrayscaleActive)
        {
            return context.IsGrayscaleTemperatureShiftEnabled
                ? Format(localization.StatusGrayscaleText, context.CurrentConfiguration)
                : localization.StatusGrayscaleNeutralText;
        }

        if (context.GrayscaleSnoozeRemaining is { } remaining)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                localization.StatusGrayscaleSnoozedText,
                FormatDuration(remaining, localization)
            );
        }

        if (context.IsGrayscaleHeldUntilScheduleChange)
            return localization.StatusGrayscaleHeldText;

        if (context.IsManualOverride)
            return Format(localization.StatusManualOverrideText, context.CurrentConfiguration);

        var stage = context.Phase.Stage;
        var target = GetStageConfiguration(context, stage);

        if (context.Phase.IsTransitioning)
        {
            var from = GetStageConfiguration(
                context,
                GetPreviousStage(stage, context.IsEveningIncluded)
            );

            var template = TryGetTransitionTemplate(
                GetTrend(from.Temperature, target.Temperature),
                GetTrend(from.Brightness * 100, target.Brightness * 100),
                localization
            );

            // Only mention what's actually changing; if nothing is, describe the stage itself
            if (template is not null)
                return Format(template, target, GetTargetPhrase(stage, localization));
        }

        return Format(GetScheduleTemplate(stage, localization), target);
    }
}
