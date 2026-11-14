using AwesomeAssertions;
using Xunit;

namespace LightBulb.Core.Tests;

public class MonitorOffsetScalingSpecs
{
    private const double DayTemperature = 6600;
    private const double EveningTemperature = 4500;
    private const double NightTemperature = 2650;

    private static double Mireds(double temperature) => 1_000_000 / temperature;

    private static double Apply(
        double temperature,
        double offset,
        MonitorOffsetScaling scaling,
        double referenceTemperature = DayTemperature
    ) =>
        MonitorTemperatureOffset.Apply(
            temperature,
            offset,
            scaling,
            referenceTemperature,
            minimumTemperature: 500,
            maximumTemperature: 20_000
        );

    private static MonitorColorContext CreateContext(
        ColorConfiguration activeConfiguration,
        MonitorOffsetScaling scaling
    ) =>
        new(
            activeConfiguration,
            ColorConfiguration.Default,
            IsGloballyPaused: false,
            MinimumTemperature: 500,
            MaximumTemperature: 20_000,
            MinimumBrightness: 0.1,
            MaximumBrightness: 1,
            scaling,
            ReferenceTemperature: DayTemperature
        );

    private static MonitorProfile CreateProfile(
        int temperatureOffset,
        double brightnessOffset = 0
    ) =>
        new()
        {
            DevicePath = @"\\?\DISPLAY#TEST#1",
            TemperatureOffset = temperatureOffset,
            BrightnessOffset = brightnessOffset,
        };

    [Theory]
    [InlineData(-2000)]
    [InlineData(-1000)]
    [InlineData(25)]
    [InlineData(2000)]
    public void I_observe_perceptual_offsets_matching_exact_kelvin_math_at_the_day_temperature(
        int offset
    )
    {
        // Act
        var temperature = Apply(DayTemperature, offset, MonitorOffsetScaling.Perceptual);

        // Assert
        temperature.Should().Be(DayTemperature + offset);
    }

    [Fact]
    public void I_observe_perceptual_offsets_scaled_down_in_kelvin_at_night()
    {
        // Act
        var temperature = Apply(NightTemperature, -1000, MonitorOffsetScaling.Perceptual);

        // Assert
        // -1000 K at 6600 K is +27.06 mireds, which is about -177 K at 2650 K
        temperature.Should().BeApproximately(2472.71, 0.01);
    }

    [Theory]
    [InlineData(-2000)]
    [InlineData(-500)]
    [InlineData(300)]
    [InlineData(2000)]
    public void I_observe_perceptual_offsets_shifting_evening_and_night_by_the_same_mireds_as_the_day(
        int offset
    )
    {
        // Arrange
        var dayShift = Mireds(DayTemperature + offset) - Mireds(DayTemperature);

        // Act
        var evening = Apply(EveningTemperature, offset, MonitorOffsetScaling.Perceptual);
        var night = Apply(NightTemperature, offset, MonitorOffsetScaling.Perceptual);

        // Assert
        (Mireds(evening) - Mireds(EveningTemperature))
            .Should()
            .BeApproximately(dayShift, 1e-9);
        (Mireds(night) - Mireds(NightTemperature)).Should().BeApproximately(dayShift, 1e-9);

        // Warmer temperatures need fewer Kelvin for the same visual change
        System
            .Math.Abs(night - NightTemperature)
            .Should()
            .BeLessThan(System.Math.Abs(evening - EveningTemperature));
        System.Math.Abs(evening - EveningTemperature).Should().BeLessThan(System.Math.Abs(offset));
    }

    [Theory]
    [InlineData(DayTemperature)]
    [InlineData(EveningTemperature)]
    [InlineData(NightTemperature)]
    public void I_observe_fixed_offsets_applied_as_the_same_kelvin_at_any_time_of_day(
        double cycleTemperature
    )
    {
        // Act
        var cooler = Apply(cycleTemperature, 1500, MonitorOffsetScaling.Fixed);
        var warmer = Apply(cycleTemperature, -1000, MonitorOffsetScaling.Fixed);

        // Assert
        cooler.Should().Be(cycleTemperature + 1500);
        warmer.Should().Be(cycleTemperature - 1000);
    }

    [Theory]
    [InlineData(MonitorOffsetScaling.Perceptual)]
    [InlineData(MonitorOffsetScaling.Fixed)]
    public void I_observe_a_zero_offset_leave_the_temperature_unchanged(
        MonitorOffsetScaling scaling
    )
    {
        // Act & assert
        Apply(NightTemperature, 0, scaling).Should().Be(NightTemperature);
        Apply(3333.3, 0, scaling).Should().Be(3333.3);
    }

    [Theory]
    [InlineData(MonitorOffsetScaling.Perceptual)]
    [InlineData(MonitorOffsetScaling.Fixed)]
    public void I_observe_offset_temperatures_clamped_to_the_supported_range(
        MonitorOffsetScaling scaling
    )
    {
        // Act
        var hottest = Apply(19_000, 2000, scaling);
        var coldest = Apply(600, -2000, scaling);
        var extremeCold = Apply(510, -2000, scaling);

        // Assert
        hottest.Should().Be(20_000);
        coldest.Should().BeInRange(500, 600);
        extremeCold.Should().BeInRange(500, 510);
    }

    [Fact]
    public void I_observe_fixed_offsets_clamped_to_the_lowest_supported_temperature()
    {
        // Act & assert
        Apply(600, -2000, MonitorOffsetScaling.Fixed).Should().Be(500);
    }

    [Fact]
    public void I_observe_perceptual_offsets_stay_in_range_when_the_day_temperature_plus_offset_is_out_of_range()
    {
        // Act
        // 2000 K - 2000 K would be 0 K, so the offset is limited to the lowest supported temperature
        var atReference = Apply(2000, -2000, MonitorOffsetScaling.Perceptual, 2000);
        var elsewhere = Apply(4000, -2000, MonitorOffsetScaling.Perceptual, 2000);

        // 19,000 K + 2000 K would exceed the highest supported temperature
        var beyondMaximum = Apply(19_000, 2000, MonitorOffsetScaling.Perceptual, 19_000);

        // Assert
        atReference.Should().Be(500);
        elsewhere.Should().BeInRange(500, 4000);
        beyondMaximum.Should().Be(20_000);
    }

    [Fact]
    public void I_observe_a_secondary_display_offset_scaled_perceptually_at_night()
    {
        // Act
        var configuration = MonitorColorResolver.Resolve(
            CreateContext(
                new ColorConfiguration(NightTemperature, 0.85),
                MonitorOffsetScaling.Perceptual
            ),
            CreateProfile(-1000, 0.1)
        );

        // Assert
        configuration.Temperature.Should().BeApproximately(2472.71, 0.01);
        configuration.Brightness.Should().BeApproximately(0.95, 1e-9);
    }

    [Fact]
    public void I_observe_a_secondary_display_offset_by_fixed_kelvin_at_night()
    {
        // Act
        var configuration = MonitorColorResolver.Resolve(
            CreateContext(
                new ColorConfiguration(NightTemperature, 0.85),
                MonitorOffsetScaling.Fixed
            ),
            CreateProfile(-1000)
        );

        // Assert
        configuration.Temperature.Should().Be(1650);
    }

    [Theory]
    [InlineData(MonitorOffsetScaling.Perceptual)]
    [InlineData(MonitorOffsetScaling.Fixed)]
    public void I_observe_a_secondary_display_brightness_capped_at_100_percent_with_any_scaling(
        MonitorOffsetScaling scaling
    )
    {
        // Act
        var configuration = MonitorColorResolver.Resolve(
            CreateContext(new ColorConfiguration(DayTemperature, 1), scaling),
            CreateProfile(2000, 0.25)
        );

        // Assert
        configuration.Temperature.Should().Be(8600);
        configuration.Brightness.Should().Be(1);
    }

    [Theory]
    [InlineData(MonitorOffsetScaling.Perceptual)]
    [InlineData(MonitorOffsetScaling.Fixed)]
    public void I_observe_the_primary_display_ignore_its_offsets_with_any_scaling(
        MonitorOffsetScaling scaling
    )
    {
        // Arrange
        var active = new ColorConfiguration(NightTemperature, 0.85);

        // Act
        var configuration = MonitorColorResolver.Resolve(
            CreateContext(active, scaling),
            CreateProfile(-2000, -0.25),
            isPrimary: true
        );

        // Assert
        configuration.Should().Be(active);
    }
}
