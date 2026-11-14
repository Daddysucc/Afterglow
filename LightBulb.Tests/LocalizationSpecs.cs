using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using LightBulb.Localization;
using LightBulb.Services;
using Xunit;

namespace LightBulb.Tests;

public class LocalizationSpecs : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "LightBulb.Tests." + Guid.NewGuid().ToString("N")
    );

    public LocalizationSpecs() => Directory.CreateDirectory(_directoryPath);

    public static TheoryData<Language> TranslatedLanguages { get; } =
        new(Enum.GetValues<Language>().Where(l => l is not (Language.System or Language.English)));

    private static IReadOnlyDictionary<string, string> English =>
        LocalizationManager.EnglishStrings;

    private static string[] GetPlaceholders(string text) =>
        Regex
            .Matches(text, @"\{(\d+)\}")
            .Select(m => m.Value)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToArray();

    [Theory]
    [MemberData(nameof(TranslatedLanguages))]
    public void I_can_see_every_part_of_the_interface_translated_in_each_supported_language(
        Language language
    )
    {
        // Act
        var localization = LocalizationManager.GetLocalization(language);

        // Assert
        localization.Should().NotBeSameAs(English);
        localization.Keys.Should().BeEquivalentTo(English.Keys);
        localization.Values.Should().AllSatisfy(v => v.Should().NotBeNullOrWhiteSpace());
    }

    [Theory]
    [MemberData(nameof(TranslatedLanguages))]
    public void I_can_see_translated_text_with_the_same_values_filled_in_as_in_english(
        Language language
    )
    {
        // Arrange
        var localization = LocalizationManager.GetLocalization(language);
        var arguments = new object[] { "A", "B", "C" };

        foreach (var (key, english) in English)
        {
            if (!localization.TryGetValue(key, out var translation))
                continue;

            // Assert
            GetPlaceholders(translation)
                .Should()
                .Equal(GetPlaceholders(english), $"'{key}' must use the same placeholders");

            // Formatting must never throw at runtime
            var format = () => string.Format(translation, arguments);
            format.Should().NotThrow($"'{key}' must be a valid format string");
        }
    }

    // The status sentence is a single line at the top of the dashboard, which fits about this many
    // characters at the smallest window size. English is kept shorter, so translations have room.
    private const int MaxStatusSentenceLength = 70;
    private const int MaxEnglishStatusSentenceLength = 57;

    private static readonly string[] StatusSentenceKeys =
    [
        nameof(LocalizationManager.StatusDayScheduleText),
        nameof(LocalizationManager.StatusEveningScheduleText),
        nameof(LocalizationManager.StatusNightScheduleText),
        nameof(LocalizationManager.StatusWarmingText),
        nameof(LocalizationManager.StatusCoolingText),
        nameof(LocalizationManager.StatusDimmingText),
        nameof(LocalizationManager.StatusBrighteningText),
        nameof(LocalizationManager.StatusWarmingDimmingText),
        nameof(LocalizationManager.StatusWarmingBrighteningText),
        nameof(LocalizationManager.StatusCoolingDimmingText),
        nameof(LocalizationManager.StatusCoolingBrighteningText),
        nameof(LocalizationManager.StatusManualOverrideText),
        nameof(LocalizationManager.StatusPausedRemoteStreamingText),
        nameof(LocalizationManager.StatusPausedFullScreenText),
        nameof(LocalizationManager.StatusPausedScreenLockedText),
        nameof(LocalizationManager.StatusPausedManuallyText),
        nameof(LocalizationManager.StatusPausedAutomaticallyText),
        nameof(LocalizationManager.StatusPreviewText),
    ];

    private static readonly string[] StatusTargetKeys =
    [
        nameof(LocalizationManager.StatusForDayText),
        nameof(LocalizationManager.StatusForEveningText),
        nameof(LocalizationManager.StatusForNightText),
    ];

    public static TheoryData<Language> AllLanguages { get; } =
        new(Enum.GetValues<Language>().Where(l => l is not Language.System));

    // Every status sentence a language can show, with the longest possible values filled in
    private static string[] RenderAllStatusSentences(Language language)
    {
        var localization = LocalizationManager.GetLocalization(language);

        return StatusSentenceKeys
            .SelectMany(key =>
                StatusTargetKeys.Select(targetKey =>
                    string.Format(
                        LocalizationManager.Resolve(localization, key),
                        "10000",
                        "100",
                        LocalizationManager.Resolve(localization, targetKey)
                    )
                )
            )
            .Distinct()
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public void I_can_read_every_status_sentence_in_full_in_the_dashboard_header(Language language)
    {
        // Act
        var sentences = RenderAllStatusSentences(language);

        // Assert
        sentences
            .Should()
            .AllSatisfy(s =>
                s.Length.Should()
                    .BeLessThanOrEqualTo(
                        MaxStatusSentenceLength,
                        $"'{s}' must fit the dashboard header"
                    )
            );
    }

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public void I_can_read_every_status_sentence_as_a_plain_sentence(Language language)
    {
        // Act
        var sentences = RenderAllStatusSentences(language);

        // Assert
        sentences
            .Should()
            .AllSatisfy(s =>
            {
                s.Should().NotContainAny("·", "—", "–");
                s.Trim().Should().Be(s);
            });
    }

    [Fact]
    public void I_can_read_every_english_status_sentence_with_room_to_spare()
    {
        // Act
        var sentences = RenderAllStatusSentences(Language.English);

        // Assert
        sentences
            .Should()
            .AllSatisfy(s =>
                s.Length.Should().BeLessThanOrEqualTo(MaxEnglishStatusSentenceLength, s)
            );
    }

    [Fact]
    public void I_can_see_english_text_for_anything_a_translation_does_not_have()
    {
        // Arrange
        var partial = new Dictionary<string, string>
        {
            [nameof(LocalizationManager.DayStateLabel)] = "Tag",
            [nameof(LocalizationManager.NightStateLabel)] = "   ",
        };

        // Act & assert
        LocalizationManager
            .Resolve(partial, nameof(LocalizationManager.DayStateLabel))
            .Should()
            .Be("Tag");

        // Missing
        LocalizationManager
            .Resolve(partial, nameof(LocalizationManager.EveningStateLabel))
            .Should()
            .Be(English[nameof(LocalizationManager.EveningStateLabel)]);

        // Blank
        LocalizationManager
            .Resolve(partial, nameof(LocalizationManager.NightStateLabel))
            .Should()
            .Be(English[nameof(LocalizationManager.NightStateLabel)]);

        // Unknown everywhere
        LocalizationManager.Resolve(partial, "NoSuchKey").Should().Contain("NoSuchKey");
    }

    [Fact]
    public void I_can_switch_the_interface_language_and_see_the_new_features_translated()
    {
        // Arrange
        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(filePath, """{ "IsFirstTimeExperienceEnabled": false }""");

        var settings = new SettingsService(filePath);
        settings.Load();

        using var localization = new LocalizationManager(settings);

        // Act
        settings.Language = Language.German;

        // Assert
        localization.EveningStateLabel.Should().Be("Abend");
        localization.DisplaysCardTitle.Should().Be("Verbundene Bildschirme");
        localization.PerceptualOffsetScalingOption.Should().Be("Mired (Wahrnehmung)");

        // Act
        settings.Language = Language.English;

        // Assert
        localization.EveningStateLabel.Should().Be("Evening");
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
