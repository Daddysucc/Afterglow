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

        var settingsDialog = viewModelManager.GetSettingsViewModel();
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
