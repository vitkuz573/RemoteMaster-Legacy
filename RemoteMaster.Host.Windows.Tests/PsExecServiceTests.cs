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
    private readonly Mock<IHubContext<ServiceHub, IServiceClient>> _mockHubContext;
    private readonly Mock<IServiceClient> _mockServiceClient;
    private readonly Mock<ICommandExecutor> _mockCommandExecutor;

    public PsExecServiceTests()
    {
        _mockHostConfigurationService = new Mock<IHostConfigurationService>();
        _mockHubContext = new Mock<IHubContext<ServiceHub, IServiceClient>>();
        _mockServiceClient = new Mock<IServiceClient>();
        _mockCommandExecutor = new Mock<ICommandExecutor>();

        _mockHubContext.Setup(h => h.Clients.All).Returns(_mockServiceClient.Object);
    }

    [Fact]
    public async Task EnableAsync_ShouldExecuteCommands()
    {
        // Arrange
        var service = new PsExecService(_mockHostConfigurationService.Object, _mockCommandExecutor.Object);
        _mockHostConfigurationService.Setup(s => s.LoadConfigurationAsync(It.IsAny<bool>())).ReturnsAsync(new HostConfiguration { Server = "192.168.1.1" });

        // Act
        await service.EnableAsync();

        // Assert
        _mockHostConfigurationService.Verify(s => s.LoadConfigurationAsync(It.IsAny<bool>()), Times.Once);
        _mockCommandExecutor.Verify(e => e.ExecuteCommandAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DisableAsync_ShouldExecuteCommands()
    {
        // Arrange
        var service = new PsExecService(_mockHostConfigurationService.Object, _mockCommandExecutor.Object);

        // Act
        await service.DisableAsync();

        // Assert
        _mockCommandExecutor.Verify(e => e.ExecuteCommandAsync(It.IsAny<string>()), Times.AtLeastOnce);
    }
}