using System;
using System.Text;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

public static class InputDesktop
{
    private const uint DesktopReadObjects = 0x0001;
    private const int UoiName = 2;

    /// <summary>
    /// Checks whether the workstation is locked (or another secure desktop, such as the UAC
    /// prompt, is receiving input instead of the user's regular desktop).
    /// </summary>
    public static bool IsLocked()
    {
        // The input desktop can't be opened at all while the secure desktop is active
        var desktopHandle = NativeMethods.OpenInputDesktop(0, false, DesktopReadObjects);
        if (desktopHandle == 0)
            return true;

        try
        {
            var name = new StringBuilder(256);
            if (
                !NativeMethods.GetUserObjectInformation(
                    desktopHandle,
                    UoiName,
                    name,
                    name.Capacity * sizeof(char),
                    out _
                )
            )
            {
                return false;
            }

            // The regular user desktop is called "Default", the lock screen is on "Winlogon"
            return !string.Equals(name.ToString(), "Default", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            NativeMethods.CloseDesktop(desktopHandle);
        }
    }
}
