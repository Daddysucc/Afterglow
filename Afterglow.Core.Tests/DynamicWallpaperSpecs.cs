using System;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Core.Tests;

public class DynamicWallpaperSpecs
{
    private static readonly SolarTimes SolarTimes = new(new TimeOnly(7, 0), new TimeOnly(17, 0));

    private static DateTimeOffset At(int hour, int minute = 0) =>
        new(2026, 11, 26, hour, minute, 0, TimeSpan.Zero);

    [Fact]
    public void I_get_a_night_sky_at_night_and_a_day_sky_during_the_day()
    {
        // Act
        var night = DynamicWallpaper.GetPalette(-30);
        var day = DynamicWallpaper.GetPalette(60);

        // Assert
        night.SkyTop.Should().Be(new WallpaperColor(6, 10, 34));
        night.GlowStrength.Should().Be(0);
        day.SkyTop.Should().Be(new WallpaperColor(72, 110, 236));
    }

    [Fact]
    public void I_get_the_strongest_glow_around_sunset()
    {
        // Act
        var sunset = DynamicWallpaper.GetPalette(-2);
        var day = DynamicWallpaper.GetPalette(30);
        var dusk = DynamicWallpaper.GetPalette(-12);

        // Assert
        sunset.GlowStrength.Should().BeGreaterThan(day.GlowStrength);
        sunset.GlowStrength.Should().BeGreaterThan(dusk.GlowStrength);
    }

    [Fact]
    public void I_get_a_sky_that_changes_smoothly_as_the_sun_moves()
    {
        // Act & assert (no jumps between nearby elevations)
        for (var elevation = -25.0; elevation < 40; elevation += 0.1)
        {
            DynamicWallpaper
                .GetDifference(
                    DynamicWallpaper.GetPalette(elevation),
                    DynamicWallpaper.GetPalette(elevation + 0.1)
                )
                .Should()
                .BeLessThanOrEqualTo(3);
        }
    }

    [Fact]
    public void I_get_the_sun_on_the_horizon_at_sunrise_and_sunset()
    {
        // Act & assert
        DynamicWallpaper.GetSunElevation(SolarTimes, At(7), 45).Should().BeApproximately(0, 0.01);
        DynamicWallpaper.GetSunElevation(SolarTimes, At(17), 45).Should().BeApproximately(0, 0.01);
    }

    [Fact]
    public void I_get_the_sun_above_the_horizon_during_the_day_and_below_at_night()
    {
        // Act
        var noon = DynamicWallpaper.GetSunElevation(SolarTimes, At(12), 45);
        var midnight = DynamicWallpaper.GetSunElevation(SolarTimes, At(0), 45);

        // Assert (late November at 45°: about 25° at noon)
        noon.Should().BeInRange(20, 30);
        midnight.Should().BeLessThan(-18);
    }

    [Fact]
    public void I_get_the_sun_setting_and_going_down_at_a_steady_rate()
    {
        // Act
        var beforeSunset = DynamicWallpaper.GetSunElevation(SolarTimes, At(16), 45);
        var afterSunset = DynamicWallpaper.GetSunElevation(SolarTimes, At(18), 45);

        // Assert (about as far below an hour after as above an hour before)
        afterSunset.Should().BeApproximately(-beforeSunset, 1.5);
    }

    [Fact]
    public void I_get_the_sun_path_without_a_location_for_sunrise_and_sunset_entered_by_hand()
    {
        // Act
        var noon = DynamicWallpaper.GetSunElevation(SolarTimes, At(12), null);

        // Assert
        noon.Should().BeGreaterThan(0);
    }

    [Fact]
    public void I_get_a_big_change_split_into_small_steps()
    {
        // Arrange
        var from = DynamicWallpaper.GetPalette(30);
        var to = DynamicWallpaper.GetPalette(-18);

        // Act
        var current = from;
        for (var step = 0; step < 100; step++)
        {
            var previous = current;
            current = DynamicWallpaper.StepTowards(current, to, 6);
            DynamicWallpaper.GetDifference(previous, current).Should().BeLessThanOrEqualTo(6);
        }

        // Assert
        current.Should().Be(to);
    }

    [Fact]
    public void I_get_a_small_change_made_in_one_step()
    {
        // Arrange
        var a = new WallpaperColor(100, 100, 100);
        var b = new WallpaperColor(103, 100, 98);

        // Act & assert
        DynamicWallpaper.StepTowards(a, b, 6).Should().Be(b);
    }

    [Fact]
    public void I_get_skies_drawn_for_stops_along_the_whole_path_of_the_sun()
    {
        // Act
        var stops = DynamicWallpaper.GetStops();

        // Assert
        stops.Should().HaveCount(DynamicWallpaper.StopCount);
        stops.Should().BeInAscendingOrder();
        stops[0].Should().Be(-18);
        stops[^1].Should().Be(30);
        DynamicWallpaper.GetStopIndex(-40).Should().Be(0);
        DynamicWallpaper.GetStopIndex(70).Should().Be(stops.Count - 1);
    }

    [Fact]
    public void I_get_about_as_much_change_from_each_stop_to_the_next()
    {
        // Act
        var stops = DynamicWallpaper.GetStops();

        // Assert (small, even steps, closer together where the sky changes quickly)
        for (var i = 1; i < stops.Count; i++)
        {
            DynamicWallpaper
                .GetDifference(
                    DynamicWallpaper.GetPalette(stops[i - 1]),
                    DynamicWallpaper.GetPalette(stops[i])
                )
                .Should()
                .BeLessThanOrEqualTo(13);
        }

        (stops[^1] - stops[^2]).Should().BeGreaterThan(stops[24] - stops[23]);
    }

    [Fact]
    public void I_get_the_accent_color_of_the_part_of_the_day()
    {
        // Act & assert (the night after bedtime, the golden hour in the evening and around
        // sunrise and sunset, and the day otherwise)
        DynamicWallpaper.GetAccentTime(CycleStage.Night, -30).Should().Be(AccentTime.Night);
        DynamicWallpaper.GetAccentTime(CycleStage.Night, 10).Should().Be(AccentTime.Night);
        DynamicWallpaper.GetAccentTime(CycleStage.Evening, -20).Should().Be(AccentTime.GoldenHour);
        DynamicWallpaper.GetAccentTime(CycleStage.Day, 2).Should().Be(AccentTime.GoldenHour);
        DynamicWallpaper.GetAccentTime(CycleStage.Day, -3).Should().Be(AccentTime.GoldenHour);
        DynamicWallpaper.GetAccentTime(CycleStage.Day, 25).Should().Be(AccentTime.Day);
    }

    [Fact]
    public void I_get_an_accent_color_from_the_sky_for_each_part_of_the_day()
    {
        // Act
        var day = DynamicWallpaper.GetAccentColor(AccentTime.Day);
        var goldenHour = DynamicWallpaper.GetAccentColor(AccentTime.GoldenHour);
        var night = DynamicWallpaper.GetAccentColor(AccentTime.Night);

        // Assert (a blue day, a warm golden hour, and a dark, muted night)
        day.Blue.Should().BeGreaterThan(day.Red);
        goldenHour.Red.Should().BeGreaterThan(goldenHour.Blue);
        (night.Red + night.Green + night.Blue).Should().BeLessThan(200);
        new[] { day, goldenHour, night }.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(-2)]
    [InlineData(15)]
    [InlineData(60)]
    public void I_get_the_same_sky_without_weather_as_before_there_was_weather(double elevation)
    {
        // Act
        var clear = DynamicWallpaper.GetPalette(elevation, SkyConditions.Clear);

        // Assert
        clear.Should().Be(DynamicWallpaper.GetPalette(elevation));
    }

    private static int Saturation(WallpaperColor c) =>
        Math.Max(c.Red, Math.Max(c.Green, c.Blue)) - Math.Min(c.Red, Math.Min(c.Green, c.Blue));

    [Fact]
    public void I_get_a_greyer_sky_with_less_glow_the_cloudier_it_is()
    {
        // Act
        var clear = DynamicWallpaper.GetPalette(-2, SkyConditions.Clear);
        var some = DynamicWallpaper.GetPalette(-2, new SkyConditions(LowClouds: 0.4));
        var overcast = DynamicWallpaper.GetPalette(-2, new SkyConditions(LowClouds: 1));

        // Assert
        Saturation(some.SkyHorizon).Should().BeLessThan(Saturation(clear.SkyHorizon));
        Saturation(overcast.SkyHorizon).Should().BeLessThan(Saturation(some.SkyHorizon));
        some.GlowStrength.Should().BeLessThan(clear.GlowStrength);
        overcast.GlowStrength.Should().BeLessThan(some.GlowStrength);
    }

    [Fact]
    public void I_get_a_lighter_sky_with_more_glow_under_high_clouds_than_under_low_ones()
    {
        // Act
        var high = DynamicWallpaper.GetPalette(15, new SkyConditions(HighClouds: 1));
        var low = DynamicWallpaper.GetPalette(15, new SkyConditions(LowClouds: 1));

        // Assert
        high.SkyTop.Green.Should().BeGreaterThan(low.SkyTop.Green);
        high.GlowStrength.Should().BeGreaterThan(low.GlowStrength);
    }

    [Fact]
    public void I_get_a_darker_sky_in_rain_and_storms_and_a_paler_one_in_snow()
    {
        // Arrange
        static int Brightness(WallpaperPalette p) => p.SkyTop.Red + p.SkyTop.Green + p.SkyTop.Blue;
        var overcast = new SkyConditions(LowClouds: 1);

        // Act
        var cloudy = DynamicWallpaper.GetPalette(15, overcast);
        var rain = DynamicWallpaper.GetPalette(15, overcast with { Rain = 1 });
        var storm = DynamicWallpaper.GetPalette(15, overcast with { Rain = 1, Storm = 1 });
        var snow = DynamicWallpaper.GetPalette(15, overcast with { Snow = 1 });

        // Assert
        Brightness(rain).Should().BeLessThan(Brightness(cloudy));
        Brightness(storm).Should().BeLessThan(Brightness(rain));
        Brightness(snow).Should().BeGreaterThan(Brightness(cloudy));
    }

    [Fact]
    public void I_get_a_flatter_sky_in_fog_and_a_redder_glow_in_haze_and_a_tan_sky_in_dust()
    {
        // Act
        var clear = DynamicWallpaper.GetPalette(-2, SkyConditions.Clear);
        var fog = DynamicWallpaper.GetPalette(-2, new SkyConditions(Fog: 1));
        var haze = DynamicWallpaper.GetPalette(-2, new SkyConditions(Haze: 1));
        var day = DynamicWallpaper.GetPalette(30, SkyConditions.Clear);
        var dust = DynamicWallpaper.GetPalette(30, new SkyConditions(Dust: 1));

        // Assert
        WallpaperColor
            .GetDifference(fog.SkyTop, fog.SkyHorizon)
            .Should()
            .BeLessThan(WallpaperColor.GetDifference(clear.SkyTop, clear.SkyHorizon));
        (haze.Glow.Red - haze.Glow.Blue).Should().BeGreaterThan(clear.Glow.Red - clear.Glow.Blue);
        (dust.SkyTop.Red - dust.SkyTop.Blue)
            .Should()
            .BeGreaterThan(day.SkyTop.Red - day.SkyTop.Blue);
    }

    [Fact]
    public void I_get_a_small_change_in_the_sky_for_a_small_change_in_the_weather()
    {
        // Act
        var a = DynamicWallpaper.GetPalette(10, new SkyConditions(LowClouds: 0.5, Rain: 0.2));
        var b = DynamicWallpaper.GetPalette(10, new SkyConditions(LowClouds: 0.52, Rain: 0.21));

        // Assert
        DynamicWallpaper.GetDifference(a, b).Should().BeLessThan(DynamicWallpaper.StepSize);
    }

    [Fact]
    public void I_get_steps_between_clear_skies_no_bigger_than_the_step_size()
    {
        // Arrange
        var stops = DynamicWallpaper.GetStops();

        // Act & Assert
        DynamicWallpaper.StepSize.Should().BeInRange(5, 20);
        for (var i = 1; i < stops.Count; i++)
        {
            DynamicWallpaper
                .GetDifference(
                    DynamicWallpaper.GetPalette(stops[i - 1]),
                    DynamicWallpaper.GetPalette(stops[i])
                )
                .Should()
                .BeLessThanOrEqualTo(DynamicWallpaper.StepSize + 1);
        }
    }

    [Fact]
    public void I_get_a_deeper_blue_sky_by_day_high_up_and_the_same_at_night()
    {
        // Act
        var day = DynamicWallpaper.GetPalette(30, SkyConditions.Clear);
        var dayHigh = DynamicWallpaper.GetPalette(30, new SkyConditions(Altitude: 1));
        var night = DynamicWallpaper.GetPalette(-30, SkyConditions.Clear);
        var nightHigh = DynamicWallpaper.GetPalette(-30, new SkyConditions(Altitude: 1));

        // Assert
        dayHigh.SkyTop.Green.Should().BeLessThan(day.SkyTop.Green);
        (dayHigh.SkyTop.Blue - dayHigh.SkyTop.Red)
            .Should()
            .BeGreaterThan(day.SkyTop.Blue - day.SkyTop.Red);
        nightHigh.Should().Be(night);
        SkyConditions.GetAltitude(300).Should().Be(0);
        SkyConditions.GetAltitude(null).Should().Be(0);
        SkyConditions.GetAltitude(4000).Should().Be(1);
    }

    [Fact]
    public void I_get_the_real_height_of_the_sun_where_I_am()
    {
        // Arrange
        var greenwich = new GeoLocation(51.48, 0);
        var equinoxNoon = new DateTimeOffset(2026, 3, 20, 12, 7, 0, TimeSpan.Zero);
        var midsummerNoon = new DateTimeOffset(2026, 6, 21, 12, 2, 0, TimeSpan.Zero);
        var midnight = new DateTimeOffset(2026, 3, 20, 0, 7, 0, TimeSpan.Zero);

        // Act & Assert
        DynamicWallpaper
            .GetSunElevation(greenwich, equinoxNoon)
            .Should()
            .BeApproximately(38.5, 0.5);
        DynamicWallpaper
            .GetSunElevation(greenwich, midsummerNoon)
            .Should()
            .BeApproximately(62, 0.5);
        DynamicWallpaper.GetSunElevation(greenwich, midnight).Should().BeApproximately(-38.5, 0.5);
    }

    [Theory]
    [InlineData(51.48, 0)]
    [InlineData(-33.87, 151.21)]
    [InlineData(40.71, -74.0)]
    public void I_get_the_sun_at_the_horizon_at_sunrise_and_sunset(
        double latitude,
        double longitude
    )
    {
        // Arrange
        var location = new GeoLocation(latitude, longitude);
        var day = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        var times = SolarTimes.Calculate(location, day);

        // (the sunrise and sunset are in UTC here, as the day is)
        DateTimeOffset At(TimeOnly time) => new(day.Date + time.ToTimeSpan(), TimeSpan.Zero);

        // Act & Assert
        DynamicWallpaper
            .GetSunElevation(location, At(times.Sunrise))
            .Should()
            .BeApproximately(0, 1);
        DynamicWallpaper.GetSunElevation(location, At(times.Sunset)).Should().BeApproximately(0, 1);
    }

    [Fact]
    public void I_get_the_sun_staying_up_or_down_all_day_near_the_poles()
    {
        // Arrange
        var north = new GeoLocation(78.2, 15.6);
        var summer = new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);
        var winter = new DateTimeOffset(2026, 12, 21, 12, 0, 0, TimeSpan.Zero);

        // Act & Assert
        DynamicWallpaper.GetSunElevation(north, summer).Should().BeGreaterThan(0);
        DynamicWallpaper.GetSunElevation(north, winter).Should().BeLessThan(0);
    }
}
