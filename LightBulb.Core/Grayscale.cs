using System;

namespace LightBulb.Core;

/// <summary>
/// When grayscale turns on by itself.
/// </summary>
public enum GrayscaleSchedule
{
    /// <summary>Never; grayscale is only turned on manually.</summary>
    Off,

    /// <summary>During the night stage.</summary>
    NightOnly,

    /// <summary>During the evening and night stages.</summary>
    EveningAndNight,

    /// <summary>Always (during every stage).</summary>
    On,
}

/// <summary>
/// Decides whether grayscale should be on, combining its schedule with manual toggles.
/// </summary>
/// <remarks>
/// A manual toggle overrides the schedule until the schedule next turns grayscale on or off by
/// itself. For example, turning grayscale off during a scheduled night keeps it off until the
/// morning, and the next evening the schedule takes over again.
/// </remarks>
public class GrayscaleController
{
    // Neutral white point, at which color temperature leaves the screen unchanged
    public static double NeutralTemperature => ColorConfiguration.Default.Temperature;

    private bool? _lastScheduledState;

    /// <summary>
    /// Whether the schedule turns grayscale on and off by itself as the day goes on. Grayscale can
    /// then be disabled until the next schedule change; otherwise only for a while.
    /// </summary>
    public static bool HasScheduleChanges(GrayscaleSchedule schedule) =>
        schedule is GrayscaleSchedule.NightOnly or GrayscaleSchedule.EveningAndNight;

    /// <summary>
    /// Whether grayscale is temporarily disabled for a while (until the given time).
    /// </summary>
    public static bool IsSnoozed(DateTimeOffset? snoozedUntil, DateTimeOffset instant) =>
        snoozedUntil is { } until && instant < until;

    public static bool IsScheduled(GrayscaleSchedule schedule, CyclePhase phase) =>
        schedule switch
        {
            GrayscaleSchedule.NightOnly => phase.Stage == CycleStage.Night,
            GrayscaleSchedule.EveningAndNight => phase.Stage
                is CycleStage.Evening
                    or CycleStage.Night,
            GrayscaleSchedule.On => true,
            _ => false,
        };

    /// <summary>
    /// Returns the manual override that still applies: it's cleared once the schedule turns
    /// grayscale on or off by itself.
    /// </summary>
    public bool? UpdateOverride(bool isScheduled, bool? manualOverride)
    {
        var isScheduleChanged = _lastScheduledState is { } last && last != isScheduled;
        _lastScheduledState = isScheduled;

        return isScheduleChanged ? null : manualOverride;
    }

    /// <summary>
    /// Whether grayscale is wanted, regardless of whether LightBulb is paused.
    /// </summary>
    public static bool IsWanted(bool isScheduled, bool? manualOverride) =>
        manualOverride ?? isScheduled;

    /// <summary>
    /// Returns the manual override after toggling grayscale. A toggle that brings grayscale back
    /// in line with the schedule clears the override.
    /// </summary>
    public static bool? Toggle(bool isScheduled, bool? manualOverride)
    {
        var isWanted = !IsWanted(isScheduled, manualOverride);
        return isWanted == isScheduled ? null : isWanted;
    }

    /// <summary>
    /// Whether grayscale is actually applied. It's suspended while LightBulb is paused, and while
    /// any display is paused on its own (e.g. for a fullscreen game), since grayscale always
    /// applies to all displays at once.
    /// </summary>
    public static bool IsApplied(bool isWanted, bool isLightBulbActive, bool isAnyDisplayPaused) =>
        isWanted && isLightBulbActive && !isAnyDisplayPaused;

    /// <summary>
    /// Configuration to apply while grayscale may be active: either the scheduled one (which
    /// tints grayscale with the color temperature), or a neutral white point at the scheduled
    /// brightness.
    /// </summary>
    public static ColorConfiguration GetConfiguration(
        ColorConfiguration configuration,
        bool isGrayscaleApplied,
        bool isTemperatureShiftEnabled
    ) =>
        isGrayscaleApplied && !isTemperatureShiftEnabled
            ? configuration with
            {
                Temperature = NeutralTemperature,
            }
            : configuration;
}
