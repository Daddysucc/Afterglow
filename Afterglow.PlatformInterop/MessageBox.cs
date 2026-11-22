using System.Diagnostics;
using System.Runtime.InteropServices;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

// A native message box, for when the app's own UI isn't available (e.g. before it starts)
public static class MessageBox
{
    private const uint IconError = 0x10;
    private const uint IconInformation = 0x40;

    private static void Show(string title, string message, uint type)
    {
        // Returns zero only on failure
        if (NativeMethods.MessageBox(0, message, title, type) == 0)
        {
            Debug.WriteLine(
                "Failed to show message box. " + $"Error {Marshal.GetLastWin32Error()}."
            );
        }
    }

    public static void ShowError(string title, string message) => Show(title, message, IconError);

    public static void ShowInfo(string title, string message) =>
        Show(title, message, IconInformation);
}
