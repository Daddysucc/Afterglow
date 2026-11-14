using AwesomeAssertions;
using Xunit;

namespace LightBulb.Core.Tests;

public class PopupPlacementSpecs
{
    // 3840x2160 display with a 48 px tall taskbar at the bottom
    private static readonly ScreenRect Screen = new(0, 0, 3840, 2160);
    private static readonly ScreenRect BottomWorkArea = new(0, 0, 3840, 2112);

    private static void AssertWithin(
        (int X, int Y) position,
        int width,
        int height,
        ScreenRect workArea
    )
    {
        position.X.Should().BeGreaterThanOrEqualTo(workArea.Left);
        position.Y.Should().BeGreaterThanOrEqualTo(workArea.Top);
        (position.X + width).Should().BeLessThanOrEqualTo(workArea.Right);
        (position.Y + height).Should().BeLessThanOrEqualTo(workArea.Bottom);
    }

    [Fact]
    public void I_observe_the_menu_open_right_above_a_tray_icon_on_a_bottom_taskbar()
    {
        // Arrange
        var icon = new ScreenRect(3000, 2120, 3032, 2152);

        // Act
        var position = PopupPlacement.Calculate(icon, 400, 500, BottomWorkArea, 8);

        // Assert
        position.Should().Be((3016 - 200, 2120 - 8 - 500));
        AssertWithin(position, 400, 500, BottomWorkArea);
    }

    [Fact]
    public void I_observe_the_menu_stay_on_screen_for_a_tray_icon_in_the_far_corner()
    {
        // Arrange
        var icon = new ScreenRect(3810, 2120, 3838, 2152);

        // Act
        var position = PopupPlacement.Calculate(icon, 400, 500, BottomWorkArea, 8);

        // Assert
        position.X.Should().Be(3840 - 400);
        AssertWithin(position, 400, 500, BottomWorkArea);
    }

    [Fact]
    public void I_observe_the_menu_open_below_a_tray_icon_on_a_top_taskbar()
    {
        // Arrange
        var workArea = new ScreenRect(0, 48, 3840, 2160);
        var icon = new ScreenRect(3000, 8, 3032, 40);

        // Act
        var position = PopupPlacement.Calculate(icon, 400, 500, workArea, 8);

        // Assert
        position.Y.Should().Be(40 + 8);
        AssertWithin(position, 400, 500, workArea);
    }

    [Fact]
    public void I_observe_the_menu_open_beside_a_tray_icon_on_a_side_taskbar()
    {
        // Arrange
        var leftWorkArea = new ScreenRect(64, 0, 3840, 2160);
        var leftIcon = new ScreenRect(16, 2000, 48, 2032);

        var rightWorkArea = new ScreenRect(0, 0, 3776, 2160);
        var rightIcon = new ScreenRect(3792, 2000, 3824, 2032);

        // Act
        var left = PopupPlacement.Calculate(leftIcon, 400, 500, leftWorkArea, 8);
        var right = PopupPlacement.Calculate(rightIcon, 400, 500, rightWorkArea, 8);

        // Assert
        // The icon is within the taskbar, so the menu ends up flush against the taskbar edge
        left.X.Should().Be(leftWorkArea.Left);
        AssertWithin(left, 400, 500, leftWorkArea);

        right.X.Should().Be(rightWorkArea.Right - 400);
        AssertWithin(right, 400, 500, rightWorkArea);
    }

    [Fact]
    public void I_observe_the_menu_open_above_the_cursor_when_the_tray_icon_location_is_unknown()
    {
        // Arrange
        var cursor = new ScreenRect(1500, 1000, 1500, 1000);

        // Act
        var position = PopupPlacement.Calculate(cursor, 400, 500, BottomWorkArea, 8);

        // Assert
        position.Should().Be((1300, 1000 - 8 - 500));
    }

    [Fact]
    public void I_observe_the_menu_open_below_the_cursor_when_there_is_no_room_above()
    {
        // Arrange
        var cursor = new ScreenRect(1500, 100, 1500, 100);

        // Act
        var position = PopupPlacement.Calculate(cursor, 400, 500, BottomWorkArea, 8);

        // Assert
        position.Y.Should().Be(108);
        AssertWithin(position, 400, 500, BottomWorkArea);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void I_observe_the_menu_stay_within_the_work_area_at_any_display_scaling(double scale)
    {
        // Arrange
        // Physical sizes grow with the display scaling, while the screen stays the same
        var width = (int)(284 * scale);
        var height = (int)(300 * scale);
        var icon = new ScreenRect(3800, 2116, 3800 + (int)(24 * scale), 2116 + (int)(24 * scale));

        // Act
        var position = PopupPlacement.Calculate(
            icon,
            width,
            height,
            BottomWorkArea,
            (int)(8 * scale)
        );

        // Assert
        AssertWithin(position, width, height, BottomWorkArea);
    }

    [Fact]
    public void I_observe_a_menu_larger_than_the_work_area_aligned_with_its_top_left_corner()
    {
        // Arrange
        var workArea = new ScreenRect(100, 100, 500, 400);
        var icon = new ScreenRect(450, 410, 470, 430);

        // Act
        var position = PopupPlacement.Calculate(icon, 800, 600, workArea, 8);

        // Assert
        position.Should().Be((100, 100));
    }
}
