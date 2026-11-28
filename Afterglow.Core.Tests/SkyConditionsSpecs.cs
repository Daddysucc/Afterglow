using System;
using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Core.Tests;

public class SkyConditionsSpecs
{
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    private const long T0 = 1795876200;

    [Fact]
    public void I_get_the_clouds_at_each_height_when_they_are_reported()
    {
        // Act
        var conditions = SkyConditions.FromReading(
            new WeatherReading(
                CloudCover: 70,
                CloudCoverLow: 10,
                CloudCoverMid: 20,
                CloudCoverHigh: 60
            )
        );

        // Assert
        conditions.LowClouds.Should().BeApproximately(0.1, 1e-9);
        conditions.MidClouds.Should().BeApproximately(0.2, 1e-9);
        conditions.HighClouds.Should().BeApproximately(0.6, 1e-9);
    }

    [Fact]
    public void I_get_the_clouds_from_the_total_cover_or_the_sunlight_or_the_weather_code_when_that_is_all_there_is()
    {
        // Act
        var total = SkyConditions.FromReading(new WeatherReading(CloudCover: 50));
        var sunlight = SkyConditions.FromReading(
            new WeatherReading(ShortwaveRadiation: 200, DirectRadiation: 0, DiffuseRadiation: 200)
        );
        var code = SkyConditions.FromReading(new WeatherReading(WeatherCode: 3));

        // Assert
        total.MidClouds.Should().BeApproximately(0.5, 1e-9);
        sunlight.MidClouds.Should().Be(1);
        code.MidClouds.Should().Be(1);
    }

    [Fact]
    public void I_get_rain_snow_fog_and_storms_from_the_weather_code_when_their_amounts_are_missing()
    {
        // Act
        var rain = SkyConditions.FromReading(new WeatherReading(WeatherCode: 63));
        var snow = SkyConditions.FromReading(new WeatherReading(WeatherCode: 75));
        var fog = SkyConditions.FromReading(new WeatherReading(WeatherCode: 45));
        var storm = SkyConditions.FromReading(new WeatherReading(WeatherCode: 99));

        // Assert
        rain.Rain.Should().BeGreaterThan(0);
        snow.Snow.Should().Be(1);
        fog.Fog.Should().Be(1);
        storm.Storm.Should().Be(1);
        storm.LowClouds.Should().Be(1);
    }

    [Fact]
    public void I_get_the_reported_amounts_rather_than_what_the_weather_code_says()
    {
        // Act
        var conditions = SkyConditions.FromReading(
            new WeatherReading(
                WeatherCode: 65,
                RainPerHour: 1,
                ShowersPerHour: 1,
                Visibility: 3000,
                AerosolOpticalDepth: 0.575,
                Dust: 250
            )
        );

        // Assert
        conditions.Rain.Should().BeApproximately(0.5, 1e-9);
        conditions.LowClouds.Should().BeGreaterThanOrEqualTo(0.85);
        conditions.Fog.Should().BeApproximately(0.5, 1e-9);
        conditions.Haze.Should().BeApproximately(0.5, 1e-9);
        conditions.Dust.Should().BeApproximately(0.5, 1e-9);
    }

    [Fact]
    public void I_get_values_that_cannot_be_right_ignored()
    {
        // Act
        var conditions = SkyConditions.FromReading(
            new WeatherReading(
                WeatherCode: 2,
                CloudCover: 150,
                RainPerHour: double.NaN,
                Visibility: -5,
                Dust: double.PositiveInfinity
            )
        );

        // Assert
        conditions.MidClouds.Should().Be(0.5);
        conditions.Rain.Should().Be(0);
        conditions.Fog.Should().Be(0);
        conditions.Dust.Should().Be(0);
    }

    [Fact]
    public void I_get_a_clear_sky_from_a_clear_reading()
    {
        // Act
        var conditions = SkyConditions.FromReading(
            new WeatherReading(
                WeatherCode: 0,
                CloudCover: 0,
                CloudCoverLow: 0,
                CloudCoverMid: 0,
                CloudCoverHigh: 0,
                RainPerHour: 0,
                SnowfallPerHour: 0,
                Visibility: 40000,
                AerosolOpticalDepth: 0.05,
                Dust: 0
            )
        );

        // Assert
        conditions.IsClear.Should().BeTrue();
    }

    [Fact]
    public void I_get_a_forecast_with_each_value_from_the_15_minute_or_else_the_hourly_forecast()
    {
        // Arrange
        var weather = Json(
            $$"""
            {
              "minutely_15": {
                "time": [{{T0}}, {{T0 + 900}}],
                "weather_code": [3, 3],
                "cloud_cover": [100, null],
                "rain": [0.5, 0.5]
              },
              "hourly": {
                "time": [{{T0}}, {{T0 + 3600}}],
                "cloud_cover": [40, 40],
                "visibility": [3000, 3000]
              }
            }
            """
        );

        // Act
        var forecast = WeatherForecast.Parse(weather);

        // Assert
        forecast.Slots.Should().HaveCount(2);
        forecast.Slots[0].Conditions.MidClouds.Should().Be(1);
        forecast.Slots[0].Conditions.Rain.Should().BeApproximately(0.5, 1e-9);
        forecast.Slots[1].Conditions.Fog.Should().BeApproximately(0.5, 1e-9);
        forecast.Slots[1].Conditions.LowClouds.Should().BeGreaterThanOrEqualTo(0.85);
    }

    [Fact]
    public void I_get_a_forecast_from_the_hourly_one_or_the_air_alone_when_that_is_all_there_is()
    {
        // Arrange
        var hourly = Json($$"""{ "hourly": { "time": [{{T0}}], "cloud_cover": [80] } }""");
        var air = Json(
            $$"""{ "hourly": { "time": [{{T0}}], "aerosol_optical_depth": [1.0], "dust": [null] } }"""
        );

        // Act
        var fromHourly = WeatherForecast.Parse(hourly);
        var fromAir = WeatherForecast.Parse(null, air);

        // Assert
        fromHourly.Slots.Should().HaveCount(4);
        fromHourly.Slots[0].Conditions.MidClouds.Should().BeApproximately(0.8, 1e-9);
        fromAir.Slots.Should().NotBeEmpty();
        fromAir.Slots[0].Conditions.Haze.Should().Be(1);
        fromAir.Slots[0].Conditions.MidClouds.Should().Be(0);
    }

    [Theory]
    [InlineData("""{ "error": true, "reason": "Nope" }""")]
    [InlineData("""{ "minutely_15": { "time": [] } }""")]
    [InlineData("""{ "minutely_15": { "time": [3, 2, 1], "cloud_cover": [1, 2, 3] } }""")]
    [InlineData("""{ "minutely_15": { "time": ["a"], "cloud_cover": [1] } }""")]
    [InlineData("""[]""")]
    public void I_get_an_empty_forecast_from_a_response_that_cannot_be_used(string json)
    {
        // Act
        var forecast = WeatherForecast.Parse(Json(json));

        // Assert
        forecast.Slots.Should().BeEmpty();
    }

    [Fact]
    public void I_get_the_weather_smoothed_so_that_a_change_starts_ahead_of_time()
    {
        // Arrange
        var start = DateTimeOffset.FromUnixTimeSeconds(T0);
        var slots = new (DateTimeOffset, SkyConditions)[8];
        for (var i = 0; i < slots.Length; i++)
            slots[i] = (start.AddMinutes(15 * i), new SkyConditions(LowClouds: i < 4 ? 0 : 1));
        var forecast = new WeatherForecast(slots);

        // Act
        var longBefore = forecast.GetAt(start)!.Value.LowClouds;
        var justBefore = forecast.GetAt(start.AddMinutes(40))!.Value.LowClouds;
        var at = forecast.GetAt(start.AddMinutes(60))!.Value.LowClouds;

        // Assert
        longBefore.Should().Be(0);
        justBefore.Should().BeInRange(0.01, 0.99);
        at.Should().BeGreaterThan(justBefore);
    }

    [Fact]
    public void I_get_no_weather_outside_the_forecast()
    {
        // Arrange
        var start = DateTimeOffset.FromUnixTimeSeconds(T0);
        var forecast = new WeatherForecast([(start, new SkyConditions(LowClouds: 1))]);

        // Act & Assert
        forecast.GetAt(start.AddMinutes(-1)).Should().BeNull();
        forecast.GetAt(start.AddMinutes(15)).Should().BeNull();
        new WeatherForecast([]).GetAt(start).Should().BeNull();
    }

    [Fact]
    public void I_send_my_location_rounded_to_a_tenth_of_a_degree()
    {
        // Act
        var url = WeatherForecast.GetUrl(new GeoLocation(-33.87, 151.21));
        var nearZero = WeatherForecast.GetAirQualityUrl(new GeoLocation(-0.04, 0.0));

        // Assert
        url.Should().Contain("latitude=-33.9&longitude=151.2");
        nearZero.Should().Contain("latitude=0.0&longitude=0.0");
    }

    [Fact]
    public void I_get_the_last_weather_fading_away_slowly_once_the_forecast_runs_out()
    {
        // Arrange
        var start = DateTimeOffset.FromUnixTimeSeconds(T0);
        var forecast = new WeatherForecast([
            (start, new SkyConditions(LowClouds: 1, Rain: 1)),
            (start.AddMinutes(15), new SkyConditions(LowClouds: 1, Rain: 1)),
        ]);
        var end = start.AddMinutes(30);
        var fade = TimeSpan.FromHours(6);

        // Act
        var soon = forecast.GetAt(end.AddMinutes(10), fade)!.Value;
        var halfway = forecast.GetAt(end.AddHours(3), fade)!.Value;
        var gone = forecast.GetAt(end.AddHours(6), fade);
        var before = forecast.GetAt(start.AddMinutes(-1), fade);

        // Assert
        soon.Rain.Should().BeGreaterThan(0.9);
        halfway.Rain.Should().BeApproximately(0.5, 0.01);
        gone.Should().BeNull();
        before.Should().BeNull();
    }

    [Fact]
    public void I_get_the_forecast_back_as_it_was_written_down()
    {
        // Arrange
        var start = DateTimeOffset.FromUnixTimeSeconds(T0);
        var forecast = new WeatherForecast(
            [
                (start, new SkyConditions(LowClouds: 0.5, Haze: 0.25)),
                (start.AddMinutes(15), new SkyConditions(Rain: 1, Storm: 0.7, Fog: 0.1)),
            ],
            altitude: 0.375
        );
        var fetchedAt = DateTimeOffset.FromUnixTimeSeconds(T0 - 60);

        // Act
        var json = forecast.Write("https://example/forecast", fetchedAt);
        var read = WeatherForecast.TryRead(json);

        // Assert
        read.Should().NotBeNull();
        var (copy, source, when) = read!.Value;
        source.Should().Be("https://example/forecast");
        when.Should().Be(fetchedAt);
        copy.Altitude.Should().Be(0.375);
        copy.Slots.Should().Equal(forecast.Slots);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{ "version": 2, "source": "x", "fetched": 0, "altitude": 0, "slots": [] }""")]
    [InlineData(
        """{ "version": 1, "source": "x", "fetched": 0, "altitude": 0, "slots": [[0, 1]] }"""
    )]
    [InlineData(
        """{ "version": 1, "source": "x", "fetched": 0, "altitude": 0, "slots": [[0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0]] }"""
    )]
    [InlineData("""{ "version": 1, "source": "x", "fetched": 0, "altitude": 7, "slots": [] }""")]
    [InlineData("""{ "version": 1, "fetched": 0, "altitude": 0, "slots": [] }""")]
    public void I_get_nothing_from_a_written_forecast_that_is_not_quite_right(string json)
    {
        // Act & Assert
        WeatherForecast.TryRead(json).Should().BeNull();
    }
}
