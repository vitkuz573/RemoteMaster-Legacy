// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class WakeOnLanServiceTests
{
    [Fact]
    public void WakeUp_SendsMagicPacket()
    {
        // Arrange
        var packetSenderMock = new Mock<IPacketSender>();
        var wakeOnLanService = new WakeOnLanService(packetSenderMock.Object);
        var macAddress = PhysicalAddress.Parse("01:23:45:67:89:AB");
        const int port = 9;

        var macBytes = macAddress.GetAddressBytes();
        var expectedPacket = Enumerable.Repeat((byte)0xFF, 6)
                                       .Concat(Enumerable.Repeat(macBytes, 16).SelectMany(b => b))
                                       .ToArray();

        var expectedEndPoint = new IPEndPoint(IPAddress.Broadcast, port);

        // Act
        wakeOnLanService.WakeUp(macAddress, port);

        // Assert
        packetSenderMock.Verify(ps => ps.Send(It.Is<byte[]>(packet => packet.SequenceEqual(expectedPacket)),
                It.Is<IPEndPoint>(ep => ep.Equals(expectedEndPoint))),
            Times.Once);
    }

    [Fact]
    public void WakeUp_DefaultPort()
    {
        // Arrange
        var packetSenderMock = new Mock<IPacketSender>();
        var wakeOnLanService = new WakeOnLanService(packetSenderMock.Object);
        var macAddress = PhysicalAddress.Parse("01:23:45:67:89:AB");
        const int defaultPort = 9;

        var macBytes = macAddress.GetAddressBytes();
        var expectedPacket = Enumerable.Repeat((byte)0xFF, 6)
                                       .Concat(Enumerable.Repeat(macBytes, 16).SelectMany(b => b))
                                       .ToArray();

        var expectedEndPoint = new IPEndPoint(IPAddress.Broadcast, defaultPort);

        // Act
        wakeOnLanService.WakeUp(macAddress);

        // Assert
        packetSenderMock.Verify(ps => ps.Send(It.Is<byte[]>(packet => packet.SequenceEqual(expectedPacket)),
                It.Is<IPEndPoint>(ep => ep.Equals(expectedEndPoint))),
            Times.Once);
    }
}
