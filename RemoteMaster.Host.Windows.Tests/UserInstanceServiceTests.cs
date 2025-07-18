﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class UserInstanceServiceTests
{
    private readonly Mock<IInstanceManagerService> _instanceStarterServiceMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly MockFileSystem _mockFileSystem;
    private readonly UserInstanceService _userInstanceService;

    public UserInstanceServiceTests()
    {
        Mock<ISessionChangeEventService> sessionChangeEventServiceMock = new();
        _instanceStarterServiceMock = new Mock<IInstanceManagerService>();
        _processServiceMock = new Mock<IProcessService>();
        _mockFileSystem = new MockFileSystem();
        Mock<ILogger<UserInstanceService>> loggerMock = new();

        _userInstanceService = new UserInstanceService(sessionChangeEventServiceMock.Object, _instanceStarterServiceMock.Object, _processServiceMock.Object, _mockFileSystem, loggerMock.Object);
    }

    [Fact]
    public async Task Start_ShouldStartNewInstance()
    {
        // Arrange
        const int processId = 1234;

        _instanceStarterServiceMock
            .Setup(x => x.StartNewInstance(
                It.IsAny<string>(),
                It.Is<string>(mode => mode == "user"),
                It.IsAny<string[]>(),
                It.IsAny<ProcessStartInfo>(),
                It.IsAny<INativeProcessOptions>()))
            .Returns(processId);

        // Act
        await _userInstanceService.StartAsync();

        // Assert
        _instanceStarterServiceMock.Verify(
            x => x.StartNewInstance(
                null,
                It.Is<string>(mode => mode == "user"),
                It.IsAny<string[]>(),
                It.IsAny<ProcessStartInfo>(),
                It.IsAny<INativeProcessOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task Stop_ShouldStopRunningInstances()
    {
        // Arrange
        var processMock = new Mock<IProcess>();
        processMock.Setup(p => p.Id).Returns(1234);
        processMock.Setup(p => p.GetCommandLineAsync()).ReturnsAsync(["user"]);
        var processes = new[] { processMock.Object };

        _processServiceMock
            .Setup(x => x.GetProcessesByName(It.IsAny<string>()))
            .Returns(processes);

        // Act
        await _userInstanceService.StopAsync();

        // Assert
        processMock.Verify(p => p.Kill(), Times.Once);
    }

    [Fact]
    public async Task IsRunning_ShouldReturnTrueIfUserInstanceIsRunning()
    {
        // Arrange
        var processMock = new Mock<IProcess>();
        processMock.Setup(p => p.GetCommandLineAsync()).ReturnsAsync(["user"]);
        var processes = new[] { processMock.Object };

        _processServiceMock
            .Setup(x => x.GetProcessesByName(It.IsAny<string>()))
            .Returns(processes);

        // Act
        var isRunning = await _userInstanceService.IsRunningAsync();

        // Assert
        Assert.True(isRunning);
    }

    [Fact]
    public async Task IsRunning_ShouldReturnFalseIfNoUserInstanceIsRunning()
    {
        // Arrange
        _processServiceMock
            .Setup(x => x.GetProcessesByName(It.IsAny<string>()))
            .Returns([]);

        // Act
        var isRunning = await _userInstanceService.IsRunningAsync();

        // Assert
        Assert.False(isRunning);
    }
}
