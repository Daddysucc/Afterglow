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

    private static string[] GetLines(string text) =>
        text.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

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

    [Theory]
    [MemberData(nameof(TranslatedLanguages))]
    public void I_can_see_as_many_status_phrases_in_each_language_as_in_english(Language language)
    {
        // Arrange
        var localization = LocalizationManager.GetLocalization(language);

        foreach (
            var key in new[]
            {
                nameof(LocalizationManager.StatusDayPhrases),
                nameof(LocalizationManager.StatusEveningPhrases),
                nameof(LocalizationManager.StatusNightPhrases),
            }
        )
        {
            // Act
            var phrases = GetLines(localization[key]);

            // Assert
            phrases
                .Should()
                .HaveSameCount(GetLines(English[key]), $"'{key}' has one phrase per line");
            phrases.Should().OnlyHaveUniqueItems();

            // The status sentence is a single line at the top of the dashboard, which fits about
            // as many characters as the longest English phrase, even at the smallest window size
            phrases
                .Should()
                .AllSatisfy(p =>
                    p.Length.Should()
                        .BeLessThanOrEqualTo(
                            MaxStatusPhraseLength,
                            $"'{p}' must fit the dashboard header"
                        )
                );
        }
    }

    private const int MaxStatusPhraseLength = 70;

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
