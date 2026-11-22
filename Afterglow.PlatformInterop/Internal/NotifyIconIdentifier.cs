using System;
using System.Runtime.InteropServices;

namespace Afterglow.PlatformInterop.Internal;

// NOTIFYICONIDENTIFIER
[StructLayout(LayoutKind.Sequential)]
internal struct NotifyIconIdentifier
{
    public int Size;
    public nint WindowHandle;
    public int Id;
    public Guid GuidItem;
}
