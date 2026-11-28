using System;
using System.Globalization;
using Afterglow.Core;
using Afterglow.Localization;
using Afterglow.PlatformInterop;
using Afterglow.Services;
using Afterglow.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Afterglow.ViewModels.Components.Settings;

/// <summary>
/// A connected display, along with its preferences.
/// </summary>
public partial class MonitorItemViewModel(
    SettingsService settingsService,
    LocalizationManager localizationManager,
    DisplayMonitor monitor,
    int number,
    string displayName
) : ObservableObject
{
    private ColorConfiguration? _effectiveConfiguration;

    public LocalizationManager LocalizationManager { get; } = localizationManager;

    public DisplayMonitor Monitor { get; } = monitor;

    // 1-based position: the primary display first, then the rest from left to right
    public int Number { get; } = number;

    // Friendly name, with a suffix when several connected displays share the same name
    public string DisplayName { get; } = displayName;

    public bool IsPrimary => Monitor.IsPrimary;

    private MonitorProfile? Profile => settingsService.TryGetMonitorProfile(Monitor.DevicePath);

    private void UpdateProfile(Func<MonitorProfile, MonitorProfile> update) =>
        settingsService.UpdateMonitorProfile(Monitor.DevicePath, Monitor.FriendlyName, update);

    public bool IsEnabled
    {
        get => Profile?.IsEnabled ?? true;
        set => UpdateProfile(p => p with { IsEnabled = value });
    }

    public double TemperatureOffset
    {
        get => Profile?.TemperatureOffset ?? 0;
        set =>
            UpdateProfile(p =>
                p with
                {
                    TemperatureOffset = MonitorProfile.NormalizeTemperatureOffset(value),
                }
            );
    }

    public double BrightnessOffset
    {
        get => Profile?.BrightnessOffset ?? 0;
        set =>
            UpdateProfile(p =>
                p with
                {
                    BrightnessOffset = MonitorProfile.NormalizeBrightnessOffset(value),
                }
            );
    }

    public double MaxTemperatureOffset => MonitorProfile.MaxTemperatureOffset;

    public double MinTemperatureOffset => -MonitorProfile.MaxTemperatureOffset;

    public double TemperatureOffsetStep => MonitorProfile.TemperatureOffsetStep;

    public double MaxBrightnessOffset => MonitorProfile.MaxBrightnessOffset;

    public double MinBrightnessOffset => -MonitorProfile.MaxBrightnessOffset;

    // (typed in: the number in it is applied, kept within bounds as the slider would; anything
    // else is ignored)
    public string TemperatureOffsetText
    {
        get => TemperatureOffset.ToString("+0;-0;0", CultureInfo.CurrentCulture) + " K";
        set
        {
            if (NumberInput.TryParse(value, CultureInfo.CurrentCulture, out var number))
                TemperatureOffset = number;
        }
    }

    public string BrightnessOffsetText
    {
        get => (BrightnessOffset * 100).ToString("+0;-0;0", CultureInfo.CurrentCulture) + "%";
        set
        {
            if (NumberInput.TryParse(value, CultureInfo.CurrentCulture, out var number))
                BrightnessOffset = number / 100;
        }
    }

    public bool HasOffsets => Profile?.HasOffsets ?? false;

    // e.g. "Effective now: 4250 K · 95%", or a note that the display is excluded
    public string EffectiveText =>
        !IsEnabled ? LocalizationManager.ExcludedNeutralText
        : _effectiveConfiguration is { } configuration
            ? string.Format(
                LocalizationManager.EffectiveNowText,
                configuration.Temperature.ToString("F0", CultureInfo.CurrentCulture),
                configuration.Brightness.ToString("P0", CultureInfo.CurrentCulture)
            )
        : "";

    [RelayCommand]
    private void ResetOffsets() =>
        UpdateProfile(p => p with { TemperatureOffset = 0, BrightnessOffset = 0 });

    // Called when the settings change
    public void Refresh() => OnPropertyChanged(string.Empty);

    // Called when the current color configuration of the display changes
    public void UpdateEffectiveConfiguration(ColorConfiguration? configuration)
    {
        if (_effectiveConfiguration == configuration)
            return;

        _effectiveConfiguration = configuration;
        OnPropertyChanged(nameof(EffectiveText));
    }
}

/// <summary>
/// A display that has saved preferences, but is not currently connected.
/// </summary>
public partial class DisconnectedMonitorItemViewModel(
    SettingsService settingsService,
    MonitorProfile profile
) : ObservableObject
{
    public string DevicePath => profile.DevicePath;

    public string Name =>
        string.IsNullOrWhiteSpace(profile.LastKnownName)
            ? profile.DevicePath
            : profile.LastKnownName;

    [RelayCommand]
    private void Forget() => settingsService.ForgetMonitorProfile(profile.DevicePath);
}
