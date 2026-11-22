using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Afterglow.Localization;
using Afterglow.Models;
using Afterglow.Services;
using Afterglow.Utils;
using CommunityToolkit.Mvvm.Input;
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
            _updateSubscription.Dispose();

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

    public bool IsUpdateCheckEnabled
    {
        get => SettingsService.IsUpdateCheckEnabled;
        set => SettingsService.IsUpdateCheckEnabled = value;
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
}
