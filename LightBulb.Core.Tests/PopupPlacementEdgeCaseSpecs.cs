using AwesomeAssertions;
using Xunit;

namespace LightBulb.Core.Tests;

// Display arrangements, orientations, scalings, and taskbar setups beyond the common case
public class PopupPlacementEdgeCaseSpecs
{
    // Menu size in logical pixels
    private const int MenuWidth = 284;
    private const int MenuHeight = 300;
    private const int Gap = 8;

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

    private static void AssertNotOverlapping(
        (int X, int Y) position,
        int width,
        int height,
        ScreenRect anchor
    )
    {
        var overlaps =
            position.X < anchor.Right
            && position.X + width > anchor.Left
            && position.Y < anchor.Bottom
            && position.Y + height > anchor.Top;

        overlaps.Should().BeFalse();
    }

    [Fact]
    public void I_observe_the_menu_placed_correctly_on_a_display_to_the_left_of_the_primary_one()
    {
        // Arrange
        // 1920x1080 display to the left of the primary display, with a 40 px taskbar at the bottom
        var workArea = new ScreenRect(-1920, 0, 0, 1040);
        var icon = new ScreenRect(-200, 1046, -176, 1070);

        // Act
        var position = PopupPlacement.Calculate(icon, MenuWidth, MenuHeight, workArea, Gap);

        // Assert
        position.Should().Be((-188 - MenuWidth / 2, 1046 - Gap - MenuHeight));
        AssertWithin(position, MenuWidth, MenuHeight, workArea);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);
    }

    [Fact]
    public void I_observe_the_menu_kept_on_screen_near_the_negative_edge_of_a_display_above_the_primary_one()
    {
        // Arrange
        // 2560x1440 display above and to the left of the primary display, taskbar on its left
        var workArea = new ScreenRect(-2500, -1440, 60, 0);
        var icon = new ScreenRect(-2550, -60, -2510, -20);

        // Act
        var position = PopupPlacement.Calculate(icon, MenuWidth, MenuHeight, workArea, Gap);

        // Assert
        position.X.Should().Be(workArea.Left);
        AssertWithin(position, MenuWidth, MenuHeight, workArea);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);
    }

    [Theory]
    [InlineData(0, 0)] // primary
    [InlineData(3840, 0)] // to the right
    [InlineData(-1080, -400)] // to the left and slightly above
    public void I_observe_the_menu_fit_on_a_portrait_display(int left, int top)
    {
        // Arrange
        // 1080x1920 display rotated by 90 degrees, with a 48 px taskbar at the bottom
        var workArea = new ScreenRect(left, top, left + 1080, top + 1872);
        var icon = new ScreenRect(left + 1040, top + 1884, left + 1064, top + 1908);

        // Act
        var position = PopupPlacement.Calculate(icon, MenuWidth, MenuHeight, workArea, Gap);

        // Assert
        AssertWithin(position, MenuWidth, MenuHeight, workArea);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);

        // Right above the icon, but never over the taskbar
        (position.Y + MenuHeight)
            .Should()
            .Be(System.Math.Min(icon.Top - Gap, workArea.Bottom));
    }

    [Theory]
    // 4K primary display at 150%
    [InlineData(0, 0, 3840, 2160, 1.5)]
    // 1080p secondary display at 100%, to the right of it
    [InlineData(3840, 0, 5760, 1080, 1.0)]
    // 1080p secondary display at 100%, to the left of it
    [InlineData(-1920, 0, 0, 1080, 1.0)]
    // 1440p display at 125%, above it
    [InlineData(0, -1440, 2560, 0, 1.25)]
    public void I_observe_the_menu_scaled_to_the_display_it_opens_on_in_a_mixed_dpi_setup(
        int left,
        int top,
        int right,
        int bottom,
        double scale
    )
    {
        // Arrange
        // Logical sizes are scaled by the target display's own scaling, and positions are physical
        var taskbarHeight = (int)(48 * scale);
        var iconSize = (int)(24 * scale);

        var display = new ScreenRect(left, top, right, bottom);
        var workArea = display with { Bottom = bottom - taskbarHeight };
        var icon = new ScreenRect(
            right - 3 * iconSize,
            bottom - taskbarHeight + (taskbarHeight - iconSize) / 2,
            right - 2 * iconSize,
            bottom - taskbarHeight + (taskbarHeight + iconSize) / 2
        );

        var width = (int)(MenuWidth * scale);
        var height = (int)(MenuHeight * scale);

        // Act
        var position = PopupPlacement.Calculate(
            icon,
            width,
            height,
            workArea,
            (int)(Gap * scale),
            display
        );

        // Assert
        AssertWithin(position, width, height, workArea);
        AssertNotOverlapping(position, width, height, icon);
        // Right above the icon, but never over the taskbar
        (position.Y + height)
            .Should()
            .Be(System.Math.Min(icon.Top - (int)(Gap * scale), workArea.Bottom));
    }

    // 1920x1080 display, with an auto-hidden 48 px taskbar (which doesn't reduce the work area).
    // The icon positions are while the taskbar is shown, since that's when it can be clicked.
    private static readonly ScreenRect Display = new(0, 0, 1920, 1080);

    [Fact]
    public void I_observe_the_menu_open_above_an_auto_hidden_taskbar_at_the_bottom()
    {
        // Arrange
        var icon = new ScreenRect(1800, 1044, 1824, 1068);

        // Act
        var position = PopupPlacement.Calculate(icon, MenuWidth, MenuHeight, Display, Gap, Display);

        // Assert
        (position.Y + MenuHeight)
            .Should()
            .Be(icon.Top - Gap);
        AssertWithin(position, MenuWidth, MenuHeight, Display);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);
    }

    [Fact]
    public void I_observe_the_menu_open_below_an_auto_hidden_taskbar_at_the_top()
    {
        // Arrange
        var icon = new ScreenRect(1800, 12, 1824, 36);

        // Act
        var position = PopupPlacement.Calculate(icon, MenuWidth, MenuHeight, Display, Gap, Display);

        // Assert
        position.Y.Should().Be(icon.Bottom + Gap);
        AssertWithin(position, MenuWidth, MenuHeight, Display);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);
    }

    [Fact]
    public void I_observe_the_menu_open_beside_an_auto_hidden_taskbar_on_the_left()
    {
        // Arrange
        var icon = new ScreenRect(12, 1000, 36, 1024);

        // Act
        var position = PopupPlacement.Calculate(icon, MenuWidth, MenuHeight, Display, Gap, Display);

        // Assert
        position.X.Should().Be(icon.Right + Gap);
        AssertWithin(position, MenuWidth, MenuHeight, Display);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);
    }

    [Fact]
    public void I_observe_the_menu_open_beside_an_auto_hidden_taskbar_on_the_right()
    {
        // Arrange
        var icon = new ScreenRect(1884, 1000, 1908, 1024);

        // Act
        var position = PopupPlacement.Calculate(icon, MenuWidth, MenuHeight, Display, Gap, Display);

        // Assert
        (position.X + MenuWidth)
            .Should()
            .Be(icon.Left - Gap);
        AssertWithin(position, MenuWidth, MenuHeight, Display);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void I_observe_the_menu_open_next_to_an_icon_in_the_overflow_flyout(bool isAutoHidden)
    {
        // Arrange
        // The overflow flyout (^) floats above the taskbar, within the work area
        var workArea = isAutoHidden ? Display : Display with { Bottom = 1032 };
        var icon = new ScreenRect(1700, 900, 1724, 924);

        // Act
        var position = PopupPlacement.Calculate(
            icon,
            MenuWidth,
            MenuHeight,
            workArea,
            Gap,
            Display
        );

        // Assert
        (position.Y + MenuHeight)
            .Should()
            .Be(icon.Top - Gap);
        AssertWithin(position, MenuWidth, MenuHeight, workArea);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);
    }

    [Fact]
    public void I_observe_the_menu_open_below_an_icon_in_the_overflow_flyout_when_there_is_no_room_above()
    {
        // Arrange
        // Taskbar at the top, so the flyout is right below it
        var workArea = Display with
        {
            Top = 48,
        };
        var icon = new ScreenRect(1700, 120, 1724, 144);

        // Act
        var position = PopupPlacement.Calculate(
            icon,
            MenuWidth,
            MenuHeight,
            workArea,
            Gap,
            Display
        );

        // Assert
        position.Y.Should().Be(icon.Bottom + Gap);
        AssertWithin(position, MenuWidth, MenuHeight, workArea);
        AssertNotOverlapping(position, MenuWidth, MenuHeight, icon);
    }

    [Fact]
    public void I_observe_no_failure_when_there_is_no_display_to_place_the_menu_on()
    {
        // Arrange
        // No display is active (e.g. booted with the monitor off), so all areas are empty
        var empty = new ScreenRect(0, 0, 0, 0);

        // Act
        var position = PopupPlacement.Calculate(empty, MenuWidth, MenuHeight, empty, Gap, empty);

        // Assert
        position.Should().Be((0, 0));
    }

    [Fact]
    public void I_observe_no_failure_with_a_zero_or_negative_menu_size()
    {
        // Act
        var position = PopupPlacement.Calculate(
            new ScreenRect(100, 100, 120, 120),
            -50,
            0,
            Display,
            Gap,
            Display
        );

        // Assert
        AssertWithin(position, 0, 0, Display);
    }
}
