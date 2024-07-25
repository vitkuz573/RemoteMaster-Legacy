// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Hubs;
using RemoteMaster.Host.Windows.Services;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Tests;

public class PsExecServiceTests
{
    private readonly Mock<IHostConfigurationService> _mockHostConfigurationService;
    private readonly Mock<ICommandExecutor> _mockCommandExecutor;
    private readonly Mock<IFirewallService> _mockFirewallService;

    public PsExecServiceTests()
    {
        _mockHostConfigurationService = new Mock<IHostConfigurationService>();
        Mock<IHubContext<ServiceHub, IServiceClient>> mockHubContext = new();
        Mock<IServiceClient> mockServiceClient = new();
        _mockCommandExecutor = new Mock<ICommandExecutor>();
        _mockFirewallService = new Mock<IFirewallService>();

        mockHubContext.Setup(h => h.Clients.All).Returns(mockServiceClient.Object);
    }

    [Fact]
    public async Task EnableAsync_ShouldExecuteCommands()
    {
        // Arrange
        var service = new PsExecService(_mockHostConfigurationService.Object, _mockCommandExecutor.Object, _mockFirewallService.Object);
        _mockHostConfigurationService.Setup(s => s.LoadConfigurationAsync(It.IsAny<bool>())).ReturnsAsync(new HostConfiguration { Server = "192.168.1.1" });

        // Act
        await service.EnableAsync();

        // Assert
        _mockHostConfigurationService.Verify(s => s.LoadConfigurationAsync(It.IsAny<bool>()), Times.Once);
        _mockCommandExecutor.Verify(e => e.ExecuteCommandAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Disable_ShouldExecuteCommands()
    {
        // Arrange
        var service = new PsExecService(_mockHostConfigurationService.Object, _mockCommandExecutor.Object, _mockFirewallService.Object);

        // Act
        service.Disable();

        // Assert
        _mockFirewallService.Verify(e => e.RemoveRule(It.IsAny<string>()), Times.Once);
        _mockFirewallService.Verify(e => e.DisableRuleGroup(It.IsAny<string>()), Times.Once);
    }
}
