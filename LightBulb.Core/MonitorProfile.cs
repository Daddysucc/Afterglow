using System;
using System.Text.Json.Serialization;

namespace LightBulb.Core;

/// <summary>
/// Per-display preferences, matched to a display by its persistent device path.
/// </summary>
public record MonitorProfile
{
    public const int MaxTemperatureOffset = 1000;
    public const int TemperatureOffsetStep = 50;
    public const double MaxBrightnessOffset = 0.25;
    public const double BrightnessOffsetStep = 0.01;

    // Persistent hardware identifier, used to recognize the display when it's reconnected
    public string DevicePath { get; init; } = "";

    // Shown for displays that are currently disconnected
    public string LastKnownName { get; init; } = "";

    // When disabled (excluded), the display is always kept at neutral colors
    public bool IsEnabled { get; init; } = true;

    // Added to the target color temperature, in Kelvin
    public int TemperatureOffset { get; init; }

    // Added to the target brightness
    public double BrightnessOffset { get; init; }

    [JsonIgnore]
    public bool HasOffsets => TemperatureOffset != 0 || Math.Abs(BrightnessOffset) >= 0.005;

    // Halfway values round away from zero (e.g. 125 K becomes 150 K), rather than to an even step
    public static int NormalizeTemperatureOffset(double offset) =>
        (int)(
            Math.Round(
                Math.Clamp(offset, -MaxTemperatureOffset, MaxTemperatureOffset)
                    / TemperatureOffsetStep,
                MidpointRounding.AwayFromZero
            ) * TemperatureOffsetStep
        );

    public static double NormalizeBrightnessOffset(double offset) =>
        Math.Round(
            Math.Round(
                Math.Clamp(offset, -MaxBrightnessOffset, MaxBrightnessOffset)
                    / BrightnessOffsetStep,
                MidpointRounding.AwayFromZero
            ) * BrightnessOffsetStep,
            2
        );
}
