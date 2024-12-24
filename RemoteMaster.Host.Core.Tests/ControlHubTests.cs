// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Host.Core.Tests;

public class ControlHubTests : IDisposable
{
    private readonly Mock<IAppState> _mockAppState;
    private readonly Mock<IApplicationVersionProvider> _mockApplicationVersionProvider;
    private readonly Mock<IViewerFactory> _mockViewerFactory;
    private readonly Mock<IScriptService> _mockScriptService;
    private readonly Mock<IInputService> _mockInputService;
    private readonly Mock<IPowerService> _mockPowerService;
    private readonly Mock<IHardwareService> _mockHardwareService;
    private readonly Mock<IShutdownService> _mockShutdownService;
    private readonly Mock<IScreenCapturingService> _mockScreenCapturingService;
    private readonly Mock<IAudioStreamingService> _mockAudioStreamingService;
    private readonly Mock<IWorkStationSecurityService> _mockWorkStationSecurityService;
    private readonly Mock<IScreenCastingService> _mockScreenCastingService;
    private readonly Mock<IOperatingSystemInformationService> _mockOperatingSystemInformationService;
    private readonly Mock<IClipboardService> _mockClipboardService;
    private readonly Mock<ILogger<ControlHub>> _mockLogger;
    private readonly Mock<IHubCallerClients<IControlClient>> _mockClients;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<IControlClient> _mockClientProxy;
    private readonly Mock<HubCallerContext> _mockHubCallerContext;
    private readonly ControlHub _controlHub;

    public ControlHubTests()
    {
        _mockAppState = new Mock<IAppState>();
        _mockApplicationVersionProvider = new Mock<IApplicationVersionProvider>();
        _mockViewerFactory = new Mock<IViewerFactory>();
        _mockScriptService = new Mock<IScriptService>();
        _mockInputService = new Mock<IInputService>();
        _mockPowerService = new Mock<IPowerService>();
        _mockHardwareService = new Mock<IHardwareService>();
        _mockShutdownService = new Mock<IShutdownService>();
        _mockScreenCapturingService = new Mock<IScreenCapturingService>();
        _mockWorkStationSecurityService = new Mock<IWorkStationSecurityService>();
        _mockScreenCastingService = new Mock<IScreenCastingService>();
        _mockAudioStreamingService = new Mock<IAudioStreamingService>();
        _mockOperatingSystemInformationService = new Mock<IOperatingSystemInformationService>();
        _mockClipboardService = new Mock<IClipboardService>();
        _mockLogger = new Mock<ILogger<ControlHub>>();
        _mockClients = new Mock<IHubCallerClients<IControlClient>>();
        _mockGroups = new Mock<IGroupManager>();
        _mockClientProxy = new Mock<IControlClient>();
        _mockHubCallerContext = new Mock<HubCallerContext>();

        _mockClients.Setup(clients => clients.Caller).Returns(_mockClientProxy.Object);

        _controlHub = new ControlHub(
            _mockAppState.Object,
            _mockApplicationVersionProvider.Object,
            _mockViewerFactory.Object,
            _mockScriptService.Object,
            _mockInputService.Object,
            _mockPowerService.Object,
            _mockHardwareService.Object,
            _mockShutdownService.Object,
            _mockScreenCapturingService.Object,
            _mockWorkStationSecurityService.Object,
            _mockScreenCastingService.Object,
            _mockAudioStreamingService.Object,
            _mockOperatingSystemInformationService.Object,
            _mockClipboardService.Object,
            _mockLogger.Object)
        {
            Clients = _mockClients.Object,
            Groups = _mockGroups.Object,
            Context = _mockHubCallerContext.Object
        };
    }

    private void SetHubContext(string connectionId)
    {
        _mockHubCallerContext.Setup(c => c.ConnectionId).Returns(connectionId);
    }

    private void SetupAppState(string connectionId, IViewer? viewer)
    {
        _mockAppState.Setup(a => a.TryGetViewer(connectionId, out viewer)).Returns(viewer != null);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldRemoveViewer_WhenViewerExists()
    {
        // Arrange
        var viewerMock = new Mock<IViewer>();
        const string connectionId = "connectionId";
        viewerMock.Setup(v => v.ConnectionId).Returns(connectionId);

        SetHubContext(connectionId);
        SetupAppState(connectionId, viewerMock.Object);

        _mockAppState.Setup(a => a.TryRemoveViewer(connectionId))
            .Returns(true);

        // Act
        await _controlHub.OnDisconnectedAsync(null);

        // Assert
        _mockAppState.Verify(a => a.TryRemoveViewer(connectionId), Times.Once);
    }

    [Fact]
    public void HandleMouseInput_ShouldCallHandleMouseInput()
    {
        // Arrange
        var dto = new MouseInputDto();
        var viewerMock = new Mock<IViewer>();
        viewerMock.Setup(v => v.ConnectionId).Returns("connectionId");

        const string connectionId = "connectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewerMock.Object);

        // Act
        _controlHub.HandleMouseInput(dto);

        // Assert
        _mockInputService.Verify(i => i.HandleMouseInput(dto, connectionId), Times.Once);
    }

    [Fact]
    public void ChangeSelectedScreen_ShouldSetSelectedScreen_WhenViewerExists()
    {
        // Arrange
        const string displayName = "Display1";
        var screenMock = new Mock<IScreen>();
        screenMock.Setup(s => s.DeviceName).Returns(displayName);

        _mockScreenCapturingService.Setup(s => s.FindScreenByName(displayName)).Returns(screenMock.Object);

        var capturingContextMock = new Mock<ICapturingContext>();
        capturingContextMock.SetupProperty(c => c.SelectedScreen);

        var viewerMock = new Mock<IViewer>();
        viewerMock.Setup(v => v.CapturingContext).Returns(capturingContextMock.Object);
        viewerMock.Setup(v => v.ConnectionId).Returns("connectionId");

        const string connectionId = "connectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewerMock.Object);

        // Act
        _controlHub.ChangeSelectedScreen(displayName);

        // Assert
        Assert.Equal(screenMock.Object, capturingContextMock.Object.SelectedScreen);
        _mockScreenCapturingService.Verify(s => s.FindScreenByName(displayName), Times.Once);
    }

    [Fact]
    public void SetImageQuality_ShouldSetImageQuality_WhenViewerExists()
    {
        // Arrange
        const int quality = 80;

        var capturingContextMock = new Mock<ICapturingContext>();
        capturingContextMock.SetupProperty(c => c.ImageQuality);

        var viewerMock = new Mock<IViewer>();
        viewerMock.Setup(v => v.CapturingContext).Returns(capturingContextMock.Object);
        viewerMock.Setup(v => v.ConnectionId).Returns("connectionId");

        const string connectionId = "connectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewerMock.Object);

        // Act
        _controlHub.SetImageQuality(quality);

        // Assert
        Assert.Equal(quality, capturingContextMock.Object.ImageQuality);
    }

    [Fact]
    public void ToggleDrawCursor_ShouldSetDrawCursor_WhenViewerExists()
    {
        // Arrange
        const bool drawCursor = true;

        var capturingContextMock = new Mock<ICapturingContext>();
        capturingContextMock.SetupProperty(c => c.DrawCursor);

        var viewerMock = new Mock<IViewer>();
        viewerMock.Setup(v => v.CapturingContext).Returns(capturingContextMock.Object);
        viewerMock.Setup(v => v.ConnectionId).Returns("connectionId");

        const string connectionId = "connectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewerMock.Object);

        // Act
        _controlHub.ToggleDrawCursor(drawCursor);

        // Assert
        Assert.Equal(drawCursor, capturingContextMock.Object.DrawCursor);
    }

    [Fact]
    public void HandleKeyboardInput_ShouldCallHandleKeyboardInput()
    {
        // Arrange
        var dto = new KeyboardInputDto(It.IsAny<string>(), It.IsAny<bool>());
        var viewerMock = new Mock<IViewer>();
        viewerMock.Setup(v => v.ConnectionId).Returns("connectionId");

        const string connectionId = "connectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewerMock.Object);

        // Act
        _controlHub.HandleKeyboardInput(dto);

        // Assert
        _mockInputService.Verify(i => i.HandleKeyboardInput(dto, connectionId), Times.Once);
    }

    [Fact]
    public void ToggleInput_ShouldToggleInput()
    {
        // Arrange
        const bool inputEnabled = true;

        // Act
        _controlHub.ToggleInput(inputEnabled);

        // Assert
        _mockInputService.VerifySet(i => i.InputEnabled = inputEnabled, Times.Once);
    }

    [Fact]
    public void BlockUserInput_ShouldBlockUserInput()
    {
        // Arrange
        const bool blockInput = true;

        // Act
        _controlHub.BlockUserInput(blockInput);

        // Assert
        _mockInputService.VerifySet(i => i.BlockUserInput = blockInput, Times.Once);
    }

    [Fact]
    public void TerminateHost_ShouldShutdownHost()
    {
        // Act
        _controlHub.TerminateHost();

        // Assert
        _mockShutdownService.Verify(s => s.ImmediateShutdown(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void SendRebootHost_ShouldRebootHost()
    {
        // Arrange
        var powerActionRequest = new PowerActionRequest();

        // Act
        _controlHub.RebootHost(powerActionRequest);

        // Assert
        _mockPowerService.Verify(p => p.Reboot(powerActionRequest), Times.Once);
    }

    [Fact]
    public void SendShutdownHost_ShouldShutdownHost()
    {
        // Arrange
        var powerActionRequest = new PowerActionRequest();

        // Act
        _controlHub.ShutdownHost(powerActionRequest);

        // Assert
        _mockPowerService.Verify(p => p.Shutdown(powerActionRequest), Times.Once);
    }

    [Fact]
    public void SetMonitorState_ShouldSetMonitorState()
    {
        // Arrange
        const MonitorState state = MonitorState.On;

        // Act
        _controlHub.SetMonitorState(state);

        // Assert
        _mockHardwareService.Verify(h => h.SetMonitorState(state), Times.Once);
    }

    [Fact]
    public void ExecuteScript_ShouldExecuteScript()
    {
        // Arrange
        var scriptExecutionRequest = new ScriptExecutionRequest("echo Hello World", Shell.Cmd);

        // Act
        _controlHub.ExecuteScript(scriptExecutionRequest);

        // Assert
        _mockScriptService.Verify(s => s.Execute(scriptExecutionRequest), Times.Once);
    }

    public void Dispose()
    {
        _controlHub.Dispose();
    }
}
