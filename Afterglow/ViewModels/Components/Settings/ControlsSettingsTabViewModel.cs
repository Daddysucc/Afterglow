using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Localization;
using Afterglow.Models;
using Afterglow.Services;
using Afterglow.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PowerKit.Extensions;

namespace Afterglow.ViewModels.Components.Settings;

public partial class ControlsSettingsTabViewModel : SettingsTabViewModelBase
{
    private readonly UpdateService _updateService;
    private readonly IDisposable _updateSubscription;

    public ControlsSettingsTabViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        UpdateService updateService
    )
        : base(settingsService, localizationManager, 3)
    {
        _updateService = updateService;
        _updateSubscription = updateService.WatchProperties(
            [o => o.Status, o => o.AvailableVersion, o => o.InstallState, o => o.DownloadProgress],
            () =>
            {
                OnPropertyChanged(nameof(UpdateStatusText));
                OnPropertyChanged(nameof(IsUpdateStatusLink));
                CheckForUpdatesCommand.NotifyCanExecuteChanged();
            }
        );
    }

    public override string DisplayName => LocalizationManager.ControlsTabName;

    protected override void ResetTabSettings(SettingsService defaults)
    {
        SettingsService.IsAutoStartEnabled = defaults.IsAutoStartEnabled;
        SettingsService.IsStartMinimizedEnabled = defaults.IsStartMinimizedEnabled;
        SettingsService.IsUpdateCheckEnabled = defaults.IsUpdateCheckEnabled;
        SettingsService.UpdateCheckFrequency = defaults.UpdateCheckFrequency;
        SettingsService.Language = defaults.Language;
        SettingsService.IsConfigurationSmoothingEnabled = defaults.IsConfigurationSmoothingEnabled;
        SettingsService.IsExtendedGammaRangeUnlocked = defaults.IsExtendedGammaRangeUnlocked;
        SettingsService.IsGammaPollingEnabled = defaults.IsGammaPollingEnabled;
        SettingsService.GammaPollingInterval = defaults.GammaPollingInterval;
        SettingsService.ToggleHotKey = defaults.ToggleHotKey;
        SettingsService.ToggleWindowHotKey = defaults.ToggleWindowHotKey;
        SettingsService.IncreaseTemperatureOffsetHotKey = defaults.IncreaseTemperatureOffsetHotKey;
        SettingsService.DecreaseTemperatureOffsetHotKey = defaults.DecreaseTemperatureOffsetHotKey;
        SettingsService.IncreaseBrightnessOffsetHotKey = defaults.IncreaseBrightnessOffsetHotKey;
        SettingsService.DecreaseBrightnessOffsetHotKey = defaults.DecreaseBrightnessOffsetHotKey;
        SettingsService.ResetConfigurationOffsetHotKey = defaults.ResetConfigurationOffsetHotKey;
        SettingsService.ToggleGrayscaleHotKey = defaults.ToggleGrayscaleHotKey;
        SettingsService.ToggleLightsPauseHotKey = defaults.ToggleLightsPauseHotKey;
    }

    // ---- Updates ----

    // What the last check found (or that this build is never checked), if anything
    public string? UpdateStatusText =>
        !UpdateService.IsSupported
            ? LocalizationManager.UpdateCheckUnavailableText
            : _updateService.Status switch
            {
                UpdateCheckStatus.Checking => LocalizationManager.UpdateCheckingText,
                UpdateCheckStatus.UpToDate => string.Format(
                    LocalizationManager.UpToDateText,
                    Program.DisplayName,
                    Program.VersionString
                ),
                UpdateCheckStatus.UpdateAvailable
                    when _updateService.AvailableVersion is { } version =>
                    UpdateText.TryGetProgress(
                        LocalizationManager,
                        _updateService.InstallState,
                        _updateService.DownloadProgress
                    )
                        ?? UpdateText.GetAction(
                            LocalizationManager,
                            _updateService.InstallState,
                            version
                        ),
                UpdateCheckStatus.Failed => LocalizationManager.UpdateCheckFailedText,
                _ => null,
            };

    // When there's an update, the status can be clicked to install it (or, if that failed, to
    // open its download page), unless it's being installed already
    public bool IsUpdateStatusLink =>
        _updateService.Status == UpdateCheckStatus.UpdateAvailable
        && _updateService.AvailableVersion is not null
        && !UpdateText.IsBusy(_updateService.InstallState);

    private bool CanCheckForUpdates() =>
        UpdateService.IsSupported && _updateService.Status != UpdateCheckStatus.Checking;

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync() => await _updateService.CheckNowAsync();

    [RelayCommand]
    private async Task InstallUpdateAsync() => await _updateService.UpdateAsync();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updateSubscription.Dispose();
        }

        base.Dispose(disposing);
    }

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

    // The choice of how often to check for updates: never (0), or one of the frequencies, in order
    public int UpdateCheckFrequencyIndex
    {
        get =>
            SettingsService.IsUpdateCheckEnabled
                ? (int)SettingsService.UpdateCheckFrequency + 1
                : 0;
        set
        {
            if (value <= 0)
            {
                SettingsService.IsUpdateCheckEnabled = false;
            }
            else if (Enum.IsDefined((UpdateCheckFrequency)(value - 1)))
            {
                // The frequency first, so that turning checks on applies it right away
                SettingsService.UpdateCheckFrequency = (UpdateCheckFrequency)(value - 1);
                SettingsService.IsUpdateCheckEnabled = true;
            }

            OnPropertyChanged();
        }
    }

    // e.g. "Version 3.3.0 · Last checked: 21/11/2026 22:05"
    public string VersionText =>
        string.Format(LocalizationManager.CurrentVersionText, Program.VersionString)
        + " · "
        + (
            SettingsService.LastUpdateCheckTime is { } lastCheckTime
                ? string.Format(
                    LocalizationManager.LastUpdateCheckText,
                    lastCheckTime.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
                )
                : LocalizationManager.NeverCheckedText
        );

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

    public HotKey ToggleLightsPauseHotKey
    {
        get => SettingsService.ToggleLightsPauseHotKey;
        set => SettingsService.ToggleLightsPauseHotKey = value;
    }
}
