using System.Runtime.InteropServices;

namespace LightBulb.PlatformInterop.Internal;

internal static partial class NativeMethods
{
    private const string Dwmapi = "dwmapi.dll";

    // Returns an HRESULT
    [DllImport(Dwmapi)]
    public static extern int DwmGetWindowAttribute(
        nint hwnd,
        int dwAttribute,
        out int pvAttribute,
        int cbAttribute
    );
}
