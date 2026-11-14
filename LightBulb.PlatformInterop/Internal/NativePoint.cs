using System.Runtime.InteropServices;

namespace LightBulb.PlatformInterop.Internal;

// POINT
[StructLayout(LayoutKind.Sequential)]
internal readonly record struct NativePoint(int X, int Y);
