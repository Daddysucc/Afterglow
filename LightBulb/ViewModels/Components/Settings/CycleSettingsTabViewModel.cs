using System;
using LightBulb.Core;
using LightBulb.Localization;
using LightBulb.Services;
using PowerKit.Extensions;

namespace LightBulb.ViewModels.Components.Settings;

// Day, evening, and night values are kept in order (day >= evening >= night) by the settings
// service, so changing one of them may push the others along with it.
public class CycleSettingsTabViewModel(
    SettingsService settingsService,
    LocalizationManager localizationManager
) : SettingsTabViewModelBase(settingsService, localizationManager, 0)
{
    public override string DisplayName => LocalizationManager.CycleTabName;

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumDayTemperature => Math.Max(6600, DayTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumDayTemperature => Math.Min(2500, DayTemperature);

    public double DayTemperature
    {
        get => SettingsService.DayConfiguration.Temperature;
        set => SettingsService.SetStageTemperature(CycleStage.Day, value);
    }

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumEveningTemperature => Math.Max(6600, EveningTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumEveningTemperature => Math.Min(2500, EveningTemperature);

    public double EveningTemperature
    {
        get => SettingsService.EveningConfiguration.Temperature;
        set => SettingsService.SetStageTemperature(CycleStage.Evening, value);
    }

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumNightTemperature => Math.Max(6600, NightTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumNightTemperature => Math.Min(2500, NightTemperature);

    public double NightTemperature
    {
        get => SettingsService.NightConfiguration.Temperature;
        set => SettingsService.SetStageTemperature(CycleStage.Night, value);
    }

    public double DayBrightness
    {
        get => SettingsService.DayConfiguration.Brightness;
        set => SettingsService.SetStageBrightness(CycleStage.Day, value);
    }

    public double EveningBrightness
    {
        get => SettingsService.EveningConfiguration.Brightness;
        set => SettingsService.SetStageBrightness(CycleStage.Evening, value);
    }

    public double NightBrightness
    {
        get => SettingsService.NightConfiguration.Brightness;
        set => SettingsService.SetStageBrightness(CycleStage.Night, value);
    }

    public TimeSpan ConfigurationTransitionDuration
    {
        get => SettingsService.ConfigurationTransitionDuration;
        set =>
            SettingsService.ConfigurationTransitionDuration = TimeSpan.FromHours(
                value.TotalHours.Clamp(0, 5)
            );
    }

    public double ConfigurationTransitionOffset
    {
        get => SettingsService.ConfigurationTransitionOffset;
        set => SettingsService.ConfigurationTransitionOffset = value.Clamp(0, 1);
    }

    public bool IsEveningConfigurationEnabled
    {
        get => SettingsService.IsEveningConfigurationEnabled;
        set => SettingsService.IsEveningConfigurationEnabled = value;
    }

    public TimeOnly NightStartTime
    {
        get => SettingsService.NightStartTime;
        set => SettingsService.NightStartTime = value;
    }
}
