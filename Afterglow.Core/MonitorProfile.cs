using System;
using System.Text.Json.Serialization;

namespace Afterglow.Core;

/// <summary>
/// Per-display preferences, matched to a display by its persistent device path.
/// </summary>
public record MonitorProfile
{
    // Enough to match very different panels (e.g. a TV next to an OLED monitor),
    // in steps fine enough for exact matching
    public const int MaxTemperatureOffset = 2000;
    public const int TemperatureOffsetStep = 25;
    public const double MaxBrightnessOffset = 0.25;
    public const double BrightnessOffsetStep = 0.01;

    // Properties are settable (rather than init-only) so that the source-generated JSON deserializer
    // keeps the default values below for properties that are missing from the settings file,
    // instead of resetting them to false / null. Profiles are still only changed with 'with'.

    // Persistent hardware identifier, used to recognize the display when it's reconnected
    public string DevicePath { get; set; } = "";

    // Shown for displays that are currently disconnected
    public string LastKnownName { get; set; } = "";

    // When disabled (excluded), the display is always kept at neutral colors
    public bool IsEnabled { get; set; } = true;

    // Added to the target color temperature, in Kelvin. When offsets are scaled perceptually,
    // this is the offset at the day temperature (see MonitorOffsetScaling).
    public int TemperatureOffset { get; set; }

    // Added to the target brightness
    public double BrightnessOffset { get; set; }

    [JsonIgnore]
    public bool HasOffsets => TemperatureOffset != 0 || Math.Abs(BrightnessOffset) >= 0.005;

    // Halfway values round away from zero (e.g. 12.5 K becomes 25 K), rather than to an even step
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
