using System;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Core.Tests;

public class CycleSpecs
{
    [Fact]
    public void I_observe_transitions_that_start_and_end_gently()
    {
        // Arrange
        var solarTimes = new SolarTimes(new TimeOnly(07, 00), new TimeOnly(18, 00));
        var transitionDuration = new TimeSpan(01, 30, 00);
        var dayConfiguration = new ColorConfiguration(6600, 1);
        var nightConfiguration = new ColorConfiguration(3600, 0.8);

        double GetTemperature(DateTimeOffset instant) =>
            Cycle
                .InterpolateConfiguration(
                    solarTimes,
                    dayConfiguration,
                    nightConfiguration,
                    transitionDuration,
                    0,
                    instant
                )
                .Temperature;

        // Act
        // Every minute around sunset, which includes the whole transition
        var start = new DateTimeOffset(2019, 01, 01, 15, 30, 00, TimeSpan.Zero);
        var temperatures = new double[5 * 60];
        for (var i = 0; i < temperatures.Length; i++)
            temperatures[i] = GetTemperature(start.AddMinutes(i));

        var first = Array.FindIndex(temperatures, t => t < dayConfiguration.Temperature);
        var last = Array.FindLastIndex(temperatures, t => t > nightConfiguration.Temperature);

        // Assert
        // The transition is found, and only ever moves toward the night temperature
        first.Should().BePositive();
        last.Should().BeGreaterThan(first + 60);
        for (var i = 1; i < temperatures.Length; i++)
            temperatures[i].Should().BeLessThanOrEqualTo(temperatures[i - 1]);

        // Halfway through, the temperature is halfway between the two
        var middle = (first - 1 + last + 1) / 2.0;
        var halfway = (dayConfiguration.Temperature + nightConfiguration.Temperature) / 2;
        GetTemperature(start.AddMinutes(middle)).Should().BeApproximately(halfway, 5);

        // The first and last minutes change much less than the middle one does
        var firstStep = temperatures[first - 1] - temperatures[first];
        var lastStep = temperatures[last] - temperatures[last + 1];
        var middleStep =
            GetTemperature(start.AddMinutes(middle - 0.5))
            - GetTemperature(start.AddMinutes(middle + 0.5));

        firstStep.Should().BeLessThan(middleStep / 10);
        lastStep.Should().BeLessThan(middleStep / 10);
    }

    [Fact]
    public void I_observe_day_time_color_configuration_during_day_time()
    {
        // Arrange
        var solarTimes = new SolarTimes(new TimeOnly(07, 00), new TimeOnly(18, 00));
        var transitionDuration = new TimeSpan(01, 30, 00);
        var transitionOffset = 0;
        var dayConfiguration = new ColorConfiguration(6600, 1);
        var nightConfiguration = new ColorConfiguration(3600, 0.85);

        var instant = new DateTimeOffset(2019, 01, 01, 14, 00, 00, TimeSpan.Zero);

        // Act
        var configuration = Cycle.InterpolateConfiguration(
            solarTimes,
            dayConfiguration,
            nightConfiguration,
            transitionDuration,
            transitionOffset,
            instant
        );

        // Assert
        configuration.Should().Be(dayConfiguration);
    }

    [Fact]
    public void I_observe_night_time_color_configuration_during_night_time()
    {
        // Arrange
        var solarTimes = new SolarTimes(new TimeOnly(07, 00), new TimeOnly(18, 00));
        var transitionDuration = new TimeSpan(01, 30, 00);
        var transitionOffset = 0;
        var dayConfiguration = new ColorConfiguration(6600, 1);
        var nightConfiguration = new ColorConfiguration(3600, 0.85);

        var instant = new DateTimeOffset(2019, 01, 01, 02, 00, 00, TimeSpan.Zero);

        // Act
        var configuration = Cycle.InterpolateConfiguration(
            solarTimes,
            dayConfiguration,
            nightConfiguration,
            transitionDuration,
            transitionOffset,
            instant
        );

        // Assert
        configuration.Should().Be(nightConfiguration);
    }

    [Fact]
    public void I_observe_intermediate_color_configuration_during_a_transition_from_day_time_to_night_time()
    {
        // Arrange
        var solarTimes = new SolarTimes(new TimeOnly(07, 00), new TimeOnly(18, 00));
        var transitionDuration = new TimeSpan(01, 30, 00);
        var transitionOffset = 0;
        var dayConfiguration = new ColorConfiguration(6600, 1);
        var nightConfiguration = new ColorConfiguration(3600, 0.85);

        var instant = new DateTimeOffset(2019, 01, 01, 18, 30, 00, TimeSpan.Zero);

        // Act
        var configuration = Cycle.InterpolateConfiguration(
            solarTimes,
            dayConfiguration,
            nightConfiguration,
            transitionDuration,
            transitionOffset,
            instant
        );

        // Assert
        configuration.Temperature.Should().BeLessThan(dayConfiguration.Temperature);
        configuration.Temperature.Should().BeGreaterThan(nightConfiguration.Temperature);
        configuration.Brightness.Should().BeLessThan(dayConfiguration.Brightness);
        configuration.Brightness.Should().BeGreaterThan(nightConfiguration.Brightness);
    }

    [Fact]
    public void I_observe_intermediate_color_configuration_during_a_transition_from_night_time_to_day_time()
    {
        // Arrange
        var solarTimes = new SolarTimes(new TimeOnly(07, 00), new TimeOnly(18, 00));
        var transitionDuration = new TimeSpan(01, 30, 00);
        var transitionOffset = 0;
        var dayConfiguration = new ColorConfiguration(6600, 1);
        var nightConfiguration = new ColorConfiguration(3600, 0.85);

        // Sunrise transition runs from 05:30 to 07:00
        var instant = new DateTimeOffset(2019, 01, 01, 06, 30, 00, TimeSpan.Zero);

        // Act
        var configuration = Cycle.InterpolateConfiguration(
            solarTimes,
            dayConfiguration,
            nightConfiguration,
            transitionDuration,
            transitionOffset,
            instant
        );

        // Assert
        configuration.Temperature.Should().BeLessThan(dayConfiguration.Temperature);
        configuration.Temperature.Should().BeGreaterThan(nightConfiguration.Temperature);
        configuration.Brightness.Should().BeLessThan(dayConfiguration.Brightness);
        configuration.Brightness.Should().BeGreaterThan(nightConfiguration.Brightness);
    }

    [Fact]
    public void I_observe_no_abrupt_changes_in_color_configuration_when_the_night_is_shorter_than_both_transitions()
    {
        // Arrange
        // Sunset transition (23:30 to 01:10) overlaps with sunrise transition (00:50 to 02:30)
        var solarTimes = new SolarTimes(new TimeOnly(02, 30), new TimeOnly(23, 30));
        var transitionDuration = new TimeSpan(01, 40, 00);
        var transitionOffset = 0;
        var dayConfiguration = new ColorConfiguration(6600, 1);
        var nightConfiguration = new ColorConfiguration(3600, 0.85);

        var startInstant = new DateTimeOffset(2019, 01, 01, 00, 00, 00, TimeSpan.Zero);

        for (
            var offset = TimeSpan.Zero;
            offset < TimeSpan.FromDays(1);
            offset += TimeSpan.FromMinutes(1)
        )
        {
            var instant1 = startInstant + offset;
            var instant2 = instant1 + TimeSpan.FromMinutes(1);

            // Act
            var configuration1 = Cycle.InterpolateConfiguration(
                solarTimes,
                dayConfiguration,
                nightConfiguration,
                transitionDuration,
                transitionOffset,
                instant1
            );

            var configuration2 = Cycle.InterpolateConfiguration(
                solarTimes,
                dayConfiguration,
                nightConfiguration,
                transitionDuration,
                transitionOffset,
                instant2
            );

            // Assert
            Math.Abs(configuration1.Temperature - configuration2.Temperature)
                .Should()
                .BeLessThan(
                    (dayConfiguration.Temperature - nightConfiguration.Temperature) / 10,
                    $"there should be no abrupt change from {instant1} to {instant2}"
                );
        }
    }

    [Fact]
    public void I_observe_the_same_color_configuration_right_before_and_right_after_midnight()
    {
        // Arrange
        var solarTimes = new SolarTimes(new TimeOnly(07, 00), new TimeOnly(18, 00));
        var transitionDuration = new TimeSpan(01, 30, 00);
        var transitionOffset = 0;
        var dayConfiguration = new ColorConfiguration(6600, 1);
        var nightConfiguration = new ColorConfiguration(3600, 0.85);

        var instant1 = new DateTimeOffset(2019, 01, 01, 00, 00, 00, TimeSpan.Zero);

        var instant2 = new DateTimeOffset(2019, 01, 01, 23, 59, 59, TimeSpan.Zero);

        // Act
        var configuration1 = Cycle.InterpolateConfiguration(
            solarTimes,
            dayConfiguration,
            nightConfiguration,
            transitionDuration,
            transitionOffset,
            instant1
        );

        var configuration2 = Cycle.InterpolateConfiguration(
            solarTimes,
            dayConfiguration,
            nightConfiguration,
            transitionDuration,
            transitionOffset,
            instant2
        );

        // Assert
        configuration1.Should().Be(configuration2);
    }

    [Fact]
    public void I_observe_no_abrupt_changes_in_color_configuration_during_the_whole_cycle()
    {
        // Arrange
        var solarTimes = new SolarTimes(new TimeOnly(07, 20), new TimeOnly(23, 35));
        var transitionDuration = new TimeSpan(01, 30, 00);
        var transitionOffset = 0;
        var dayConfiguration = new ColorConfiguration(6600, 1);
        var nightConfiguration = new ColorConfiguration(3600, 0.85);

        // Act
        var startInstant = new DateTimeOffset(2019, 01, 01, 00, 00, 00, TimeSpan.Zero);

        for (
            var offset = TimeSpan.Zero;
            offset < TimeSpan.FromDays(1);
            offset += TimeSpan.FromMinutes(1)
        )
        {
            var instant1 = startInstant + offset;
            var instant2 = instant1 + TimeSpan.FromMinutes(1);

            var configuration1 = Cycle.InterpolateConfiguration(
                solarTimes,
                dayConfiguration,
                nightConfiguration,
                transitionDuration,
                transitionOffset,
                instant1
            );

            var configuration2 = Cycle.InterpolateConfiguration(
                solarTimes,
                dayConfiguration,
                nightConfiguration,
                transitionDuration,
                transitionOffset,
                instant2
            );

            // Assert
            var isHarshJump =
                Math.Abs(configuration1.Temperature - configuration2.Temperature)
                    >= Math.Abs(dayConfiguration.Temperature - nightConfiguration.Temperature) / 2
                || Math.Abs(configuration1.Brightness - configuration2.Brightness)
                    >= Math.Abs(dayConfiguration.Brightness - nightConfiguration.Brightness) / 2;

            Assert.False(
                isHarshJump,
                $"Detected a harsh jump in color configuration from {instant1} to {instant2}: "
                    + $"{configuration1} -> {configuration2}."
            );
        }
    }
}
