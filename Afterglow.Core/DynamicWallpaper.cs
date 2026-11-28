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

    /// <summary>
    /// Gets the sky for the given elevation of the sun under the given weather: the clear sky,
    /// greyer and flatter the cloudier it is (with less of the glow), darker the harder it rains,
    /// paler the harder it snows, and washed out towards a single grey in fog.
    /// </summary>
    public static WallpaperPalette GetPalette(double sunElevation, SkyConditions conditions)
    {
        if (conditions.IsClear)
            return GetPalette(sunElevation);

        var levels = GetLevels(sunElevation);
        ApplyWeather(levels, conditions, sunElevation);

        WallpaperColor ColorAt(int i) =>
            WallpaperColor.From(levels[i], levels[i + 1], levels[i + 2]);

        return new WallpaperPalette(
            ColorAt(0),
            ColorAt(3),
            ColorAt(6),
            ColorAt(9),
            levels[12] / 255
        );
    }

    // Changes the clear sky's levels (see GetLevels) for the weather. Every part grows smoothly
    // from nothing, so a clear sky stays exactly as it is, and a little weather changes it a
    // little:
    // - clouds turn the colors towards a grey of the same brightness (slightly cool, as clouds
    //   are) and hide the glow on the horizon: low ones the most, and high, thin ones only soften
    //   and whiten the sky (and leave the glow, which lights them up at sunset)
    // - rain and storms darken it, and snow makes it paler (snow clouds are pale even at night)
    // - fog washes the sky's colors out towards one another
    // - haze (smoke and pollution) whitens the sky and reddens the glow
    // - dust turns it a dusty tan
    // - high up (where there's less air overhead), the sky above is a deeper blue by day
    private static void ApplyWeather(double[] levels, SkyConditions conditions, double sunElevation)
    {
        static double Part(double value, double power = 1) =>
            Math.Pow(Math.Clamp(double.IsFinite(value) ? value : 0, 0, 1), power);

        var low = Part(conditions.LowClouds, 1.3);
        var mid = Part(conditions.MidClouds, 1.3);
        var high = Part(conditions.HighClouds, 1.3);
        var rain = Part(conditions.Rain);
        var snow = Part(conditions.Snow);
        var storm = Part(conditions.Storm);
        var fog = Part(conditions.Fog);
        var haze = Part(conditions.Haze);
        var dust = Part(conditions.Dust);

        // (only by day: from the end of civil twilight to the sun being well up)
        var altitude = Part(conditions.Altitude) * Math.Clamp((sunElevation + 6) / 16, 0, 1);

        // (each layer covers what the ones before it leave)
        var grey = 1 - (1 - 0.8 * low) * (1 - 0.6 * mid) * (1 - 0.25 * high) * (1 - 0.05 * snow);
        var brightness = Math.Max(
            1 - 0.15 * low - 0.08 * mid - 0.2 * rain - 0.25 * storm + 0.15 * snow,
            0.3
        );
        var lift = 14 * snow + 10 * fog + 6 * high + 8 * haze;
        var glow = Math.Max(
            (1 - 0.75 * low)
                * (1 - 0.5 * mid)
                * (1 - 0.15 * rain)
                * (1 - 0.5 * fog)
                * (1 + 0.3 * haze),
            0.05
        );

        // The four colors (the top, the middle, the horizon and the glow)
        for (var i = 0; i < 12; i += 3)
        {
            var (r, g, b) = (levels[i], levels[i + 1], levels[i + 2]);
            var luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;

            double Tint(double value, double cool) =>
                (value + (luminance * cool - value) * grey) * brightness;

            (r, g, b) = (Tint(r, 0.97), Tint(g, 0.99), Tint(b, 1.06));

            // Dust: towards a tan of the same brightness
            r += (luminance * 1.2 - r) * 0.75 * dust;
            g += (luminance * 1.0 - g) * 0.75 * dust;
            b += (luminance * 0.7 - b) * 0.75 * dust;

            (levels[i], levels[i + 1], levels[i + 2]) = (r, g, b);
        }

        // Altitude: the top (and a little of the middle) darker in red and green than in blue,
        // and the horizon a little less washed out
        foreach (var (i, amount) in new[] { (0, 1.0), (3, 0.5), (6, 0.25) })
        {
            levels[i] *= 1 - 0.25 * altitude * amount;
            levels[i + 1] *= 1 - 0.2 * altitude * amount;
            levels[i + 2] *= 1 - 0.05 * altitude * amount;
        }

        // Haze: the sky (not the glow) less blue, and the glow redder
        for (var i = 0; i < 9; i += 3)
            levels[i + 2] -= levels[i + 2] * 0.15 * haze;
        levels[9] += 25 * haze;
        levels[11] -= 30 * haze;

        // Fog: the top, the middle and the horizon towards their average
        for (var channel = 0; channel < 3; channel++)
        {
            var average = (levels[channel] + levels[3 + channel] + levels[6 + channel]) / 3;
            for (var i = channel; i < 9; i += 3)
                levels[i] += (average - levels[i]) * 0.6 * fog;
        }

        for (var i = 0; i < 12; i++)
            levels[i] = Math.Clamp(levels[i] + lift, 0, 255);

        levels[12] = Math.Clamp(levels[12] * glow, 0, 255);
    }

    // Above and below these, the sky doesn't change
    private static double HighestStop => Skies[^1].Elevation;
    private static double LowestStop => Skies[0].Elevation;

    /// <summary>
    /// The number of clear skies drawn ahead of time (see <see cref="GetStops" />).
    /// </summary>
    public const int StopCount = 48;

    private static readonly (double[] Stops, int Step) ClearSkies = CreateStops(StopCount);

    private static double[] Stops => ClearSkies.Stops;

    /// <summary>
    /// How much the clear sky changes from one stop to the next (in levels of the color channel
    /// that changes the most, see <see cref="GetDifference" />): small enough to be hard to notice.
    /// Every other change of the wallpaper (with the weather, or catching up) is kept to steps of
    /// this size as well.
    /// </summary>
    public static int StepSize => ClearSkies.Step;

    /// <summary>
    /// The sun's elevations (from the lowest to the highest) that the wallpaper's clear skies are
    /// drawn for, spaced so that the sky changes by about as much from each one to the next: closer
    /// together around sunrise and sunset, where the sky changes quickly, and further apart high
    /// in the day. Morning and evening share them, as the sky looks the same at the same height.
    /// </summary>
    public static IReadOnlyList<double> GetStops() => Stops;

    private static (double[] Stops, int Step) CreateStops(int count)
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

        return (stops, (int)Math.Ceiling(distance / (count - 1)));
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

    // The sun's elevation (in degrees) below which the day's accent color gives way to the
    // golden hour's, around sunrise and sunset
    private const double GoldenHourElevation = 6;

    /// <summary>
    /// Gets the part of the day that the accent color shows: the night after bedtime, the golden
    /// hour around sunrise and sunset (and through the evening until bedtime), and the day.
    /// </summary>
    public static AccentTime GetAccentTime(CycleStage stage, double sunElevation) =>
        stage switch
        {
            CycleStage.Night => AccentTime.Night,
            CycleStage.Evening => AccentTime.GoldenHour,
            _ when sunElevation < GoldenHourElevation => AccentTime.GoldenHour,
            _ => AccentTime.Day,
        };

    /// <summary>
    /// Gets the accent color for a part of the day: a color of the wallpaper's sky at that time
    /// (the day's blue, the glow of the sunset, and the night's deep blue, which is muted so
    /// that it stays out of the way at night).
    /// </summary>
    public static WallpaperColor GetAccentColor(AccentTime time) =>
        time switch
        {
            AccentTime.Night => GetPalette(LowestStop).SkyMiddle,
            AccentTime.GoldenHour => GetPalette(0).Glow,
            _ => GetPalette(HighestStop).SkyMiddle,
        };

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
    /// Gets how high the sun actually is (in degrees, negative below the horizon) at the given
    /// location and instant, as seen from there (with the bending of light near the horizon, which
    /// is why the sun is up at sunrise).
    /// </summary>
    /// <remarks>
    /// NOAA's solar position equations: accurate to well under a degree for centuries around now,
    /// at the cost of a few dozen arithmetic operations. Unlike the smooth arc between sunrise and
    /// sunset (see the other overload), it follows the real sun through twilight, and also near
    /// the poles, where the sun may not rise or set at all.
    /// </remarks>
    public static double GetSunElevation(GeoLocation location, DateTimeOffset instant)
    {
        static double Rad(double degrees) => degrees * Math.PI / 180;
        static double Deg(double radians) => radians * 180 / Math.PI;

        var utc = instant.UtcDateTime;
        var julianDay = utc.ToOADate() + 2415018.5;
        var t = (julianDay - 2451545) / 36525;

        // The sun's position along its path through the year
        var meanLongitude = (280.46646 + t * (36000.76983 + t * 0.0003032)) % 360;
        var meanAnomaly = 357.52911 + t * (35999.05029 - 0.0001537 * t);
        var eccentricity = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);
        var center =
            Math.Sin(Rad(meanAnomaly)) * (1.914602 - t * (0.004817 + 0.000014 * t))
            + Math.Sin(Rad(2 * meanAnomaly)) * (0.019993 - 0.000101 * t)
            + Math.Sin(Rad(3 * meanAnomaly)) * 0.000289;
        var omega = 125.04 - 1934.136 * t;
        var apparentLongitude = meanLongitude + center - 0.00569 - 0.00478 * Math.Sin(Rad(omega));

        // How far north or south of the equator it is
        var meanObliquity =
            23 + (26 + (21.448 - t * (46.815 + t * (0.00059 - t * 0.001813))) / 60) / 60;
        var obliquity = meanObliquity + 0.00256 * Math.Cos(Rad(omega));
        var declination = Math.Asin(Math.Sin(Rad(obliquity)) * Math.Sin(Rad(apparentLongitude)));

        // How far the sun is from noon where the user is (in minutes, with the equation of time)
        var y = Math.Pow(Math.Tan(Rad(obliquity / 2)), 2);
        var equationOfTime =
            4
            * Deg(
                y * Math.Sin(2 * Rad(meanLongitude))
                    - 2 * eccentricity * Math.Sin(Rad(meanAnomaly))
                    + 4
                        * eccentricity
                        * y
                        * Math.Sin(Rad(meanAnomaly))
                        * Math.Cos(2 * Rad(meanLongitude))
                    - 0.5 * y * y * Math.Sin(4 * Rad(meanLongitude))
                    - 1.25 * eccentricity * eccentricity * Math.Sin(2 * Rad(meanAnomaly))
            );
        var solarMinutes =
            ((utc.TimeOfDay.TotalMinutes + equationOfTime + 4 * location.Longitude) % 1440 + 1440)
            % 1440;
        var hourAngle = solarMinutes / 4 - 180;

        var latitude = Rad(location.Latitude);
        var cosZenith =
            Math.Sin(latitude) * Math.Sin(declination)
            + Math.Cos(latitude) * Math.Cos(declination) * Math.Cos(Rad(hourAngle));
        var elevation = 90 - Deg(Math.Acos(Math.Clamp(cosZenith, -1, 1)));

        // The bending of light near the horizon (Sæmundsson's formula), which lifts the sun by
        // about half a degree as it rises and sets (and not at all once it's well below)
        if (elevation > -1)
            elevation += 1.02 / Math.Tan(Rad(elevation + 10.3 / (elevation + 5.11))) / 60;

        return elevation;
    }

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

/// <summary>
/// The parts of the day that the accent color follows (see
/// <see cref="DynamicWallpaper.GetAccentTime" />).
/// </summary>
public enum AccentTime
{
    Day,
    GoldenHour,
    Night,
}
