using System;
using AwesomeAssertions;
using Xunit;

namespace LightBulb.Core.Tests;

public class SettingsValueSpecs
{
    [Fact]
    public void I_can_parse_the_default_streaming_port()
    {
        // Act
        var ports = StreamingPortList.Parse(StreamingPortList.Default);

        // Assert
        ports.Should().Equal(48010);
    }

    [Theory]
    [InlineData("48010, 27036", new[] { 48010, 27036 })]
    [InlineData("48010,27036", new[] { 48010, 27036 })]
    [InlineData(" 48010 ;  27036 ", new[] { 48010, 27036 })]
    [InlineData("48010 27036 47984", new[] { 48010, 27036, 47984 })]
    public void I_can_parse_custom_comma_separated_streaming_ports(string value, int[] expected)
    {
        // Act
        var ports = StreamingPortList.Parse(value);

        // Assert
        ports.Should().Equal(expected);
    }

    [Theory]
    [InlineData("48010, abc, 27036", new[] { 48010, 27036 })] // not a number
    [InlineData("0, 48010, 65536, 70000", new[] { 48010 })] // out of range
    [InlineData("-1, +27036, 48010", new[] { 48010 })] // signs
    [InlineData("48010.5, 27036", new[] { 27036 })] // fractions
    [InlineData("48010, 48010, 27036", new[] { 48010, 27036 })] // duplicates
    public void I_observe_invalid_streaming_ports_being_filtered_out(string value, int[] expected)
    {
        // Act
        var ports = StreamingPortList.Parse(value);

        // Assert
        ports.Should().Equal(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc, , ;")]
    public void I_observe_no_streaming_ports_for_empty_or_invalid_input(string? value)
    {
        // Act
        var ports = StreamingPortList.Parse(value);

        // Assert
        ports.Should().BeEmpty();
    }

    [Fact]
    public void I_can_normalize_streaming_ports_to_the_canonical_form()
    {
        // Act
        var normalized = StreamingPortList.Normalize(" 48010;27036 ,abc, 48010 ");

        // Assert
        normalized.Should().Be("48010, 27036");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(40, 40)]
    [InlineData(180, 180)]
    [InlineData(181, 180)]
    [InlineData(300, 180)]
    [InlineData(-30, 0)]
    public void I_observe_the_transition_duration_limited_to_3_hours(
        int minutes,
        int expectedMinutes
    )
    {
        // Act
        var duration = Cycle.ClampTransitionDuration(TimeSpan.FromMinutes(minutes));

        // Assert
        duration.Should().Be(TimeSpan.FromMinutes(expectedMinutes));
    }
}
