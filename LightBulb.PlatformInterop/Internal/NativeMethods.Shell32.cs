using System;
using System.Runtime.InteropServices;

namespace LightBulb.PlatformInterop.Internal;

internal static partial class NativeMethods
{
    private const string Shell32 = "shell32.dll";
    private const string ShCore = "shcore.dll";

    [DllImport(Shell32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData lpData);

    // Returns an HRESULT
    [DllImport(Shell32)]
    public static extern int Shell_NotifyIconGetRect(
        ref NotifyIconIdentifier identifier,
        out Rect iconLocation
    );

    // Returns an HRESULT
    [DllImport(ShCore)]
    public static extern int GetDpiForMonitor(
        nint hmonitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY
    );

    // Returns an HRESULT; the path must be freed with CoTaskMemFree
    [DllImport(Shell32)]
    public static extern int SHGetKnownFolderPath(
        ref Guid rfid,
        uint dwFlags,
        nint hToken,
        out nint ppszPath
    );
}
