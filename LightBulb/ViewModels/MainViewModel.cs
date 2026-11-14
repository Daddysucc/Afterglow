using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightBulb.Framework;
using LightBulb.Localization;
using LightBulb.Models;
using LightBulb.PlatformInterop;
using LightBulb.Services;
using LightBulb.Utils.Extensions;
using LightBulb.ViewModels.Components;
using LightBulb.ViewModels.Components.Settings;
using PowerKit.Extensions;
using Process = System.Diagnostics.Process;

namespace LightBulb.ViewModels;

public partial class MainViewModel(
    DashboardViewModel dashboard,
    ViewModelManager viewModelManager,
    DialogManager dialogManager,
    SettingsService settingsService,
    LocalizationManager localizationManager,
    UpdateService updateService
) : ViewModelBase
{
    private readonly Timer _checkForUpdatesTimer = new(
        TimeSpan.FromHours(3),
        async () =>
        {
            try
            {
                var updateVersion = await updateService.CheckForUpdatesAsync();
                if (updateVersion is not null)
                    await updateService.PrepareUpdateAsync(updateVersion);
            }
            catch
            {
                // Failure to update shouldn't crash the application
            }
        }
    );

    public LocalizationManager LocalizationManager { get; } = localizationManager;

    public DashboardViewModel Dashboard { get; } = dashboard;

    private IDisposable? _eventSubscription;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    public IReadOnlyList<ColorPresetOption> PresetOptions =>
        [
            new(ColorPreset.Recommended, LocalizationManager.RecommendedPresetName),
            new(ColorPreset.LateNight, LocalizationManager.LateNightPresetName),
            new(ColorPreset.Daylight, LocalizationManager.DaylightPresetName),
            new(ColorPreset.Custom, LocalizationManager.CustomPresetName),
        ];

    // Reflects whichever preset matches the current settings
    public ColorPresetOption? SelectedPresetOption
    {
        get
        {
            var preset = settingsService.GetMatchingPreset();
            return PresetOptions.First(o => o.Preset == preset);
        }
        set
        {
            // The selector may briefly clear its selection when the options are refreshed
            if (value is null)
                return;

            // The selector also echoes back the selection it receives from this property
            // (e.g. when it's first bound, or when the settings change and the matching
            // preset becomes 'Custom'). Only an actual change by the user should do anything,
            // otherwise this would re-save the settings or pop up the settings dialog.
            if (value.Preset == settingsService.GetMatchingPreset())
                return;

            if (value.Preset == ColorPreset.Custom)
            {
                // Custom colors are configured in settings. Until they're changed there,
                // the selector keeps showing the preset that matches the current settings.
                Dispatcher.UIThread.Post(() => ShowGeneralSettingsCommand.Execute(null));
            }
            else
            {
                settingsService.ApplyPreset(
                    ColorPresetDefinition.All.First(d => d.Preset == value.Preset)
                );

                settingsService.Save();
            }

            OnPropertyChanged();
        }
    }

    private async Task FinalizePendingUpdateAsync()
    {
        var updateVersion = updateService.TryGetLastPreparedUpdate();
        if (updateVersion is null)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.UpdateAvailableTitle,
            string.Format(LocalizationManager.UpdateAvailableMessage, Program.Name, updateVersion),
            LocalizationManager.InstallButton,
            LocalizationManager.CancelButton
        );

        // Bring the user's attention to this dialog, even if the app is hidden
        Application.Current?.ApplicationLifetime?.TryGetMainWindow()?.ShowActivateFocus();

        if (await dialogManager.ShowDialogAsync(dialog) != true)
            return;

        updateService.FinalizeUpdate(updateVersion);

        App.Shutdown(2);
    }

    private async Task ShowDevelopmentBuildMessageAsync()
    {
        if (!Program.IsDevelopmentBuild)
            return;

        // If debugging, the user is likely a developer
        if (Debugger.IsAttached)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.UnstableBuildTitle,
            string.Format(LocalizationManager.UnstableBuildMessage, Program.Name),
            LocalizationManager.SeeReleasesButton,
            LocalizationManager.CloseButton
        );

        if (await dialogManager.ShowDialogAsync(dialog) == true)
            Process.StartShellExecute(Program.ProjectReleasesUrl);
    }

    private async Task ShowGammaRangeMessageAsync()
    {
        if (settingsService.IsExtendedGammaRangeUnlocked)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.LimitedGammaRangeTitle,
            string.Format(LocalizationManager.LimitedGammaRangeMessage, Program.Name),
            LocalizationManager.FixButton,
            LocalizationManager.CloseButton
        );

        if (await dialogManager.ShowDialogAsync(dialog) != true)
            return;

        settingsService.IsExtendedGammaRangeUnlocked = true;
        settingsService.Save();
    }

    private async Task ShowFirstTimeExperienceMessageAsync()
    {
        if (!settingsService.IsFirstTimeExperienceEnabled)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.WelcomeTitle,
            string.Format(LocalizationManager.WelcomeMessage, Program.Name),
            LocalizationManager.OkButton,
            LocalizationManager.CloseButton
        );

        // Disable this message in the future
        settingsService.IsFirstTimeExperienceEnabled = false;
        settingsService.IsAutoStartEnabled = true;
        settingsService.Save();

        if (await dialogManager.ShowDialogAsync(dialog) != true)
            return;

        var settingsDialog = viewModelManager.GetSettingsViewModel();
        settingsDialog.ActivateTab<LocationSettingsTabViewModel>();

        await dialogManager.ShowDialogAsync(settingsDialog);
    }

    public override async Task InitializeAsync()
    {
        _eventSubscription = PowerKit.Disposable.Merge(
            // Keep the preset selector in sync with changes made elsewhere (e.g. in settings)
            settingsService.WatchAllProperties(() =>
                OnPropertyChanged(nameof(SelectedPresetOption))
            ),
            LocalizationManager.WatchProperty(
                o => o.Language,
                _ =>
                {
                    OnPropertyChanged(nameof(PresetOptions));
                    OnPropertyChanged(nameof(SelectedPresetOption));
                }
            )
        );

        await FinalizePendingUpdateAsync();

        await ShowDevelopmentBuildMessageAsync();
        await ShowGammaRangeMessageAsync();
        await ShowFirstTimeExperienceMessageAsync();

        _checkForUpdatesTimer.Start();
    }

    private async Task ShowSettingsTabAsync<TTab>()
        where TTab : SettingsTabViewModelBase
    {
        // Persist any pending changes made on the dashboard first, otherwise they'd be
        // discarded if the user cancels the settings dialog (which reloads settings from disk)
        Dashboard.FlushPendingSave();

        var settingsDialog = viewModelManager.GetSettingsViewModel();
        settingsDialog.ActivateTab<TTab>();

        await dialogManager.ShowDialogAsync(settingsDialog);
    }

    [RelayCommand]
    private async Task ShowSettingsAsync() =>
        await ShowSettingsTabAsync<GeneralSettingsTabViewModel>();

    [RelayCommand]
    private async Task ShowGeneralSettingsAsync() =>
        await ShowSettingsTabAsync<GeneralSettingsTabViewModel>();

    [RelayCommand]
    private async Task ShowLocationSettingsAsync() =>
        await ShowSettingsTabAsync<LocationSettingsTabViewModel>();

    [RelayCommand]
    private void ShowAbout() => Process.StartShellExecute(Program.ProjectUrl);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _eventSubscription?.Dispose();
            _checkForUpdatesTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
