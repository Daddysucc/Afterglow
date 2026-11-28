using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Afterglow.Framework;
using Afterglow.Localization;
using Afterglow.Models;
using Afterglow.PlatformInterop;
using Afterglow.Services;
using Afterglow.Utils.Extensions;
using Afterglow.ViewModels.Components;
using Afterglow.ViewModels.Components.Settings;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerKit.Extensions;
using Process = System.Diagnostics.Process;

namespace Afterglow.ViewModels;

public partial class MainViewModel(
    DashboardViewModel dashboard,
    ViewModelManager viewModelManager,
    DialogManager dialogManager,
    SettingsService settingsService,
    LocalizationManager localizationManager,
    UpdateService updateService
) : ViewModelBase
{
    public LocalizationManager LocalizationManager { get; } = localizationManager;

    public DashboardViewModel Dashboard { get; } = dashboard;

    [ObservableProperty]
    public partial bool IsOpen { get; set; }

    private async Task ShowDevelopmentBuildMessageAsync()
    {
        if (!Program.IsDevelopmentBuild)
            return;

        // If debugging, the user is likely a developer
        if (Debugger.IsAttached)
            return;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.UnstableBuildTitle,
            string.Format(LocalizationManager.UnstableBuildMessage, Program.DisplayName),
            LocalizationManager.SeeReleasesButton,
            LocalizationManager.CloseButton
        );

        if (await dialogManager.ShowDialogAsync(dialog) == true)
            Process.StartShellExecute(Program.ProjectReleasesUrl);
    }

    private async Task ShowGammaRangeMessageAsync()
    {
        if (
            settingsService.IsExtendedGammaRangeUnlocked
            || !settingsService.IsGammaRangePromptEnabled
        )
            return;

        // Only asked once, rather than on every launch: it can still be unlocked in the settings
        settingsService.IsGammaRangePromptEnabled = false;

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.LimitedGammaRangeTitle,
            string.Format(LocalizationManager.LimitedGammaRangeMessage, Program.DisplayName),
            LocalizationManager.FixButton,
            LocalizationManager.CloseButton
        );

        if (await dialogManager.ShowDialogAsync(dialog) == true)
            settingsService.IsExtendedGammaRangeUnlocked = true;

        settingsService.Save();
    }

    private async Task ShowFirstTimeExperienceMessageAsync()
    {
        if (!settingsService.IsFirstTimeExperienceEnabled)
            return;

        // Starting with Windows from a temporary location (e.g. Downloads) would stop working as
        // soon as it's tidied up, so it's only enabled automatically from a permanent location
        var isTemporaryLocation = StartOptions.Current.IsRunningFromTemporaryLocation;

        var message = string.Format(LocalizationManager.WelcomeMessage, Program.DisplayName);
        if (isTemporaryLocation)
        {
            message +=
                Environment.NewLine
                + Environment.NewLine
                + string.Format(
                    LocalizationManager.TemporaryLocationNote,
                    Program.DisplayName,
                    Path.GetFileName(Program.ExecutableFilePath)
                );
        }

        var dialog = viewModelManager.GetMessageBoxViewModel(
            LocalizationManager.WelcomeTitle,
            message,
            LocalizationManager.OkButton,
            LocalizationManager.CloseButton
        );

        // Disable this message in the future
        settingsService.IsFirstTimeExperienceEnabled = false;
        settingsService.IsAutoStartEnabled = !isTemporaryLocation;
        settingsService.Save();

        if (await dialogManager.ShowDialogAsync(dialog) != true)
            return;

        // Disposed once closed, so that the settings stop listening to the app's services
        using var settings = viewModelManager.CreateSettingsViewModel();
        var settingsDialog = settings.Value;
        settingsDialog.ActivateTab<ScheduleSettingsTabViewModel>();

        await dialogManager.ShowDialogAsync(settingsDialog);
    }

    private bool _isInitialized;

    // Whether there's something to tell the user as soon as the app starts (so the window is
    // shown, even if it's set to start hidden)
    public bool HasStartupMessages =>
        (Program.IsDevelopmentBuild && !Debugger.IsAttached)
        || (
            settingsService is
            { IsExtendedGammaRangeUnlocked: false, IsGammaRangePromptEnabled: true }
        )
        || settingsService.IsFirstTimeExperienceEnabled;

    // Shown the first time the window is (rather than each time it's created again)
    public override async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        await ShowDevelopmentBuildMessageAsync();
        await ShowGammaRangeMessageAsync();
        await ShowFirstTimeExperienceMessageAsync();

        // An update that's waiting is shown the first time the window is
        await ShowUpdateAsync();
    }

    private async Task ShowSettingsTabAsync<TTab>()
        where TTab : SettingsTabViewModelBase
    {
        // Settings can be requested from several places (e.g. the tray menu) while already open
        if (_isSettingsOpen)
            return;

        // Persist any pending changes made on the dashboard first, so that they're not
        // overwritten or lost while the settings page is open
        Dashboard.FlushPendingSave();

        // Disposed once closed, so that the settings stop listening to the app's services
        using var settings = viewModelManager.CreateSettingsViewModel();
        var settingsDialog = settings.Value;
        settingsDialog.ActivateTab<TTab>();

        _isSettingsOpen = true;
        _openSettings = settingsDialog;
        try
        {
            await dialogManager.ShowDialogAsync(settingsDialog);
        }
        finally
        {
            _isSettingsOpen = false;
            _openSettings = null;
        }
    }

    private bool _isSettingsOpen;
    private Dialogs.SettingsViewModel? _openSettings;

    [RelayCommand]
    private async Task ShowSettingsAsync() =>
        await ShowSettingsTabAsync<CycleSettingsTabViewModel>();

    [RelayCommand]
    private async Task ShowLocationSettingsAsync() =>
        await ShowSettingsTabAsync<ScheduleSettingsTabViewModel>();

    private bool _isUpdateShown;

    /// <summary>
    /// Shows the update that's available (its version, and what's changed), to install it right
    /// away or later. Putting it off ("Later") means it isn't brought up again by itself for that
    /// version, only when asked for (e.g. when its notification is clicked).
    /// </summary>
    public async Task ShowUpdateAsync(bool isRequested = false)
    {
        if (_isUpdateShown || updateService.AvailableVersion is not { } version)
            return;

        var versionText = version.ToString(3);
        if (
            !isRequested
            && string.Equals(
                settingsService.DismissedUpdateVersion,
                versionText,
                StringComparison.Ordinal
            )
        )
            return;

        if (_isSettingsOpen)
        {
            // Not brought up by itself over the settings (which show it too, with their own
            // button). Asked for, the settings are closed (as their back button would, keeping
            // every change) to show it.
            if (!isRequested)
                return;

            _openSettings?.BackCommand.Execute(null);
        }

        _isUpdateShown = true;
        try
        {
            var dialog = viewModelManager.GetMessageBoxViewModel(
                string.Format(LocalizationManager.UpdateAvailableTitle, Program.Name, versionText),
                await updateService.GetChangesAsync(LocalizationManager.UpdateChangesHeading)
                    ?? LocalizationManager.UpdateNoNotesText,
                LocalizationManager.UpdateNowButton,
                LocalizationManager.LaterButton
            );

            if (await dialogManager.ShowDialogAsync(dialog) == true)
            {
                await updateService.UpdateAsync();
            }
            else
            {
                settingsService.DismissedUpdateVersion = versionText;
                settingsService.TrySaveToFile();
            }
        }
        finally
        {
            _isUpdateShown = false;
        }
    }

    [RelayCommand]
    private void ShowAbout() => Process.StartShellExecute(Program.ProjectUrl);

    // Whether the window should be hidden to the system tray right after launch
    public bool IsStartHidden =>
        StartOptions.Current.IsInitiallyHidden || settingsService.IsStartMinimizedEnabled;

    [RelayCommand]
    private void ShowWindow() => App.ShowMainWindow();

    [RelayCommand]
    private async Task ShowWindowWithSettingsAsync()
    {
        // (a window that's only just been created has to load before it can show the settings)
        await App.ShowMainWindowAsync();
        await ShowSettingsAsync();
    }

    [RelayCommand]
    private void Exit() => App.Shutdown();
}
