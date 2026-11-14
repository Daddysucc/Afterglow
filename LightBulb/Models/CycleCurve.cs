using System;
using System.Collections.Generic;
using LightBulb.Core;

namespace LightBulb.Models;

public enum CycleCurveSegmentKind
{
    Day,
    Evening,
    Night,
    Transition,
}

// Start and end are fractions of the day, where 0 is 00:00 and 1 is 24:00
public readonly record struct CycleCurveSegment(
    double Start,
    double End,
    CycleCurveSegmentKind Kind
);

/// <summary>
/// Color configuration over one day (00:00 to 24:00), sampled at regular intervals.
/// </summary>
public class CycleCurve(
    IReadOnlyList<ColorConfiguration> samples,
    IReadOnlyList<CycleCurveSegment> segments
)
{
    // Evenly spaced over the day, the first sample being at 00:00 and the last one at 24:00
    public IReadOnlyList<ColorConfiguration> Samples { get; } = samples;

    // Non-overlapping and sorted, covering the whole day
    public IReadOnlyList<CycleCurveSegment> Segments { get; } = segments;

    public ColorConfiguration GetValueAt(double fraction)
    {
        if (Samples.Count == 0)
            return ColorConfiguration.Default;

        var position = Math.Clamp(fraction, 0, 1) * (Samples.Count - 1);
        var index = (int)Math.Floor(position);

        if (index >= Samples.Count - 1)
            return Samples[^1];

        var weight = position - index;
        var a = Samples[index];
        var b = Samples[index + 1];

        return new ColorConfiguration(
            a.Temperature + (b.Temperature - a.Temperature) * weight,
            a.Brightness + (b.Brightness - a.Brightness) * weight
        );
    }

    public static IReadOnlyList<CycleCurveSegment> GetSegments(CycleSchedule schedule)
    {
        var segments = new List<CycleCurveSegment>();

        // Adds a segment between two times of day, splitting it in two if it wraps around midnight
        void Add(TimeOnly start, TimeOnly end, CycleCurveSegmentKind kind)
        {
            // Subtracting time-of-day values wraps around midnight
            var length = (end - start).TotalDays;
            if (length <= 0)
                return;

            var startFraction = start.ToTimeSpan().TotalDays;
            var endFraction = startFraction + length;

            if (endFraction <= 1)
            {
                segments.Add(new CycleCurveSegment(startFraction, endFraction, kind));
            }
            else
            {
                segments.Add(new CycleCurveSegment(startFraction, 1, kind));
                segments.Add(new CycleCurveSegment(0, endFraction - 1, kind));
            }
        }

        Add(schedule.SunriseStart, schedule.SunriseEnd, CycleCurveSegmentKind.Transition);
        Add(schedule.SunriseEnd, schedule.SunsetStart, CycleCurveSegmentKind.Day);
        Add(schedule.SunsetStart, schedule.SunsetEnd, CycleCurveSegmentKind.Transition);

        if (schedule.IsEveningIncluded)
        {
            Add(schedule.SunsetEnd, schedule.NightTransitionStart, CycleCurveSegmentKind.Evening);
            Add(
                schedule.NightTransitionStart,
                schedule.NightTransitionEnd,
                CycleCurveSegmentKind.Transition
            );
        }

        Add(schedule.NightTransitionEnd, schedule.SunriseStart, CycleCurveSegmentKind.Night);

        segments.Sort((a, b) => a.Start.CompareTo(b.Start));
        return segments;
    }
}
