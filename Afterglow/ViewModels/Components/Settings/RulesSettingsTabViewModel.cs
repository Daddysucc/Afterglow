using System;
using System.Collections.Generic;
using System.Linq;
using Afterglow.Core;
using Afterglow.Localization;
using Afterglow.Models;
using Afterglow.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerKit;
using PowerKit.Extensions;

namespace Afterglow.ViewModels.Components.Settings;

public partial class RulesSettingsTabViewModel : SettingsTabViewModelBase
{
    private readonly ExternalApplicationService _externalApplicationService;
    private readonly IDisposable _eventSubscription;

    public RulesSettingsTabViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        ExternalApplicationService externalApplicationService
    )
        : base(settingsService, localizationManager, 2)
    {
        _externalApplicationService = externalApplicationService;

        _eventSubscription = Disposable.Merge(
            this.WatchProperty(
                o => o.IsApplicationWhitelistEnabled,
                _ => RefreshApplicationsCommand.NotifyCanExecuteChanged()
            ),
            this.WatchProperty(
                o => o.IsWhitelistedApplicationPauseEnabled,
                _ => RefreshApplicationsCommand.NotifyCanExecuteChanged()
            )
        );
    }

    public override string DisplayName => LocalizationManager.RulesTabName;

    protected override void ResetTabSettings(SettingsService defaults)
    {
        SettingsService.IsApplicationWhitelistEnabled = defaults.IsApplicationWhitelistEnabled;
        SettingsService.IsPauseWhenFullScreenEnabled = defaults.IsPauseWhenFullScreenEnabled;
        SettingsService.IsPauseWhenScreenLockedEnabled = defaults.IsPauseWhenScreenLockedEnabled;
        SettingsService.IsPerMonitorFullscreenPauseEnabled =
            defaults.IsPerMonitorFullscreenPauseEnabled;
        SettingsService.IsPerMonitorWhitelistPauseEnabled =
            defaults.IsPerMonitorWhitelistPauseEnabled;
        SettingsService.IsStreamingAutoPauseEnabled = defaults.IsStreamingAutoPauseEnabled;
        SettingsService.IsWhitelistedApplicationPauseEnabled =
            defaults.IsWhitelistedApplicationPauseEnabled;
        SettingsService.IsWhitelistedWindowTitlePauseEnabled =
            defaults.IsWhitelistedWindowTitlePauseEnabled;
        SettingsService.StreamingPorts = defaults.StreamingPorts;
        SettingsService.WhitelistedApplications = defaults.WhitelistedApplications;
        SettingsService.WhitelistedWindowTitles = defaults.WhitelistedWindowTitles;
    }

    // ---- Smart auto-pause ----

    public bool IsPauseWhenFullScreenEnabled
    {
        get => SettingsService.IsPauseWhenFullScreenEnabled;
        set => SettingsService.IsPauseWhenFullScreenEnabled = value;
    }

    public bool IsPerMonitorFullscreenPauseEnabled
    {
        get => SettingsService.IsPerMonitorFullscreenPauseEnabled;
        set => SettingsService.IsPerMonitorFullscreenPauseEnabled = value;
    }

    public bool IsStreamingAutoPauseEnabled
    {
        get => SettingsService.IsStreamingAutoPauseEnabled;
        set => SettingsService.IsStreamingAutoPauseEnabled = value;
    }

    private string? _rejectedStreamingPorts;

    // Comma-separated local TCP ports, stored in the canonical form (invalid entries are dropped)
    public string StreamingPorts
    {
        get => _rejectedStreamingPorts ?? SettingsService.StreamingPorts;
        set
        {
            SettingsService.StreamingPorts = StreamingPortList.Normalize(value);

            if (string.Equals(SettingsService.StreamingPorts, value, StringComparison.Ordinal))
                return;

            // The input was normalized. If the stored value didn't change as a result, bindings
            // wouldn't refresh and the input would keep showing what was typed. So briefly report
            // the typed value and then the actual one, which makes the input show the normalized value.
            _rejectedStreamingPorts = value;
            OnPropertyChanged();

            Dispatcher.UIThread.Post(() =>
            {
                _rejectedStreamingPorts = null;
                OnPropertyChanged(nameof(StreamingPorts));
            });
        }
    }

    public bool IsPauseWhenScreenLockedEnabled
    {
        get => SettingsService.IsPauseWhenScreenLockedEnabled;
        set => SettingsService.IsPauseWhenScreenLockedEnabled = value;
    }

    // ---- Application whitelist ----

    public bool IsApplicationWhitelistEnabled
    {
        get => SettingsService.IsApplicationWhitelistEnabled;
        set => SettingsService.IsApplicationWhitelistEnabled = value;
    }

    [ObservableProperty]
    public partial IReadOnlyList<ExternalApplication>? Applications { get; set; }

    public IReadOnlyList<ExternalApplication>? WhitelistedApplications
    {
        get => SettingsService.WhitelistedApplications;
        set => SettingsService.WhitelistedApplications = value;
    }

    public bool IsWhitelistedApplicationPauseEnabled
    {
        get => SettingsService.IsWhitelistedApplicationPauseEnabled;
        set => SettingsService.IsWhitelistedApplicationPauseEnabled = value;
    }

    public bool IsWhitelistedWindowTitlePauseEnabled
    {
        get => SettingsService.IsWhitelistedWindowTitlePauseEnabled;
        set => SettingsService.IsWhitelistedWindowTitlePauseEnabled = value;
    }

    public bool IsPerMonitorWhitelistPauseEnabled
    {
        get => SettingsService.IsPerMonitorWhitelistPauseEnabled;
        set => SettingsService.IsPerMonitorWhitelistPauseEnabled = value;
    }

    public IReadOnlyList<string> WhitelistedWindowTitles =>
        SettingsService.WhitelistedWindowTitles ?? [];

    // Keyword being typed, before it's added to the list
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddWindowTitleCommand))]
    public partial string? NewWindowTitle { get; set; }

    private bool CanAddWindowTitle() => !string.IsNullOrWhiteSpace(NewWindowTitle);

    [RelayCommand(CanExecute = nameof(CanAddWindowTitle))]
    private void AddWindowTitle()
    {
        SettingsService.WhitelistedWindowTitles = WindowTitleKeywords.Normalize([
            .. WhitelistedWindowTitles,
            NewWindowTitle,
        ]);

        NewWindowTitle = "";
    }

    [RelayCommand]
    private void RemoveWindowTitle(string keyword) =>
        SettingsService.WhitelistedWindowTitles = [
            .. WhitelistedWindowTitles.Where(k =>
                !string.Equals(k, keyword, StringComparison.OrdinalIgnoreCase)
            ),
        ];

    private bool CanRefreshApplications() =>
        IsApplicationWhitelistEnabled && IsWhitelistedApplicationPauseEnabled;

    [RelayCommand(CanExecute = nameof(CanRefreshApplications))]
    private void RefreshApplications()
    {
        var applications = new HashSet<ExternalApplication>();

        // Add previously whitelisted applications
        // (this has to be done first to preserve references in selected applications)
        foreach (var application in WhitelistedApplications ?? [])
            applications.Add(application);

        // Add all running applications
        foreach (var application in _externalApplicationService.GetAllRunningApplications())
            applications.Add(application);

        Applications = applications.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _eventSubscription.Dispose();

        base.Dispose(disposing);
    }
}
