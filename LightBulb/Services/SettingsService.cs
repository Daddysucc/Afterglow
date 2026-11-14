using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Cogwheel;
using CommunityToolkit.Mvvm.ComponentModel;
using LightBulb.Core;
using LightBulb.Framework;
using LightBulb.Localization;
using LightBulb.Models;
using LightBulb.PlatformInterop;
using Microsoft.Win32;

namespace LightBulb.Services;

[ObservableObject]
public partial class SettingsService()
    : SettingsBase(StartOptions.Current.SettingsPath, SettingsSerializerContext.Default)
{
    private readonly RegistrySwitch<int> _extendedGammaRangeSwitch = new(
        RegistryHive.LocalMachine,
        @"Software\Microsoft\Windows NT\CurrentVersion\ICM",
        "GdiICMGammaRange",
        256
    );

    private readonly RegistrySwitch<string> _autoStartSwitch = new(
        RegistryHive.CurrentUser,
        @"Software\Microsoft\Windows\CurrentVersion\Run",
        Program.Name,
        $"\"{Program.ExecutableFilePath}\" {StartOptions.IsInitiallyHiddenArgument}"
    );

    [ObservableProperty]
    public partial bool IsFirstTimeExperienceEnabled { get; set; } = true;

    [ObservableProperty]
    [JsonIgnore] // comes from registry
    public partial bool IsExtendedGammaRangeUnlocked { get; set; }

    // General

    public double MinimumTemperature => 500;

    public double MaximumTemperature => 20_000;

    public double MinimumBrightness => 0.1;

    public double MaximumBrightness => 1;

    [ObservableProperty]
    public partial ColorConfiguration DayConfiguration { get; set; } =
        ColorPresetDefinition.Recommended.Day;

    [ObservableProperty]
    public partial ColorConfiguration NightConfiguration { get; set; } =
        ColorPresetDefinition.Recommended.Night;

    [ObservableProperty]
    public partial bool IsEveningConfigurationEnabled { get; set; } = true;

    [ObservableProperty]
    public partial ColorConfiguration EveningConfiguration { get; set; } =
        ColorPresetDefinition.Recommended.Evening;

    [ObservableProperty]
    public partial TimeOnly NightStartTime { get; set; } = new(22, 00);

    [ObservableProperty]
    public partial TimeSpan ConfigurationTransitionDuration { get; set; } =
        TimeSpan.FromMinutes(40);

    [ObservableProperty]
    public partial double ConfigurationTransitionOffset { get; set; }

    [ObservableProperty]
    public partial TimeSpan ConfigurationSmoothingMaxDuration { get; set; } =
        TimeSpan.FromSeconds(5);

    // Location

    [ObservableProperty]
    public partial bool IsManualSunriseSunsetEnabled { get; set; } = true;

    [ObservableProperty]
    [JsonPropertyName("ManualSunriseTime")]
    public partial TimeOnly ManualSunrise { get; set; } = new(07, 20);

    [ObservableProperty]
    [JsonPropertyName("ManualSunsetTime")]
    public partial TimeOnly ManualSunset { get; set; } = new(16, 30);

    [ObservableProperty]
    public partial GeoLocation? Location { get; set; }

    // Advanced

    [ObservableProperty]
    public partial ThemeVariant Theme { get; set; }

    [ObservableProperty]
    public partial Language Language { get; set; }

    [ObservableProperty]
    [JsonIgnore] // comes from registry
    public partial bool IsAutoStartEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsAutoUpdateEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsDefaultToDayConfigurationEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsConfigurationSmoothingEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsPauseWhenFullScreenEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsGammaPollingEnabled { get; set; }

    // Application whitelist

    [ObservableProperty]
    public partial bool IsApplicationWhitelistEnabled { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<ExternalApplication>? WhitelistedApplications { get; set; }

    // HotKeys

    [ObservableProperty]
    public partial HotKey ToggleHotKey { get; set; }

    [ObservableProperty]
    [JsonPropertyName("FocusWindowHotKey")]
    public partial HotKey ToggleWindowHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey IncreaseTemperatureOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey DecreaseTemperatureOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey IncreaseBrightnessOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey DecreaseBrightnessOffsetHotKey { get; set; }

    [ObservableProperty]
    public partial HotKey ResetConfigurationOffsetHotKey { get; set; }

    public override void Reset()
    {
        base.Reset();

        // Don't reset the first-time experience
        IsFirstTimeExperienceEnabled = false;

        // Trigger UI updates
        OnPropertyChanged(string.Empty);
    }

    public override void Save()
    {
        // Disallow auto-start in debug mode to make things simpler
#if DEBUG
        IsAutoStartEnabled = false;
#endif

        base.Save();

        // Update values in the registry
        try
        {
            _extendedGammaRangeSwitch.IsSet = IsExtendedGammaRangeUnlocked;
            _autoStartSwitch.IsSet = IsAutoStartEnabled;
        }
        catch (Win32Exception)
        {
            // This can happen if the user doesn't have the necessary permissions to update
            // the corresponding registry keys, and privilege elevation has failed.
            // Throwing an exception here is very messy, so we'll just ignore it.
            // https://github.com/Tyrrrz/LightBulb/issues/335
        }

        // Trigger UI updates
        OnPropertyChanged(string.Empty);
    }

    public override bool Load()
    {
        var wasLoaded = base.Load();

        // The evening is an intermediate stage, so keep it between the day and night values.
        // This matters for settings files saved before the evening stage existed, where the
        // default evening configuration may not fit the user's own day and night configurations.
        EveningConfiguration = new ColorConfiguration(
            ClampBetween(
                EveningConfiguration.Temperature,
                NightConfiguration.Temperature,
                DayConfiguration.Temperature
            ),
            ClampBetween(
                EveningConfiguration.Brightness,
                NightConfiguration.Brightness,
                DayConfiguration.Brightness
            )
        );

        // Get values from the registry
        IsExtendedGammaRangeUnlocked = _extendedGammaRangeSwitch.IsSet;
        IsAutoStartEnabled = _autoStartSwitch.IsSet;

        // Trigger UI updates
        OnPropertyChanged(string.Empty);

        return wasLoaded;
    }

    public ColorConfiguration GetStageConfiguration(CycleStage stage) =>
        stage switch
        {
            CycleStage.Day => DayConfiguration,
            CycleStage.Evening => EveningConfiguration,
            _ => NightConfiguration,
        };

    // Day, evening, and night values are kept in order (day >= evening >= night),
    // so changing one stage may push the others along with it.
    private void SetStageValues(
        CycleStage stage,
        double value,
        Func<ColorConfiguration, double> getValue,
        Func<ColorConfiguration, double, ColorConfiguration> withValue
    )
    {
        var day = getValue(DayConfiguration);
        var evening = getValue(EveningConfiguration);
        var night = getValue(NightConfiguration);

        switch (stage)
        {
            case CycleStage.Day:
                day = value;
                evening = Math.Min(evening, day);
                night = Math.Min(night, evening);
                break;
            case CycleStage.Evening:
                evening = value;
                day = Math.Max(day, evening);
                night = Math.Min(night, evening);
                break;
            default:
                night = value;
                evening = Math.Max(evening, night);
                day = Math.Max(day, evening);
                break;
        }

        DayConfiguration = withValue(DayConfiguration, day);
        EveningConfiguration = withValue(EveningConfiguration, evening);
        NightConfiguration = withValue(NightConfiguration, night);
    }

    public void SetStageTemperature(CycleStage stage, double temperature) =>
        SetStageValues(
            stage,
            Math.Clamp(temperature, MinimumTemperature, MaximumTemperature),
            c => c.Temperature,
            (c, v) => c with { Temperature = v }
        );

    public void SetStageBrightness(CycleStage stage, double brightness) =>
        SetStageValues(
            stage,
            Math.Clamp(brightness, MinimumBrightness, MaximumBrightness),
            c => c.Brightness,
            (c, v) => c with { Brightness = v }
        );

    public ColorPreset GetMatchingPreset()
    {
        if (!IsEveningConfigurationEnabled)
            return ColorPreset.Custom;

        foreach (var preset in ColorPresetDefinition.All)
        {
            if (
                DayConfiguration == preset.Day
                && EveningConfiguration == preset.Evening
                && NightConfiguration == preset.Night
            )
            {
                return preset.Preset;
            }
        }

        return ColorPreset.Custom;
    }

    public void ApplyPreset(ColorPresetDefinition preset)
    {
        IsEveningConfigurationEnabled = true;
        DayConfiguration = preset.Day;
        EveningConfiguration = preset.Evening;
        NightConfiguration = preset.Night;
    }

    // Unlike Math.Clamp, this doesn't throw if the bounds are in the wrong order
    private static double ClampBetween(double value, double bound1, double bound2) =>
        Math.Clamp(value, Math.Min(bound1, bound2), Math.Max(bound1, bound2));
}

public partial class SettingsService
{
    [JsonSerializable(typeof(SettingsService))]
    [JsonSourceGenerationOptions(UseStringEnumConverter = true)]
    private partial class SettingsSerializerContext : JsonSerializerContext;
}
