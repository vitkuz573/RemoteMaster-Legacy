// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Hubs;

namespace RemoteMaster.Host.Windows.Tests;

public class ServiceHubTests
{
    private readonly Mock<IHubCallerClients<IServiceClient>> _mockClients;
    private readonly Mock<HubCallerContext> _mockHubCallerContext;
    private readonly ServiceHub _serviceHub;

    public ServiceHubTests()
    {
        _mockClients = new Mock<IHubCallerClients<IServiceClient>>();
        _mockHubCallerContext = new Mock<HubCallerContext>();
        Mock<IPsExecService> mockPsExecService = new();
        Mock<ILogger<ServiceHub>> mockLogger = new();
        Mock<IServiceClient> mockClientProxy = new();
        Mock<IGroupManager> mockGroups = new();

        _mockClients.Setup(clients => clients.Caller).Returns(mockClientProxy.Object);

        _serviceHub = new ServiceHub(mockPsExecService.Object, mockLogger.Object)
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

    [Fact]
    public async Task SendCommandToService_ShouldSendCommandToGroup()
    {
        // Arrange
        const string command = "TestCommand";
        const string connectionId = "testConnectionId";

        SetHubContext(connectionId);

        var mockGroupClient = new Mock<IServiceClient>();
        _mockClients.Setup(c => c.Group("Services")).Returns(mockGroupClient.Object);

        // Act
        await _serviceHub.SendCommandToService(command);

        // Assert
        mockGroupClient.Verify(s => s.ReceiveCommand(command), Times.Once);
    }
}
