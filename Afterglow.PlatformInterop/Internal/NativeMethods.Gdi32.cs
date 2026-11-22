using System.Runtime.InteropServices;

namespace Afterglow.PlatformInterop.Internal;

internal static partial class NativeMethods
{
    private const string Gdi32 = "gdi32.dll";

    [DllImport(Gdi32, SetLastError = true)]
    public static extern nint CreateDC(
        string? lpszDriver,
        string? lpszDevice,
        string? lpszOutput,
        nint lpInitData
    );

    [DllImport(Gdi32, SetLastError = true)]
    public static extern bool DeleteDC(nint hdc);

    [DllImport(Gdi32, SetLastError = true)]
    public static extern bool GetDeviceGammaRamp(nint hdc, out GammaRamp lpRamp);

    // Takes the red, green, and blue ramps (256 values each) one after another
    // in a single array, which is passed without being copied
    [DllImport(Gdi32, EntryPoint = "SetDeviceGammaRamp", SetLastError = true)]
    public static extern bool SetDeviceGammaRamp(nint hdc, [In] ushort[] lpRamp);
}
