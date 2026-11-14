using System;

namespace LightBulb.Core;

/// <summary>
/// Effective transition times for a single day of the cycle, after any overlapping
/// transitions have been resolved.
/// </summary>
/// <remarks>
/// The cycle goes: night → (sunrise) → day → (sunset) → evening → (night start) → night.
/// When the evening stage is not included, the sunset transition goes straight to night,
/// and both night transition times are equal to <see cref="SunsetEnd" />.
/// </remarks>
public readonly record struct CycleSchedule(
    TimeOnly SunriseStart,
    TimeOnly SunriseEnd,
    TimeOnly SunsetStart,
    TimeOnly SunsetEnd,
    TimeOnly NightTransitionStart,
    TimeOnly NightTransitionEnd,
    bool IsEveningIncluded
);
