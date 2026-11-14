using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LightBulb.Core;
using LightBulb.PlatformInterop;
using PowerKit;
using PowerKit.Extensions;

namespace LightBulb.Services;

public partial class GammaService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly IDisposable _eventSubscription;

    private bool _isUpdatingGamma;

    // Keyed by GDI device name (e.g. \.DISPLAY1), which stays valid as long as the display is connected
    private IReadOnlyDictionary<string, DeviceContext> _deviceContexts =
        new Dictionary<string, DeviceContext>();
    private bool _areDeviceContextsValid;
    private DateTimeOffset _lastGammaInvalidationTimestamp = DateTimeOffset.MinValue;

    private readonly Dictionary<string, ColorConfiguration> _lastConfigurations = new(
        StringComparer.OrdinalIgnoreCase
    );
    private DateTimeOffset _lastUpdateTimestamp = DateTimeOffset.MinValue;

    public GammaService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        // Listen to all system events that may indicate that the device context or gamma was changed from the outside
        _eventSubscription = Disposable.Merge(
            // https://github.com/Tyrrrz/LightBulb/issues/223
            SystemHook.TryRegister(SystemHook.Ids.ForegroundWindowChanged, InvalidateGamma)
                ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.ConsoleDisplayStateChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.PowerSavingStatusChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.SessionDisplayStatusChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.MonitorPowerStateChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            PowerSettingNotification.TryRegister(
                PowerSettingNotification.Ids.AwayModeChanged,
                InvalidateGamma
            ) ?? Disposable.Null,
            SystemEvent.Register(SystemEvent.Ids.DisplayChanged, InvalidateDeviceContexts),
            SystemEvent.Register(SystemEvent.Ids.PaletteChanged, InvalidateDeviceContexts),
            SystemEvent.Register(SystemEvent.Ids.SettingsChanged, InvalidateDeviceContexts),
            SystemEvent.Register(SystemEvent.Ids.SystemColorsChanged, InvalidateDeviceContexts)
        );
    }

    private void EnsureValidDeviceContexts()
    {
        if (_areDeviceContextsValid)
            return;

        _areDeviceContextsValid = true;

        Disposable.Merge(_deviceContexts.Values).Dispose();

        var deviceContexts = new Dictionary<string, DeviceContext>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var monitor in Monitor.GetAll())
        {
            var deviceName = monitor.TryGetDeviceName();
            if (string.IsNullOrWhiteSpace(deviceName) || deviceContexts.ContainsKey(deviceName))
                continue;

            var deviceContext = DeviceContext.TryCreate(deviceName);
            if (deviceContext is not null)
                deviceContexts[deviceName] = deviceContext;
        }

        _deviceContexts = deviceContexts;

        // Force gamma to be re-applied on all (possibly new) displays
        _lastConfigurations.Clear();
    }

    private bool IsGammaStale()
    {
        var instant = DateTimeOffset.Now;

        // Assume gamma continues to be stale for some time after it has been invalidated.
        // This needs to be reasonably long because some external overrides (e.g. Windows
        // applying its own gamma ramp when the Quick Settings panel is opened for the
        // first time) don't happen immediately after the triggering event, but shortly
        // after it -- so we need to keep re-checking for a while to catch and correct them.
        // https://github.com/Tyrrrz/LightBulb/issues/448
        if ((instant - _lastGammaInvalidationTimestamp).Duration() <= TimeSpan.FromSeconds(2))
        {
            // Avoid spamming gamma updates on frequent invalidation sources (e.g. foreground window changes).
            return (instant - _lastUpdateTimestamp).Duration() >= TimeSpan.FromMilliseconds(200);
        }

        // If polling is enabled, assume gamma is stale after some time has passed since the last update
        if (
            _settingsService.IsGammaPollingEnabled
            && (instant - _lastUpdateTimestamp).Duration() > _settingsService.GammaPollingInterval
        )
        {
            return true;
        }

        return false;
    }

    private bool IsSignificantChange(string deviceName, ColorConfiguration configuration)
    {
        // Nothing to compare to
        if (!_lastConfigurations.TryGetValue(deviceName, out var lastConfiguration))
            return true;

        return Math.Abs(configuration.Temperature - lastConfiguration.Temperature) > 15
            || Math.Abs(configuration.Brightness - lastConfiguration.Brightness) > 0.01;
    }

    public void InvalidateGamma()
    {
        // Don't invalidate gamma when we're in the process of changing it ourselves,
        // to avoid an infinite loop.
        if (_isUpdatingGamma)
            return;

        _lastGammaInvalidationTimestamp = DateTimeOffset.Now;
        Debug.WriteLine("Gamma invalidated.");
    }

    public void InvalidateDeviceContexts()
    {
        _areDeviceContextsValid = false;
        Debug.WriteLine("Device contexts invalidated.");

        InvalidateGamma();
    }

    /// <summary>
    /// Sets gamma on each display, using the configuration resolved for its GDI device name.
    /// </summary>
    public void SetGamma(Func<string, ColorConfiguration> getConfiguration)
    {
        // Device contexts are re-created after the display configuration changes (e.g. hot-plug)
        EnsureValidDeviceContexts();

        var isStale = IsGammaStale();
        var isUpdated = false;

        _isUpdatingGamma = true;

        try
        {
            foreach (var (deviceName, deviceContext) in _deviceContexts)
            {
                // Some displays don't support gamma ramps (e.g. certain virtual displays, or HDR
                // and color-managed ones). A failure on one display must not affect the others.
                try
                {
                    var configuration = getConfiguration(deviceName);

                    // Avoid unnecessary changes as updating too often will cause stuttering
                    if (!isStale && !IsSignificantChange(deviceName, configuration))
                        continue;

                    deviceContext.SetGamma(
                        GetRed(configuration) * configuration.Brightness,
                        GetGreen(configuration) * configuration.Brightness,
                        GetBlue(configuration) * configuration.Brightness
                    );

                    _lastConfigurations[deviceName] = configuration;
                    isUpdated = true;

                    Debug.WriteLine($"Updated gamma on {deviceName} to {configuration}.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to update gamma on {deviceName}: {ex}");
                }
            }
        }
        finally
        {
            _isUpdatingGamma = false;
        }

        if (isUpdated)
            _lastUpdateTimestamp = DateTimeOffset.Now;
    }

    public void Dispose()
    {
        // Reset gamma on all contexts
        foreach (var deviceContext in _deviceContexts.Values)
            deviceContext.ResetGamma();

        _eventSubscription.Dispose();
        Disposable.Merge(_deviceContexts.Values).Dispose();
    }
}

public partial class GammaService
{
    private static double GetRed(ColorConfiguration configuration)
    {
        // Algorithm taken from http://tannerhelland.com/4435/convert-temperature-rgb-algorithm-code

        if (configuration.Temperature > 6600)
        {
            return (
                Math.Pow(configuration.Temperature / 100 - 60, -0.1332047592) * 329.698727446 / 255
            ).Clamp(0, 1);
        }

        return 1;
    }

    private static double GetGreen(ColorConfiguration configuration)
    {
        // Algorithm taken from http://tannerhelland.com/4435/convert-temperature-rgb-algorithm-code

        if (configuration.Temperature > 6600)
        {
            return (
                Math.Pow(configuration.Temperature / 100 - 60, -0.0755148492) * 288.1221695283 / 255
            ).Clamp(0, 1);
        }

        return (
            (Math.Log(configuration.Temperature / 100) * 99.4708025861 - 161.1195681661) / 255
        ).Clamp(0, 1);
    }

    private static double GetBlue(ColorConfiguration configuration)
    {
        // Algorithm taken from http://tannerhelland.com/4435/convert-temperature-rgb-algorithm-code

        if (configuration.Temperature >= 6600)
            return 1;

        if (configuration.Temperature <= 1900)
            return 0;

        return (
            (Math.Log(configuration.Temperature / 100 - 10) * 138.5177312231 - 305.0447927307) / 255
        ).Clamp(0, 1);
    }
}
