// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Tests;

public class ControlHubTests
{
    private readonly Mock<IAppState> _mockAppState;
    private readonly Mock<IScriptService> _mockScriptService;
    private readonly Mock<IInputService> _mockInputService;
    private readonly Mock<IPowerService> _mockPowerService;
    private readonly Mock<IHardwareService> _mockHardwareService;
    private readonly Mock<IShutdownService> _mockShutdownService;
    private readonly Mock<IHostConfigurationService> _mockHostConfigurationService;
    private readonly Mock<IHostLifecycleService> _mockHostLifecycleService;
    private readonly Mock<IHubCallerClients<IControlClient>> _mockClients;
    private readonly Mock<HubCallerContext> _mockHubCallerContext;
    private readonly ControlHub _controlHub;

    public ControlHubTests()
    {
        _mockAppState = new Mock<IAppState>();
        Mock<IViewerFactory> mockViewerFactory = new();
        _mockScriptService = new Mock<IScriptService>();
        _mockInputService = new Mock<IInputService>();
        _mockPowerService = new Mock<IPowerService>();
        _mockHardwareService = new Mock<IHardwareService>();
        _mockShutdownService = new Mock<IShutdownService>();
        Mock<IScreenCapturingService> mockScreenCapturingService = new();
        _mockHostConfigurationService = new Mock<IHostConfigurationService>();
        _mockHostLifecycleService = new Mock<IHostLifecycleService>();
        Mock<IWorkStationSecurityService> mockWorkStationSecurityService = new();
        Mock<IScreenCastingService> mockScreenCastingService = new();
        Mock<IOperatingSystemInformationService> mockOperatingSystemInformationService = new();
        Mock<ILogger<ControlHub>> mockLogger = new();
        _mockClients = new Mock<IHubCallerClients<IControlClient>>();
        Mock<IGroupManager> mockGroups = new();
        Mock<IControlClient> mockClientProxy = new();
        _mockHubCallerContext = new Mock<HubCallerContext>();

        _mockClients.Setup(clients => clients.Caller).Returns(mockClientProxy.Object);

        _controlHub = new ControlHub(
            _mockAppState.Object,
            mockViewerFactory.Object,
            _mockScriptService.Object,
            _mockInputService.Object,
            _mockPowerService.Object,
            _mockHardwareService.Object,
            _mockShutdownService.Object,
            mockScreenCapturingService.Object,
            _mockHostConfigurationService.Object,
            _mockHostLifecycleService.Object,
            mockWorkStationSecurityService.Object,
            mockScreenCastingService.Object,
            mockOperatingSystemInformationService.Object,
            mockLogger.Object)
        {
            Clients = _mockClients.Object,
            Groups = mockGroups.Object,
            Context = _mockHubCallerContext.Object
        };
    }

    private void SetHubContext(string connectionId)
    {
        _mockHubCallerContext.Setup(c => c.ConnectionId).Returns(connectionId);
    }

    private void SetupAppState(string connectionId, IViewer? viewer)
    {
        _mockAppState.Setup(a => a.TryGetViewer(connectionId, out viewer)).Returns(true);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldRemoveViewer_WhenViewerExists()
    {
        // Arrange
        var viewer = new Mock<IViewer>();

        SetHubContext("connectionId");
        SetupAppState("connectionId", viewer.Object);

        _mockAppState.Setup(a => a.TryRemoveViewer("connectionId")).Callback(() => viewer.Object.Dispose()).Returns(true);

        // Act
        await _controlHub.OnDisconnectedAsync(null);

        // Assert
        viewer.Verify(v => v.Dispose(), Times.Once, "Viewer.Dispose was not called once");
        _mockAppState.Verify(a => a.TryRemoveViewer("connectionId"), Times.Once, "TryRemoveViewer was not called once");
        _mockAppState.Verify(a => a.TryRemoveViewer(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void HandleMouseInput_ShouldCallHandleMouseInput()
    {
        // Arrange
        var dto = new MouseInputDto();
        var viewer = new Mock<IViewer>();
        var screenCapturing = new Mock<IScreenCapturingService>().Object;
        viewer.Setup(v => v.ScreenCapturing).Returns(screenCapturing);

        const string connectionId = "testConnectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewer.Object);

        // Act
        _controlHub.HandleMouseInput(dto);

        // Assert
        _mockInputService.Verify(i => i.HandleMouseInput(dto, screenCapturing), Times.Once);
    }

    [Fact]
    public void HandleKeyboardInput_ShouldCallHandleKeyboardInput()
    {
        // Arrange
        var dto = new KeyboardInputDto(It.IsAny<string>(), It.IsAny<bool>());

        // Act
        _controlHub.HandleKeyboardInput(dto);

        // Assert
        _mockInputService.Verify(i => i.HandleKeyboardInput(dto), Times.Once);
    }

    [Fact]
    public void SendSelectedScreen_ShouldSetSelectedScreen_WhenViewerExists()
    {
        // Arrange
        const string displayName = "Display1";
        var viewer = new Mock<IViewer>();
        var screenCapturing = new Mock<IScreenCapturingService>().Object;
        viewer.Setup(v => v.ScreenCapturing).Returns(screenCapturing);

        const string connectionId = "testConnectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewer.Object);

        // Act
        _controlHub.ChangeSelectedScreen(displayName);

        // Assert
        Mock.Get(screenCapturing).Verify(s => s.SetSelectedScreen(displayName), Times.Once);
    }

    [Fact]
    public void SendToggleInput_ShouldToggleInput()
    {
        // Arrange
        const bool inputEnabled = true;

        // Act
        _controlHub.ToggleInput(inputEnabled);

        // Assert
        _mockInputService.VerifySet(i => i.InputEnabled = inputEnabled, Times.Once);
    }

    [Fact]
    public void SendBlockUserInput_ShouldBlockUserInput()
    {
        // Arrange
        const bool blockInput = true;

        // Act
        _controlHub.BlockUserInput(blockInput);

        // Assert
        _mockInputService.VerifySet(i => i.BlockUserInput = blockInput, Times.Once);
    }

    [Fact]
    public void SendImageQuality_ShouldSetImageQuality_WhenViewerExists()
    {
        // Arrange
        const int quality = 80;
        var viewer = new Mock<IViewer>();
        var screenCapturing = new Mock<IScreenCapturingService>().Object;
        viewer.Setup(v => v.ScreenCapturing).Returns(screenCapturing);

        const string connectionId = "testConnectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewer.Object);

        // Act
        _controlHub.SetImageQuality(quality);

        // Assert
        Mock.Get(screenCapturing).VerifySet(s => s.ImageQuality = quality, Times.Once);
    }

    [Fact]
    public void SendToggleCursorTracking_ShouldSetTrackCursor_WhenViewerExists()
    {
        // Arrange
        const bool trackCursor = true;
        var viewer = new Mock<IViewer>();
        var screenCapturing = new Mock<IScreenCapturingService>().Object;
        viewer.Setup(v => v.ScreenCapturing).Returns(screenCapturing);

        const string connectionId = "testConnectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewer.Object);

        // Act
        _controlHub.ToggleDrawCursor(trackCursor);

        // Assert
        Mock.Get(screenCapturing).VerifySet(s => s.DrawCursor = trackCursor, Times.Once);
    }

    [Fact]
    public void SendKillHost_ShouldShutdownHost()
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
    public void SendMonitorState_ShouldSetMonitorState()
    {
        // Arrange
        const MonitorState state = MonitorState.On;

        // Act
        _controlHub.SetMonitorState(state);

        // Assert
        _mockHardwareService.Verify(h => h.SetMonitorState(state), Times.Once);
    }

    [Fact]
    public void SendScript_ShouldExecuteScript()
    {
        // Arrange
        var scriptExecutionRequest = new ScriptExecutionRequest("TestContent", Shell.Cmd);

        // Act
        _controlHub.ExecuteScript(scriptExecutionRequest);

        // Assert
        _mockScriptService.Verify(s => s.Execute(scriptExecutionRequest), Times.Once);
    }

    [Fact]
    public async Task SendCommandToService_ShouldSendCommandToGroup()
    {
        // Arrange
        const string command = "TestCommand";
        const string connectionId = "testConnectionId";

        SetHubContext(connectionId);

        var mockGroupClient = new Mock<IControlClient>();
        _mockClients.Setup(c => c.Group("Services")).Returns(mockGroupClient.Object);

        // Act
        await _controlHub.SendCommandToService(command);

        // Assert
        mockGroupClient.Verify(c => c.ReceiveCommand(command), Times.Once);
    }

    [Fact]
    public async Task Move_ShouldUpdateHostConfigurationAndRenewCertificate()
    {
        // Arrange
        var macAddress = PhysicalAddress.Parse("00:11:22:33:44:55");

        var hostMoveRequest = new HostMoveRequest(macAddress, "NewOrg", ["NewOU"]);

        var subject = new SubjectDto("OldOrg", ["OldOU"]);

        var hostConfiguration = new HostConfiguration(It.IsAny<string>(), subject, It.IsAny<HostDto>());

        var organizationAddress = new AddressDto("TestLocality", "TestState", "US");

        _mockHostConfigurationService.Setup(h => h.LoadConfigurationAsync()).ReturnsAsync(hostConfiguration);
        _mockHostLifecycleService.Setup(h => h.GetOrganizationAddressAsync(It.IsAny<string>())).ReturnsAsync(organizationAddress);

        // Act
        await _controlHub.MoveHost(hostMoveRequest);

        // Assert
        _mockHostConfigurationService.Verify(h => h.SaveConfigurationAsync(It.Is<HostConfiguration>(hc =>
            hc.Subject.Organization == hostMoveRequest.Organization &&
            hc.Subject.OrganizationalUnit.SequenceEqual(hostMoveRequest.OrganizationalUnit)
        )), Times.Once);

        _mockHostLifecycleService.Verify(h => h.IssueCertificateAsync(hostConfiguration, organizationAddress), Times.Once);
    }
}
