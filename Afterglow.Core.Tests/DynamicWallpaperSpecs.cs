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
    public void I_get_a_warmer_accent_color_for_a_warmer_screen()
    {
        // Act
        var warmer = DynamicWallpaper.GetAccentColor(3000, 1, 4, 2.2);
        var cooler = DynamicWallpaper.GetAccentColor(6500, 1, 4, 2.2);

        // Assert (less green and blue for every bit of red)
        ((double)warmer.Green / warmer.Red)
            .Should()
            .BeLessThan((double)cooler.Green / cooler.Red);
        ((double)warmer.Blue / Math.Max(warmer.Red, (byte)1))
            .Should()
            .BeLessThan((double)cooler.Blue / cooler.Red);
    }

    [Fact]
    public void I_get_a_dimmer_accent_color_for_a_dimmer_screen()
    {
        // Act
        var dim = DynamicWallpaper.GetAccentColor(6500, 0.5, 4, 2.2);
        var bright = DynamicWallpaper.GetAccentColor(6500, 1, 4, 2.2);

        // Assert
        dim.Red.Should().BeLessThan(bright.Red);
        dim.Red.Should().BeGreaterThan(0);
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
}
