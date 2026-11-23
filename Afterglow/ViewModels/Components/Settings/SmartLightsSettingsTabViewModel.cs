using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Afterglow.Core;
using Afterglow.Localization;
using Afterglow.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using PowerKit.Extensions;

namespace Afterglow.ViewModels.Components.Settings;

/// <summary>
/// Smart lights (Philips Hue lights following the cycle, through the Hue bridge).
/// </summary>
public partial class SmartLightsSettingsTabViewModel : SettingsTabViewModelBase
{
    private readonly HueService _hueService;
    private readonly IDisposable _hueSubscription;

    public SmartLightsSettingsTabViewModel(
        SettingsService settingsService,
        LocalizationManager localizationManager,
        HueService hueService
    )
        : base(settingsService, localizationManager, 4)
    {
        _hueService = hueService;

        _hueSubscription = PowerKit.Disposable.Merge(
            hueService.WatchProperties(
                [
                    o => o.Status,
                    o => o.FollowedLightCount,
                    o => o.UnresponsiveLightCount,
                    o => o.IsConnecting,
                    o => o.PausedUntil,
                ],
                () =>
                {
                    OnPropertyChanged(nameof(HueStatusText));
                    OnPropertyChanged(nameof(HueConnectButtonText));
                    OnPropertyChanged(nameof(HueConnectButtonTooltip));
                    OnPropertyChanged(nameof(HueConnectButtonIcon));
                    OnPropertyChanged(nameof(IsHuePaired));
                    OnPropertyChanged(nameof(IsHueControlShown));
                }
            ),
            hueService.WatchProperty(
                o => o.Rooms,
                _ =>
                {
                    UpdateHueRooms();
                    UpdateHueLights();
                }
            ),
            settingsService.WatchProperty(
                o => o.HueRoomIds,
                _ =>
                {
                    UpdateHueRooms();
                    UpdateHueLights();
                }
            ),
            hueService.WatchProperties([o => o.Lights, o => o.IsFollowingLights], UpdateHueLights),
            settingsService.WatchProperty(o => o.HueExcludedLightIds, _ => UpdateHueLights()),
            // The rooms are loaded when they're about to be shown
            this.WatchProperty(
                o => o.IsActive,
                isActive =>
                {
                    if (isActive)
                        _ = hueService.RefreshRoomsAsync();
                }
            )
        );
    }

    public override string DisplayName => LocalizationManager.SmartLightsTabName;

    protected override void ResetTabSettings(SettingsService defaults)
    {
        // (the connection to the bridge is kept, as making it again takes pressing its button)
        SettingsService.IsHueEnabled = defaults.IsHueEnabled;
        SettingsService.HueRoomIds = defaults.HueRoomIds;
        SettingsService.HueExcludedLightIds = defaults.HueExcludedLightIds;
        SettingsService.IsHueTemperatureFollowed = defaults.IsHueTemperatureFollowed;
        SettingsService.IsHueBrightnessFollowed = defaults.IsHueBrightnessFollowed;
        SettingsService.LightWarmth = defaults.LightWarmth;
        SettingsService.LightDimming = defaults.LightDimming;
        SettingsService.IsHuePausedWithScreen = defaults.IsHuePausedWithScreen;
        SettingsService.IsHueReclaimEnabled = defaults.IsHueReclaimEnabled;
        SettingsService.HueReclaimDuration = defaults.HueReclaimDuration;
        SettingsService.HuePauseAction = defaults.HuePauseAction;
        SettingsService.HuePauseTemperature = defaults.HuePauseTemperature;
        SettingsService.HuePauseBrightness = defaults.HuePauseBrightness;
        SettingsService.HueShutdownAction = defaults.HueShutdownAction;
        SettingsService.HueShutdownTemperature = defaults.HueShutdownTemperature;
        SettingsService.HueShutdownBrightness = defaults.HueShutdownBrightness;
        SettingsService.IsHueTurnedOnAtStart = defaults.IsHueTurnedOnAtStart;
    }

    public bool IsHueEnabled
    {
        get => SettingsService.IsHueEnabled;
        set
        {
            SettingsService.IsHueEnabled = value;

            if (value)
                _ = _hueService.RefreshRoomsAsync();
        }
    }

    public string? HueBridgeAddress
    {
        get => SettingsService.HueBridgeAddress;
        set =>
            SettingsService.HueBridgeAddress = string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
    }

    public bool IsHueTemperatureFollowed
    {
        get => SettingsService.IsHueTemperatureFollowed;
        set => SettingsService.IsHueTemperatureFollowed = value;
    }

    public bool IsHueBrightnessFollowed
    {
        get => SettingsService.IsHueBrightnessFollowed;
        set => SettingsService.IsHueBrightnessFollowed = value;
    }

    public double LightWarmth
    {
        get => SettingsService.LightWarmth;
        set =>
            SettingsService.LightWarmth = Math.Round(
                Math.Clamp(
                    value,
                    HueLightController.MinimumWarmth,
                    HueLightController.MaximumWarmth
                ),
                1
            );
    }

    // e.g. "×2.0"
    public string LightWarmthText => "×" + LightWarmth.ToString("0.0", CultureInfo.CurrentCulture);

    public double LightDimming
    {
        get => SettingsService.LightDimming;
        set =>
            SettingsService.LightDimming = Math.Round(
                Math.Clamp(
                    value,
                    HueLightController.MinimumDimming,
                    HueLightController.MaximumDimming
                ),
                1
            );
    }

    // e.g. "×1.0"
    public string LightDimmingText =>
        "×" + LightDimming.ToString("0.0", CultureInfo.CurrentCulture);

    public bool IsHuePausedWithScreen
    {
        get => SettingsService.IsHuePausedWithScreen;
        set => SettingsService.IsHuePausedWithScreen = value;
    }

    // As listed: left alone until turned off and on (0), or followed again after a while (1)
    public int HueReclaimModeIndex
    {
        get => SettingsService.IsHueReclaimEnabled ? 1 : 0;
        set => SettingsService.IsHueReclaimEnabled = value == 1;
    }

    public bool IsHueReclaimDurationShown => SettingsService.IsHueReclaimEnabled;

    // How long, in minutes (in steps of 5)
    public double HueReclaimMinutes
    {
        get => SettingsService.HueReclaimDuration.TotalMinutes;
        set =>
            SettingsService.HueReclaimDuration = TimeSpan.FromMinutes(
                Math.Round(
                    Math.Clamp(
                        value,
                        HueService.MinimumReclaimDuration.TotalMinutes,
                        HueService.MaximumReclaimDuration.TotalMinutes
                    ) / 5
                ) * 5
            );
    }

    public double MinimumHueReclaimMinutes => HueService.MinimumReclaimDuration.TotalMinutes;
    public double MaximumHueReclaimMinutes => HueService.MaximumReclaimDuration.TotalMinutes;

    // e.g. "45 min", "1 h 15 min"
    public string HueReclaimText =>
        FormatDuration(TimeSpan.FromMinutes(HueReclaimMinutes), LocalizationManager);

    internal static string FormatDuration(TimeSpan duration, LocalizationManager localization) =>
        ((int)duration.TotalHours, duration.Minutes) switch
        {
            (0, var minutes) => string.Format(
                CultureInfo.CurrentCulture,
                localization.MinutesValueText,
                minutes
            ),
            (var hours, 0) => string.Format(
                CultureInfo.CurrentCulture,
                localization.HoursValueText,
                hours
            ),
            var (hours, minutes) => string.Format(
                CultureInfo.CurrentCulture,
                localization.HoursMinutesValueText,
                hours,
                minutes
            ),
        };

    // As listed: left as they are, put back, turned off, or set to a custom look
    public int HueShutdownActionIndex
    {
        get => (int)SettingsService.HueShutdownAction;
        set =>
            SettingsService.HueShutdownAction = Enum.IsDefined((HueShutdownAction)value)
                ? (HueShutdownAction)value
                : HueShutdownAction.Leave;
    }

    public bool IsHueShutdownLookShown =>
        SettingsService.HueShutdownAction == HueShutdownAction.Custom;

    // As listed: left as they are, put back, turned off, or set to a custom look
    public int HuePauseActionIndex
    {
        get => (int)SettingsService.HuePauseAction;
        set =>
            SettingsService.HuePauseAction = Enum.IsDefined((HuePauseAction)value)
                ? (HuePauseAction)value
                : HuePauseAction.Restore;
    }

    public bool IsHuePauseLookShown => SettingsService.HuePauseAction == HuePauseAction.Custom;

    // Which custom look is being previewed on the lights (while one of its sliders is held down,
    // so that quickly moving a slider, or using the keyboard, doesn't make the lights flicker)
    private bool? _isPausePreviewed;

    /// <summary>
    /// Starts showing a custom look on the lights (the one for pauses, or for shutdown), as one of
    /// its sliders is held down.
    /// </summary>
    public void BeginHueLookPreview(bool isPause)
    {
        _isPausePreviewed = isPause;
        PreviewHueLook();
    }

    /// <summary>
    /// Sets the lights back to what they follow, as the slider was let go of.
    /// </summary>
    public void EndHueLookPreview()
    {
        if (_isPausePreviewed is null)
            return;

        _isPausePreviewed = null;
        _hueService.EndLookPreview();
    }

    private void PreviewHueLook()
    {
        if (_isPausePreviewed is true)
            _hueService.PreviewLook(
                SettingsService.HuePauseTemperature,
                SettingsService.HuePauseBrightness
            );
        else if (_isPausePreviewed is false)
            _hueService.PreviewLook(
                SettingsService.HueShutdownTemperature,
                SettingsService.HueShutdownBrightness
            );
    }

    // The custom looks (each shown on the lights while one of its sliders is held down)
    public double HuePauseTemperature
    {
        get => SettingsService.HuePauseTemperature;
        set
        {
            SettingsService.HuePauseTemperature = RoundTemperature(value);
            PreviewHueLook();
        }
    }

    public double HuePauseBrightness
    {
        get => SettingsService.HuePauseBrightness;
        set
        {
            SettingsService.HuePauseBrightness = RoundBrightness(value);
            PreviewHueLook();
        }
    }

    public double HueShutdownTemperature
    {
        get => SettingsService.HueShutdownTemperature;
        set
        {
            SettingsService.HueShutdownTemperature = RoundTemperature(value);
            PreviewHueLook();
        }
    }

    public double HueShutdownBrightness
    {
        get => SettingsService.HueShutdownBrightness;
        set
        {
            SettingsService.HueShutdownBrightness = RoundBrightness(value);
            PreviewHueLook();
        }
    }

    // e.g. "2700 K", "30%"
    public string HuePauseTemperatureText => FormatTemperature(HuePauseTemperature);
    public string HuePauseBrightnessText => FormatBrightness(HuePauseBrightness);
    public string HueShutdownTemperatureText => FormatTemperature(HueShutdownTemperature);
    public string HueShutdownBrightnessText => FormatBrightness(HueShutdownBrightness);

    public double MinimumLookTemperature => HueService.MinimumLookTemperature;
    public double MaximumLookTemperature => HueService.MaximumLookTemperature;

    private static double RoundTemperature(double value) =>
        Math.Round(
            Math.Clamp(value, HueService.MinimumLookTemperature, HueService.MaximumLookTemperature)
                / 50
        ) * 50;

    private static double RoundBrightness(double value) =>
        Math.Round(Math.Clamp(value, 0.01, 1), 2);

    private static string FormatTemperature(double kelvin) =>
        kelvin.ToString("0", CultureInfo.CurrentCulture) + " K";

    private static string FormatBrightness(double brightness) =>
        brightness.ToString("P0", CultureInfo.CurrentCulture);

    public bool IsHueTurnedOnAtStart
    {
        get => SettingsService.IsHueTurnedOnAtStart;
        set => SettingsService.IsHueTurnedOnAtStart = value;
    }

    public bool IsHuePaired => _hueService.IsPaired;

    // The settings for how lights follow (once Hue control is turned on, and connected)
    public bool IsHueControlShown => IsHueEnabled && _hueService.IsPaired;

    public string? HueStatusText =>
        _hueService.Status switch
        {
            HueStatus.NotConnected => LocalizationManager.HueNotConnectedText,
            HueStatus.Searching => LocalizationManager.HueSearchingText,
            HueStatus.NoBridgeFound => LocalizationManager.HueNoBridgeFoundText,
            HueStatus.WaitingForButton => LocalizationManager.HueWaitingForButtonText,
            HueStatus.ButtonNotPressed => LocalizationManager.HueButtonNotPressedText,
            HueStatus.Connected => LocalizationManager.HueConnectedText,
            HueStatus.NoRooms => LocalizationManager.HueNoRoomsText,
            HueStatus.Paused => LocalizationManager.HuePausedText,
            HueStatus.PausedWithScreen => LocalizationManager.HuePausedWithScreenText,
            HueStatus.PausedByUser => _hueService.PausedUntil is { } until
            && until != DateTimeOffset.MaxValue
                ? string.Format(
                    CultureInfo.CurrentCulture,
                    LocalizationManager.HuePausedUntilText,
                    until.LocalDateTime.ToString("t", CultureInfo.CurrentCulture)
                )
                : LocalizationManager.HuePausedUntilResumedText,
            // (none of the lights are on, so that it doesn't look like something's wrong)
            HueStatus.Following when _hueService.FollowedLightCount == 0 =>
                LocalizationManager.HueFollowingNoneText,
            HueStatus.Following when _hueService.UnresponsiveLightCount > 0 => string.Format(
                CultureInfo.CurrentCulture,
                LocalizationManager.HueFollowingUnresponsiveText,
                _hueService.FollowedLightCount,
                _hueService.UnresponsiveLightCount
            ),
            HueStatus.Following => string.Format(
                CultureInfo.CurrentCulture,
                LocalizationManager.HueFollowingText,
                _hueService.FollowedLightCount
            ),
            HueStatus.Unreachable => LocalizationManager.HueUnreachableText,
            HueStatus.Unauthorized => LocalizationManager.HueUnauthorizedText,
            HueStatus.CertificateChanged => LocalizationManager.HueCertificateChangedText,
            _ => null,
        };

    // Disconnects while connected, and connects (again) otherwise
    private bool IsHueDisconnectable =>
        !_hueService.IsConnecting && _hueService.IsPaired && !_hueService.NeedsReconnecting;

    public string HueConnectButtonText =>
        _hueService.IsConnecting ? LocalizationManager.HueCancelButton
        : IsHueDisconnectable ? LocalizationManager.HueDisconnectButton
        : _hueService.IsPaired ? LocalizationManager.HueReconnectButton
        : LocalizationManager.HueConnectButton;

    public string HueConnectButtonTooltip =>
        IsHueDisconnectable
            ? LocalizationManager.HueDisconnectTooltip
            : LocalizationManager.HueConnectTooltip;

    public MaterialIconKind HueConnectButtonIcon =>
        IsHueDisconnectable ? MaterialIconKind.LinkVariantOff : MaterialIconKind.LinkVariant;

    // The bridge's rooms and zones, to choose from (kept, so that the check boxes aren't rebuilt
    // whenever any setting changes)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHueRooms))]
    public partial IReadOnlyList<HueRoomViewModel> HueRooms { get; private set; } = [];

    public bool HasHueRooms => HueRooms.Count > 0;

    private void UpdateHueRooms()
    {
        var selected = SettingsService.HueRoomIds ?? [];

        // Rooms that are already shown only have their check box updated
        if (
            HueRooms.Count == _hueService.Rooms.Count
            && HueRooms
                .Zip(_hueService.Rooms)
                .All(p => p.First.Id == p.Second.Id && p.First.Name == p.Second.Name)
        )
        {
            foreach (var room in HueRooms)
                room.Refresh(selected.Contains(room.Id));

            return;
        }

        HueRooms = _hueService
            .Rooms.Select(r => new HueRoomViewModel(
                r.Id,
                r.Name,
                selected.Contains(r.Id),
                SetHueRoomSelected
            ))
            .ToArray();
    }

    private void SetHueRoomSelected(string roomId, bool isSelected)
    {
        var selected = (SettingsService.HueRoomIds ?? []).Where(id => id != roomId).ToList();
        if (isSelected)
            selected.Add(roomId);

        SettingsService.HueRoomIds = selected;
    }

    // The lights in the chosen rooms, by room, to choose which of them follow
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHueLights))]
    [NotifyPropertyChangedFor(nameof(IsHueLightListShown))]
    public partial IReadOnlyList<HueLightGroupViewModel> HueLightGroups { get; private set; } = [];

    public bool HasHueLights => HueLightGroups.Count > 0;

    // Whether the list of lights is opened (by clicking on how many are following)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHueLightListShown))]
    public partial bool IsHueLightListOpen { get; set; }

    public bool IsHueLightListShown => IsHueLightListOpen && HasHueLights;

    [RelayCommand]
    private void ToggleHueLightList() => IsHueLightListOpen = !IsHueLightListOpen;

    private void UpdateHueLights()
    {
        var roomIds = SettingsService.HueRoomIds ?? [];
        var excludedIds = SettingsService.HueExcludedLightIds ?? [];
        var lights = _hueService.Lights;

        // Rooms in the same order as they're listed, and lights by name within each
        var groups = _hueService
            .Rooms.Where(r => roomIds.Contains(r.Id))
            .Select(room => new HueLightGroupViewModel(
                room.Name,
                lights
                    .Where(l => room.Contains(l.Id, l.DeviceId))
                    .OrderBy(l => l.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(l =>
                    {
                        var isIncluded = !excludedIds.Contains(l.Id);
                        return new HueLightViewModel(
                            l.Id,
                            string.IsNullOrWhiteSpace(l.Name)
                                ? LocalizationManager.HueUnnamedLightText
                                : l.Name,
                            GetHueLightStateText(l, isIncluded),
                            isIncluded,
                            SetHueLightIncluded,
                            isIncluded
                            && l.IsOn
                            && l.State == HueLightState.LeftAlone
                            && _hueService.IsFollowingLights
                                ? (Action<string>)_hueService.FollowAgain
                                : null,
                            LocalizationManager.HueFollowAgainButton,
                            LocalizationManager.HueFollowAgainTooltip
                        );
                    })
                    .ToArray()
            ))
            .Where(g => g.Lights.Count > 0)
            .ToArray();

        HueLightGroups = groups;
    }

    private string GetHueLightStateText(HueLightInfo light, bool isIncluded) =>
        !isIncluded ? LocalizationManager.HueLightExcludedText
        : !light.IsOn ? LocalizationManager.HueLightOffText
        : !light.IsAdjustable ? LocalizationManager.HueLightNotAdjustableText
        : !light.CanFollow ? LocalizationManager.HueLightNothingFollowedText
        : !_hueService.IsFollowingLights ? LocalizationManager.HueLightPausedText
        : light.State switch
        {
            HueLightState.Following => LocalizationManager.HueLightFollowingText,
            HueLightState.NotResponding => LocalizationManager.HueLightNotRespondingText,
            HueLightState.LeftAlone when light.ReclaimAt is { } reclaimAt => string.Format(
                CultureInfo.CurrentCulture,
                LocalizationManager.HueLightLeftAloneUntilText,
                reclaimAt.ToLocalTime().ToString("t", CultureInfo.CurrentCulture)
            ),
            HueLightState.LeftAlone => LocalizationManager.HueLightLeftAloneText,
            _ => LocalizationManager.HueLightWaitingText,
        };

    private void SetHueLightIncluded(string lightId, bool isIncluded)
    {
        var excluded = (SettingsService.HueExcludedLightIds ?? [])
            .Where(id => id != lightId)
            .ToList();
        if (!isIncluded)
            excluded.Add(lightId);

        SettingsService.HueExcludedLightIds = excluded;
    }

    // Connects to the bridge (or stops waiting for its button to be pressed, so it can be
    // clicked again while it runs)
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ConnectHueAsync()
    {
        if (_hueService.IsConnecting)
            _hueService.CancelConnect();
        else if (IsHueDisconnectable)
            _hueService.Disconnect();
        else
            await _hueService.ConnectAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            EndHueLookPreview();
            _hueSubscription.Dispose();
        }

        base.Dispose(disposing);
    }
}
