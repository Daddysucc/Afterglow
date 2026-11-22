using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Afterglow.PlatformInterop;
using AwesomeAssertions;
using Xunit;

namespace Afterglow.Tests;

public class RemoteStreamingSpecs
{
    private static readonly int[] Ports = [48010, 27036];

    private static IEnumerable<(TcpState, int)> Connections(params (TcpState, int)[] connections) =>
        connections;

    [Fact]
    public void I_observe_streaming_while_a_connection_is_established_on_a_configured_port()
    {
        // Act
        var isActive = RemoteSession.IsStreamingActive(
            Ports,
            () => false,
            () => Connections((TcpState.Listen, 48010), (TcpState.Established, 27036))
        );

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void I_observe_no_streaming_while_configured_ports_are_only_listening_or_closing()
    {
        // Act
        var isActive = RemoteSession.IsStreamingActive(
            Ports,
            () => false,
            () =>
                Connections(
                    (TcpState.Listen, 48010),
                    (TcpState.TimeWait, 48010),
                    (TcpState.Established, 443)
                )
        );

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void I_observe_streaming_during_a_remote_desktop_session_regardless_of_ports()
    {
        // Act
        var isActive = RemoteSession.IsStreamingActive([], () => true, () => Connections());

        // Assert
        isActive.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(NetworkExceptions))]
    public void I_observe_no_streaming_and_no_crash_when_network_connections_cannot_be_queried(
        Exception exception
    )
    {
        // Act
        var isActive = RemoteSession.IsStreamingActive(Ports, () => false, () => throw exception);

        // Assert
        isActive.Should().BeFalse();
    }

    public static TheoryData<Exception> NetworkExceptions =>
        new()
        {
            new NetworkInformationException(),
            new SocketException(),
            new InvalidOperationException(),
        };

    [Fact]
    public void I_observe_connections_still_being_checked_when_the_remote_desktop_check_fails()
    {
        // Act
        var isActive = RemoteSession.IsStreamingActive(
            Ports,
            () => throw new InvalidOperationException(),
            () => Connections((TcpState.Established, 48010))
        );

        // Assert
        isActive.Should().BeTrue();
    }
}
