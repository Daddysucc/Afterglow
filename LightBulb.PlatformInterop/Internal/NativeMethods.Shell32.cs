using System.Runtime.InteropServices;

namespace LightBulb.PlatformInterop.Internal;

internal static partial class NativeMethods
{
    private const string Shell32 = "shell32.dll";

    [DllImport(Shell32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData lpData);

    [DllImport(Shell32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern uint ExtractIconEx(
        string lpszFile,
        int nIconIndex,
        nint[]? phiconLarge,
        nint[]? phiconSmall,
        uint nIcons
    );
}
