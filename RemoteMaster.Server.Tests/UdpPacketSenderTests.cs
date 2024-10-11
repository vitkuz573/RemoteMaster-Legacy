// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class UdpPacketSenderTests
{
    private readonly Mock<IUdpClient> _udpClientMock;
    private readonly UdpPacketSender _udpPacketSender;

    public UdpPacketSenderTests()
    {
        _udpClientMock = new Mock<IUdpClient>();
        Mock<ILogger<UdpPacketSender>> loggerMock = new();

        _udpPacketSender = new UdpPacketSender(() => _udpClientMock.Object, loggerMock.Object);
    }

    [Fact]
    public void Send_NullPacket_ReturnsArgumentNullExceptionResult()
    {
        // Arrange
        byte[] packet = null!;
        var endPoint = new IPEndPoint(IPAddress.Loopback, 12345);

        // Act
        var result = _udpPacketSender.Send(packet, endPoint);

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.FirstOrDefault();
        Assert.NotNull(errorDetails);
        Assert.Contains("packet", errorDetails.Message);
    }

    [Fact]
    public void Send_ValidPacket_CallsUdpClientSend()
    {
        // Arrange
        var packet = new byte[] { 1, 2, 3, 4 };
        var endPoint = new IPEndPoint(IPAddress.Loopback, 12345);

        // Act
        var result = _udpPacketSender.Send(packet, endPoint);

        // Assert
        Assert.True(result.IsSuccess);
        _udpClientMock.Verify(client => client.Send(packet, packet.Length, endPoint), Times.Once);
    }
}