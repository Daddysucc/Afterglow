using System;
using System.Globalization;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Localization;
using Afterglow.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerKit.Extensions;

namespace Afterglow.ViewModels.Components.Settings;

public partial class ScheduleSettingsTabViewModel : SettingsTabViewModelBase
{
    private readonly IDisposable _eventSubscription;

    public ScheduleSettingsTabViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager
    )
        : base(settingsService, localizationManager, 1)
    {
        _eventSubscription = this.WatchProperty(
            o => o.Location,
            v => LocationQuery = v?.ToString(),
            true
        );
    }

    public override string DisplayName => LocalizationManager.ScheduleTabName;

    protected override void ResetTabSettings(SettingsService defaults)
    {
        SettingsService.IsManualSunriseSunsetEnabled = defaults.IsManualSunriseSunsetEnabled;
        SettingsService.Location = defaults.Location;
        SettingsService.ManualSunrise = defaults.ManualSunrise;
        SettingsService.ManualSunset = defaults.ManualSunset;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AutoResolveLocationCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResolveLocationCommand))]
    public partial bool IsBusy { get; set; }

    // Times currently in effect: either the manual ones or the ones calculated from the location
    private SolarTimes EffectiveSolarTimes =>
        SettingsService is { IsManualSunriseSunsetEnabled: false, Location: { } location }
            ? SolarTimes.Calculate(location, DateTimeOffset.Now)
            : new SolarTimes(SettingsService.ManualSunrise, SettingsService.ManualSunset);

    public string SunriseSummaryText =>
        EffectiveSolarTimes.Sunrise.ToString("t", CultureInfo.CurrentCulture);

    public string SunsetSummaryText =>
        EffectiveSolarTimes.Sunset.ToString("t", CultureInfo.CurrentCulture);

    public string NightStartSummaryText =>
        SettingsService.IsEveningConfigurationEnabled
            ? SettingsService.NightStartTime.ToString("t", CultureInfo.CurrentCulture)
            : LocalizationManager.OffLabel;

    public string LocationStatusText =>
        Location is { } location
            ? string.Format(
                LocalizationManager.LocationInUseText,
                location.Latitude.ToString("0.####", CultureInfo.CurrentCulture),
                location.Longitude.ToString("0.####", CultureInfo.CurrentCulture)
            )
            : LocalizationManager.LocationNotSetText;

    public bool IsManualSunriseSunsetEnabled
    {
        get => SettingsService.IsManualSunriseSunsetEnabled;
        set => SettingsService.IsManualSunriseSunsetEnabled = value;
    }

    public TimeOnly ManualSunrise
    {
        get => SettingsService.ManualSunrise;
        set => SettingsService.ManualSunrise = value;
    }

    public TimeOnly ManualSunset
    {
        get => SettingsService.ManualSunset;
        set => SettingsService.ManualSunset = value;
    }

    [ObservableProperty]
    public partial bool IsLocationError { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResolveLocationCommand))]
    public partial string? LocationQuery { get; set; }

    public GeoLocation? Location
    {
        get => SettingsService.Location;
        set => SettingsService.Location = value;
    }

    private bool CanAutoResolveLocation() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanAutoResolveLocation))]
    private async Task AutoResolveLocationAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        IsLocationError = false;

        try
        {
            Location = await GeoLocation.GetCurrentAsync();
        }
        catch
        {
            IsLocationError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanResolveLocation() =>
        !IsBusy
        && !string.IsNullOrWhiteSpace(LocationQuery)
        && LocationQuery != Location?.ToString();

    [RelayCommand(CanExecute = nameof(CanResolveLocation))]
    private async Task ResolveLocationAsync()
    {
        if (
            IsBusy
            || string.IsNullOrWhiteSpace(LocationQuery)
            || LocationQuery == Location?.ToString()
        )
            return;

        IsBusy = true;
        IsLocationError = false;

        try
        {
            Location =
                GeoLocation.TryParse(LocationQuery) ?? await GeoLocation.SearchAsync(LocationQuery);
        }
        catch
        {
            IsLocationError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _eventSubscription.Dispose();

        base.Dispose(disposing);
    }
}
