using LightBulb.PlatformInterop.Internal;

namespace LightBulb.PlatformInterop;

public static class RemoteSession
{
    private const int SmRemoteSession = 0x1000;

    /// <summary>
    /// Checks whether the current session is a Remote Desktop (RDP) session.
    /// </summary>
    public static bool IsActive() => NativeMethods.GetSystemMetrics(SmRemoteSession) != 0;
}
