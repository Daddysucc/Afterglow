using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using LightBulb.Core;
using LightBulb.PlatformInterop;
using LightBulb.Services;
using Xunit;

namespace LightBulb.Tests;

// The real screen effect is never used here: the service is given a fake one
public class GrayscaleSpecs : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "LightBulb.Tests." + Guid.NewGuid().ToString("N")
    );

    public GrayscaleSpecs() => Directory.CreateDirectory(_directoryPath);

    // ---- Schedule ----

    [Theory]
    [InlineData(GrayscaleSchedule.Off, CycleStage.Day, false)]
    [InlineData(GrayscaleSchedule.Off, CycleStage.Evening, false)]
    [InlineData(GrayscaleSchedule.Off, CycleStage.Night, false)]
    [InlineData(GrayscaleSchedule.NightOnly, CycleStage.Day, false)]
    [InlineData(GrayscaleSchedule.NightOnly, CycleStage.Evening, false)]
    [InlineData(GrayscaleSchedule.NightOnly, CycleStage.Night, true)]
    [InlineData(GrayscaleSchedule.EveningAndNight, CycleStage.Day, false)]
    [InlineData(GrayscaleSchedule.EveningAndNight, CycleStage.Evening, true)]
    [InlineData(GrayscaleSchedule.EveningAndNight, CycleStage.Night, true)]
    [InlineData(GrayscaleSchedule.On, CycleStage.Day, true)]
    [InlineData(GrayscaleSchedule.On, CycleStage.Evening, true)]
    [InlineData(GrayscaleSchedule.On, CycleStage.Night, true)]
    public void I_can_have_grayscale_turn_on_by_itself_during_the_scheduled_stages(
        GrayscaleSchedule schedule,
        CycleStage stage,
        bool expected
    )
    {
        // Act & assert
        GrayscaleController
            .IsScheduled(schedule, new CyclePhase(stage, false))
            .Should()
            .Be(expected);
    }

    [Fact]
    public void I_can_have_grayscale_turn_on_as_soon_as_the_transition_into_a_scheduled_stage_starts()
    {
        // Act & assert
        GrayscaleController
            .IsScheduled(GrayscaleSchedule.NightOnly, new CyclePhase(CycleStage.Night, true))
            .Should()
            .BeTrue();

        GrayscaleController
            .IsScheduled(GrayscaleSchedule.NightOnly, new CyclePhase(CycleStage.Day, true))
            .Should()
            .BeFalse();
    }

    // ---- Manual toggle ----

    [Fact]
    public void I_can_toggle_grayscale_on_and_off_manually_without_a_schedule()
    {
        // Arrange
        var controller = new GrayscaleController();

        // Act
        var afterOn = GrayscaleController.Toggle(false, null);
        var keptOn = controller.UpdateOverride(false, afterOn);
        var afterOff = GrayscaleController.Toggle(false, keptOn);

        // Assert
        GrayscaleController.IsWanted(false, keptOn).Should().BeTrue();
        GrayscaleController.IsWanted(false, afterOff).Should().BeFalse();

        // Without a schedule, the manual choice stays until it's toggled again
        controller.UpdateOverride(false, afterOn).Should().Be(afterOn);
    }

    [Fact]
    public void I_can_turn_grayscale_off_for_one_night_and_have_the_schedule_take_over_again_later()
    {
        // Arrange
        var controller = new GrayscaleController();
        controller.UpdateOverride(true, null); // Night, grayscale on by schedule

        // Act
        var manualOverride = GrayscaleController.Toggle(true, null);
        var duringNight = controller.UpdateOverride(true, manualOverride);
        var nextMorning = controller.UpdateOverride(false, duringNight);
        var nextNight = controller.UpdateOverride(true, nextMorning);

        // Assert
        GrayscaleController.IsWanted(true, duringNight).Should().BeFalse();
        GrayscaleController.IsWanted(false, nextMorning).Should().BeFalse();
        nextMorning.Should().BeNull();
        GrayscaleController.IsWanted(true, nextNight).Should().BeTrue();
    }

    [Fact]
    public void I_can_turn_grayscale_on_early_and_have_it_follow_the_schedule_from_then_on()
    {
        // Arrange
        var controller = new GrayscaleController();
        controller.UpdateOverride(false, null); // Evening, schedule is night only

        // Act
        var manualOverride = GrayscaleController.Toggle(false, null);
        var beforeNight = controller.UpdateOverride(false, manualOverride);
        var atNight = controller.UpdateOverride(true, beforeNight);
        var nextMorning = controller.UpdateOverride(false, atNight);

        // Assert
        GrayscaleController.IsWanted(false, beforeNight).Should().BeTrue();
        GrayscaleController.IsWanted(true, atNight).Should().BeTrue();
        GrayscaleController.IsWanted(false, nextMorning).Should().BeFalse();
    }

    [Fact]
    public void I_can_toggle_back_to_what_the_schedule_wants_without_leaving_an_override_behind()
    {
        // Act & assert
        GrayscaleController.Toggle(true, false).Should().BeNull();
        GrayscaleController.Toggle(false, true).Should().BeNull();
    }

    [Fact]
    public void I_keep_a_saved_manual_choice_after_restarting_until_the_schedule_changes()
    {
        // Arrange
        var controller = new GrayscaleController();

        // Act
        // The first update after starting has nothing to compare to
        var manualOverride = controller.UpdateOverride(true, false);

        // Assert
        manualOverride.Should().BeFalse();
    }

    // ---- Pauses ----

    [Fact]
    public void I_see_grayscale_suspended_while_lightbulb_is_paused_and_restored_when_it_resumes()
    {
        // Act & assert
        GrayscaleController.IsApplied(true, true, false).Should().BeTrue();
        GrayscaleController.IsApplied(true, false, false).Should().BeFalse();
        GrayscaleController.IsApplied(true, true, false).Should().BeTrue();
    }

    [Fact]
    public void I_see_grayscale_suspended_while_a_fullscreen_application_pauses_one_display()
    {
        // Act & assert
        // Grayscale always applies to all displays, so it can't spare the one running the game
        GrayscaleController.IsApplied(true, true, true).Should().BeFalse();
        GrayscaleController.IsApplied(false, true, false).Should().BeFalse();
    }

    // ---- Color temperature ----

    [Fact]
    public void I_see_the_color_temperature_tint_grayscale_when_temperature_shifting_is_enabled()
    {
        // Arrange
        var night = new ColorConfiguration(3200, 0.85);

        // Act & assert
        GrayscaleController.GetConfiguration(night, true, true).Should().Be(night);
    }

    [Fact]
    public void I_see_a_neutral_white_point_with_my_brightness_when_temperature_shifting_is_disabled()
    {
        // Arrange
        var night = new ColorConfiguration(3200, 0.85);

        // Act
        var configuration = GrayscaleController.GetConfiguration(night, true, false);

        // Assert
        configuration.Temperature.Should().Be(GrayscaleController.NeutralTemperature);
        configuration.Temperature.Should().Be(ColorConfiguration.Default.Temperature);
        configuration.Brightness.Should().Be(0.85);
    }

    [Fact]
    public void I_see_the_scheduled_temperature_again_as_soon_as_grayscale_turns_off()
    {
        // Arrange
        var night = new ColorConfiguration(3200, 0.85);

        // Act & assert
        GrayscaleController.GetConfiguration(night, false, false).Should().Be(night);
    }

    // ---- Screen effect ----

    [Fact]
    public void I_get_grayscale_based_on_the_luminance_of_each_color()
    {
        // Arrange
        var matrix = FullscreenColorEffect.GrayscaleMatrix;
        float At(int row, int column) => matrix[row * 5 + column];

        // Assert
        matrix.Should().HaveCount(25);

        // Each output color channel gets the same BT.709 luminance of the input color
        for (var column = 0; column < 3; column++)
        {
            At(0, column).Should().Be(0.2126f);
            At(1, column).Should().Be(0.7152f);
            At(2, column).Should().Be(0.0722f);
            (At(0, column) + At(1, column) + At(2, column)).Should().BeApproximately(1, 1e-6f);
        }

        // Alpha and the translation row are left alone
        At(3, 3).Should().Be(1);
        At(4, 4).Should().Be(1);
        matrix.Sum().Should().BeApproximately(3 + 2, 1e-5f);
    }

    [Fact]
    public void I_get_an_unchanged_screen_from_the_identity_effect()
    {
        // Arrange
        var matrix = FullscreenColorEffect.IdentityMatrix;

        // Assert
        matrix.Should().HaveCount(25);
        Enumerable.Range(0, 25).Should().AllSatisfy(i => matrix[i].Should().Be(i % 6 == 0 ? 1 : 0));
    }

    // ---- Service lifecycle ----

    private sealed class FakeScreenEffect
    {
        public bool IsAvailable { get; set; } = true;
        public bool IsGrayscale { get; private set; }
        public int ApplyCount { get; private set; }
        public int ResetCount { get; private set; }

        public bool TryApply()
        {
            ApplyCount++;
            IsGrayscale = IsAvailable;
            return IsAvailable;
        }

        public void Reset()
        {
            ResetCount++;
            IsGrayscale = false;
        }
    }

    private static (GrayscaleService Service, FakeScreenEffect Effect) CreateService(
        Func<DateTimeOffset>? getNow = null
    )
    {
        var effect = new FakeScreenEffect();
        var service = new GrayscaleService(
            effect.TryApply,
            effect.Reset,
            action => action(),
            getNow ?? (() => DateTimeOffset.Now)
        );

        return (service, effect);
    }

    [Fact]
    public void I_do_not_get_the_magnification_api_touched_until_grayscale_is_turned_on()
    {
        // Arrange
        var (service, effect) = CreateService();

        // Act
        service.SetEnabled(false);

        // Assert
        effect.ApplyCount.Should().Be(0);
        service.IsApplied.Should().BeFalse();
    }

    [Fact]
    public void I_can_turn_grayscale_on_and_off_and_on_again()
    {
        // Arrange
        var (service, effect) = CreateService();

        // Act & assert
        service.SetEnabled(true);
        effect.IsGrayscale.Should().BeTrue();

        // Repeated updates don't re-apply it
        service.SetEnabled(true);
        effect.ApplyCount.Should().Be(1);

        service.SetEnabled(false);
        effect.IsGrayscale.Should().BeFalse();
        service.IsApplied.Should().BeFalse();

        service.SetEnabled(true);
        effect.IsGrayscale.Should().BeTrue();
    }

    [Fact]
    public void I_do_not_get_the_screen_left_grayscale_when_lightbulb_exits()
    {
        // Arrange
        var (service, effect) = CreateService();
        service.SetEnabled(true);

        // Act
        service.Dispose();
        service.SetEnabled(true);

        // Assert
        effect.IsGrayscale.Should().BeFalse();
        effect.ApplyCount.Should().Be(1);
    }

    [Fact]
    public void I_do_not_get_a_failing_magnification_api_retried_on_every_update()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 11, 14, 22, 0, 0, TimeSpan.Zero);
        var (service, effect) = CreateService(() => now);
        effect.IsAvailable = false;

        // Act & assert
        service.SetEnabled(true);
        service.IsApplied.Should().BeFalse();
        effect.ResetCount.Should().Be(1, "a failed attempt must not leave the API initialized");

        now += TimeSpan.FromSeconds(1);
        service.SetEnabled(true);
        effect.ApplyCount.Should().Be(1);

        effect.IsAvailable = true;
        now += GrayscaleService.RetryDelay;
        service.SetEnabled(true);
        service.IsApplied.Should().BeTrue();
    }

    // ---- Settings ----

    private SettingsService LoadSettings(string json)
    {
        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(filePath, json);

        var settings = new SettingsService(filePath);
        settings.Load();

        return settings;
    }

    [Fact]
    public void I_get_grayscale_off_with_temperature_shifting_by_default_in_older_settings_files()
    {
        // Act
        var settings = LoadSettings("""{ "IsFirstTimeExperienceEnabled": false }""");

        // Assert
        settings.GrayscaleSchedule.Should().Be(GrayscaleSchedule.Off);
        settings.IsGrayscaleTemperatureShiftEnabled.Should().BeTrue();
        settings.GrayscaleOverride.Should().BeNull();
        settings.ToggleGrayscaleHotKey.Should().Be(LightBulb.Models.HotKey.None);
    }

    [Fact]
    public void I_can_save_and_load_grayscale_settings()
    {
        // Arrange
        var settings = LoadSettings("{}");
        settings.GrayscaleSchedule = GrayscaleSchedule.EveningAndNight;
        settings.IsGrayscaleTemperatureShiftEnabled = false;
        settings.GrayscaleOverride = false;

        // Act
        settings.TrySaveToFile().Should().BeTrue();
        var reloaded = new SettingsService(Path.Combine(_directoryPath, "Settings.json"));
        reloaded.Load();

        // Assert
        reloaded.GrayscaleSchedule.Should().Be(GrayscaleSchedule.EveningAndNight);
        reloaded.IsGrayscaleTemperatureShiftEnabled.Should().BeFalse();
        reloaded.GrayscaleOverride.Should().BeFalse();
    }

    [Fact]
    public void I_get_grayscale_off_when_the_settings_file_has_an_unknown_schedule_number()
    {
        // Act
        var settings = LoadSettings(
            """{ "GrayscaleSchedule": 7, "NightConfiguration": { "Temperature": 3000, "Brightness": 0.8 } }"""
        );

        // Assert
        settings.GrayscaleSchedule.Should().Be(GrayscaleSchedule.Off);
        settings.NightConfiguration.Should().Be(new ColorConfiguration(3000, 0.8));
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
