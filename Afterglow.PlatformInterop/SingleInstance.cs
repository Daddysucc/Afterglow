using System;
using System.Diagnostics;
using System.Threading;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

/// <summary>
/// Makes sure that only one instance of the app runs at a time (per user session), and lets
/// a later instance bring the running one to the front, rather than seemingly doing nothing.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activationEvent;
    private RegisteredWaitHandle? _activationRegistration;

    private SingleInstance(Mutex mutex, EventWaitHandle activationEvent)
    {
        _mutex = mutex;
        _activationEvent = activationEvent;
    }

    private static string GetMutexName(string name) => name + "_Identity";

    private static string GetActivationEventName(string name) => name + "_Activate";

    /// <summary>
    /// Claims the single instance with the given name, or returns <c>null</c> if another
    /// instance already has it.
    /// </summary>
    public static SingleInstance? TryAcquire(string name)
    {
        var mutex = new Mutex(true, GetMutexName(name), out var isOnlyInstance);
        if (!isOnlyInstance)
        {
            mutex.Dispose();
            return null;
        }

        // Created right away, so that activation requests aren't lost while the app is starting
        var activationEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            GetActivationEventName(name)
        );

        return new SingleInstance(mutex, activationEvent);
    }

    /// <summary>
    /// Whether an instance with the given name is running, without claiming it.
    /// </summary>
    public static bool IsRunning(string name)
    {
        try
        {
            if (!Mutex.TryOpenExisting(GetMutexName(name), out var mutex))
                return false;

            mutex.Dispose();
            return true;
        }
        // It exists, but belongs to a process this one isn't allowed to open it from
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>
    /// Asks the running instance with the given name to show itself. Returns <c>false</c> if it
    /// can't be asked (e.g. it's an older version, which doesn't listen for this).
    /// </summary>
    public static bool TryActivateRunningInstance(string name)
    {
        if (!EventWaitHandle.TryOpenExisting(GetActivationEventName(name), out var activationEvent))
            return false;

        using (activationEvent)
        {
            // Windows only lets a background process bring its window to the front if the
            // foreground process (this one, which the user has just started) allows it
            try
            {
                const int asfwAny = -1;
                if (!NativeMethods.AllowSetForegroundWindow(asfwAny))
                    Debug.WriteLine("Failed to allow the running instance to come to the front.");
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // Not essential
            }

            return activationEvent.Set();
        }
    }

    /// <summary>
    /// Calls the given action (on a thread pool thread) whenever another instance asks this one
    /// to show itself.
    /// </summary>
    public void ListenForActivation(Action onActivated)
    {
        _activationRegistration?.Unregister(null);
        _activationRegistration = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, _) => onActivated(),
            null,
            Timeout.Infinite,
            false
        );
    }

    public void Dispose()
    {
        _activationRegistration?.Unregister(null);
        _activationEvent.Dispose();
        _mutex.Dispose();
    }
}
