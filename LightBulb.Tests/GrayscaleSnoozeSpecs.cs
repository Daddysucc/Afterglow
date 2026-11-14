using System;
using System.IO;
using AwesomeAssertions;
using LightBulb.Core;
using LightBulb.Localization;
using LightBulb.Services;
using LightBulb.ViewModels.Components.Settings;
using Xunit;

namespace LightBulb.Tests;

public class GrayscaleSnoozeSpecs : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 22, 0, 0, TimeSpan.Zero);

    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "LightBulb.Tests." + Guid.NewGuid().ToString("N")
    );

    public GrayscaleSnoozeSpecs() => Directory.CreateDirectory(_directoryPath);

    private SettingsService LoadSettings(string json)
    {
        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(filePath, json);

        var settings = new SettingsService(filePath);
        settings.Load();

        return settings;
    }

    // ---- "On" schedule ----

    [Fact]
    public void I_can_turn_grayscale_off_for_now_while_it_is_always_on()
    {
        // Arrange
        var controller = new GrayscaleController();
        controller.UpdateOverride(true, null);

        // Act
        var manualOverride = GrayscaleController.Toggle(true, null);

        // Assert
        // The schedule never changes by itself, so the choice lasts until toggled back
        var later = controller.UpdateOverride(true, manualOverride);
        GrayscaleController.IsWanted(true, later).Should().BeFalse();

        GrayscaleController.Toggle(true, later).Should().BeNull();
    }

    [Fact]
    public void I_get_grayscale_back_after_restarting_when_it_is_always_on()
    {
        // Act
        var settings = LoadSettings(
            """{ "GrayscaleSchedule": "On", "GrayscaleOverride": false }"""
        );

        // Assert
        settings.GrayscaleOverride.Should().BeNull();
    }

    [Fact]
    public void I_keep_a_manual_choice_after_restarting_with_a_schedule_that_changes()
    {
        // Act
        var settings = LoadSettings(
            """{ "GrayscaleSchedule": "NightOnly", "GrayscaleOverride": false }"""
        );

        // Assert
        settings.GrayscaleOverride.Should().BeFalse();
    }

    [Fact]
    public void I_get_a_new_grayscale_schedule_applied_right_away_without_an_earlier_manual_choice()
    {
        // Arrange
        var settings = LoadSettings("""{ "GrayscaleSchedule": "NightOnly" }""");
        using var localization = new LocalizationManager(settings);
        using var viewModel = new CycleSettingsTabViewModel(settings, localization);

        settings.GrayscaleOverride = false;

        // Act
        viewModel.GrayscaleScheduleIndex = (int)GrayscaleSchedule.On;

        // Assert
        settings.GrayscaleSchedule.Should().Be(GrayscaleSchedule.On);
        settings.GrayscaleOverride.Should().BeNull();
    }

    [Fact]
    public void I_keep_my_manual_choice_when_the_same_schedule_is_selected_again()
    {
        // Arrange
        var settings = LoadSettings("""{ "GrayscaleSchedule": "NightOnly" }""");
        using var localization = new LocalizationManager(settings);
        using var viewModel = new CycleSettingsTabViewModel(settings, localization);

        settings.GrayscaleOverride = false;

        // Act
        viewModel.GrayscaleScheduleIndex = (int)GrayscaleSchedule.NightOnly;

        // Assert
        settings.GrayscaleOverride.Should().BeFalse();
    }

    // ---- Snooze ----

    [Theory]
    [InlineData(GrayscaleSchedule.Off, false)]
    [InlineData(GrayscaleSchedule.On, false)]
    [InlineData(GrayscaleSchedule.NightOnly, true)]
    [InlineData(GrayscaleSchedule.EveningAndNight, true)]
    public void I_am_offered_the_right_ways_to_disable_grayscale_for_my_schedule(
        GrayscaleSchedule schedule,
        bool isUntilScheduleChangeOffered
    )
    {
        // Act & assert
        // Durations are offered when the schedule doesn't change during the day, and
        // "until the next schedule change" when it does
        GrayscaleController
            .HasScheduleChanges(schedule)
            .Should()
            .Be(isUntilScheduleChangeOffered);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    public void I_can_disable_grayscale_for_a_while_and_have_it_come_back_by_itself(int minutes)
    {
        // Arrange
        var until = Now + TimeSpan.FromMinutes(minutes);

        // Act & assert
        GrayscaleController.IsSnoozed(until, Now).Should().BeTrue();
        GrayscaleController.IsSnoozed(until, until - TimeSpan.FromSeconds(1)).Should().BeTrue();
        GrayscaleController.IsSnoozed(until, until).Should().BeFalse();
        GrayscaleController.IsSnoozed(until, until + TimeSpan.FromMinutes(1)).Should().BeFalse();
    }

    [Fact]
    public void I_can_resume_grayscale_at_any_time()
    {
        // Act & assert
        GrayscaleController.IsSnoozed(null, Now).Should().BeFalse();
    }

    [Fact]
    public void I_see_grayscale_suspended_while_disabled_and_restored_once_the_time_is_up()
    {
        // Arrange
        var until = Now + TimeSpan.FromMinutes(15);

        bool IsApplied(DateTimeOffset instant) =>
            GrayscaleController.IsApplied(
                GrayscaleController.IsWanted(true, null)
                    && !GrayscaleController.IsSnoozed(until, instant),
                true,
                false
            );

        // Act & assert
        IsApplied(Now).Should().BeFalse();
        IsApplied(until).Should().BeTrue();
    }

    [Fact]
    public void I_can_disable_grayscale_until_the_next_schedule_change()
    {
        // Arrange
        // Night only schedule, at night
        var controller = new GrayscaleController();
        controller.UpdateOverride(true, null);

        // Act
        // Disabling until the next schedule change is the same as turning it off manually
        var heldOff = GrayscaleController.Toggle(true, null);
        var stillNight = controller.UpdateOverride(true, heldOff);
        var morning = controller.UpdateOverride(false, stillNight);
        var nextNight = controller.UpdateOverride(true, morning);

        // Assert
        stillNight.Should().BeFalse();
        morning.Should().BeNull();
        GrayscaleController.IsWanted(true, nextNight).Should().BeTrue();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directoryPath, true);
        }
        catch
        {
            // Ignore
        }
    }
}
