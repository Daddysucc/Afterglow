using System;

namespace Afterglow.Core;

/// <summary>
/// The color that white takes on at a given color temperature, as Afterglow shows it on screens.
/// </summary>
public static class WhitePoint
{
    /// <summary>
    /// Gets the red, green, and blue levels (0 to 1, as sent to a display) of white at the
    /// given color temperature.
    /// </summary>
    // Algorithm taken from http://tannerhelland.com/4435/convert-temperature-rgb-algorithm-code
    public static (double Red, double Green, double Blue) GetRgb(double kelvin)
    {
        var red =
            kelvin > 6600
                ? Math.Clamp(Math.Pow(kelvin / 100 - 60, -0.1332047592) * 329.698727446 / 255, 0, 1)
                : 1;

        var green =
            kelvin > 6600
                ? Math.Clamp(
                    Math.Pow(kelvin / 100 - 60, -0.0755148492) * 288.1221695283 / 255,
                    0,
                    1
                )
                : Math.Clamp((Math.Log(kelvin / 100) * 99.4708025861 - 161.1195681661) / 255, 0, 1);

        var blue =
            kelvin >= 6600 ? 1
            : kelvin <= 1900 ? 0
            : Math.Clamp(
                (Math.Log(kelvin / 100 - 10) * 138.5177312231 - 305.0447927307) / 255,
                0,
                1
            );

        return (red, green, blue);
    }

    /// <summary>
    /// Gets how much light (relative luminance, 0 to 1) a typical (sRGB) screen gives off for
    /// white at the given color temperature, compared to its untinted white. Warm tints cut the
    /// green and blue channels, so a warm screen is also a dimmer one.
    /// </summary>
    public static double GetLuminance(double kelvin)
    {
        var (red, green, blue) = GetRgb(kelvin);

        return Math.Clamp(
            0.2126 * Math.Pow(red, 2.2)
                + 0.7152 * Math.Pow(green, 2.2)
                + 0.0722 * Math.Pow(blue, 2.2),
            0,
            1
        );
    }

    /// <summary>
    /// Gets the CIE 1931 chromaticity of white at the given color temperature, as a typical
    /// (sRGB) screen shows it.
    /// </summary>
    public static (double X, double Y) GetChromaticity(double kelvin)
    {
        var (red, green, blue) = GetRgb(kelvin);

        // Levels sent to a display are gamma-encoded
        var r = Math.Pow(red, 2.2);
        var g = Math.Pow(green, 2.2);
        var b = Math.Pow(blue, 2.2);

        var x = 0.4124 * r + 0.3576 * g + 0.1805 * b;
        var y = 0.2126 * r + 0.7152 * g + 0.0722 * b;
        var z = 0.0193 * r + 0.1192 * g + 0.9505 * b;
        var sum = x + y + z;

        return (x / sum, y / sum);
    }
}
