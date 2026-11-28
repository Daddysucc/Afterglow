using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Afterglow.Core;

/// <summary>
/// The weather that the dynamic wallpaper's sky shows (see <see cref="DynamicWallpaper" />), each
/// part from 0 (none) to 1 (as much as the sky shows).
/// </summary>
/// <param name="LowClouds">Low clouds (grey, heavy, and hiding the glow of sunset).</param>
/// <param name="MidClouds">Clouds at middle height.</param>
/// <param name="HighClouds">High, thin clouds (which only soften and whiten the sky).</param>
/// <param name="Rain">How hard it rains (1 from about 4 mm an hour).</param>
/// <param name="Snow">How hard it snows (1 from about 2 cm an hour).</param>
/// <param name="Storm">How stormy it is (a thunderstorm, darker than its rain alone).</param>
/// <param name="Fog">How thick the fog is (1 from about 1 km of visibility or less).</param>
/// <param name="Haze">How hazy the air is (from fine particles, like smoke or pollution).</param>
/// <param name="Dust">How much dust there is in the air (as from a dust storm).</param>
/// <param name="Altitude">How high up the user is (from 0 up to about 500 m, to 1 at about 4 km),
/// where there's less air overhead: a deeper blue by day.</param>
public readonly record struct SkyConditions(
    double LowClouds = 0,
    double MidClouds = 0,
    double HighClouds = 0,
    double Rain = 0,
    double Snow = 0,
    double Storm = 0,
    double Fog = 0,
    double Haze = 0,
    double Dust = 0,
    double Altitude = 0
)
{
    /// <summary>
    /// A clear sky (the same as having no weather at all).
    /// </summary>
    public static SkyConditions Clear => default;

    internal double[] ToArray() =>
        [LowClouds, MidClouds, HighClouds, Rain, Snow, Storm, Fog, Haze, Dust, Altitude];

    internal static SkyConditions FromArray(double[] v) =>
        new(v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8], v[9]);

    public bool IsClear => ToArray().All(v => v <= 0);

    internal static SkyConditions Lerp(SkyConditions a, SkyConditions b, double amount)
    {
        var (from, to) = (a.ToArray(), b.ToArray());
        return FromArray(from.Select((v, i) => v + (to[i] - v) * amount).ToArray());
    }

    internal static SkyConditions Average(IReadOnlyList<SkyConditions> all)
    {
        var sum = new double[10];
        foreach (var conditions in all)
        {
            var values = conditions.ToArray();
            for (var i = 0; i < sum.Length; i++)
                sum[i] += values[i];
        }

        return FromArray(sum.Select(v => v / all.Count).ToArray());
    }

    /// <summary>
    /// Gets how high up the user is (see <see cref="Altitude" />) from the elevation of the ground
    /// (in meters), or 0 if it's unknown.
    /// </summary>
    public static double GetAltitude(double? elevation) =>
        elevation is { } meters && double.IsFinite(meters) && meters is >= -500 and <= 9000
            ? Math.Clamp((meters - 500) / 3500, 0, 1)
            : 0;

    /// <summary>
    /// Gets the conditions from what the weather service reports for a time. Anything it doesn't
    /// report there (not every value is available everywhere) is worked out from what it does
    /// report, as far as that goes (e.g. the clouds from the sunlight or the weather code), and is
    /// left out otherwise.
    /// </summary>
    public static SkyConditions FromReading(WeatherReading reading)
    {
        // (anything that can't be right counts as not reported)
        reading = reading.Sanitized();

        var code = reading.WeatherCode ?? -1;
        static double Clamp(double value) => Math.Clamp(value, 0, 1);

        // What the WMO weather code says (for whatever isn't reported as an amount)
        var (codeClouds, codeRain, codeSnow, codeStorm, codeFog) = code switch
        {
            0 => (0.0, 0.0, 0.0, 0.0, 0.0),
            1 => (0.25, 0, 0, 0, 0),
            2 => (0.5, 0, 0, 0, 0),
            3 => (1, 0, 0, 0, 0),
            // Fog, and fog that leaves rime
            45 or 48 => (1, 0, 0, 0, 1),
            // Drizzle (light, moderate, dense), and freezing drizzle
            51 => (1, 0.1, 0, 0, 0),
            53 => (1, 0.2, 0, 0, 0),
            55 => (1, 0.3, 0, 0, 0),
            56 => (1, 0.15, 0, 0, 0),
            57 => (1, 0.3, 0, 0, 0),
            // Rain (slight, moderate, heavy), and freezing rain
            61 => (1, 0.3, 0, 0, 0),
            63 => (1, 0.6, 0, 0, 0),
            65 => (1, 1, 0, 0, 0),
            66 => (1, 0.4, 0, 0, 0),
            67 => (1, 0.9, 0, 0, 0),
            // Snow (slight, moderate, heavy), and snow grains
            71 => (1, 0, 0.3, 0, 0),
            73 => (1, 0, 0.6, 0, 0),
            75 => (1, 0, 1, 0, 0),
            77 => (1, 0, 0.3, 0, 0),
            // Rain showers (slight, moderate, violent)
            80 => (0.8, 0.3, 0, 0, 0),
            81 => (0.9, 0.6, 0, 0, 0),
            82 => (1, 1, 0, 0, 0),
            // Snow showers (slight, heavy)
            85 => (0.9, 0, 0.4, 0, 0),
            86 => (1, 0, 0.9, 0, 0),
            // Thunderstorms, and with hail
            95 => (1, 0.6, 0, 0.7, 0),
            96 => (1, 0.8, 0, 1, 0),
            99 => (1, 0.8, 0, 1, 0),
            _ => (0, 0, 0, 0, 0),
        };

        // The clouds, from the most detailed report there is: at each height, or in all, or from
        // how much of the sunlight comes through diffused (only while there's enough of it, by
        // day), or from the weather code
        double low,
            mid,
            high;
        if (reading is { CloudCoverLow: { } l, CloudCoverMid: { } m, CloudCoverHigh: { } h })
            (low, mid, high) = (Clamp(l / 100), Clamp(m / 100), Clamp(h / 100));
        else if (reading.CloudCover is { } total)
            (low, mid, high) = (0, Clamp(total / 100), 0);
        else if (
            reading
                is {
                    ShortwaveRadiation: >= 50,
                    DirectRadiation: { } direct,
                    DiffuseRadiation: { } diffuse,
                }
            && direct + diffuse > 0
        )
            (low, mid, high) = (0, Clamp((diffuse / (direct + diffuse) - 0.25) / 0.65), 0);
        else
            (low, mid, high) = (0, codeClouds, 0);

        var rain =
            reading.RainPerHour is not null || reading.ShowersPerHour is not null
                ? Clamp(((reading.RainPerHour ?? 0) + (reading.ShowersPerHour ?? 0)) / 4)
                : codeRain;

        var snow = reading.SnowfallPerHour is { } snowfall ? Clamp(snowfall / 2) : codeSnow;

        // (the lightning potential, where it's reported, makes a storm out of a heavy shower; it's
        // in J/kg, and a few of them already mean lightning is likely)
        var storm = Math.Max(codeStorm, Clamp((reading.LightningPotential ?? 0) / 5));

        var fog = reading.Visibility is { } visibility
            ? Clamp((5000 - visibility) / 4000)
            : codeFog;

        // (aerosol optical depth: about 0.1 in clean air, from 0.5 hazy, and over 1 in smoke)
        var haze = reading.AerosolOpticalDepth is { } depth ? Clamp((depth - 0.15) / 0.85) : 0;

        // (in µg/m³: a few hundred is a dust storm)
        var dust = reading.Dust is { } amount ? Clamp(amount / 500) : 0;

        // Rain, snow and storms come from low, heavy clouds, whatever the cover says
        if (rain > 0 || snow > 0)
            low = Math.Max(low, 0.85);
        if (storm > 0)
            low = Math.Max(low, storm);

        return new SkyConditions(low, mid, high, rain, snow, storm, fog, haze, dust);
    }
}

/// <summary>
/// What the weather service reports for a time, each value null where it isn't available.
/// Rain, showers and snowfall are per hour.
/// </summary>
public readonly record struct WeatherReading(
    int? WeatherCode = null,
    double? CloudCover = null,
    double? CloudCoverLow = null,
    double? CloudCoverMid = null,
    double? CloudCoverHigh = null,
    double? RainPerHour = null,
    double? ShowersPerHour = null,
    double? SnowfallPerHour = null,
    double? Visibility = null,
    double? ShortwaveRadiation = null,
    double? DirectRadiation = null,
    double? DiffuseRadiation = null,
    double? LightningPotential = null,
    double? AerosolOpticalDepth = null,
    double? Dust = null
)
{
    // Whether there's anything to go on at all (a time that the models don't have yet has none)
    public bool HasAny =>
        Sanitized() is var r
        && (
            r.WeatherCode is not null
            || r.CloudCover is not null
            || r.CloudCoverLow is not null
            || r.ShortwaveRadiation is not null
            || r.Visibility is not null
            || r.AerosolOpticalDepth is not null
            || r.Dust is not null
        );

    // The same, without any value that can't be right (not a number, or out of its range)
    internal WeatherReading Sanitized()
    {
        static double? Range(double? value, double min, double max) =>
            value is { } v && double.IsFinite(v) && v >= min && v <= max ? v : null;

        return new WeatherReading(
            WeatherCode is >= 0 and <= 99 ? WeatherCode : null,
            Range(CloudCover, 0, 100),
            Range(CloudCoverLow, 0, 100),
            Range(CloudCoverMid, 0, 100),
            Range(CloudCoverHigh, 0, 100),
            Range(RainPerHour, 0, 500),
            Range(ShowersPerHour, 0, 500),
            Range(SnowfallPerHour, 0, 200),
            Range(Visibility, 0, 1_000_000),
            Range(ShortwaveRadiation, 0, 1500),
            Range(DirectRadiation, 0, 1500),
            Range(DiffuseRadiation, 0, 1500),
            Range(LightningPotential, 0, 10_000),
            Range(AerosolOpticalDepth, 0, 20),
            Range(Dust, 0, 100_000)
        );
    }
}

/// <summary>
/// A weather forecast from Open-Meteo for the next few hours, in 15-minute slots.
/// </summary>
/// <remarks>
/// <para>
/// The weather comes in 15-minute steps from high-resolution models in North America and Central
/// Europe, filled in from hourly ones everywhere else, along with the hourly forecast (for
/// anything the 15-minute one doesn't have). The air (haze, smoke and dust) comes from the air
/// quality forecast, hourly. Either way, a request or two covers hours ahead.
/// </para>
/// <para>
/// Not every value is available everywhere (e.g. the lightning potential only in Europe), so
/// each one is taken from the 15-minute forecast, or else the hourly one, or else worked out
/// from the others (see <see cref="SkyConditions.FromReading" />).
/// </para>
/// </remarks>
public sealed class WeatherForecast
{
    public static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(15);

    // How far ahead it's asked for (6 hours): the forecast is fetched again well before it runs
    // out, and if it can't be, the sky goes back to clear rather than showing old weather
    private const int SlotCount = 24;
    private const int HourCount = 7;

    // The weather shown at a time is the forecast's average from a little before it to a while
    // after it, sampled every few minutes: so a change starts ahead of time (and is about halfway
    // there when the forecast has it), and one that's over again soon barely shows
    private static readonly TimeSpan SmoothingBefore = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SmoothingAfter = TimeSpan.FromMinutes(45);
    private static readonly TimeSpan SmoothingInterval = TimeSpan.FromMinutes(5);

    private const string WeatherVariables =
        "weather_code,cloud_cover,cloud_cover_low,cloud_cover_mid,cloud_cover_high,rain,showers,"
        + "snowfall,visibility,shortwave_radiation,direct_radiation,diffuse_radiation,"
        + "lightning_potential";

    private const string AirVariables = "aerosol_optical_depth,dust";

    public WeatherForecast(
        IReadOnlyList<(DateTimeOffset Start, SkyConditions Conditions)> slots,
        double altitude = 0
    )
    {
        Slots = slots.OrderBy(s => s.Start).ToArray();
        Altitude = altitude;
    }

    /// <summary>
    /// How high up the place the forecast is for is (see <see cref="SkyConditions.Altitude" />),
    /// 0 if that's unknown.
    /// </summary>
    public double Altitude { get; }

    public IReadOnlyList<(DateTimeOffset Start, SkyConditions Conditions)> Slots { get; }

    private bool Covers(DateTimeOffset instant) =>
        Slots.Count > 0 && instant >= Slots[0].Start && instant < Slots[^1].Start + SlotDuration;

    // Between two slots, the weather goes from one to the other (and after the last one, it stays)
    private SkyConditions GetInterpolatedAt(DateTimeOffset instant)
    {
        for (var i = Slots.Count - 1; i >= 0; i--)
        {
            var (start, conditions) = Slots[i];
            if (instant < start)
                continue;

            if (i == Slots.Count - 1)
                return conditions;

            var (nextStart, next) = Slots[i + 1];
            var amount = (instant - start) / (nextStart - start);

            return SkyConditions.Lerp(conditions, next, Math.Clamp(amount, 0, 1));
        }

        return Slots[0].Conditions;
    }

    /// <summary>
    /// Gets the weather to show at the given time, or null if the forecast doesn't cover it.
    /// </summary>
    /// <remarks>
    /// Smoothed over time (see the remarks on the smoothing above), so that the sky follows the
    /// weather gently.
    /// </remarks>
    public SkyConditions? GetAt(DateTimeOffset instant)
    {
        if (!Covers(instant))
            return null;

        var samples = new List<SkyConditions>();
        for (
            var sample = instant - SmoothingBefore;
            sample <= instant + SmoothingAfter;
            sample += SmoothingInterval
        )
        {
            // (only as far as the forecast goes)
            if (Covers(sample))
                samples.Add(GetInterpolatedAt(sample));
        }

        return SkyConditions.Average(samples);
    }

    /// <summary>
    /// The same, but past the end of the forecast (e.g. while it can't be fetched again), the
    /// weather it ended with fades away to clear over the given time, rather than stopping (as
    /// the weather tends to stay as it was for a while).
    /// </summary>
    public SkyConditions? GetAt(DateTimeOffset instant, TimeSpan fadeAfterEnd)
    {
        if (GetAt(instant) is { } conditions)
            return conditions;

        // (a forecast that starts later than now means the clock was changed: nothing to go on)
        if (Slots.Count == 0 || instant < Slots[0].Start)
            return null;

        var end = Slots[^1].Start + SlotDuration;
        var sinceEnd = instant - end;
        if (sinceEnd >= fadeAfterEnd || GetAt(end - TimeSpan.FromTicks(1)) is not { } last)
            return null;

        return SkyConditions.Lerp(last, SkyConditions.Clear, sinceEnd / fadeAfterEnd);
    }

    /// <summary>
    /// Writes the forecast down (to be read back with <see cref="TryRead" />), along with where
    /// it's for (to tell whether it still applies) and when it was fetched.
    /// </summary>
    public string Write(string source, DateTimeOffset fetchedAt)
    {
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", 1);
            writer.WriteString("source", source);
            writer.WriteNumber("fetched", fetchedAt.ToUnixTimeSeconds());
            writer.WriteNumber("altitude", Altitude);
            writer.WriteStartArray("slots");
            foreach (var (start, conditions) in Slots)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(start.ToUnixTimeSeconds());
                foreach (var value in conditions.ToArray())
                    writer.WriteNumberValue(Math.Round(value, 4));
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Reads back a forecast written with <see cref="Write" />, or null if it can't be read
    /// (anything that isn't quite right counts as nothing).
    /// </summary>
    public static (WeatherForecast Forecast, string Source, DateTimeOffset FetchedAt)? TryRead(
        string json
    )
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (
                root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out var version)
                || version.ValueKind != JsonValueKind.Number
                || version.GetInt32() != 1
                || root.GetProperty("source").GetString() is not { Length: > 0 } source
            )
                return null;

            var fetchedAt = DateTimeOffset.FromUnixTimeSeconds(
                root.GetProperty("fetched").GetInt64()
            );
            var altitude = root.GetProperty("altitude").GetDouble();

            var slots = new List<(DateTimeOffset, SkyConditions)>();
            foreach (var slot in root.GetProperty("slots").EnumerateArray())
            {
                var values = slot.EnumerateArray().ToArray();
                if (values.Length != 11)
                    return null;

                var parts = values.Skip(1).Select(v => v.GetDouble()).ToArray();
                if (parts.Any(v => !double.IsFinite(v) || v < 0 || v > 1))
                    return null;

                slots.Add(
                    (
                        DateTimeOffset.FromUnixTimeSeconds(values[0].GetInt64()),
                        SkyConditions.FromArray(parts)
                    )
                );
            }

            return double.IsFinite(altitude) && altitude is >= 0 and <= 1
                ? (new WeatherForecast(slots, altitude), source, fetchedAt)
                : null;
        }
        catch (Exception ex)
            when (ex
                    is JsonException
                        or InvalidOperationException
                        or KeyNotFoundException
                        or FormatException
                        or ArgumentException
            )
        {
            return null;
        }
    }

    // The values of a part of a response ("minutely_15" or "hourly"), by the time
    private sealed class Series
    {
        private readonly long[] _times;
        private readonly JsonElement _data;

        private Series(long[] times, JsonElement data) => (_times, _data) = (times, data);

        public IReadOnlyList<long> Times => _times;

        public static Series? TryCreate(JsonElement? json, string name) =>
            json is { ValueKind: JsonValueKind.Object } root
            && root.TryGetProperty(name, out var data)
            && data.TryGetProperty("time", out var times)
            && times.ValueKind == JsonValueKind.Array
            && times
                .EnumerateArray()
                .All(t => t.ValueKind == JsonValueKind.Number && t.TryGetInt64(out _))
            && times.EnumerateArray().Select(t => t.GetInt64()).ToArray() is var values
            && values.Length > 0
            // (times out of order can't be read right, so that part isn't used at all)
            && values.Zip(values.Skip(1)).All(p => p.First < p.Second)
                ? new Series(values, data)
                : null;

        private double? GetAt(string name, int index) =>
            _data.TryGetProperty(name, out var values)
            && values.ValueKind == JsonValueKind.Array
            && index < values.GetArrayLength()
            && values[index].ValueKind == JsonValueKind.Number
            && values[index].TryGetDouble(out var value)
            && double.IsFinite(value)
                ? value
                : null;

        // The value at the time, in between the two around it if it falls between them (or the
        // one that has it, if only one does), or null if there's none within an hour
        public double? Get(string name, long time)
        {
            var next = Array.FindIndex(_times, t => t >= time);
            if (next < 0)
                return _times.Length > 0 && time - _times[^1] <= 3600
                    ? GetAt(name, _times.Length - 1)
                    : null;

            if (_times[next] == time || next == 0)
                return _times[next] - time <= 3600 ? GetAt(name, next) : null;

            var (before, after) = (GetAt(name, next - 1), GetAt(name, next));
            if (before is { } b && after is { } a)
            {
                var amount = (double)(time - _times[next - 1]) / (_times[next] - _times[next - 1]);
                return b + (a - b) * amount;
            }

            return before ?? after;
        }
    }

    /// <summary>
    /// Reads the forecast from Open-Meteo's responses: the weather (see <see cref="GetUrl" />),
    /// and the air, if it's there (see <see cref="GetAirQualityUrl" />).
    /// </summary>
    public static WeatherForecast Parse(JsonElement? weather, JsonElement? airQuality = null)
    {
        var quarterly = Series.TryCreate(weather, "minutely_15");
        var hourly = Series.TryCreate(weather, "hourly");
        var air = Series.TryCreate(airQuality, "hourly");

        // The slots of the 15-minute forecast, or else (where it's missing) the hourly one's
        // hours, split into quarters (or the air's, if that's all there is)
        static long[] Quarters(Series series) =>
            series
                .Times.SelectMany(t => Enumerable.Range(0, 4).Select(q => t + q * 900L))
                .ToArray();

        var times =
            quarterly is not null ? quarterly.Times.ToArray()
            : hourly is not null ? Quarters(hourly)
            : air is not null ? Quarters(air)
            : [];

        // (amounts over 15 minutes, made hourly)
        double? Get(string name, long time, bool isAmount = false) =>
            quarterly?.Get(name, time) is { } value
                ? (isAmount ? value * 4 : value)
                : hourly?.Get(name, time);

        // (the elevation of the ground that the forecast is for, which comes with it)
        var altitude = SkyConditions.GetAltitude(
            weather is { ValueKind: JsonValueKind.Object } w
            && w.TryGetProperty("elevation", out var elevation)
            && elevation.ValueKind == JsonValueKind.Number
                ? elevation.GetDouble()
                : null
        );

        var slots = new List<(DateTimeOffset, SkyConditions)>();
        foreach (var time in times)
        {
            var reading = new WeatherReading(
                Get("weather_code", time) is { } code ? (int)Math.Round(code) : null,
                Get("cloud_cover", time),
                Get("cloud_cover_low", time),
                Get("cloud_cover_mid", time),
                Get("cloud_cover_high", time),
                Get("rain", time, true),
                Get("showers", time, true),
                Get("snowfall", time, true),
                Get("visibility", time),
                Get("shortwave_radiation", time),
                Get("direct_radiation", time),
                Get("diffuse_radiation", time),
                Get("lightning_potential", time),
                air?.Get("aerosol_optical_depth", time),
                air?.Get("dust", time)
            );

            // (a slot that the models don't have yet)
            if (!reading.HasAny)
                continue;

            slots.Add(
                (
                    DateTimeOffset.FromUnixTimeSeconds(time),
                    SkyConditions.FromReading(reading) with
                    {
                        Altitude = altitude,
                    }
                )
            );
        }

        return new WeatherForecast(slots, altitude);
    }

    // The location rounded to a tenth of a degree (about 10 km), which doesn't change the weather,
    // so that no more than that is sent
    private static string GetLocationQuery(GeoLocation location)
    {
        static string Format(double value)
        {
            // (+ 0.0 turns a negative zero, e.g. from -0.04, into a plain one)
            var rounded = Math.Round(value, 1, MidpointRounding.AwayFromZero) + 0.0;
            return rounded.ToString("0.0", CultureInfo.InvariantCulture);
        }

        return $"latitude={Format(location.Latitude)}&longitude={Format(location.Longitude)}";
    }

    /// <summary>
    /// Gets the address of the weather forecast for the given location.
    /// </summary>
    public static string GetUrl(GeoLocation location) =>
        "https://api.open-meteo.com/v1/forecast?"
        + GetLocationQuery(location)
        + $"&minutely_15={WeatherVariables}&forecast_minutely_15={SlotCount}"
        + $"&hourly={WeatherVariables}&forecast_hours={HourCount}"
        + "&timeformat=unixtime";

    /// <summary>
    /// Gets the address of the air quality forecast (haze, smoke and dust) for the given location.
    /// </summary>
    public static string GetAirQualityUrl(GeoLocation location) =>
        "https://air-quality-api.open-meteo.com/v1/air-quality?"
        + GetLocationQuery(location)
        + $"&hourly={AirVariables}&forecast_hours={HourCount}"
        + "&timeformat=unixtime";
}
