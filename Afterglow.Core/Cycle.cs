using System;
using Afterglow.Core.Utils.Extensions;

namespace Afterglow.Core;

public static class Cycle
{
    private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

    /// <summary>
    /// Longest transition duration that can be configured.
    /// </summary>
    public static TimeSpan MaxTransitionDuration { get; } = TimeSpan.FromHours(3);

    /// <summary>
    /// Limits a transition duration to the supported range (0 to 3 hours).
    /// </summary>
    public static TimeSpan ClampTransitionDuration(TimeSpan duration) =>
        duration < TimeSpan.Zero ? TimeSpan.Zero
        : duration > MaxTransitionDuration ? MaxTransitionDuration
        : duration;

    public static TimeOnly GetSunriseStart(
        TimeOnly sunrise,
        TimeSpan transitionDuration,
        double transitionOffset
    )
    {
        // Offset = 0; Start at (sunrise - transition)
        // Offset = 0.5; Start at (sunrise - transition/2)
        // Offset = 1; Start at (sunrise)

        var shift = transitionDuration * (1 - transitionOffset);
        return sunrise.Add(-shift);
    }

    public static TimeOnly GetSunriseEnd(
        TimeOnly sunrise,
        TimeSpan transitionDuration,
        double transitionOffset
    )
    {
        // Offset = 0; End at (sunrise)
        // Offset = 0.5; End at (sunrise + transition/2)
        // Offset = 1; End at (sunrise + transition)

        var shift = transitionDuration * transitionOffset;
        return sunrise.Add(shift);
    }

    public static TimeOnly GetSunsetStart(
        TimeOnly sunset,
        TimeSpan transitionDuration,
        double transitionOffset
    )
    {
        // Offset = 0; Start at (sunset)
        // Offset = 0.5; Start at (sunset - transition/2)
        // Offset = 1; Start at (sunset - transition)

        var shift = transitionDuration * transitionOffset;
        return sunset.Add(-shift);
    }

    public static TimeOnly GetSunsetEnd(
        TimeOnly sunset,
        TimeSpan transitionDuration,
        double transitionOffset
    )
    {
        // Offset = 0; End at (sunset + transition)
        // Offset = 0.5; End at (sunset + transition/2)
        // Offset = 1; End at (sunset)

        var shift = transitionDuration * (1 - transitionOffset);
        return sunset.Add(shift);
    }

    // Start is the time elapsed since the beginning of the cycle (i.e. the sunrise transition start)
    private readonly record struct Transition(
        TimeSpan Start,
        TimeSpan Duration,
        CycleStage Target,
        bool IsSunrise,
        bool IsSkipped
    );

    // Transitions are sorted chronologically, the first one always being the sunrise.
    // The cycle wraps around one day after its origin.
    private readonly record struct Plan(
        TimeOnly Origin,
        Transition[] Transitions,
        bool IsEveningIncluded
    );

    private static Plan GetPlan(
        SolarTimes solarTimes,
        TimeSpan transitionDuration,
        double transitionOffset,
        TimeOnly? nightStart
    )
    {
        // Transitions can't be negative or longer than the cycle itself
        var duration = TimeSpan.FromTicks(Math.Clamp(transitionDuration.Ticks, 0, OneDay.Ticks));

        var origin = GetSunriseStart(solarTimes.Sunrise, duration, transitionOffset);

        // Subtracting time-of-day values wraps around midnight, so positions are always within [0, 1 day)
        TimeSpan GetPosition(TimeOnly time) => time - origin;

        var sunsetStart = GetPosition(
            GetSunsetStart(solarTimes.Sunset, duration, transitionOffset)
        );

        // The night transition is positioned relative to the night start time the same way
        // the sunset transition is positioned relative to the sunset.
        var nightTransitionStart = nightStart is not null
            ? GetPosition(GetSunsetStart(nightStart.Value, duration, transitionOffset))
            : (TimeSpan?)null;

        // The evening stage only makes sense if the night starts after the sunset transition
        // has finished. Otherwise (e.g. night start is set earlier than the sunset), skip it
        // and transition straight from day to night instead.
        var isEveningIncluded = nightTransitionStart >= sunsetStart + duration;

        Transition[] transitions = isEveningIncluded
            ?
            [
                new(TimeSpan.Zero, duration, CycleStage.Day, true, false),
                new(sunsetStart, duration, CycleStage.Evening, false, false),
                new(nightTransitionStart!.Value, duration, CycleStage.Night, false, false),
            ]
            :
            [
                new(TimeSpan.Zero, duration, CycleStage.Day, true, false),
                new(sunsetStart, duration, CycleStage.Night, false, false),
            ];

        // A transition that would still be running when the next one starts is shortened,
        // so that it finishes exactly as the next one begins. This keeps the curve continuous
        // when transitions overlap (e.g. very short nights or a night start right before sunrise).
        // A transition left with no time at all is overridden by the next one and is skipped.
        for (var i = 0; i < transitions.Length; i++)
        {
            var nextStart = i + 1 < transitions.Length ? transitions[i + 1].Start : OneDay;
            var available = nextStart - transitions[i].Start;

            transitions[i] = transitions[i] with
            {
                Duration = available < duration ? available : duration,
                IsSkipped = available <= TimeSpan.Zero,
            };
        }

        return new Plan(origin, transitions, isEveningIncluded);
    }

    public static CycleSchedule GetSchedule(
        SolarTimes solarTimes,
        TimeSpan transitionDuration,
        double transitionOffset,
        TimeOnly? nightStart = null
    )
    {
        var plan = GetPlan(solarTimes, transitionDuration, transitionOffset, nightStart);

        TimeOnly GetStart(Transition transition) => plan.Origin.Add(transition.Start);
        TimeOnly GetEnd(Transition transition) =>
            plan.Origin.Add(transition.Start + transition.Duration);

        var sunrise = plan.Transitions[0];
        var sunset = plan.Transitions[1];

        // When there's no evening, collapse the night transition onto the end of the sunset
        var night = plan.IsEveningIncluded
            ? plan.Transitions[2]
            : sunset with
            {
                Start = sunset.Start + sunset.Duration,
                Duration = TimeSpan.Zero,
            };

        return new CycleSchedule(
            GetStart(sunrise),
            GetEnd(sunrise),
            GetStart(sunset),
            GetEnd(sunset),
            GetStart(night),
            GetEnd(night),
            plan.IsEveningIncluded
        );
    }

    // Finds the most recent transition that has started at the given instant, the one before it,
    // and how much time has elapsed since the former started
    private static (Transition Current, Transition Previous, TimeSpan Elapsed) Locate(
        Plan plan,
        DateTimeOffset instant
    )
    {
        var transitions = plan.Transitions;
        var position = instant.ToTimeOnly() - plan.Origin;

        // There's always at least one, because the first non-skipped transition starts at the origin
        var currentIndex = 0;
        for (var i = 0; i < transitions.Length; i++)
        {
            if (!transitions[i].IsSkipped && transitions[i].Start <= position)
                currentIndex = i;
        }

        // Find the transition before it, wrapping around to the end of the previous cycle.
        // The last transition is never skipped, so this always terminates.
        var previousIndex = currentIndex;
        do
        {
            previousIndex = (previousIndex - 1 + transitions.Length) % transitions.Length;
        } while (transitions[previousIndex].IsSkipped);

        var current = transitions[currentIndex];
        return (current, transitions[previousIndex], position - current.Start);
    }

    private static double Interpolate(
        Plan plan,
        double dayValue,
        double eveningValue,
        double nightValue,
        DateTimeOffset instant
    )
    {
        double GetValue(CycleStage stage) =>
            stage switch
            {
                CycleStage.Day => dayValue,
                CycleStage.Evening => eveningValue,
                _ => nightValue,
            };

        var (current, previous, elapsed) = Locate(plan, instant);
        var fromValue = GetValue(previous.Target);
        var toValue = GetValue(current.Target);

        // Transition has finished (this also covers zero-length transitions)
        if (elapsed >= current.Duration)
            return toValue;

        var progress = elapsed / current.Duration;

        // Sunrise eases in, sunset and night ease out
        return current.IsSunrise
            ? toValue + (fromValue - toValue) * Math.Cos(progress * Math.PI / 2)
            : fromValue + (toValue - fromValue) * Math.Sin(progress * Math.PI / 2);
    }

    /// <summary>
    /// Gets the stage the cycle is in (or transitioning into) at the given instant.
    /// Pass <c>null</c> as <paramref name="nightStart" /> for the classic two-stage cycle.
    /// </summary>
    public static CyclePhase GetPhase(
        SolarTimes solarTimes,
        TimeSpan transitionDuration,
        double transitionOffset,
        TimeOnly? nightStart,
        DateTimeOffset instant
    )
    {
        var plan = GetPlan(solarTimes, transitionDuration, transitionOffset, nightStart);
        var (current, _, elapsed) = Locate(plan, instant);

        return new CyclePhase(current.Target, elapsed < current.Duration);
    }

    private static ColorConfiguration InterpolateConfiguration(
        Plan plan,
        ColorConfiguration dayConfiguration,
        ColorConfiguration eveningConfiguration,
        ColorConfiguration nightConfiguration,
        DateTimeOffset instant
    ) =>
        new(
            Interpolate(
                plan,
                dayConfiguration.Temperature,
                eveningConfiguration.Temperature,
                nightConfiguration.Temperature,
                instant
            ),
            Interpolate(
                plan,
                dayConfiguration.Brightness,
                eveningConfiguration.Brightness,
                nightConfiguration.Brightness,
                instant
            )
        );

    /// <summary>
    /// Classic two-stage cycle: day → (sunset) → night → (sunrise) → day.
    /// </summary>
    public static ColorConfiguration InterpolateConfiguration(
        SolarTimes solarTimes,
        ColorConfiguration dayConfiguration,
        ColorConfiguration nightConfiguration,
        TimeSpan transitionDuration,
        double transitionOffset,
        DateTimeOffset instant
    ) =>
        InterpolateConfiguration(
            GetPlan(solarTimes, transitionDuration, transitionOffset, null),
            dayConfiguration,
            nightConfiguration,
            nightConfiguration,
            instant
        );

    /// <summary>
    /// Three-stage cycle: day → (sunset) → evening → (night start) → night → (sunrise) → day.
    /// If the night starts before the sunset transition has finished, the evening stage is skipped.
    /// </summary>
    public static ColorConfiguration InterpolateConfiguration(
        SolarTimes solarTimes,
        ColorConfiguration dayConfiguration,
        ColorConfiguration eveningConfiguration,
        ColorConfiguration nightConfiguration,
        TimeOnly nightStart,
        TimeSpan transitionDuration,
        double transitionOffset,
        DateTimeOffset instant
    ) =>
        InterpolateConfiguration(
            GetPlan(solarTimes, transitionDuration, transitionOffset, nightStart),
            dayConfiguration,
            eveningConfiguration,
            nightConfiguration,
            instant
        );
}
