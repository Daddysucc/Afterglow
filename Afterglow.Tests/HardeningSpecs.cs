using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Afterglow.Core;
using Afterglow.Framework;
using Afterglow.Localization;
using Afterglow.PlatformInterop;
using Afterglow.Services;
using Afterglow.ViewModels.Components.Settings;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Xunit;

namespace Afterglow.Tests;

public class HardeningSpecs : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "Afterglow.Tests." + Guid.NewGuid().ToString("N")
    );

    // A throwaway key in the current user's registry hive, deleted afterwards
    private readonly string _registryKeyName =
        @"Software\Afterglow.Tests\" + Guid.NewGuid().ToString("N");

    public HardeningSpecs() => Directory.CreateDirectory(_directoryPath);

    private SettingsService LoadSettings(string json)
    {
        var filePath = Path.Combine(_directoryPath, "Settings.json");
        File.WriteAllText(filePath, json);

        var settings = new SettingsService(filePath);
        settings.Load();

        return settings;
    }

    // ---- Registry ----

    [Fact]
    public void I_do_not_get_a_crash_when_a_registry_value_has_an_unexpected_type()
    {
        // Arrange
        // E.g. a value written by another program, or edited by hand
        using (var key = Registry.CurrentUser.CreateSubKey(_registryKeyName))
            key.SetValue("Value", "not a number");

        var registrySwitch = new RegistrySwitch<int>(
            RegistryHive.CurrentUser,
            _registryKeyName,
            "Value",
            256
        );

        // Act & assert
        registrySwitch.IsSet.Should().BeFalse();

        registrySwitch.IsSet = true;
        registrySwitch.IsSet.Should().BeTrue();
    }

    [Fact]
    public void I_can_turn_a_registry_switch_on_and_off_repeatedly()
    {
        // Arrange
        var registrySwitch = new RegistrySwitch<string>(
            RegistryHive.CurrentUser,
            _registryKeyName,
            "Value",
            "enabled"
        );

        // Act & assert
        registrySwitch.IsSet.Should().BeFalse();

        registrySwitch.IsSet = true;
        registrySwitch.IsSet = true;
        registrySwitch.IsSet.Should().BeTrue();

        registrySwitch.IsSet = false;
        registrySwitch.IsSet = false;
        registrySwitch.IsSet.Should().BeFalse();
    }

    // ---- Settings file location ----

    [Fact]
    public void I_get_my_settings_saved_to_a_folder_that_does_not_exist_yet()
    {
        // Arrange
        // E.g. the first launch of an installed copy, which keeps settings in the app data folder
        var filePath = Path.Combine(_directoryPath, "AppData", "Afterglow", "Settings.json");
        var settings = new SettingsService(filePath);

        // Act
        var wasLoaded = settings.Load();
        var isSaved = settings.TrySaveToFile();

        // Assert
        wasLoaded.Should().BeFalse();
        isSaved.Should().BeTrue();
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public void I_do_not_get_a_crash_when_settings_cannot_be_saved_where_they_are_expected()
    {
        // Arrange
        // A file sits where the settings folder should be
        var blockingFilePath = Path.Combine(_directoryPath, "Blocked");
        File.WriteAllText(blockingFilePath, "");

        var settings = new SettingsService(Path.Combine(blockingFilePath, "Settings.json"));

        // Act
        var wasLoaded = settings.Load();
        var isSaved = settings.TrySaveToFile();

        // Assert
        wasLoaded.Should().BeFalse();
        isSaved.Should().BeFalse();
        File.Exists(blockingFilePath + ".bak").Should().BeFalse();
    }

    // ---- Location ----

    [Theory]
    [InlineData(123, 10)]
    [InlineData(-90.5, 10)]
    [InlineData(45, 200)]
    [InlineData(45, -180.01)]
    public void I_get_coordinates_that_do_not_exist_on_earth_ignored_when_loading_settings(
        double latitude,
        double longitude
    )
    {
        // Act
        var settings = LoadSettings(
            string.Create(
                CultureInfo.InvariantCulture,
                $$"""{ "Location": { "Latitude": {{latitude}}, "Longitude": {{longitude}} } }"""
            )
        );

        // Assert
        settings.Location.Should().BeNull();
    }

    [Fact]
    public void I_keep_valid_coordinates_when_loading_settings()
    {
        // Act
        var settings = LoadSettings("""{ "Location": { "Latitude": -90, "Longitude": 180 } }""");

        // Assert
        settings.Location.Should().Be(new GeoLocation(-90, 180));
    }

    [Theory]
    [InlineData("91, 10")]
    [InlineData("45, 181")]
    [InlineData("41.25°N, 190°W")]
    public void I_get_coordinates_that_do_not_exist_on_earth_treated_as_a_place_to_search_for(
        string query
    )
    {
        // Act & assert
        GeoLocation.TryParse(query).Should().BeNull();
    }

    [Theory]
    [InlineData("90, 180", 90, 180)]
    [InlineData("-90, -180", -90, -180)]
    [InlineData("41.25°N, 120.9762°W", 41.25, -120.9762)]
    public void I_can_enter_any_coordinates_that_exist_on_earth(
        string query,
        double latitude,
        double longitude
    )
    {
        // Act & assert
        GeoLocation.TryParse(query).Should().Be(new GeoLocation(latitude, longitude));
    }

    [Fact]
    public void I_never_get_a_crash_from_sunrise_and_sunset_calculations_with_any_coordinates()
    {
        // Arrange
        double[] values =
        [
            -1e300,
            -1000,
            -180,
            -90,
            -89.999,
            -66.56,
            0,
            double.Epsilon,
            66.56,
            89.999,
            90,
            180,
            1000,
            1e300,
        ];

        // Act & assert
        foreach (var latitude in values)
        foreach (var longitude in values)
        {
            for (var day = 0; day < 366; day += 7)
            {
                var instant = new DateTimeOffset(
                    2026,
                    1,
                    1,
                    12,
                    0,
                    0,
                    TimeSpan.FromHours(day % 27 - 12)
                ).AddDays(day);

                var calculate = () =>
                    SolarTimes.Calculate(new GeoLocation(latitude, longitude), instant);

                calculate.Should().NotThrow($"({latitude}, {longitude}) on day {day}");
            }
        }
    }

    // ---- Efficiency ----

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, true)]
    // The whitelist looks up the foreground window on its own, opening its process only if needed
    [InlineData(false, true, false)]
    [InlineData(true, true, true)]
    public void I_do_not_get_the_foreground_application_looked_up_when_nothing_needs_it(
        bool isPauseWhenFullScreenEnabled,
        bool isApplicationWhitelistEnabled,
        bool expected
    )
    {
        // Arrange
        var settings = new AutoPauseSettings(
            IsStreamingAutoPauseEnabled: true,
            IsPauseWhenScreenLockedEnabled: true,
            isPauseWhenFullScreenEnabled,
            IsPerMonitorFullScreenPauseEnabled: true,
            isApplicationWhitelistEnabled
        );

        // Act & assert
        settings.IsForegroundApplicationNeeded.Should().Be(expected);
    }

    // ---- Disposal ----

    private sealed class FakeViewModel : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    [Fact]
    public void I_get_everything_created_for_the_settings_released_once_they_are_closed()
    {
        // Arrange
        var services = new ServiceCollection()
            .AddTransient<FakeViewModel>()
            .BuildServiceProvider(true);

        // Act
        var owned = ViewModelManager.CreateOwned<FakeViewModel>(services);
        var viewModel = owned.Value;

        var isDisposedWhileOpen = viewModel.IsDisposed;
        owned.Dispose();

        // Assert
        isDisposedWhileOpen.Should().BeFalse();
        viewModel.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void I_do_not_get_closed_settings_tabs_reacting_to_setting_changes()
    {
        // Arrange
        var settings = LoadSettings("{}");
        using var localization = new LocalizationManager(settings);
        var tab = new CycleSettingsTabViewModel(settings, localization);

        var notificationCount = 0;
        tab.PropertyChanged += (_, _) => notificationCount++;

        // Act
        settings.NightStartTime = new TimeOnly(23, 0);
        var whileOpen = notificationCount;

        tab.Dispose();
        notificationCount = 0;
        settings.NightStartTime = new TimeOnly(23, 30);

        // Assert
        whileOpen.Should().BePositive();
        notificationCount.Should().Be(0);
    }

    // ---- Branding ----

    [Fact]
    public void I_see_the_app_called_afterglow_everywhere()
    {
        // Assert
        Program.DisplayName.Should().Be("Afterglow");
        Program.ProjectUrl.Should().Contain("Afterglow");

        // Names the executable, settings folder, startup entry, and single-instance lock
        Program.Name.Should().Be("Afterglow");
    }

    public static TheoryData<Language> AllLanguages { get; } =
        new(Enum.GetValues<Language>().Where(l => l is not Language.System));

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public void I_see_afterglow_rather_than_lightbulb_in_every_language(Language language)
    {
        // Arrange
        var localization = LocalizationManager.GetLocalization(language);

        // Assert
        localization
            .Select(p => p.Value)
            .Should()
            .AllSatisfy(v => Regex.IsMatch(v, @"\bLightBulb\b").Should().BeFalse(v));
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(_registryKeyName, false);
            Registry.CurrentUser.DeleteSubKey(@"Software\Afterglow.Tests", false);
        }
        catch
        {
            // Ignore
        }

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
