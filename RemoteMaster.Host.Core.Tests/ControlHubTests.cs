// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Tests;

public class ControlHubTests
{
    private readonly Mock<IAppState> _mockAppState;
    private readonly Mock<IViewerFactory> _mockViewerFactory;
    private readonly Mock<IScriptService> _mockScriptService;
    private readonly Mock<IInputService> _mockInputService;
    private readonly Mock<IPowerService> _mockPowerService;
    private readonly Mock<IHardwareService> _mockHardwareService;
    private readonly Mock<IShutdownService> _mockShutdownService;
    private readonly Mock<IScreenCapturerService> _mockScreenCapturerService;
    private readonly Mock<IHostConfigurationService> _mockHostConfigurationService;
    private readonly Mock<IHostLifecycleService> _mockHostLifecycleService;
    private readonly Mock<ICertificateStoreService> _mockCertificateStoreService;
    private readonly Mock<IHubCallerClients<IControlClient>> _mockClients;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<IControlClient> _mockClientProxy;
    private readonly Mock<HubCallerContext> _mockHubCallerContext;
    private readonly ControlHub _controlHub;

    public ControlHubTests()
    {
        _mockAppState = new Mock<IAppState>();
        _mockViewerFactory = new Mock<IViewerFactory>();
        _mockScriptService = new Mock<IScriptService>();
        _mockInputService = new Mock<IInputService>();
        _mockPowerService = new Mock<IPowerService>();
        _mockHardwareService = new Mock<IHardwareService>();
        _mockShutdownService = new Mock<IShutdownService>();
        _mockScreenCapturerService = new Mock<IScreenCapturerService>();
        _mockHostConfigurationService = new Mock<IHostConfigurationService>();
        _mockHostLifecycleService = new Mock<IHostLifecycleService>();
        _mockCertificateStoreService = new Mock<ICertificateStoreService>();
        _mockClients = new Mock<IHubCallerClients<IControlClient>>();
        _mockGroups = new Mock<IGroupManager>();
        _mockClientProxy = new Mock<IControlClient>();
        _mockHubCallerContext = new Mock<HubCallerContext>();

        _mockClients.Setup(clients => clients.Caller).Returns(_mockClientProxy.Object);

        _controlHub = new ControlHub(
            _mockAppState.Object,
            _mockViewerFactory.Object,
            _mockScriptService.Object,
            _mockInputService.Object,
            _mockPowerService.Object,
            _mockHardwareService.Object,
            _mockShutdownService.Object,
            _mockScreenCapturerService.Object,
            _mockHostConfigurationService.Object,
            _mockHostLifecycleService.Object,
            _mockCertificateStoreService.Object)
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

    private void SetupAppState(string connectionId, IViewer viewer)
    {
        _mockAppState.Setup(a => a.TryGetViewer(connectionId, out viewer)).Returns(true);
    }

    [Fact]
    public async Task ConnectAs_ShouldReceiveThumbnail_WhenIntentionIsReceiveThumbnail()
    {
        // Arrange
        var connectionRequest = new ConnectionRequest(Intention.ReceiveThumbnail, "TestGroup", "TestUser", "TestRole");
        var thumbnail = new byte[] { 1, 2, 3 };

        _mockScreenCapturerService.Setup(s => s.GetThumbnail(500, 300)).Returns(thumbnail);

        // Act
        await _controlHub.ConnectAs(connectionRequest);

        // Assert
        _mockClientProxy.Verify(c => c.ReceiveThumbnail(thumbnail), Times.Once);
        _mockClientProxy.Verify(c => c.ReceiveCloseConnection(), Times.Once);
    }

    [Fact]
    public async Task ConnectAs_ShouldManageDevice_WhenIntentionIsManageDevice()
    {
        // Arrange
        var connectionRequest = new ConnectionRequest(Intention.ManageDevice, "TestGroup", "TestUser", "TestRole");
        var viewer = new Mock<IViewer>().Object;

        _mockViewerFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(viewer);
        _mockAppState.Setup(a => a.TryAddViewer(viewer)).Returns(true);

        var version = new Version(1, 0, 0);
        var assembly = Assembly.GetExecutingAssembly();
        var mockTransportFeature = new Mock<IHttpTransportFeature>();
        mockTransportFeature.Setup(tf => tf.TransportType).Returns(HttpTransportType.WebSockets);

        var mockHubContext = new Mock<HubCallerContext>();
        mockHubContext.Setup(c => c.Features).Returns(new FeatureCollection());
        mockHubContext.Object.Features.Set(mockTransportFeature.Object);

        _controlHub.Context = mockHubContext.Object;

        // Act
        await _controlHub.ConnectAs(connectionRequest);

        // Assert
        _mockClientProxy.Verify(c => c.ReceiveHostVersion(It.IsAny<Version>()), Times.Once);
        _mockClientProxy.Verify(c => c.ReceiveTransportType(HttpTransportType.WebSockets.ToString()), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_ShouldRemoveViewer_WhenViewerExists()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var viewerMock = new Mock<IViewer>();
        viewerMock.Setup(v => v.CancellationTokenSource).Returns(cancellationTokenSource);
        var viewer = viewerMock.Object;

        SetHubContext("connectionId");
        SetupAppState("connectionId", viewer);

        _mockAppState.Setup(a => a.TryRemoveViewer("connectionId")).Returns(true);

        // Act
        await _controlHub.OnDisconnectedAsync(null);

        // Assert
        Assert.True(cancellationTokenSource.IsCancellationRequested);
        _mockAppState.Verify(a => a.TryRemoveViewer("connectionId"), Times.Once);
    }

    [Fact]
    public void SendMouseInput_ShouldCallSendMouseInput()
    {
        // Arrange
        var dto = new MouseInputDto();
        var viewer = new Mock<IViewer>();
        var screenCapturer = new Mock<IScreenCapturerService>().Object;
        viewer.Setup(v => v.ScreenCapturer).Returns(screenCapturer);

        var connectionId = "testConnectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewer.Object);

        // Act
        _controlHub.SendMouseInput(dto);

        // Assert
        _mockInputService.Verify(i => i.SendMouseInput(dto, screenCapturer), Times.Once);
    }

    [Fact]
    public void SendKeyboardInput_ShouldCallSendKeyboardInput()
    {
        // Arrange
        var dto = new KeyboardInputDto();

        // Act
        _controlHub.SendKeyboardInput(dto);

        // Assert
        _mockInputService.Verify(i => i.SendKeyboardInput(dto), Times.Once);
    }

    [Fact]
    public void SendSelectedScreen_ShouldSetSelectedScreen_WhenViewerExists()
    {
        // Arrange
        var displayName = "Display1";
        var viewer = new Mock<IViewer>();
        var screenCapturer = new Mock<IScreenCapturerService>().Object;
        viewer.Setup(v => v.ScreenCapturer).Returns(screenCapturer);

        var connectionId = "testConnectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewer.Object);

        // Act
        _controlHub.SendSelectedScreen(displayName);

        // Assert
        Mock.Get(screenCapturer).Verify(s => s.SetSelectedScreen(displayName), Times.Once);
    }

    [Fact]
    public void SendToggleInput_ShouldToggleInput()
    {
        // Arrange
        var inputEnabled = true;

        // Act
        _controlHub.SendToggleInput(inputEnabled);

        // Assert
        _mockInputService.VerifySet(i => i.InputEnabled = inputEnabled, Times.Once);
    }

    [Fact]
    public void SendBlockUserInput_ShouldBlockUserInput()
    {
        // Arrange
        var blockInput = true;

        // Act
        _controlHub.SendBlockUserInput(blockInput);

        // Assert
        _mockInputService.VerifySet(i => i.BlockUserInput = blockInput, Times.Once);
    }

    [Fact]
    public void SendImageQuality_ShouldSetImageQuality_WhenViewerExists()
    {
        // Arrange
        var quality = 80;
        var viewer = new Mock<IViewer>();
        var screenCapturer = new Mock<IScreenCapturerService>().Object;
        viewer.Setup(v => v.ScreenCapturer).Returns(screenCapturer);

        var connectionId = "testConnectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewer.Object);

        // Act
        _controlHub.SendImageQuality(quality);

        // Assert
        Mock.Get(screenCapturer).VerifySet(s => s.ImageQuality = quality, Times.Once);
    }

    [Fact]
    public void SendToggleCursorTracking_ShouldSetTrackCursor_WhenViewerExists()
    {
        // Arrange
        var trackCursor = true;
        var viewer = new Mock<IViewer>();
        var screenCapturer = new Mock<IScreenCapturerService>().Object;
        viewer.Setup(v => v.ScreenCapturer).Returns(screenCapturer);

        var connectionId = "testConnectionId";
        SetHubContext(connectionId);
        SetupAppState(connectionId, viewer.Object);

        // Act
        _controlHub.SendToggleCursorTracking(trackCursor);

        // Assert
        Mock.Get(screenCapturer).VerifySet(s => s.TrackCursor = trackCursor, Times.Once);
    }

    [Fact]
    public void SendKillHost_ShouldShutdownHost()
    {
        // Act
        _controlHub.SendKillHost();

        // Assert
        _mockShutdownService.Verify(s => s.ImmediateShutdown(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void SendRebootComputer_ShouldRebootComputer()
    {
        // Arrange
        var powerActionRequest = new PowerActionRequest();

        // Act
        _controlHub.SendRebootComputer(powerActionRequest);

        // Assert
        _mockPowerService.Verify(p => p.Reboot(powerActionRequest), Times.Once);
    }

    [Fact]
    public void SendShutdownComputer_ShouldShutdownComputer()
    {
        // Arrange
        var powerActionRequest = new PowerActionRequest();

        // Act
        _controlHub.SendShutdownComputer(powerActionRequest);

        // Assert
        _mockPowerService.Verify(p => p.Shutdown(powerActionRequest), Times.Once);
    }

    [Fact]
    public void SendMonitorState_ShouldSetMonitorState()
    {
        // Arrange
        var state = MonitorState.On;

        // Act
        _controlHub.SendMonitorState(state);

        // Assert
        _mockHardwareService.Verify(h => h.SetMonitorState(state), Times.Once);
    }

    [Fact]
    public void SendScript_ShouldExecuteScript()
    {
        // Arrange
        var scriptExecutionRequest = new ScriptExecutionRequest("TestContent", Shell.Cmd);

        // Act
        _controlHub.SendScript(scriptExecutionRequest);

        // Assert
        _mockScriptService.Verify(s => s.Execute(scriptExecutionRequest), Times.Once);
    }

    [Fact]
    public async Task JoinGroup_ShouldAddToGroup()
    {
        // Arrange
        var groupName = "TestGroup";
        var viewer = new Mock<IViewer>().Object;

        _mockViewerFactory.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(viewer);
        _mockAppState.Setup(a => a.TryAddViewer(viewer)).Returns(true);

        var connectionId = "connectionId";
        SetHubContext(connectionId);

        // Act
        await _controlHub.JoinGroup(groupName);

        // Assert
        _mockAppState.Verify(a => a.TryAddViewer(viewer), Times.Once);
        _mockGroups.Verify(g => g.AddToGroupAsync(connectionId, groupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveGroup_ShouldRemoveFromGroup()
    {
        // Arrange
        var groupName = "TestGroup";

        var connectionId = "connectionId";
        SetHubContext(connectionId);

        // Act
        await _controlHub.LeaveGroup(groupName);

        // Assert
        _mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, groupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendCommandToService_ShouldSendCommandToGroup()
    {
        // Arrange
        var command = "TestCommand";
        var connectionId = "testConnectionId";

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
        var hostMoveRequest = new HostMoveRequest("00:11:22:33:44:55", "NewOrg", ["NewOU"]);

        var hostConfiguration = new HostConfiguration
        {
            Subject = new SubjectOptions
            {
                Organization = "OldOrg",
                OrganizationalUnit = ["OldOU"]
            }
        };

        _mockHostConfigurationService.Setup(h => h.LoadConfigurationAsync(It.IsAny<bool>())).ReturnsAsync(hostConfiguration);

        // Act
        await _controlHub.Move(hostMoveRequest);

        // Assert
        _mockHostConfigurationService.Verify(h => h.SaveConfigurationAsync(It.Is<HostConfiguration>(hc =>
            hc.Subject.Organization == hostMoveRequest.NewOrganization &&
            hc.Subject.OrganizationalUnit.SequenceEqual(hostMoveRequest.NewOrganizationalUnit)
        )), Times.Once);
        _mockHostLifecycleService.Verify(h => h.RenewCertificateAsync(hostConfiguration), Times.Once);
    }

    [Fact]
    public async Task SendRenewCertificate_ShouldRenewCertificate()
    {
        // Arrange
        var hostConfiguration = new HostConfiguration
        {
            Subject = new SubjectOptions
            {
                Organization = "Org",
                OrganizationalUnit = ["OU"]
            }
        };

        _mockHostConfigurationService.Setup(h => h.LoadConfigurationAsync(It.IsAny<bool>())).ReturnsAsync(hostConfiguration);

        // Act
        await _controlHub.SendRenewCertificate();

        // Assert
        _mockHostLifecycleService.Verify(h => h.RenewCertificateAsync(hostConfiguration), Times.Once);
    }

    [Fact]
    public void GetCertificateSerialNumber_ShouldReturnSerialNumber()
    {
        // Arrange
        var expectedSerialNumber = "123456";
        var mockCertificate = new Mock<ICertificateWrapper>();
        mockCertificate.Setup(c => c.HasPrivateKey).Returns(true);
        mockCertificate.Setup(c => c.GetSerialNumberString()).Returns(expectedSerialNumber);

        var certificates = new List<ICertificateWrapper> { mockCertificate.Object };
        _mockCertificateStoreService.Setup(s => s.GetCertificates(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, It.IsAny<string>())).Returns(certificates);

        // Act
        var result = _controlHub.GetCertificateSerialNumber();

        // Assert
        Assert.Equal(expectedSerialNumber, result);
    }
}