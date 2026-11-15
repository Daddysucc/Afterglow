using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    LocalizationManager localizationManager
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

    public override async Task InitializeAsync()
    {
        await ShowDevelopmentBuildMessageAsync();
        await ShowGammaRangeMessageAsync();
        await ShowFirstTimeExperienceMessageAsync();
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
        try
        {
            await dialogManager.ShowDialogAsync(settingsDialog);
        }
        finally
        {
            _isSettingsOpen = false;
        }
    }

    private bool _isSettingsOpen;

    [RelayCommand]
    private async Task ShowSettingsAsync() =>
        await ShowSettingsTabAsync<CycleSettingsTabViewModel>();

    [RelayCommand]
    private async Task ShowLocationSettingsAsync() =>
        await ShowSettingsTabAsync<ScheduleSettingsTabViewModel>();

    [RelayCommand]
    private void ShowAbout() => Process.StartShellExecute(Program.ProjectUrl);

    // Whether the window should be hidden to the system tray right after launch
    public bool IsStartHidden =>
        StartOptions.Current.IsInitiallyHidden || settingsService.IsStartMinimizedEnabled;

    [RelayCommand]
    private void ShowWindow() =>
        Application.Current?.ApplicationLifetime?.TryGetMainWindow()?.ShowActivateFocus();

    [RelayCommand]
    private async Task ShowWindowWithSettingsAsync()
    {
        ShowWindow();
        await ShowSettingsAsync();
    }

    [RelayCommand]
    private void Exit() => App.Shutdown();
}
