using System;
using System.Collections.Generic;

namespace Afterglow.Core;

/// <summary>
/// A color of the dynamic wallpaper (0 to 255 each).
/// </summary>
public readonly record struct WallpaperColor(byte Red, byte Green, byte Blue)
{
    internal static WallpaperColor From(double red, double green, double blue) =>
        new(ToByte(red), ToByte(green), ToByte(blue));

    private static byte ToByte(double value) => (byte)Math.Round(Math.Clamp(value, 0, 255));

    internal WallpaperColor Lerp(WallpaperColor other, double amount) =>
        From(
            Red + (other.Red - Red) * amount,
            Green + (other.Green - Green) * amount,
            Blue + (other.Blue - Blue) * amount
        );

    internal WallpaperColor Scale(double factor) =>
        From(Red * factor, Green * factor, Blue * factor);

    /// <summary>
    /// The largest difference in any channel (0 to 255).
    /// </summary>
    public static int GetDifference(WallpaperColor a, WallpaperColor b) =>
        Math.Max(
            Math.Abs(a.Red - b.Red),
            Math.Max(Math.Abs(a.Green - b.Green), Math.Abs(a.Blue - b.Blue))
        );
}

/// <summary>
/// What the dynamic wallpaper looks like: a sky that fades from its top down to the horizon, and
/// a glow rising from the horizon around where the sun is.
/// </summary>
/// <param name="SkyTop">At the top of the image.</param>
/// <param name="SkyMiddle">A little over halfway down.</param>
/// <param name="SkyHorizon">At the bottom, under the glow.</param>
/// <param name="Glow">The color of the glow on the horizon.</param>
/// <param name="GlowStrength">How strongly the glow shows at the horizon (0 to 1).</param>
public readonly record struct WallpaperPalette(
    WallpaperColor SkyTop,
    WallpaperColor SkyMiddle,
    WallpaperColor SkyHorizon,
    WallpaperColor Glow,
    double GlowStrength
);

/// <summary>
/// The dynamic wallpaper: a natural-looking sky for where the sun is (the screen's tint then
/// warms and dims it like everything else on the screen), and an accent color in the exact
/// color and brightness that lights take on (for whatever picks up the accent color, like
/// Windows Dynamic Lighting).
/// </summary>
public static class DynamicWallpaper
{
    // The sky as the sun moves, by its elevation (in degrees): night, nautical twilight, civil
    // twilight, sunset (or sunrise), golden hour, afternoon (or morning), and high in the sky
    private static readonly (double Elevation, WallpaperPalette Palette)[] Skies =
    [
        (-18, new(new(6, 10, 34), new(17, 25, 84), new(36, 50, 156), new(36, 50, 156), 0)),
        (-12, new(new(16, 20, 52), new(36, 44, 108), new(80, 88, 160), new(120, 100, 150), 0.25)),
        (-6, new(new(34, 32, 62), new(72, 60, 108), new(132, 100, 138), new(210, 124, 80), 0.6)),
        (-2, new(new(58, 42, 66), new(104, 64, 86), new(196, 96, 70), new(236, 104, 40), 1)),
        (2, new(new(50, 52, 110), new(96, 90, 156), new(206, 156, 140), new(244, 160, 70), 0.9)),
        (10, new(new(58, 80, 192), new(92, 118, 236), new(166, 176, 240), new(244, 214, 168), 0.6)),
        (
            30,
            new(new(72, 110, 236), new(100, 140, 245), new(170, 190, 245), new(240, 230, 215), 0.3)
        ),
    ];

    // The accent color never goes fully dark, even when lights would be very dim
    private const double MinimumAccentBrightness = 0.08;

    // Used for the sun's path when no location is set (sunrise and sunset entered by hand)
    private const double DefaultLatitude = 45;

    /// <summary>
    /// Gets the sky for the given elevation of the sun (in degrees, negative below the horizon).
    /// </summary>
    public static WallpaperPalette GetPalette(double sunElevation)
    {
        if (sunElevation <= Skies[0].Elevation)
            return Skies[0].Palette;

        for (var i = 1; i < Skies.Length; i++)
        {
            var (elevation, palette) = Skies[i];
            if (sunElevation > elevation)
                continue;

            var (previousElevation, previous) = Skies[i - 1];
            var amount = (sunElevation - previousElevation) / (elevation - previousElevation);

            return new WallpaperPalette(
                previous.SkyTop.Lerp(palette.SkyTop, amount),
                previous.SkyMiddle.Lerp(palette.SkyMiddle, amount),
                previous.SkyHorizon.Lerp(palette.SkyHorizon, amount),
                previous.Glow.Lerp(palette.Glow, amount),
                previous.GlowStrength + (palette.GlowStrength - previous.GlowStrength) * amount
            );
        }

        return Skies[^1].Palette;
    }

    // Above and below these, the sky doesn't change
    private static double HighestStop => Skies[^1].Elevation;
    private static double LowestStop => Skies[0].Elevation;

    /// <summary>
    /// The number of skies drawn ahead of time (see <see cref="GetStops" />).
    /// </summary>
    public const int StopCount = 24;

    private static readonly double[] Stops = CreateStops(StopCount);

    /// <summary>
    /// The sun's elevations (from the lowest to the highest) that the wallpaper's skies are drawn
    /// for, spaced so that the sky changes by about as much from each one to the next: closer
    /// together around sunrise and sunset, where the sky changes quickly, and further apart high
    /// in the day. Morning and evening share them, as the sky looks the same at the same height.
    /// </summary>
    public static IReadOnlyList<double> GetStops() => Stops;

    private static double[] CreateStops(int count)
    {
        // How much the sky changes along the way, in small increments of elevation (measured
        // before rounding, so that rounding doesn't add up into uneven steps)
        const double increment = 0.01;
        var elevations = new List<double>();
        var distances = new List<double>();
        var distance = 0.0;
        var previous = GetLevels(LowestStop);

        for (var elevation = LowestStop; elevation <= HighestStop + 1e-9; elevation += increment)
        {
            var levels = GetLevels(elevation);
            var change = 0.0;
            for (var i = 0; i < levels.Length; i++)
                change = Math.Max(change, Math.Abs(levels[i] - previous[i]));

            distance += change;
            previous = levels;

            elevations.Add(elevation);
            distances.Add(distance);
        }

        // Evenly spaced along the way, including both ends
        var stops = new double[count];
        var j = 0;
        for (var i = 0; i < count; i++)
        {
            var target = distance * i / (count - 1);
            while (j < distances.Count - 1 && distances[j] < target)
                j++;

            stops[i] = Math.Round(elevations[j], 2);
        }

        stops[0] = LowestStop;
        stops[^1] = HighestStop;

        return stops;
    }

    // The sky's color levels (and the glow's strength, on the same scale), before rounding
    private static double[] GetLevels(double sunElevation)
    {
        var upper = Math.Clamp(
            Array.FindIndex(Skies, s => s.Elevation >= sunElevation),
            1,
            Skies.Length - 1
        );
        if (sunElevation > HighestStop)
            upper = Skies.Length - 1;

        var (fromElevation, from) = Skies[upper - 1];
        var (toElevation, to) = Skies[upper];
        var amount = Math.Clamp(
            (sunElevation - fromElevation) / (toElevation - fromElevation),
            0,
            1
        );

        var levels = new List<double>();
        foreach (
            var (a, b) in new[]
            {
                (from.SkyTop, to.SkyTop),
                (from.SkyMiddle, to.SkyMiddle),
                (from.SkyHorizon, to.SkyHorizon),
                (from.Glow, to.Glow),
            }
        )
        {
            levels.Add(a.Red + (b.Red - a.Red) * amount);
            levels.Add(a.Green + (b.Green - a.Green) * amount);
            levels.Add(a.Blue + (b.Blue - a.Blue) * amount);
        }

        levels.Add((from.GlowStrength + (to.GlowStrength - from.GlowStrength) * amount) * 255);

        return levels.ToArray();
    }

    /// <summary>
    /// Gets which of the stops (see <see cref="GetStops" />) is closest to the given elevation.
    /// </summary>
    public static int GetStopIndex(double sunElevation)
    {
        var best = 0;
        for (var i = 1; i < Stops.Length; i++)
        {
            if (Math.Abs(Stops[i] - sunElevation) < Math.Abs(Stops[best] - sunElevation))
                best = i;
        }

        return best;
    }

    /// <summary>
    /// Gets the accent color: the color that lights take on for the given screen color
    /// temperature, as bright as they'd be for the given screen brightness.
    /// </summary>
    public static WallpaperColor GetAccentColor(
        double screenKelvin,
        double screenBrightness,
        double warmth,
        double dimming
    )
    {
        var lightKelvin = 1_000_000 / HueLightController.ToLightMired(screenKelvin, warmth);
        var (red, green, blue) = WhitePoint.GetRgb(lightKelvin);

        var brightness = Math.Max(
            HueLightController.ToLightBrightness(screenBrightness, dimming, screenKelvin) / 100,
            MinimumAccentBrightness
        );

        return WallpaperColor.From(red * 255, green * 255, blue * 255).Scale(brightness);
    }

    /// <summary>
    /// Gets about how high the sun is (in degrees, negative below the horizon) at the given
    /// instant, following the given sunrise and sunset, and the latitude if it's known.
    /// </summary>
    /// <remarks>
    /// The sun follows a smooth arc that rises at sunrise and sets at sunset (as Afterglow has
    /// them, whether calculated or entered by hand), up to its highest point at noon for the
    /// latitude and the time of year, and down below the horizon at night at the same rate as it
    /// set, as far as it goes at that latitude (which sets how long the twilight lasts).
    /// </remarks>
    public static double GetSunElevation(
        SolarTimes solarTimes,
        DateTimeOffset instant,
        double? latitude
    )
    {
        var dayLength = Wrap(
            (solarTimes.Sunset.ToTimeSpan() - solarTimes.Sunrise.ToTimeSpan()).TotalHours
        );

        // The sun doesn't rise (or set) at all
        if (dayLength < 0.1)
            return Skies[0].Elevation - 1;
        if (dayLength > 23.9)
            return Skies[^1].Elevation;

        var nightLength = 24 - dayLength;

        // Highest and lowest points for the latitude and the time of year (the sun's declination)
        var lat = latitude ?? DefaultLatitude;
        var declination = 23.44 * Math.Sin(2 * Math.PI * (284 + instant.DayOfYear) / 365);
        var highest = Math.Max(90 - Math.Abs(lat - declination), 1);
        var lowest = Math.Max(90 - Math.Abs(lat + declination), 1);

        var sinceSunrise = Wrap((instant.TimeOfDay - solarTimes.Sunrise.ToTimeSpan()).TotalHours);

        if (sinceSunrise < dayLength)
            return highest * Math.Sin(Math.PI * sinceSunrise / dayLength);

        // At night, it goes down as fast as it set (but no lower than it can go)
        var depth = Math.Min(highest * nightLength / dayLength, lowest);

        return -depth * Math.Sin(Math.PI * (sinceSunrise - dayLength) / nightLength);
    }

    private static double Wrap(double hours) => ((hours % 24) + 24) % 24;

    /// <summary>
    /// How different two wallpapers look: the largest difference in any color channel (0 to 255),
    /// with the glow's strength counted on the same scale.
    /// </summary>
    public static int GetDifference(WallpaperPalette a, WallpaperPalette b) =>
        Math.Max(
            Math.Max(
                WallpaperColor.GetDifference(a.SkyTop, b.SkyTop),
                WallpaperColor.GetDifference(a.SkyMiddle, b.SkyMiddle)
            ),
            Math.Max(
                Math.Max(
                    WallpaperColor.GetDifference(a.SkyHorizon, b.SkyHorizon),
                    WallpaperColor.GetDifference(a.Glow, b.Glow)
                ),
                (int)Math.Round(Math.Abs(a.GlowStrength - b.GlowStrength) * 255)
            )
        );

    /// <summary>
    /// Gets a wallpaper part of the way from one to another, so that no color channel changes by
    /// more than the given step (or the target itself, if it's that close already).
    /// </summary>
    public static WallpaperPalette StepTowards(
        WallpaperPalette from,
        WallpaperPalette to,
        int maxStep
    )
    {
        var difference = GetDifference(from, to);
        if (difference <= maxStep)
            return to;

        var amount = (double)maxStep / difference;

        return new WallpaperPalette(
            from.SkyTop.Lerp(to.SkyTop, amount),
            from.SkyMiddle.Lerp(to.SkyMiddle, amount),
            from.SkyHorizon.Lerp(to.SkyHorizon, amount),
            from.Glow.Lerp(to.Glow, amount),
            from.GlowStrength + (to.GlowStrength - from.GlowStrength) * amount
        );
    }

    /// <summary>
    /// Gets a color part of the way from one to another, so that no channel changes by more than
    /// the given step (or the target itself, if it's that close already).
    /// </summary>
    public static WallpaperColor StepTowards(WallpaperColor from, WallpaperColor to, int maxStep)
    {
        var difference = WallpaperColor.GetDifference(from, to);

        return difference <= maxStep ? to : from.Lerp(to, (double)maxStep / difference);
    }
}
