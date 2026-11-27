using System;
using System.Runtime.InteropServices;
using PowerKit;

namespace Afterglow.PlatformInterop;

/// <summary>
/// Work that should never get in the way of anything else on the computer.
/// </summary>
public static partial class BackgroundWork
{
    private const int ThreadModeBackgroundBegin = 0x00010000;
    private const int ThreadModeBackgroundEnd = 0x00020000;

    /// <summary>
    /// Puts the current thread in Windows' background mode until disposed: the lowest priority
    /// for the processor, and also for the disk and memory, so that it only uses what nothing
    /// else needs. Must be disposed on the same thread.
    /// </summary>
    public static IDisposable EnterBackgroundMode()
    {
        var thread = GetCurrentThread();
        if (!SetThreadPriority(thread, ThreadModeBackgroundBegin))
            return Disposable.Null;

        return Disposable.Create(() =>
            SetThreadPriority(GetCurrentThread(), ThreadModeBackgroundEnd)
        );
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentThread();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetThreadPriority(nint thread, int priority);
}
