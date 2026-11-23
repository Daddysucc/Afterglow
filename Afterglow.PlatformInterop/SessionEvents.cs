using System;
using Afterglow.PlatformInterop.Internal;
using PowerKit;

namespace Afterglow.PlatformInterop;

/// <summary>
/// Notifies about the computer shutting down, restarting, signing out, or going to sleep.
/// Callbacks run on the thread that owns the app's message window (the UI thread), and the
/// system waits for them to return (but only briefly).
/// </summary>
public static class SessionEvents
{
    private const int QueryEndSession = 0x0011;
    private const int EndSession = 0x0016;
    private const int PowerBroadcast = 0x0218;

    private const nint PowerSuspend = 0x0004;
    private const nint PowerResumeSuspend = 0x0007;
    private const nint PowerResumeAutomatic = 0x0012;

    /// <summary>
    /// Shutting down, restarting, or signing out is about to happen (ending is true), or was
    /// cancelled after all (ending is false).
    /// </summary>
    public static IDisposable RegisterSessionEnding(Action<bool> callback)
    {
        var query = WndProcSponge.Default.Listen(QueryEndSession, _ => callback(true));

        // Sent with a WParam of zero when the shutdown was cancelled (e.g. by another app)
        var end = WndProcSponge.Default.Listen(
            EndSession,
            m =>
            {
                if (m.WParam == 0)
                    callback(false);
            }
        );

        return Disposable.Create(() =>
        {
            query.Dispose();
            end.Dispose();
        });
    }

    /// <summary>
    /// The computer is going to sleep (suspending is true), or has woken up (suspending is false).
    /// </summary>
    public static IDisposable RegisterSleep(Action<bool> callback) =>
        WndProcSponge.Default.Listen(
            PowerBroadcast,
            m =>
            {
                if (m.WParam == PowerSuspend)
                    callback(true);
                // Both are sent when the user wakes it up, so this may be reported twice
                else if (m.WParam is PowerResumeAutomatic or PowerResumeSuspend)
                    callback(false);
            }
        );
}
