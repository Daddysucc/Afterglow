using System;
using AwesomeAssertions;
using LightBulb.Core;
using Xunit;

namespace LightBulb.Tests;

// Sweeps extreme combinations that unit tests of individual cases could miss
public class MonitorOffsetRobustnessSpecs
{
    private const double MinimumTemperature = 500;
    private const double MaximumTemperature = 20_000;

    private static readonly double[] Temperatures =
    [
        500,
        501,
        1000,
        1900,
        2650,
        4500,
        6600,
        10_000,
        19_999,
        20_000,
    ];

    [Theory]
    [InlineData(MonitorOffsetScaling.Perceptual)]
    [InlineData(MonitorOffsetScaling.Fixed)]
    public void I_always_get_a_valid_temperature_that_follows_the_offset_direction(
        MonitorOffsetScaling scaling
    )
    {
        foreach (var dayTemperature in Temperatures)
        foreach (var temperature in Temperatures)
        {
            var previous = double.NegativeInfinity;

            for (
                var offset = -MonitorProfile.MaxTemperatureOffset;
                offset <= MonitorProfile.MaxTemperatureOffset;
                offset += MonitorProfile.TemperatureOffsetStep
            )
            {
                // Act
                var result = MonitorTemperatureOffset.Apply(
                    temperature,
                    offset,
                    scaling,
                    dayTemperature,
                    MinimumTemperature,
                    MaximumTemperature
                );

                // Assert
                double.IsFinite(result).Should().BeTrue();
                result.Should().BeInRange(MinimumTemperature, MaximumTemperature);

                // A larger offset never results in a warmer display
                result.Should().BeGreaterThanOrEqualTo(previous - 1e-9);

                // Cooler offsets never warm the display, and warmer offsets never cool it
                if (offset > 0)
                    result.Should().BeGreaterThanOrEqualTo(temperature - 1e-9);
                if (offset < 0)
                    result.Should().BeLessThanOrEqualTo(temperature + 1e-9);

                previous = result;
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void I_get_fixed_offsets_when_the_day_temperature_is_unusable(double dayTemperature)
    {
        // Act
        var result = MonitorTemperatureOffset.Apply(
            2650,
            -500,
            MonitorOffsetScaling.Perceptual,
            dayTemperature,
            MinimumTemperature,
            MaximumTemperature
        );

        // Assert
        result.Should().Be(2150);
    }

    [Fact]
    public void I_get_offsets_snapped_to_whole_steps_with_halfway_values_rounded_away_from_zero()
    {
        // Act & assert
        MonitorProfile.NormalizeTemperatureOffset(12.5).Should().Be(25);
        MonitorProfile.NormalizeTemperatureOffset(-12.5).Should().Be(-25);
        MonitorProfile.NormalizeTemperatureOffset(37.5).Should().Be(50);
        MonitorProfile.NormalizeTemperatureOffset(1987.5).Should().Be(2000);
        MonitorProfile.NormalizeTemperatureOffset(double.MaxValue).Should().Be(2000);
        MonitorProfile.NormalizeTemperatureOffset(double.MinValue).Should().Be(-2000);
    }
}
