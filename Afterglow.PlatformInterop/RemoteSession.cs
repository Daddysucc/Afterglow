using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Afterglow.PlatformInterop.Internal;

namespace Afterglow.PlatformInterop;

public static class RemoteSession
{
    private const int SmRemoteSession = 0x1000;

    /// <summary>
    /// Checks whether the current session is a Remote Desktop (RDP) session.
    /// </summary>
    public static bool IsActive() => NativeMethods.GetSystemMetrics(SmRemoteSession) != 0;

    private static IEnumerable<(TcpState State, int LocalPort)> GetTcpConnections() =>
        IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpConnections()
            .Select(c => (c.State, c.LocalEndPoint.Port));

    /// <summary>
    /// Checks whether there's an established TCP connection on any of the given local ports
    /// (e.g. Sunshine's RTSP port while a client is streaming).
    /// </summary>
    public static bool HasEstablishedConnection(IReadOnlyCollection<int> localPorts) =>
        HasEstablishedConnection(localPorts, GetTcpConnections);

    private static bool HasEstablishedConnection(
        IReadOnlyCollection<int> localPorts,
        Func<IEnumerable<(TcpState State, int LocalPort)>> getTcpConnections
    ) =>
        localPorts.Count > 0
        && getTcpConnections()
            .Any(c => c.State == TcpState.Established && localPorts.Contains(c.LocalPort));

    /// <summary>
    /// Checks whether the computer is being accessed remotely: through Remote Desktop
    /// (detected regardless of port) or a streaming host listening on one of the given TCP ports.
    /// Only established connections count, since listening ports stay open even when idle.
    /// Never throws: on failure, the corresponding check is treated as negative.
    /// </summary>
    public static bool IsStreamingActive(IReadOnlyCollection<int> localPorts) =>
        IsStreamingActive(localPorts, IsActive, GetTcpConnections);

    // Separate from the above so that the checks can be substituted in tests
    internal static bool IsStreamingActive(
        IReadOnlyCollection<int> localPorts,
        Func<bool> isRemoteDesktopActive,
        Func<IEnumerable<(TcpState State, int LocalPort)>> getTcpConnections
    )
    {
        try
        {
            if (isRemoteDesktopActive())
                return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check for a remote desktop session: {ex}");
        }

        try
        {
            return HasEstablishedConnection(localPorts, getTcpConnections);
        }
        // Network information may be unavailable, e.g. on locked-down systems or with unusual
        // network adapters. This is called on a timer, so it must never interrupt it.
        catch (NetworkInformationException ex)
        {
            Debug.WriteLine($"Failed to query network connections: {ex}");
            return false;
        }
        catch (SocketException ex)
        {
            Debug.WriteLine($"Failed to query network connections: {ex}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check for active streaming connections: {ex}");
            return false;
        }
    }
}
