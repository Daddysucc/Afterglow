using System;

namespace LightBulb.Core;

/// <summary>
/// How a display's temperature offset is applied as the cycle moves between temperatures.
/// </summary>
public enum MonitorOffsetScaling
{
    /// <summary>
    /// The offset is exact at the day temperature, and is applied as the same shift in mireds
    /// (1,000,000 / K) at any other temperature. Equal shifts in mireds look roughly equal to the eye,
    /// so a display that's matched to the reference display during the day stays matched at night.
    /// </summary>
    Perceptual,

    /// <summary>
    /// The offset is added as is, in Kelvin, at all times.
    /// </summary>
    Fixed,
}

public static class MonitorTemperatureOffset
{
    private const double MiredFactor = 1_000_000;

    /// <summary>
    /// Applies a display's temperature offset (in Kelvin) to the current temperature of the cycle.
    /// </summary>
    /// <param name="temperature">Current temperature of the cycle.</param>
    /// <param name="offset">Offset of the display, in Kelvin, as measured at the reference temperature.</param>
    /// <param name="scaling">How the offset is applied at temperatures other than the reference one.</param>
    /// <param name="referenceTemperature">Temperature at which the offset is exact (the day temperature).</param>
    /// <param name="minimumTemperature">Lowest temperature that can be applied.</param>
    /// <param name="maximumTemperature">Highest temperature that can be applied.</param>
    public static double Apply(
        double temperature,
        double offset,
        MonitorOffsetScaling scaling,
        double referenceTemperature,
        double minimumTemperature,
        double maximumTemperature
    )
    {
        var isPerceptual =
            offset != 0
            && scaling == MonitorOffsetScaling.Perceptual
            && double.IsFinite(referenceTemperature)
            && referenceTemperature > 0
            && temperature > 0;

        if (!isPerceptual)
            return Math.Clamp(temperature + offset, minimumTemperature, maximumTemperature);

        // What the offset means at the reference temperature
        var referenceTarget = Math.Clamp(
            referenceTemperature + offset,
            minimumTemperature,
            maximumTemperature
        );

        // Exact Kelvin math at the reference temperature itself (without any floating-point drift)
        if (temperature == referenceTemperature)
            return referenceTarget;

        // The same shift in mireds, applied to the current temperature
        var miredShift = MiredFactor / referenceTarget - MiredFactor / referenceTemperature;
        var mireds = MiredFactor / temperature + miredShift;

        // Fewer mireds than the maximum temperature has (including zero or negative) is beyond it
        if (mireds <= MiredFactor / maximumTemperature)
            return maximumTemperature;

        return Math.Clamp(MiredFactor / mireds, minimumTemperature, maximumTemperature);
    }
}
