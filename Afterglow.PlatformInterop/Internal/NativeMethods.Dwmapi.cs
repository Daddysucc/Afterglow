using System.Runtime.InteropServices;

namespace Afterglow.PlatformInterop.Internal;

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

    // Returns an HRESULT
    [DllImport(Dwmapi, EntryPoint = "DwmGetWindowAttribute")]
    public static extern int DwmGetWindowRectAttribute(
        nint hwnd,
        int dwAttribute,
        out Rect pvAttribute,
        int cbAttribute
    );
}
