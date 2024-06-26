// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Host.Windows.Services;

namespace RemoteMaster.Host.Windows.Tests;

public class UserInstanceServiceTests
{
    private readonly Mock<ISessionChangeEventService> _sessionChangeEventServiceMock;
    private readonly Mock<IInstanceStarterService> _instanceStarterServiceMock;
    private readonly Mock<IProcessService> _processServiceMock;
    private readonly UserInstanceService _userInstanceService;

    public UserInstanceServiceTests()
    {
        _sessionChangeEventServiceMock = new Mock<ISessionChangeEventService>();
        _instanceStarterServiceMock = new Mock<IInstanceStarterService>();
        _processServiceMock = new Mock<IProcessService>();
        _userInstanceService = new UserInstanceService(_sessionChangeEventServiceMock.Object, _instanceStarterServiceMock.Object, _processServiceMock.Object);
    }

    [Fact]
    public void Start_ShouldStartNewInstance()
    {
        // Arrange
        var processId = 1234;
        _instanceStarterServiceMock
            .Setup(x => x.StartNewInstance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NativeProcessStartInfo>()))
            .Returns(processId);

        // Act
        _userInstanceService.Start();

        // Assert
        _instanceStarterServiceMock.Verify(x => x.StartNewInstance(It.IsAny<string>(), null, It.IsAny<NativeProcessStartInfo>()), Times.Once);
    }

    [Fact]
    public void Stop_ShouldStopRunningInstances()
    {
        // Arrange
        var processMock = new Mock<IProcessWrapper>();
        processMock.Setup(p => p.Id).Returns(1234);
        processMock.Setup(p => p.GetCommandLine()).Returns("host.exe --launch-mode=user");
        var processes = new[] { processMock.Object };

        _processServiceMock
            .Setup(x => x.FindProcessesByName(It.IsAny<string>()))
            .Returns(processes);

        _processServiceMock
            .Setup(x => x.HasProcessArgument(It.IsAny<IProcessWrapper>(), "--launch-mode=user"))
            .Returns(true);

        // Act
        _userInstanceService.Stop();

        // Assert
        foreach (var process in processes)
        {
            processMock.Verify(p => p.Kill(), Times.Once);
        }
    }

    [Fact]
    public void IsRunning_ShouldReturnTrueIfUserInstanceIsRunning()
    {
        // Arrange
        var processMock = new Mock<IProcessWrapper>();
        processMock.Setup(p => p.GetCommandLine()).Returns("--launch-mode=user");
        var processes = new[] { processMock.Object };

        _processServiceMock
            .Setup(x => x.FindProcessesByName(It.IsAny<string>()))
            .Returns(processes);

        _processServiceMock
            .Setup(x => x.HasProcessArgument(It.IsAny<IProcessWrapper>(), "--launch-mode=user"))
            .Returns(true);

        // Act
        var isRunning = _userInstanceService.IsRunning;

        // Assert
        Assert.True(isRunning);
    }

    [Fact]
    public void IsRunning_ShouldReturnFalseIfNoUserInstanceIsRunning()
    {
        // Arrange
        _processServiceMock
            .Setup(x => x.FindProcessesByName(It.IsAny<string>()))
            .Returns([]);

        // Act
        var isRunning = _userInstanceService.IsRunning;

        // Assert
        Assert.False(isRunning);
    }
}
