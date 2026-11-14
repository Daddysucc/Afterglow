using System;
using System.Diagnostics;
using Avalonia.Threading;
using LightBulb.PlatformInterop;

namespace LightBulb.Services;

/// <summary>
/// Turns the whole screen grayscale, using the Windows Magnification API. This is independent of
/// gamma ramps, so the color temperature still applies on top of it (as a warm monochrome tint).
/// </summary>
public class GrayscaleService : IDisposable
{
    // Systems where the Magnification API doesn't work keep failing, so don't retry on every update
    public static TimeSpan RetryDelay { get; } = TimeSpan.FromSeconds(5);

    private readonly Func<bool> _tryApply;
    private readonly Action _reset;
    private readonly Action<Action> _runOnUiThread;
    private readonly Func<DateTimeOffset> _getNow;
    private readonly bool _isHookedToProcess;

    private DateTimeOffset _lastFailureTimestamp = DateTimeOffset.MinValue;
    private bool _isDisposed;

    public GrayscaleService()
        : this(
            () => FullscreenColorEffect.TryApply(FullscreenColorEffect.GrayscaleMatrix),
            FullscreenColorEffect.Reset,
            // The Magnification API must be used from the UI (STA) thread
            action =>
            {
                if (Dispatcher.UIThread.CheckAccess())
                    action();
                else
                    Dispatcher.UIThread.Post(action);
            },
            () => DateTimeOffset.Now,
            true
        ) { }

    // Allows substituting the screen effect and the threading (e.g. in tests)
    internal GrayscaleService(
        Func<bool> tryApply,
        Action reset,
        Action<Action> runOnUiThread,
        Func<DateTimeOffset> getNow,
        bool isHookedToProcess = false
    )
    {
        _tryApply = tryApply;
        _reset = reset;
        _runOnUiThread = runOnUiThread;
        _getNow = getNow;
        _isHookedToProcess = isHookedToProcess;

        // Never leave the screen grayscale, even if the app exits unexpectedly
        if (_isHookedToProcess)
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
        }
    }

    /// <summary>
    /// Whether the screen is currently grayscale.
    /// </summary>
    public bool IsApplied { get; private set; }

    /// <summary>
    /// Turns grayscale on or off. The Magnification API is only initialized while grayscale is on.
    /// </summary>
    public void SetEnabled(bool isEnabled) =>
        _runOnUiThread(() =>
        {
            if (_isDisposed || isEnabled == IsApplied)
                return;

            if (isEnabled)
                TryEnable();
            else
                Disable();
        });

    private void TryEnable()
    {
        var now = _getNow();
        if ((now - _lastFailureTimestamp).Duration() < RetryDelay)
            return;

        bool isApplied;
        try
        {
            isApplied = _tryApply();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to turn grayscale on: {ex}");
            isApplied = false;
        }

        if (isApplied)
        {
            IsApplied = true;
            return;
        }

        // Don't keep a half-initialized API around
        _lastFailureTimestamp = now;
        Disable();
    }

    private void Disable()
    {
        try
        {
            _reset();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to turn grayscale off: {ex}");
        }

        IsApplied = false;
    }

    // These run on whatever thread raised them, possibly while the UI thread is gone,
    // so the effect is reset directly (it's safe to call from any thread)
    private void OnProcessExit(object? sender, EventArgs args) => Disable();

    // A crash on a background thread ends the process without the usual shutdown
    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs args) =>
        Disable();

    private void OnDispatcherUnhandledException(
        object? sender,
        DispatcherUnhandledExceptionEventArgs args
    ) => Disable();

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_isHookedToProcess)
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;
        }

        Disable();
    }
}
