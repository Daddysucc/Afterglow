using System;
using Afterglow.Core;
using Afterglow.Utils;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class GammaWriteTrackerSpecs
{
    private const string Display = @"\\.\DISPLAY1";
    private static readonly DateTimeOffset Start = new(2026, 11, 14, 22, 0, 0, TimeSpan.Zero);
    private static readonly ColorConfiguration Night = new(2650, 0.85);

    [Fact]
    public void I_get_gamma_written_to_a_display_that_has_not_been_written_to_yet()
    {
        // Act & assert
        new GammaWriteTracker()
            .ShouldWrite(Display, Night, false, Start)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void I_do_not_get_gamma_rewritten_for_changes_too_small_to_see()
    {
        // Arrange
        var tracker = new GammaWriteTracker();
        tracker.RecordSuccess(Display, Night);

        // Act & assert
        tracker.ShouldWrite(Display, Night, false, Start).Should().BeFalse();
        tracker
            .ShouldWrite(Display, new ColorConfiguration(2660, 0.855), false, Start)
            .Should()
            .BeFalse();
        tracker
            .ShouldWrite(Display, new ColorConfiguration(2700, 0.85), false, Start)
            .Should()
            .BeTrue();
        tracker
            .ShouldWrite(Display, new ColorConfiguration(2650, 0.80), false, Start)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void I_get_gamma_rewritten_when_it_may_have_been_changed_from_the_outside()
    {
        // Arrange
        var tracker = new GammaWriteTracker();
        tracker.RecordSuccess(Display, Night);

        // Act & assert
        tracker.ShouldWrite(Display, Night, true, Start).Should().BeTrue();
    }

    [Fact]
    public void I_get_a_rejected_gamma_write_retried_after_a_short_delay_rather_than_never()
    {
        // Arrange
        // E.g. the display wasn't ready yet after waking up
        var tracker = new GammaWriteTracker();
        tracker.RecordSuccess(Display, Night);
        tracker.RecordFailure(Display, Start);

        // Act & assert
        // Same configuration as the last successful write, which the display isn't showing anymore
        tracker
            .ShouldWrite(Display, Night, false, Start + TimeSpan.FromMilliseconds(50))
            .Should()
            .BeFalse();
        tracker
            .ShouldWrite(Display, Night, false, Start + GammaWriteTracker.RetryDelay)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void I_get_normal_change_detection_back_once_a_retried_write_succeeds()
    {
        // Arrange
        var tracker = new GammaWriteTracker();
        tracker.RecordFailure(Display, Start);

        // Act
        tracker.RecordSuccess(Display, Night);

        // Assert
        tracker
            .ShouldWrite(Display, Night, false, Start + TimeSpan.FromSeconds(5))
            .Should()
            .BeFalse();
    }

    [Fact]
    public void I_get_failures_on_one_display_kept_separate_from_other_displays()
    {
        // Arrange
        var tracker = new GammaWriteTracker();
        tracker.RecordSuccess(Display, Night);
        tracker.RecordSuccess(@"\\.\DISPLAY2", Night);
        tracker.RecordFailure(@"\\.\DISPLAY2", Start);

        // Act & assert
        tracker.ShouldWrite(Display, Night, false, Start).Should().BeFalse();
        tracker
            .ShouldWrite(@"\\.\display2", Night, false, Start + GammaWriteTracker.RetryDelay)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void I_get_gamma_written_to_all_displays_after_the_set_of_displays_changes()
    {
        // Arrange
        var tracker = new GammaWriteTracker();
        tracker.RecordSuccess(Display, Night);
        tracker.RecordFailure(@"\\.\DISPLAY2", Start);

        // Act
        tracker.Reset();

        // Assert
        tracker.ShouldWrite(Display, Night, false, Start).Should().BeTrue();
        tracker.ShouldWrite(@"\\.\DISPLAY2", Night, false, Start).Should().BeTrue();
    }
}
