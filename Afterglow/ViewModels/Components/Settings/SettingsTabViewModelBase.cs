using System;
using Afterglow.Framework;
using Afterglow.Localization;
using Afterglow.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerKit;
using PowerKit.Extensions;

namespace Afterglow.ViewModels.Components.Settings;

public abstract partial class SettingsTabViewModelBase : ViewModelBase
{
    private readonly IDisposable _eventSubscription;

    protected SettingsTabViewModelBase(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        int order
    )
    {
        SettingsService = settingsService;
        LocalizationManager = localizationManager;
        Order = order;

        _eventSubscription = Disposable.Merge(
            // Implementing classes will bind to settings properties through
            // their own properties, so make sure they stay in sync.
            // This is a bit overkill as it triggers a lot of unnecessary events,
            // but it's a simple and reliable solution.
            SettingsService.WatchAllProperties(OnAllPropertiesChanged),
            localizationManager.WatchProperty(o => o.Language, _ => OnAllPropertiesChanged())
        );
    }

    protected SettingsService SettingsService { get; }

    public LocalizationManager LocalizationManager { get; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    public int Order { get; }

    public abstract string DisplayName { get; }

    // e.g. "Reset Cycle & Colors settings" (at the bottom of the tab)
    public string ResetTabButtonText =>
        string.Format(LocalizationManager.ResetTabSettingsButton, DisplayName);

    /// <summary>
    /// Puts the settings on this tab back to their defaults (taken from the given settings, which
    /// have nothing but defaults), leaving every other setting as it is.
    /// </summary>
    protected abstract void ResetTabSettings(SettingsService defaults);

    [RelayCommand]
    private void ResetTab() => ResetTabSettings(SettingsService.CreateDefaults());

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _eventSubscription.Dispose();

        base.Dispose(disposing);
    }
}
