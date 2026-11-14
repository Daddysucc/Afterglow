using System;
using System.IO;
using AwesomeAssertions;
using LightBulb.Core;
using LightBulb.Localization;
using LightBulb.Services;
using LightBulb.Utils;
using Xunit;

namespace LightBulb.Tests;

public class StatusSentenceSpecs : IDisposable
{
    private static readonly ColorConfiguration Day = new(6500, 1);
    private static readonly ColorConfiguration Evening = new(4500, 0.9);
    private static readonly ColorConfiguration Night = new(2700, 0.75);

    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "LightBulb.Tests." + Guid.NewGuid().ToString("N")
    );

    private readonly SettingsService _settings;
    private readonly LocalizationManager _localization;

    public StatusSentenceSpecs()
    {
        Directory.CreateDirectory(_directoryPath);

        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(filePath, """{ "IsFirstTimeExperienceEnabled": false }""");

        _settings = new SettingsService(filePath);
        _settings.Load();
        _settings.Language = Language.English;

        _localization = new LocalizationManager(_settings);
    }

    private string Build(
        CycleStage stage,
        bool isTransitioning = false,
        ColorConfiguration? day = null,
        ColorConfiguration? evening = null,
        ColorConfiguration? night = null,
        bool isEveningIncluded = true,
        bool isPreviewing = false,
        bool isEnabled = true,
        PauseReason pauseReason = PauseReason.None,
        bool isManualOverride = false,
        ColorConfiguration? current = null
    ) =>
        StatusSentenceBuilder.Build(
            new StatusSentenceContext(
                isPreviewing,
                isEnabled,
                pauseReason,
                isManualOverride,
                current ?? Day,
                new CyclePhase(stage, isTransitioning),
                isEveningIncluded,
                day ?? Day,
                evening ?? Evening,
                night ?? Night
            ),
            _localization
        );

    [Theory]
    [InlineData(CycleStage.Day, "Daytime schedule active at 6500 K and 100% brightness.")]
    [InlineData(CycleStage.Evening, "Evening schedule active at 4500 K and 90% brightness.")]
    [InlineData(CycleStage.Night, "Night schedule active at 2700 K and 75% brightness.")]
    public void I_can_see_the_active_stage_and_its_values_when_nothing_is_changing(
        CycleStage stage,
        string expected
    )
    {
        // Act & assert
        Build(stage).Should().Be(expected);
    }

    [Fact]
    public void I_can_see_the_evening_transition_with_both_values_changing()
    {
        // Act & assert
        Build(CycleStage.Evening, true)
            .Should()
            .Be("Warming to 4500 K and dimming to 90% for evening.");
    }

    [Fact]
    public void I_can_see_only_the_color_mentioned_when_the_brightness_stays_the_same()
    {
        // Act & assert
        Build(CycleStage.Evening, true, evening: new ColorConfiguration(4500, 1))
            .Should()
            .Be("Warming to 4500 K for evening.");

        Build(CycleStage.Night, true, night: new ColorConfiguration(2700, 0.9))
            .Should()
            .Be("Warming to 2700 K for night.");

        Build(CycleStage.Day, true, night: new ColorConfiguration(2700, 1))
            .Should()
            .Be("Cooling to 6500 K for day.");
    }

    [Fact]
    public void I_can_see_only_the_brightness_mentioned_when_the_color_stays_the_same()
    {
        // Act & assert
        Build(CycleStage.Evening, true, evening: new ColorConfiguration(6500, 0.9))
            .Should()
            .Be("Dimming to 90% for evening.");

        Build(CycleStage.Night, true, night: new ColorConfiguration(4500, 0.75))
            .Should()
            .Be("Dimming to 75% for night.");

        Build(CycleStage.Day, true, night: new ColorConfiguration(6500, 0.75))
            .Should()
            .Be("Brightening to 100% for day.");
    }

    [Fact]
    public void I_can_see_the_night_and_sunrise_transitions_with_both_values_changing()
    {
        // Act & assert
        Build(CycleStage.Night, true)
            .Should()
            .Be("Warming to 2700 K and dimming to 75% for night.");

        Build(CycleStage.Day, true)
            .Should()
            .Be("Cooling to 6500 K and brightening to 100% for day.");
    }

    [Fact]
    public void I_can_see_the_night_transition_start_from_the_day_when_the_evening_is_skipped()
    {
        // Act & assert
        // The evening stage has the same values as the night, but the cycle comes from the day
        Build(CycleStage.Night, true, evening: Night, isEveningIncluded: false)
            .Should()
            .Be("Warming to 2700 K and dimming to 75% for night.");
    }

    [Theory]
    // Custom schedules where stages go the other way than usual
    [InlineData(4000, 0.8, 5000, 0.9, "Cooling to 5000 K and brightening to 90% for evening.")]
    [InlineData(4000, 0.9, 5000, 0.8, "Cooling to 5000 K and dimming to 80% for evening.")]
    [InlineData(5000, 0.8, 4000, 0.9, "Warming to 4000 K and brightening to 90% for evening.")]
    [InlineData(4000, 0.9, 5000, 0.9, "Cooling to 5000 K for evening.")]
    [InlineData(5000, 0.8, 5000, 0.9, "Brightening to 90% for evening.")]
    public void I_can_see_transitions_described_correctly_on_unusual_custom_schedules(
        double dayTemperature,
        double dayBrightness,
        double eveningTemperature,
        double eveningBrightness,
        string expected
    )
    {
        // Act & assert
        Build(
                CycleStage.Evening,
                true,
                day: new ColorConfiguration(dayTemperature, dayBrightness),
                evening: new ColorConfiguration(eveningTemperature, eveningBrightness)
            )
            .Should()
            .Be(expected);
    }

    [Fact]
    public void I_can_see_reverse_transitions_into_the_day_and_night()
    {
        // Act & assert
        // A night that's cooler and brighter than the day
        Build(
                CycleStage.Day,
                true,
                day: new ColorConfiguration(5000, 0.8),
                night: new ColorConfiguration(6500, 1)
            )
            .Should()
            .Be("Warming to 5000 K and dimming to 80% for day.");

        Build(
                CycleStage.Night,
                true,
                evening: new ColorConfiguration(3000, 0.7),
                night: new ColorConfiguration(3500, 0.8)
            )
            .Should()
            .Be("Cooling to 3500 K and brightening to 80% for night.");
    }

    [Fact]
    public void I_can_see_the_stage_itself_when_a_transition_changes_nothing_visible()
    {
        // Act & assert
        Build(CycleStage.Evening, true, evening: Day)
            .Should()
            .Be("Evening schedule active at 6500 K and 100% brightness.");

        // Differences that round away aren't mentioned either
        Build(
                CycleStage.Evening,
                true,
                day: new ColorConfiguration(4500.2, 0.901),
                evening: new ColorConfiguration(4500.4, 0.899)
            )
            .Should()
            .Be("Evening schedule active at 4500 K and 90% brightness.");
    }

    [Fact]
    public void I_can_see_a_manual_override_and_its_values_even_during_a_transition()
    {
        // Act & assert
        Build(
                CycleStage.Evening,
                true,
                isManualOverride: true,
                current: new ColorConfiguration(3800, 0.85)
            )
            .Should()
            .Be("Manual override active at 3800 K and 85% brightness.");
    }

    [Theory]
    [InlineData(
        PauseReason.RemoteStreaming,
        "Paused for remote streaming with native color restored."
    )]
    [InlineData(PauseReason.FullScreen, "Paused while a fullscreen application is active.")]
    [InlineData(PauseReason.ScreenLocked, "Paused while the screen is locked.")]
    [InlineData(PauseReason.Whitelist, "Paused automatically with native color restored.")]
    public void I_can_see_why_lightbulb_paused_itself_ahead_of_everything_else(
        PauseReason reason,
        string expected
    )
    {
        // Act & assert
        Build(CycleStage.Evening, true, pauseReason: reason, isManualOverride: true)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void I_can_see_that_lightbulb_was_paused_manually()
    {
        // Act & assert
        Build(CycleStage.Night, pauseReason: PauseReason.FullScreen, isEnabled: false)
            .Should()
            .Be("Paused manually with automatic schedule suspended.");
    }

    [Fact]
    public void I_can_see_the_preview_described_while_it_runs()
    {
        // Act & assert
        Build(CycleStage.Night, true, isPreviewing: true, isEnabled: false)
            .Should()
            .Be("Previewing the next 24 hours…");
    }

    [Fact]
    public void I_can_see_extreme_values_described_in_full()
    {
        // Act & assert
        Build(CycleStage.Day, day: new ColorConfiguration(10_000, 1))
            .Should()
            .Be("Daytime schedule active at 10000 K and 100% brightness.");
    }

    [Fact]
    public void I_can_see_the_status_sentence_in_my_language()
    {
        // Arrange
        _settings.Language = Language.German;

        // Act & assert
        Build(CycleStage.Evening, true)
            .Should()
            .Be("Erwärmung auf 4500 K und Dimmen auf 90 % für den Abend.");

        Build(CycleStage.Night).Should().Be("Nachtplan aktiv mit 2700 K und 75 % Helligkeit.");
    }

    public void Dispose()
    {
        _localization.Dispose();

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
