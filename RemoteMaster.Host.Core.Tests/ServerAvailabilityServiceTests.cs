// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class ServerAvailabilityServiceTests
{
    private readonly Mock<ITcpClientFactory> _tcpClientFactoryMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly Mock<ILogger<ServerAvailabilityService>> _loggerMock;
    private readonly ServerAvailabilityService _service;

    public ServerAvailabilityServiceTests()
    {
        _tcpClientFactoryMock = new Mock<ITcpClientFactory>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _loggerMock = new Mock<ILogger<ServerAvailabilityService>>();

        _service = new ServerAvailabilityService(_tcpClientFactoryMock.Object, _timeProviderMock.Object, _loggerMock.Object);
    }

    #region IsServerAvailableAsync Tests

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.0.1")]
    public async Task IsServerAvailableAsync_ShouldReturnTrue_WhenServerIsAvailable(string server)
    {
        // Arrange
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock.Setup(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

        // Act
        var result = await _service.IsServerAvailableAsync(server);

        // Assert
        Assert.True(result);
        _loggerMock.VerifyLog(LogLevel.Information, $"Server {server} is available.", Times.Once());
    }

    [Theory]
    [InlineData(1, 1000)]
    [InlineData(2, 2000)]
    [InlineData(3, 4000)]
    public async Task IsServerAvailableAsync_ShouldRetry_WhenServerIsUnavailable(int attempt, int delay)
    {
        const string server = "127.0.0.1";
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        var sequence = tcpClientMock.SetupSequence(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()));

        for (var i = 0; i < attempt; i++)
        {
            sequence.Throws<SocketException>();
        }

        sequence.Returns(Task.CompletedTask);

        // Act
        var result = await _service.IsServerAvailableAsync(server);

        // Assert
        Assert.True(result);

        _loggerMock.VerifyLog(LogLevel.Warning, $"Attempt {attempt} failed due to socket error. Retrying in {delay}ms...", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, $"Server {server} is available.", Times.Once());
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.0.1")]
    public async Task IsServerAvailableAsync_ShouldReturnFalse_AfterMaxAttempts(string server)
    {
        // Arrange
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock.Setup(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
                     .Throws<SocketException>();

        // Act
        var result = await _service.IsServerAvailableAsync(server);

        // Assert
        Assert.False(result);
        _loggerMock.VerifyLog(LogLevel.Error, $"Server {server} is unavailable after {ServerAvailabilityService.MaxConnectionAttempts} attempts.", Times.Once());
    }

    [Theory]
    [InlineData(1, 1000)]
    [InlineData(2, 2000)]
    [InlineData(3, 4000)]
    [InlineData(4, 5000)]
    public async Task IsServerAvailableAsync_ShouldDelayBetweenRetries(int attempt, int expectedDelay)
    {
        // Arrange
        const string server = "127.0.0.1";
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock.Setup(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
                     .Throws<SocketException>();

        _timeProviderMock.Setup(tp => tp.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);

        // Act
        await _service.IsServerAvailableAsync(server);

        // Assert
        _timeProviderMock.Verify(tp => tp.Delay(expectedDelay, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task IsServerAvailableAsync_ShouldHandleCancellationGracefully()
    {
        // Arrange
        const string server = "127.0.0.1";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.IsServerAvailableAsync(server, cts.Token));

        _loggerMock.VerifyLog(LogLevel.Warning, It.IsAny<string>(), Times.Never());
    }

    [Fact]
    public async Task IsServerAvailableAsync_ShouldLogError_OnUnexpectedException()
    {
        // Arrange
        const string server = "127.0.0.1";
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock.Setup(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
                     .Throws<InvalidOperationException>(); // Unexpected exception

        // Act
        var result = await _service.IsServerAvailableAsync(server);

        // Assert
        Assert.False(result);
        _loggerMock.VerifyLog(LogLevel.Error, "An unexpected error occurred while checking server availability.", Times.Once());
    }

    #endregion
}
