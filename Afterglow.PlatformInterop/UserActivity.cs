using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

/// <summary>
/// What the user is doing: whether they've touched the mouse or keyboard lately, where, and
/// whether something (like a video or a game) is keeping the display on. All cheap to check.
/// </summary>
public static partial class UserActivity
{
    /// <summary>
    /// How long it's been since the last mouse or keyboard input (game controllers don't count:
    /// see <see cref="TryGetControllerState" />).
    /// </summary>
    public static TimeSpan GetIdleTime()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        // (both wrap around every 49.7 days, so they're compared as unsigned)
        return TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.Time));
    }

    /// <summary>
    /// Whether any application is keeping the display on, as video players, browsers playing
    /// video, and most games do while they play.
    /// </summary>
    public static bool IsDisplayKeptOn()
    {
        const int systemExecutionState = 16;
        const uint displayRequired = 0x2;

        return CallNtPowerInformation(systemExecutionState, 0, 0, out var state, sizeof(uint)) == 0
            && (state & displayRequired) != 0;
    }

    /// <summary>
    /// The bounds of the displays that the user is working on: where the mouse cursor is, and
    /// where the window in focus is.
    /// </summary>
    public static IReadOnlyList<Rect> GetActiveMonitorBounds()
    {
        const uint defaultToNearest = 2;
        var bounds = new List<Rect>();

        if (NativeMethods.GetCursorPos(out var point))
        {
            var handle = NativeMethods.MonitorFromPoint(point, defaultToNearest);
            if (handle != 0 && new Monitor(handle).TryGetBounds() is { } cursorBounds)
                bounds.Add(cursorBounds);
        }

        using var window = Window.TryGetForeground();
        if (
            window?.TryGetMonitor()?.TryGetBounds() is { } windowBounds
            && !bounds.Contains(windowBounds)
        )
            bounds.Add(windowBounds);

        return bounds;
    }

    /// <summary>
    /// A value that changes whenever a game controller (any that Windows sees as an Xbox
    /// controller) is used, connected or disconnected, or <c>null</c> if that can't be told.
    /// Only meaningful compared with an earlier one.
    /// </summary>
    public static int? TryGetControllerState()
    {
        const uint maxControllers = 4;

        try
        {
            var hash = new HashCode();
            for (uint i = 0; i < maxControllers; i++)
            {
                // (the packet number goes up with every change, however short)
                hash.Add(XInputGetState(i, out var state) == 0 ? (long)state.PacketNumber : -1);
            }

            return hash.ToHashCode();
        }
        // XInput may be missing on stripped-down editions of Windows
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ControllerState
    {
        public uint PacketNumber;
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short LeftThumbX;
        public short LeftThumbY;
        public short RightThumbX;
        public short RightThumbY;
    }

    [LibraryImport("xinput1_4.dll")]
    private static partial uint XInputGetState(uint userIndex, out ControllerState state);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLastInputInfo(ref LastInputInfo info);

    [LibraryImport("powrprof.dll")]
    private static partial uint CallNtPowerInformation(
        int level,
        nint input,
        uint inputLength,
        out uint output,
        uint outputLength
    );
}
