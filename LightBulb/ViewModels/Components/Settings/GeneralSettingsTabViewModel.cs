using System;
using LightBulb.Core;
using LightBulb.Localization;
using LightBulb.Services;
using PowerKit.Extensions;

namespace LightBulb.ViewModels.Components.Settings;

// Day, evening, and night values are kept in order (day >= evening >= night),
// so changing one of them may push the others along with it.
public class GeneralSettingsTabViewModel(
    SettingsService settingsService,
    LocalizationManager localizationManager
) : SettingsTabViewModelBase(settingsService, localizationManager, 0)
{
    public override string DisplayName => LocalizationManager.GeneralTabName;

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumDayTemperature => Math.Max(6600, DayTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumDayTemperature => Math.Min(2500, DayTemperature);

    public double DayTemperature
    {
        get => SettingsService.DayConfiguration.Temperature;
        set
        {
            SettingsService.DayConfiguration = new ColorConfiguration(
                value.Clamp(SettingsService.MinimumTemperature, SettingsService.MaximumTemperature),
                DayBrightness
            );

            if (DayTemperature < EveningTemperature)
                EveningTemperature = DayTemperature;

            if (DayTemperature < NightTemperature)
                NightTemperature = DayTemperature;
        }
    }

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumEveningTemperature => Math.Max(6600, EveningTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumEveningTemperature => Math.Min(2500, EveningTemperature);

    public double EveningTemperature
    {
        get => SettingsService.EveningConfiguration.Temperature;
        set
        {
            SettingsService.EveningConfiguration = new ColorConfiguration(
                value.Clamp(SettingsService.MinimumTemperature, SettingsService.MaximumTemperature),
                EveningBrightness
            );

            if (EveningTemperature > DayTemperature)
                DayTemperature = EveningTemperature;

            if (EveningTemperature < NightTemperature)
                NightTemperature = EveningTemperature;
        }
    }

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMaximumNightTemperature => Math.Max(6600, NightTemperature);

    // This value is used for slider bounds, but it's not an actual restriction
    public double RecommendedMinimumNightTemperature => Math.Min(2500, NightTemperature);

    public double NightTemperature
    {
        get => SettingsService.NightConfiguration.Temperature;
        set
        {
            SettingsService.NightConfiguration = new ColorConfiguration(
                value.Clamp(SettingsService.MinimumTemperature, SettingsService.MaximumTemperature),
                NightBrightness
            );

            if (NightTemperature > EveningTemperature)
                EveningTemperature = NightTemperature;

            if (NightTemperature > DayTemperature)
                DayTemperature = NightTemperature;
        }
    }

    public double DayBrightness
    {
        get => SettingsService.DayConfiguration.Brightness;
        set
        {
            SettingsService.DayConfiguration = new ColorConfiguration(
                DayTemperature,
                value.Clamp(SettingsService.MinimumBrightness, SettingsService.MaximumBrightness)
            );

            if (DayBrightness < EveningBrightness)
                EveningBrightness = DayBrightness;

            if (DayBrightness < NightBrightness)
                NightBrightness = DayBrightness;
        }
    }

    public double EveningBrightness
    {
        get => SettingsService.EveningConfiguration.Brightness;
        set
        {
            SettingsService.EveningConfiguration = new ColorConfiguration(
                EveningTemperature,
                value.Clamp(SettingsService.MinimumBrightness, SettingsService.MaximumBrightness)
            );

            if (EveningBrightness > DayBrightness)
                DayBrightness = EveningBrightness;

            if (EveningBrightness < NightBrightness)
                NightBrightness = EveningBrightness;
        }
    }

    public double NightBrightness
    {
        get => SettingsService.NightConfiguration.Brightness;
        set
        {
            SettingsService.NightConfiguration = new ColorConfiguration(
                NightTemperature,
                value.Clamp(SettingsService.MinimumBrightness, SettingsService.MaximumBrightness)
            );

            if (NightBrightness > EveningBrightness)
                EveningBrightness = NightBrightness;

            if (NightBrightness > DayBrightness)
                DayBrightness = NightBrightness;
        }
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
