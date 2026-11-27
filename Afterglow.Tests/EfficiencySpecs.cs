using System;
using Afterglow.Core;
using Afterglow.PlatformInterop;
using Afterglow.Utils;
using Afterglow.ViewModels.Components;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

// The app does less work while idle, but must behave exactly as it did before
public class EfficiencySpecs
{
    // ---- Gamma ramps ----

    // How the ramps were built before, as three separate arrays
    private static (ushort[] Red, ushort[] Green, ushort[] Blue) BuildRampsAsBefore(
        double red,
        double green,
        double blue,
        int deviation
    )
    {
        var (r, g, b) = (new ushort[256], new ushort[256], new ushort[256]);

        for (var i = 0; i < 256; i++)
        {
            r[i] = (ushort)(i * 255 * red);
            g[i] = (ushort)(i * 255 * green);
            b[i] = (ushort)(i * 255 * blue);
        }

        r[255] = (ushort)(r[255] + deviation);
        g[255] = (ushort)(g[255] + deviation);
        b[255] = (ushort)(b[255] + deviation);

        return (r, g, b);
    }

    [Theory]
    [InlineData(1, 1, 1, 0)]
    [InlineData(1, 1, 1, 4)]
    [InlineData(1, 0.71, 0.42, 1)]
    [InlineData(0.85, 0.6, 0, 3)]
    [InlineData(0.1, 0.05, 0.01, 2)]
    public void I_get_the_same_gamma_applied_as_before(
        double red,
        double green,
        double blue,
        int deviation
    )
    {
        // Arrange
        var expected = BuildRampsAsBefore(red, green, blue, deviation);

        // Leftovers from a previous update must be overwritten
        var ramp = new ushort[256 * 3];
        Array.Fill(ramp, (ushort)12345);

        // Act
        DeviceContext.FillRamp(ramp, red, green, blue, deviation);

        // Assert
        ramp[..256].Should().Equal(expected.Red);
        ramp[256..512].Should().Equal(expected.Green);
        ramp[512..].Should().Equal(expected.Blue);
    }

    // ---- Sunrise and sunset ----

    [Fact]
    public void I_get_the_same_sunrise_and_sunset_times_as_when_they_are_calculated_every_time()
    {
        // Arrange
        var cache = new SolarTimesCache();
        var kyiv = new GeoLocation(50.4547, 30.5238);
        var newYork = new GeoLocation(40.7128, -74.0060);

        var instants = new[]
        {
            new DateTimeOffset(2026, 3, 29, 0, 0, 1, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 3, 29, 12, 30, 0, TimeSpan.FromHours(2)),
            // Same day, after a daylight saving time change
            new DateTimeOffset(2026, 3, 29, 23, 59, 59, TimeSpan.FromHours(3)),
            new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.FromHours(3)),
            new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.FromHours(2)),
            new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)),
        };

        // Act & assert
        foreach (var location in new[] { kyiv, newYork, kyiv })
        {
            foreach (var instant in instants)
            {
                cache
                    .Get(location, instant)
                    .Should()
                    .Be(SolarTimes.Calculate(location, instant), $"{location} at {instant}");
            }
        }
    }

    // ---- Update rate ----

    [Fact]
    public void I_get_slow_updates_only_while_nothing_changes_and_nothing_is_shown()
    {
        // Act & assert
        DashboardViewModel
            .CanUpdateSlowly(true, false, false, false, false, false)
            .Should()
            .BeTrue();

        // Any of these keeps updates at full speed
        DashboardViewModel
            .CanUpdateSlowly(false, false, false, false, false, false)
            .Should()
            .BeFalse("smoothing is in progress");
        DashboardViewModel
            .CanUpdateSlowly(true, true, false, false, false, false)
            .Should()
            .BeFalse("a transition is moving faster than slow updates keep up with");
        DashboardViewModel
            .CanUpdateSlowly(true, false, true, false, false, false)
            .Should()
            .BeFalse("the cycle preview is playing");
        DashboardViewModel
            .CanUpdateSlowly(true, false, false, true, false, false)
            .Should()
            .BeFalse("the window or the tray menu is shown");
        DashboardViewModel
            .CanUpdateSlowly(true, false, false, false, true, false)
            .Should()
            .BeFalse("gamma is polled more often than slow updates come");
        DashboardViewModel
            .CanUpdateSlowly(true, false, false, false, false, true)
            .Should()
            .BeFalse("gamma was just invalidated, or a write is being retried");
    }

    [Fact]
    public void I_get_slow_updates_during_a_long_transition_but_fast_ones_during_a_short_one()
    {
        // Arrange (a transition from 6600 K at full brightness to 3900 K at 85%)
        static ColorConfiguration At(double t) => new(6600 + (3900 - 6600) * t, 1 + (0.85 - 1) * t);

        // Act & assert (one second into a 3-hour transition, and into a 1-minute one)
        DashboardViewModel
            .IsChangingFasterThanSlowUpdates(At(0.5), At(0.5 + 1.0 / (3 * 60 * 60)))
            .Should()
            .BeFalse("each second moves the colors by far less than a gamma step");
        DashboardViewModel
            .IsChangingFasterThanSlowUpdates(At(0.5), At(0.5 + 1.0 / 60))
            .Should()
            .BeTrue("each second moves the colors by more than a gamma step");

        // (brightness alone counts too)
        DashboardViewModel
            .IsChangingFasterThanSlowUpdates(new(5000, 1), new(5000, 0.98))
            .Should()
            .BeTrue();
    }

    [Fact]
    public void I_get_slow_updates_while_gamma_is_polled_no_more_than_once_a_second()
    {
        // Act & assert
        DashboardViewModel
            .IsPolledFasterThanSlowUpdates(true, TimeSpan.FromSeconds(1))
            .Should()
            .BeFalse();
        DashboardViewModel
            .IsPolledFasterThanSlowUpdates(true, TimeSpan.FromSeconds(5))
            .Should()
            .BeFalse();
        DashboardViewModel
            .IsPolledFasterThanSlowUpdates(true, TimeSpan.FromSeconds(0.5))
            .Should()
            .BeTrue();
        DashboardViewModel
            .IsPolledFasterThanSlowUpdates(false, TimeSpan.FromSeconds(0.5))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void I_can_change_how_often_a_timer_ticks_while_it_is_running()
    {
        // Arrange
        using var ticked = new System.Threading.ManualResetEventSlim();
        using var timer = new Timer(TimeSpan.FromHours(1), TimeSpan.FromHours(1), ticked.Set);
        timer.Start();

        // Act
        timer.ChangeInterval(TimeSpan.FromHours(1), TimeSpan.Zero);

        // Assert
        ticked.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
    }

    [Fact]
    public void I_do_not_get_a_crash_when_a_disposed_timer_is_changed()
    {
        // Arrange
        var timer = new Timer(TimeSpan.FromSeconds(1), () => { });
        timer.Start();
        timer.Dispose();

        // Act & assert
        timer
            .Invoking(t => t.ChangeInterval(TimeSpan.FromSeconds(1), TimeSpan.Zero))
            .Should()
            .NotThrow();
    }
}
