using System.Collections.Generic;
using System.Linq;
using Afterglow.Framework;
using Afterglow.Localization;
using Afterglow.Services;
using Afterglow.ViewModels.Components.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Afterglow.ViewModels.Dialogs;

public partial class SettingsViewModel : DialogViewModelBase
{
    private readonly SettingsService _settingsService;

    public SettingsViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        IEnumerable<SettingsTabViewModelBase> tabs
    )
    {
        _settingsService = settingsService;
        LocalizationManager = localizationManager;
        Tabs = tabs.OrderBy(t => t.Order).ToArray();

        // Pre-select first tab
        var firstTab = Tabs.FirstOrDefault();
        if (firstTab is not null)
            ActivateTab(firstTab);
    }

    public LocalizationManager LocalizationManager { get; }

    public IReadOnlyList<SettingsTabViewModelBase> Tabs { get; }

    [ObservableProperty]
    public partial SettingsTabViewModelBase? ActiveTab { get; set; }

    [RelayCommand]
    private void ActivateTab(SettingsTabViewModelBase tab)
    {
        // Deactivate the previously selected tab
        if (ActiveTab is not null)
            ActiveTab.IsActive = false;

        ActiveTab = tab;
        tab.IsActive = true;
    }

    public void ActivateTab<T>()
        where T : SettingsTabViewModelBase
    {
        var tab = Tabs.OfType<T>().FirstOrDefault();
        if (tab is not null)
            ActivateTab(tab);
    }

    // Settings are applied live, so going back to the dashboard just persists them
    [RelayCommand]
    private void Back()
    {
        _settingsService.Save();
        Close(true);
    }
}
