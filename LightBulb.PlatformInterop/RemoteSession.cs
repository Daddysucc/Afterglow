using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using LightBulb.PlatformInterop.Internal;

namespace LightBulb.PlatformInterop;

public static class RemoteSession
{
    private const int SmRemoteSession = 0x1000;

    /// <summary>
    /// Checks whether the current session is a Remote Desktop (RDP) session.
    /// </summary>
    public static bool IsActive() => NativeMethods.GetSystemMetrics(SmRemoteSession) != 0;

    /// <summary>
    /// Checks whether there's an established TCP connection on any of the given local ports
    /// (e.g. Sunshine's RTSP port while a client is streaming).
    /// </summary>
    public static bool HasEstablishedConnection(IReadOnlyCollection<int> localPorts)
    {
        if (localPorts.Count == 0)
            return false;

        return IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpConnections()
            .Any(c => c.State == TcpState.Established && localPorts.Contains(c.LocalEndPoint.Port));
    }

    /// <summary>
    /// Checks whether the computer is being accessed remotely: through Remote Desktop
    /// (detected regardless of port) or a streaming host listening on one of the given TCP ports.
    /// Only established connections count, since listening ports stay open even when idle.
    /// </summary>
    public static bool IsStreamingActive(IReadOnlyCollection<int> localPorts)
    {
        try
        {
            if (IsActive())
                return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check for a remote desktop session: {ex}");
        }

        try
        {
            return HasEstablishedConnection(localPorts);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check for active streaming connections: {ex}");
            return false;
        }
    }
}
