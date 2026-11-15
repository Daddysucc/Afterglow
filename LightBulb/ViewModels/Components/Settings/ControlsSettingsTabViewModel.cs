using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using LightBulb.Localization;
using LightBulb.Models;
using LightBulb.Services;
using PowerKit.Extensions;
using Process = System.Diagnostics.Process;

namespace LightBulb.ViewModels.Components.Settings;

public partial class ControlsSettingsTabViewModel(
    SettingsService settingsService,
    LocalizationManager localizationManager
) : SettingsTabViewModelBase(settingsService, localizationManager, 3)
{
    public override string DisplayName => LocalizationManager.ControlsTabName;

    // ---- System preferences ----

    public IReadOnlyList<Language> AvailableLanguages { get; } = Enum.GetValues<Language>();

    public Language Language
    {
        get => SettingsService.Language;
        set => SettingsService.Language = value;
    }

    public bool IsAutoStartEnabled
    {
        get => SettingsService.IsAutoStartEnabled;
        set => SettingsService.IsAutoStartEnabled = value;
    }

    public bool IsStartMinimizedEnabled
    {
        get => SettingsService.IsStartMinimizedEnabled;
        set => SettingsService.IsStartMinimizedEnabled = value;
    }

    public bool IsConfigurationSmoothingEnabled
    {
        get => SettingsService.IsConfigurationSmoothingEnabled;
        set => SettingsService.IsConfigurationSmoothingEnabled = value;
    }

    public bool IsExtendedGammaRangeUnlocked
    {
        get => SettingsService.IsExtendedGammaRangeUnlocked;
        set => SettingsService.IsExtendedGammaRangeUnlocked = value;
    }

    public bool IsGammaPollingEnabled
    {
        get => SettingsService.IsGammaPollingEnabled;
        set => SettingsService.IsGammaPollingEnabled = value;
    }

    public double GammaPollingIntervalSeconds
    {
        get => SettingsService.GammaPollingInterval.TotalSeconds;
        set =>
            SettingsService.GammaPollingInterval = TimeSpan.FromSeconds(
                Math.Round(Math.Clamp(value, 0.5, 10) * 2) / 2
            );
    }

    public string GammaPollingIntervalText =>
        string.Format(
            LocalizationManager.SecondsValueText,
            GammaPollingIntervalSeconds.ToString("0.#", CultureInfo.CurrentCulture)
        );

    // ---- Hotkeys ----

    public HotKey ToggleHotKey
    {
        get => SettingsService.ToggleHotKey;
        set => SettingsService.ToggleHotKey = value;
    }

    public HotKey ToggleWindowHotKey
    {
        get => SettingsService.ToggleWindowHotKey;
        set => SettingsService.ToggleWindowHotKey = value;
    }

    public HotKey IncreaseTemperatureOffsetHotKey
    {
        get => SettingsService.IncreaseTemperatureOffsetHotKey;
        set => SettingsService.IncreaseTemperatureOffsetHotKey = value;
    }

    public HotKey DecreaseTemperatureOffsetHotKey
    {
        get => SettingsService.DecreaseTemperatureOffsetHotKey;
        set => SettingsService.DecreaseTemperatureOffsetHotKey = value;
    }

    public HotKey IncreaseBrightnessOffsetHotKey
    {
        get => SettingsService.IncreaseBrightnessOffsetHotKey;
        set => SettingsService.IncreaseBrightnessOffsetHotKey = value;
    }

    public HotKey DecreaseBrightnessOffsetHotKey
    {
        get => SettingsService.DecreaseBrightnessOffsetHotKey;
        set => SettingsService.DecreaseBrightnessOffsetHotKey = value;
    }

    public HotKey ResetConfigurationOffsetHotKey
    {
        get => SettingsService.ResetConfigurationOffsetHotKey;
        set => SettingsService.ResetConfigurationOffsetHotKey = value;
    }

    public HotKey ToggleGrayscaleHotKey
    {
        get => SettingsService.ToggleGrayscaleHotKey;
        set => SettingsService.ToggleGrayscaleHotKey = value;
    }

    // ---- Credits ----

    [RelayCommand]
    private void OpenUpstreamProject() =>
        Process.StartShellExecute("https://github.com/Tyrrrz/LightBulb");

    [RelayCommand]
    private void OpenUkraineStatement() => Process.StartShellExecute("https://tyrrrz.me/ukraine");

    [RelayCommand]
    private void OpenUpstreamDonation() => Process.StartShellExecute("https://tyrrrz.me/donate");
}
