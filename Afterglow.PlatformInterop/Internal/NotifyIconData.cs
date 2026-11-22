using System;
using System.Runtime.InteropServices;

namespace Afterglow.PlatformInterop.Internal;

// NOTIFYICONDATAW
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NotifyIconData
{
    public int Size;
    public nint WindowHandle;
    public int Id;
    public int Flags;
    public int CallbackMessage;
    public nint IconHandle;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string ToolTip;

    public int State;
    public int StateMask;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string Info;

    public int TimeoutOrVersion;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string InfoTitle;

    public int InfoFlags;
    public Guid GuidItem;
    public nint BalloonIconHandle;
}
