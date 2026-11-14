using System;
using LightBulb.Core;
using LightBulb.Utils.Extensions;

namespace LightBulb.Utils;

/// <summary>
/// Gradually moves a color configuration towards its target, so that the change completes
/// within the configured maximum duration (or faster, for small changes).
/// </summary>
public class ConfigurationSmoother(ColorConfiguration initial)
{
    private ColorConfiguration? _source;
    private ColorConfiguration? _target;

    public ColorConfiguration Current { get; private set; } = initial;

    public ColorConfiguration Step(
        ColorConfiguration target,
        bool isSmooth,
        TimeSpan maxDuration,
        TimeSpan interval
    )
    {
        if (!isSmooth || Current == target || maxDuration.TotalSeconds < 0.1)
        {
            Current = target;
            _source = null;
            _target = null;

            return Current;
        }

        // Check if the target has changed since the last transition started
        if (_target != target || _source is null)
        {
            _source = Current;
            _target = target;
        }

        var brightnessDelta = Math.Abs(_target.Value.Brightness - _source.Value.Brightness);
        var brightnessStep = Math.Max(
            brightnessDelta / maxDuration.TotalSeconds * interval.TotalSeconds,
            0.08
        );

        var temperatureDelta = Math.Abs(_target.Value.Temperature - _source.Value.Temperature);
        var temperatureStep = Math.Max(
            temperatureDelta / maxDuration.TotalSeconds * interval.TotalSeconds,
            30
        );

        Current = Current.StepTo(target, temperatureStep, brightnessStep);
        return Current;
    }
}
