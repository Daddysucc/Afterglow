using System.Runtime.InteropServices;

namespace Afterglow.PlatformInterop.Internal;

// 5x5 color transformation matrix, applied as [r, g, b, a, w] x M, in row-major order
[StructLayout(LayoutKind.Sequential)]
internal struct MagColorEffect
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
    public float[] Transform;
}

internal static partial class NativeMethods
{
    private const string Magnification = "Magnification.dll";

    [DllImport(Magnification, SetLastError = true)]
    public static extern bool MagInitialize();

    [DllImport(Magnification, SetLastError = true)]
    public static extern bool MagUninitialize();

    [DllImport(Magnification, SetLastError = true)]
    public static extern bool MagSetFullscreenColorEffect(ref MagColorEffect pEffect);
}
