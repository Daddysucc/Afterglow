using System.Runtime.InteropServices;

namespace Afterglow.PlatformInterop;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Rect(int Left, int Top, int Right, int Bottom)
{
    public static Rect Empty { get; } = new();
}
