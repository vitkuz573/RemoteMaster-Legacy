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
    [InlineData("127.0.0.1", 3, 1000, 5000)]
    [InlineData("192.168.0.1", 4, 500, 3000)]
    public async Task IsServerAvailableAsync_ShouldReturnTrue_WhenServerIsAvailable(string server, int maxAttempts, int initialRetryDelay, int maxRetryDelay)
    {
        // Arrange
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock.Setup(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

        // Act
        var result = await _service.IsServerAvailableAsync(server, maxAttempts, initialRetryDelay, maxRetryDelay);

        // Assert
        Assert.True(result);
        _loggerMock.VerifyLog(LogLevel.Information, $"Server {server} is available.", Times.Once());
    }

    [Theory]
    [InlineData("127.0.0.1", 1, 1000, 5000, 1000)]
    [InlineData("127.0.0.1", 2, 1000, 5000, 2000)]
    [InlineData("127.0.0.1", 3, 1000, 5000, 4000)]
    [InlineData("127.0.0.1", 4, 1000, 5000, 5000)]
    [InlineData("192.168.0.1", 2, 500, 3000, 1000)]
    public async Task IsServerAvailableAsync_ShouldRetry_WhenServerIsUnavailable(string server, int failedAttempts, int initialRetryDelay, int maxRetryDelay, int expectedDelay)
    {
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        var sequence = tcpClientMock.SetupSequence(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()));

        for (var i = 0; i < failedAttempts; i++)
        {
            sequence.Throws<SocketException>();
        }

        sequence.Returns(Task.CompletedTask);

        _timeProviderMock.Setup(tp => tp.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.IsServerAvailableAsync(server, failedAttempts + 1, initialRetryDelay, maxRetryDelay);

        // Assert
        Assert.True(result);

        _loggerMock.VerifyLog(LogLevel.Warning, $"Attempt {failedAttempts} failed due to socket error. Retrying in {expectedDelay}ms...", Times.Once());
        _loggerMock.VerifyLog(LogLevel.Information, $"Server {server} is available.", Times.Once());
    }

    [Theory]
    [InlineData("127.0.0.1", 3, 1000, 5000)]
    [InlineData("192.168.0.1", 4, 500, 3000)]
    public async Task IsServerAvailableAsync_ShouldReturnFalse_AfterMaxAttempts(string server, int maxAttempts, int initialRetryDelay, int maxRetryDelay)
    {
        // Arrange
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock.Setup(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
                     .Throws<SocketException>();

        _timeProviderMock.Setup(tp => tp.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.IsServerAvailableAsync(server, maxAttempts, initialRetryDelay, maxRetryDelay);

        // Assert
        Assert.False(result);
        _loggerMock.VerifyLog(LogLevel.Error, $"Server {server} is unavailable after {maxAttempts} attempts.", Times.Once());

        for (var i = 0; i < maxAttempts - 1; i++)
        {
            var expectedDelay = initialRetryDelay * (int)Math.Pow(2, i);
            expectedDelay = Math.Min(expectedDelay, maxRetryDelay);

            _timeProviderMock.Verify(tp => tp.Delay(expectedDelay, It.IsAny<CancellationToken>()), Times.Once());
        }
    }

    [Theory]
    [InlineData("127.0.0.1", 2, 1000, 5000, 1000)]
    [InlineData("192.168.0.1", 3, 500, 3000, 1000)]
    public async Task IsServerAvailableAsync_ShouldDelayBetweenRetries(string server, int failedAttempts, int initialRetryDelay, int maxRetryDelay, int expectedDelay)
    {
        // Arrange
        var maxAttempts = failedAttempts + 1;
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        var sequence = tcpClientMock.SetupSequence(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()));
        
        for (var i = 0; i < failedAttempts; i++)
        {
            sequence.Throws<SocketException>();
        }

        sequence.Returns(Task.CompletedTask);

        _timeProviderMock.Setup(tp => tp.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.IsServerAvailableAsync(server, maxAttempts, initialRetryDelay, maxRetryDelay);

        // Assert
        Assert.True(result);
        _timeProviderMock.Verify(tp => tp.Delay(expectedDelay, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task IsServerAvailableAsync_ShouldHandleCancellationGracefully()
    {
        // Arrange
        const string server = "127.0.0.1";
        using var cts = new CancellationTokenSource();

        var tcpClientMock = new Mock<ITcpClient>();

        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock
            .Setup(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        await Assert.ThrowsAsync<OperationCanceledException>(() => _service.IsServerAvailableAsync(server, 5, 1000, 5000, cts.Token));

        // Assert
        _loggerMock.VerifyLog(LogLevel.Information, "Operation was canceled by the user.", Times.Once());
        _timeProviderMock.Verify(tp => tp.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task IsServerAvailableAsync_ShouldLogError_OnUnexpectedException()
    {
        // Arrange
        const string server = "127.0.0.1";
        var tcpClientMock = new Mock<ITcpClient>();
        _tcpClientFactoryMock.Setup(f => f.Create()).Returns(tcpClientMock.Object);

        tcpClientMock
            .Setup(c => c.ConnectAsync(server, 5254, It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _service.IsServerAvailableAsync(server, 3, 1000, 5000);

        // Assert
        Assert.False(result);
        _loggerMock.VerifyLog(LogLevel.Error, "An unexpected error occurred while checking server availability.", Times.Once());
        _timeProviderMock.Verify(tp => tp.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    #endregion
}
