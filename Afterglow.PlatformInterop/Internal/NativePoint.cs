using System.Runtime.InteropServices;

namespace Afterglow.PlatformInterop.Internal;

// POINT
[StructLayout(LayoutKind.Sequential)]
internal readonly record struct NativePoint(int X, int Y);
