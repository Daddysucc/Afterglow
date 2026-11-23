using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Afterglow.Core;
using Afterglow.PlatformInterop;
using Afterglow.Utils;
using Avalonia.Threading;
using PowerKit;
using PowerKit.Extensions;

namespace Afterglow.Services;

public partial class GammaService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly IDisposable _eventSubscription;

    private bool _isUpdatingGamma;
    private bool _isDisposed;

    // Keyed by GDI device name (e.g. \.DISPLAY1), which stays valid as long as the display is connected
    private IReadOnlyDictionary<string, DeviceContext> _deviceContexts =
        new Dictionary<string, DeviceContext>();
    private bool _areDeviceContextsValid;
    private DateTimeOffset _lastGammaInvalidationTimestamp = DateTimeOffset.MinValue;

    private readonly GammaWriteTracker _writeTracker = new();
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

        // Never leave the displays tinted, even if the app crashes or is terminated
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
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
        _writeTracker.Reset();
    }

    // Assume gamma continues to be stale for some time after it has been invalidated.
    // This needs to be reasonably long because some external overrides (e.g. Windows
    // applying its own gamma ramp when the Quick Settings panel is opened for the
    // first time) don't happen immediately after the triggering event, but shortly
    // after it -- so we need to keep re-checking for a while to catch and correct them.
    // https://github.com/Tyrrrz/LightBulb/issues/448
    private static readonly TimeSpan StaleAfterInvalidationDuration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Occurs when gamma may have been changed from the outside, so it needs to be re-applied.
    /// </summary>
    public event Action? Invalidated;

    /// <summary>
    /// Whether gamma needs to be set frequently right now, rather than only when the configuration
    /// changes: shortly after it has been invalidated (to catch and correct delayed overrides), and
    /// while a display is waiting for a failed write to be retried.
    /// </summary>
    public bool NeedsFrequentUpdates =>
        (DateTimeOffset.Now - _lastGammaInvalidationTimestamp).Duration()
            <= StaleAfterInvalidationDuration
        || _writeTracker.HasPendingRetries;

    private bool IsGammaStale()
    {
        var instant = DateTimeOffset.Now;

        if (
            (instant - _lastGammaInvalidationTimestamp).Duration() <= StaleAfterInvalidationDuration
        )
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

    public void InvalidateGamma()
    {
        // Don't invalidate gamma when we're in the process of changing it ourselves,
        // to avoid an infinite loop.
        if (_isUpdatingGamma)
            return;

        _lastGammaInvalidationTimestamp = DateTimeOffset.Now;
        Debug.WriteLine("Gamma invalidated.");

        Invalidated?.Invoke();
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
        var instant = DateTimeOffset.Now;

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
                    if (!_writeTracker.ShouldWrite(deviceName, configuration, isStale, instant))
                        continue;

                    var isWritten = deviceContext.SetGamma(
                        GetRed(configuration) * configuration.Brightness,
                        GetGreen(configuration) * configuration.Brightness,
                        GetBlue(configuration) * configuration.Brightness
                    );

                    // A rejected ramp leaves the display unchanged, so it must be retried later
                    // rather than being remembered as applied
                    if (!isWritten)
                    {
                        _writeTracker.RecordFailure(deviceName, instant);
                        continue;
                    }

                    _writeTracker.RecordSuccess(deviceName, configuration);
                    isUpdated = true;

                    Debug.WriteLine($"Updated gamma on {deviceName} to {configuration}.");
                }
                catch (Exception ex)
                {
                    _writeTracker.RecordFailure(deviceName, instant);
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

    /// <summary>
    /// Restores neutral gamma on all displays. Safe to call from any thread and at any time,
    /// including while the app is crashing.
    /// </summary>
    public void RestoreDefaultGamma()
    {
        foreach (var deviceContext in _deviceContexts.Values)
        {
            try
            {
                deviceContext.ResetGamma();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to reset gamma: {ex}");
            }
        }
    }

    // Gamma ramps outlive the process that set them, so they're reset as soon as anything goes
    // wrong, rather than only during a regular shutdown (which a crash may skip, or cut short).
    // These may run on any thread, possibly while the UI thread is already gone.
    private void OnProcessExit(object? sender, EventArgs args) => RestoreDefaultGamma();

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs args) =>
        RestoreDefaultGamma();

    private void OnDispatcherUnhandledException(
        object? sender,
        DispatcherUnhandledExceptionEventArgs args
    )
    {
        RestoreDefaultGamma();

        // This runs on the UI thread, so if the app carries on, gamma is applied again right away
        _writeTracker.Reset();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;

        RestoreDefaultGamma();

        _eventSubscription.Dispose();
        Disposable.Merge(_deviceContexts.Values).Dispose();
    }
}

public partial class GammaService
{
    private static double GetRed(ColorConfiguration configuration) =>
        WhitePoint.GetRgb(configuration.Temperature).Red;

    private static double GetGreen(ColorConfiguration configuration) =>
        WhitePoint.GetRgb(configuration.Temperature).Green;

    private static double GetBlue(ColorConfiguration configuration) =>
        WhitePoint.GetRgb(configuration.Temperature).Blue;
}
