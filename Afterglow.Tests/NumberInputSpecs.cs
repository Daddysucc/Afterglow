using System.Globalization;
using Afterglow.Utils;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class NumberInputSpecs
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    [Theory]
    [InlineData("2.5", 2.5)]
    [InlineData("×2.5", 2.5)]
    [InlineData(" x 3 ", 3)]
    [InlineData("3,200 K", 3200)]
    [InlineData("3200K", 3200)]
    [InlineData("45%", 45)]
    [InlineData("1.5 s", 1.5)]
    [InlineData("+200 K", 200)]
    [InlineData("-10%", -10)]
    [InlineData("−10%", -10)]
    public void I_can_type_a_value_with_or_without_its_unit(string text, double expected)
    {
        // Act
        var isParsed = NumberInput.TryParse(text, English, out var value);

        // Assert
        isParsed.Should().BeTrue();
        value.Should().Be(expected);
    }

    [Fact]
    public void I_can_type_a_value_the_way_my_language_writes_numbers()
    {
        // Act & assert
        NumberInput.TryParse("2,5", German, out var german).Should().BeTrue();
        german.Should().Be(2.5);

        NumberInput.TryParse("3.200 K", German, out var thousands).Should().BeTrue();
        thousands.Should().Be(3200);

        // (French groups thousands with a space, which is typed as a plain one)
        NumberInput.TryParse("3 200 K", French, out var french).Should().BeTrue();
        french.Should().Be(3200);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("K")]
    [InlineData(null)]
    public void I_can_type_something_that_is_not_a_value_and_it_is_ignored(string? text)
    {
        // Act & assert
        NumberInput.TryParse(text, English, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("45", 45)]
    [InlineData("45 min", 45)]
    [InlineData("2 h", 120)]
    [InlineData("1 h 15 min", 75)]
    [InlineData("1h15", 75)]
    [InlineData("1h 15min", 75)]
    [InlineData("1:15", 75)]
    [InlineData("90min", 90)]
    public void I_can_type_a_duration_in_minutes_hours_or_both(string text, double expected)
    {
        // Act
        var isParsed = NumberInput.TryParseMinutes(text, English, "{0} h", out var minutes);

        // Assert
        isParsed.Should().BeTrue();
        minutes.Should().Be(expected);
    }

    [Theory]
    [InlineData("01:30:00", 90)]
    [InlineData("1:30", 90)]
    [InlineData("90", 90)]
    [InlineData("45 min", 45)]
    public void I_can_type_a_transition_duration_as_shown_or_in_minutes(string text, double minutes)
    {
        // Act
        var isParsed = NumberInput.TryParseDuration(text, English, out var duration);

        // Assert
        isParsed.Should().BeTrue();
        duration.TotalMinutes.Should().Be(minutes);
    }

    [Theory]
    [InlineData("22:30", 22, 30)]
    [InlineData("10:30 PM", 22, 30)]
    [InlineData("22", 22, 0)]
    [InlineData("6.5", 6, 30)]
    public void I_can_type_a_time_of_day_as_shown_or_as_an_hour(string text, int hour, int minute)
    {
        // Act
        var isParsed = NumberInput.TryParseTimeOfDay(text, English, out var time);

        // Assert
        isParsed.Should().BeTrue();
        time.Should().Be(new System.TimeOnly(hour, minute));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("24")]
    [InlineData("-1")]
    [InlineData("25:00")]
    public void I_can_type_something_that_is_not_a_time_and_it_is_ignored(string text)
    {
        // Act & assert
        NumberInput.TryParseTimeOfDay(text, English, out _).Should().BeFalse();
    }

    [Fact]
    public void I_can_type_a_duration_with_the_hours_unit_of_my_language()
    {
        // Act & assert
        NumberInput
            .TryParseMinutes("2 Std.", German, "{0} Std.", out var minutes)
            .Should()
            .BeTrue();
        minutes.Should().Be(120);

        NumberInput.TryParseMinutes("never", English, "{0} h", out _).Should().BeFalse();
    }
}
