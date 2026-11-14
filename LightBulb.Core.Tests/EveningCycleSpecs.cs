using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Xunit;

namespace LightBulb.Core.Tests;

public class EveningCycleSpecs
{
    // With these settings, the cycle looks like this:
    // 05:30 - 07:00  sunrise transition (night -> day)
    // 07:00 - 18:00  day
    // 18:00 - 19:30  sunset transition (day -> evening)
    // 19:30 - 22:00  evening
    // 22:00 - 23:30  night transition (evening -> night)
    // 23:30 - 05:30  night
    private static readonly SolarTimes SolarTimes = new(new TimeOnly(07, 00), new TimeOnly(18, 00));
    private static readonly TimeSpan TransitionDuration = new(01, 30, 00);
    private const double TransitionOffset = 0;
    private static readonly TimeOnly NightStart = new(22, 00);

    private static readonly ColorConfiguration DayConfiguration = new(6600, 1);
    private static readonly ColorConfiguration EveningConfiguration = new(4500, 0.95);
    private static readonly ColorConfiguration NightConfiguration = new(3200, 0.8);

    private static DateTimeOffset At(int hour, int minute, int second = 0) =>
        new(2019, 01, 01, hour, minute, second, TimeSpan.Zero);

    private static ColorConfiguration Interpolate(
        DateTimeOffset instant,
        TimeOnly? nightStart = null,
        SolarTimes? solarTimes = null,
        TimeSpan? transitionDuration = null,
        double transitionOffset = TransitionOffset
    ) =>
        Cycle.InterpolateConfiguration(
            solarTimes ?? SolarTimes,
            DayConfiguration,
            EveningConfiguration,
            NightConfiguration,
            nightStart ?? NightStart,
            transitionDuration ?? TransitionDuration,
            transitionOffset,
            instant
        );

    private static void AssertStrictlyBetween(
        ColorConfiguration configuration,
        ColorConfiguration warmer,
        ColorConfiguration cooler
    )
    {
        configuration.Temperature.Should().BeGreaterThan(warmer.Temperature);
        configuration.Temperature.Should().BeLessThan(cooler.Temperature);
        configuration.Brightness.Should().BeGreaterThan(warmer.Brightness);
        configuration.Brightness.Should().BeLessThan(cooler.Brightness);
    }

    [Fact]
    public void I_observe_day_time_color_configuration_during_day_time()
    {
        // Act
        var configuration = Interpolate(At(12, 00));

        // Assert
        configuration.Should().Be(DayConfiguration);
    }

    [Fact]
    public void I_observe_evening_color_configuration_between_sunset_and_night_start()
    {
        // Act
        var configuration = Interpolate(At(20, 30));

        // Assert
        configuration.Should().Be(EveningConfiguration);
    }

    [Fact]
    public void I_observe_night_time_color_configuration_after_night_start()
    {
        // Act
        var configuration = Interpolate(At(02, 00));

        // Assert
        configuration.Should().Be(NightConfiguration);
    }

    [Fact]
    public void I_observe_intermediate_color_configuration_during_a_transition_from_day_time_to_evening()
    {
        // Act
        var configuration = Interpolate(At(18, 45));

        // Assert
        AssertStrictlyBetween(configuration, EveningConfiguration, DayConfiguration);
    }

    [Fact]
    public void I_observe_intermediate_color_configuration_during_a_transition_from_evening_to_night_time()
    {
        // Act
        var configuration = Interpolate(At(22, 45));

        // Assert
        AssertStrictlyBetween(configuration, NightConfiguration, EveningConfiguration);
    }

    [Fact]
    public void I_observe_intermediate_color_configuration_during_a_transition_from_night_time_to_day_time()
    {
        // Act
        var configuration = Interpolate(At(06, 15));

        // Assert
        AssertStrictlyBetween(configuration, NightConfiguration, DayConfiguration);
    }

    [Fact]
    public void I_observe_the_cycle_go_through_day_evening_and_night_in_order()
    {
        // Act
        var configurations = new List<ColorConfiguration>();
        for (
            var instant = At(07, 00);
            instant <= At(07, 00).AddDays(1);
            instant += TimeSpan.FromMinutes(1)
        )
        {
            var configuration = Interpolate(instant);
            if (configurations.Count == 0 || configurations[^1] != configuration)
                configurations.Add(configuration);
        }

        // Assert
        var stages = configurations.FindAll(c =>
            c == DayConfiguration || c == EveningConfiguration || c == NightConfiguration
        );

        stages
            .Should()
            .Equal(DayConfiguration, EveningConfiguration, NightConfiguration, DayConfiguration);
    }

    [Fact]
    public void I_observe_the_evening_continue_past_midnight_when_night_starts_after_midnight()
    {
        // Arrange
        // Night transition runs from 01:00 to 02:30
        var nightStart = new TimeOnly(01, 00);

        // Act
        var beforeMidnight = Interpolate(At(23, 30), nightStart);
        var afterMidnight = Interpolate(At(00, 30), nightStart);
        var duringTransition = Interpolate(At(01, 45), nightStart);
        var lateNight = Interpolate(At(03, 00), nightStart);

        // Assert
        beforeMidnight.Should().Be(EveningConfiguration);
        afterMidnight.Should().Be(EveningConfiguration);
        AssertStrictlyBetween(duringTransition, NightConfiguration, EveningConfiguration);
        lateNight.Should().Be(NightConfiguration);
    }

    [Fact]
    public void I_observe_a_smooth_night_transition_when_it_spans_midnight()
    {
        // Arrange
        // With a 50% offset, the night transition runs from 23:15 to 00:45
        var nightStart = new TimeOnly(00, 00);
        var transitionOffset = 0.5;

        // Act
        var beforeMidnight = Interpolate(
            At(23, 59, 59),
            nightStart,
            transitionOffset: transitionOffset
        );
        var atMidnight = Interpolate(
            At(00, 00, 00),
            nightStart,
            transitionOffset: transitionOffset
        );

        // Assert
        AssertStrictlyBetween(beforeMidnight, NightConfiguration, EveningConfiguration);
        AssertStrictlyBetween(atMidnight, NightConfiguration, EveningConfiguration);
        atMidnight.Temperature.Should().BeApproximately(beforeMidnight.Temperature, 1);
        atMidnight.Brightness.Should().BeApproximately(beforeMidnight.Brightness, 0.001);
    }

    [Theory]
    [InlineData(17, 00)] // before sunset
    [InlineData(18, 30)] // during the sunset transition
    [InlineData(12, 00)] // in the middle of the day
    public void I_observe_the_evening_stage_being_skipped_when_night_starts_before_the_sunset_transition_ends(
        int nightStartHour,
        int nightStartMinute
    )
    {
        // Arrange
        var nightStart = new TimeOnly(nightStartHour, nightStartMinute);

        // Act
        var schedule = Cycle.GetSchedule(
            SolarTimes,
            TransitionDuration,
            TransitionOffset,
            nightStart
        );
        var duringDay = Interpolate(At(14, 00), nightStart);
        var duringSunset = Interpolate(At(18, 45), nightStart);
        var afterSunset = Interpolate(At(20, 30), nightStart);

        // Assert
        schedule.IsEveningIncluded.Should().BeFalse();
        duringDay.Should().Be(DayConfiguration);
        AssertStrictlyBetween(duringSunset, NightConfiguration, DayConfiguration);
        afterSunset.Should().Be(NightConfiguration);
    }

    [Fact]
    public void I_observe_the_night_transition_being_shortened_when_it_would_overlap_with_the_sunrise()
    {
        // Arrange
        // Night transition would run from 05:00 to 06:30, but sunrise starts at 05:30
        var nightStart = new TimeOnly(05, 00);

        // Act
        var schedule = Cycle.GetSchedule(
            SolarTimes,
            TransitionDuration,
            TransitionOffset,
            nightStart
        );
        var atSunriseStart = Interpolate(At(05, 30), nightStart);

        // Assert
        schedule.IsEveningIncluded.Should().BeTrue();
        schedule.NightTransitionStart.Should().Be(new TimeOnly(05, 00));
        schedule.NightTransitionEnd.Should().Be(new TimeOnly(05, 30));
        schedule.SunriseStart.Should().Be(new TimeOnly(05, 30));
        atSunriseStart.Should().Be(NightConfiguration);
    }

    [Fact]
    public void I_can_get_the_schedule_of_the_three_stage_cycle()
    {
        // Act
        var schedule = Cycle.GetSchedule(
            SolarTimes,
            TransitionDuration,
            TransitionOffset,
            NightStart
        );

        // Assert
        schedule
            .Should()
            .Be(
                new CycleSchedule(
                    new TimeOnly(05, 30),
                    new TimeOnly(07, 00),
                    new TimeOnly(18, 00),
                    new TimeOnly(19, 30),
                    new TimeOnly(22, 00),
                    new TimeOnly(23, 30),
                    true
                )
            );
    }

    [Fact]
    public void I_can_get_the_schedule_of_the_classic_two_stage_cycle()
    {
        // Act
        var schedule = Cycle.GetSchedule(SolarTimes, TransitionDuration, TransitionOffset);

        // Assert
        schedule
            .Should()
            .Be(
                new CycleSchedule(
                    new TimeOnly(05, 30),
                    new TimeOnly(07, 00),
                    new TimeOnly(18, 00),
                    new TimeOnly(19, 30),
                    new TimeOnly(19, 30),
                    new TimeOnly(19, 30),
                    false
                )
            );
    }

    [Fact]
    public void I_observe_the_classic_two_stage_cycle_when_the_evening_stage_is_not_used()
    {
        // Arrange
        var startInstant = At(00, 00);

        for (
            var offset = TimeSpan.Zero;
            offset < TimeSpan.FromDays(1);
            offset += TimeSpan.FromMinutes(1)
        )
        {
            var instant = startInstant + offset;

            // Act
            var twoStage = Cycle.InterpolateConfiguration(
                SolarTimes,
                DayConfiguration,
                NightConfiguration,
                TransitionDuration,
                TransitionOffset,
                instant
            );

            // An evening identical to the night is indistinguishable from no evening at all
            var threeStageWithoutEvening = Cycle.InterpolateConfiguration(
                SolarTimes,
                DayConfiguration,
                NightConfiguration,
                NightConfiguration,
                NightStart,
                TransitionDuration,
                TransitionOffset,
                instant
            );

            // Assert
            twoStage.Should().Be(threeStageWithoutEvening, $"at {instant}");
            twoStage.Should().NotBe(EveningConfiguration, $"at {instant}");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void I_observe_no_abrupt_changes_or_invalid_values_during_the_whole_cycle_for_any_night_start(
        double transitionOffset
    )
    {
        // Arrange
        var startInstant = At(00, 00);

        for (var nightStartHour = 0; nightStartHour < 24; nightStartHour++)
        {
            var nightStart = new TimeOnly(nightStartHour, 00);

            for (
                var offset = TimeSpan.Zero;
                offset < TimeSpan.FromDays(1);
                offset += TimeSpan.FromMinutes(1)
            )
            {
                var instant1 = startInstant + offset;
                var instant2 = instant1 + TimeSpan.FromMinutes(1);

                // Act
                var configuration1 = Interpolate(
                    instant1,
                    nightStart,
                    transitionOffset: transitionOffset
                );
                var configuration2 = Interpolate(
                    instant2,
                    nightStart,
                    transitionOffset: transitionOffset
                );

                // Assert
                double.IsFinite(configuration1.Temperature).Should().BeTrue();
                double.IsFinite(configuration1.Brightness).Should().BeTrue();

                configuration1
                    .Temperature.Should()
                    .BeInRange(NightConfiguration.Temperature, DayConfiguration.Temperature);
                configuration1
                    .Brightness.Should()
                    .BeInRange(NightConfiguration.Brightness, DayConfiguration.Brightness);

                var isHarshJump =
                    Math.Abs(configuration1.Temperature - configuration2.Temperature)
                        >= (DayConfiguration.Temperature - NightConfiguration.Temperature) / 2
                    || Math.Abs(configuration1.Brightness - configuration2.Brightness)
                        >= (DayConfiguration.Brightness - NightConfiguration.Brightness) / 2;

                Assert.False(
                    isHarshJump,
                    $"Detected a harsh jump in color configuration from {instant1} to {instant2} "
                        + $"(night start {nightStart}, offset {transitionOffset}): "
                        + $"{configuration1} -> {configuration2}."
                );
            }
        }
    }

    [Fact]
    public void I_observe_only_stage_configurations_when_the_transition_duration_is_zero()
    {
        // Arrange
        var startInstant = At(00, 00);

        for (
            var offset = TimeSpan.Zero;
            offset < TimeSpan.FromDays(1);
            offset += TimeSpan.FromMinutes(1)
        )
        {
            // Act
            var configuration = Interpolate(
                startInstant + offset,
                transitionDuration: TimeSpan.Zero
            );

            // Assert
            configuration
                .Should()
                .BeOneOf(DayConfiguration, EveningConfiguration, NightConfiguration);
        }
    }
}
