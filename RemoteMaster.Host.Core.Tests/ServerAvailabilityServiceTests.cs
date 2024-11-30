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

    [Fact]
    public async Task IsServerAvailableAsync_ShouldReturnTrue_WhenServerIsAvailable()
    {
        // Arrange
        const string server = "127.0.0.1";
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

    [Fact]
    public async Task IsServerAvailableAsync_ShouldRetry_WhenServerIsUnavailable()
    {
        // Arrange
        const string server = "127.0.0.1";
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock.SetupSequence(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
                     .Throws<SocketException>()
                     .Throws<SocketException>()
                     .Throws<SocketException>()
                     .Returns(Task.CompletedTask);

        // Act
        var result = await _service.IsServerAvailableAsync(server);

        // Assert
        Assert.True(result);
        _loggerMock.VerifyLog(LogLevel.Warning, $"Attempt 1 failed due to socket error. Retrying in {ServerAvailabilityService.ConnectionRetryDelay}ms...", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Warning, $"Attempt 2 failed due to socket error. Retrying in {2000}ms...", Times.Once()); // Expecting 2nd delay (2000ms)
        _loggerMock.VerifyLog(LogLevel.Warning, $"Attempt 3 failed due to socket error. Retrying in {4000}ms...", Times.Once()); // Expecting 3rd delay (4000ms)
        _loggerMock.VerifyLog(LogLevel.Information, $"Server {server} is available.", Times.Once());  // Server becomes available on the 4th attempt
    }

    [Fact]
    public async Task IsServerAvailableAsync_ShouldReturnFalse_AfterMaxAttempts()
    {
        // Arrange
        const string server = "127.0.0.1";
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

    [Fact]
    public async Task IsServerAvailableAsync_ShouldDelayBetweenRetries()
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
        _timeProviderMock.Verify(tp => tp.Delay(1000, It.IsAny<CancellationToken>()), Times.Once());  // First delay
        _timeProviderMock.Verify(tp => tp.Delay(2000, It.IsAny<CancellationToken>()), Times.Once());  // Second delay
        _timeProviderMock.Verify(tp => tp.Delay(4000, It.IsAny<CancellationToken>()), Times.Once());  // Third delay
        _timeProviderMock.Verify(tp => tp.Delay(5000, It.IsAny<CancellationToken>()), Times.Once());  // Fourth delay (max retry)
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

    #endregion
}
