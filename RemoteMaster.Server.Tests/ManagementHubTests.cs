// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class ManagementHubTests
{
    private readonly Mock<ICertificateService> _mockCertificateService;
    private readonly Mock<ICaCertificateService> _mockCaCertificateService;
    private readonly Mock<IDatabaseService> _mockDatabaseService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly ManagementHub _managementHub;

    public ManagementHubTests()
    {
        _mockCertificateService = new Mock<ICertificateService>();
        _mockCaCertificateService = new Mock<ICaCertificateService>();
        _mockDatabaseService = new Mock<IDatabaseService>();
        _mockNotificationService = new Mock<INotificationService>();

        _managementHub = new ManagementHub(
            _mockCertificateService.Object,
            _mockCaCertificateService.Object,
            _mockDatabaseService.Object,
            _mockNotificationService.Object
        );
    }

    [Fact]
    public async Task RegisterHostAsync_ShouldRegisterHostSuccessfully_WhenHostConfigurationIsValid()
    {
        // Arrange
        var hostConfiguration = new HostConfiguration
        {
            Host = new Computer
            {
                Name = "TestHost",
                MacAddress = "00:11:22:33:44:55",
                IpAddress = "192.168.1.1"
            },
            Subject = new SubjectOptions
            {
                Organization = "TestOrganization",
                OrganizationalUnit = ["IT"]
            }
        };

        var organization = new Organization
        {
            NodeId = Guid.NewGuid(),
            Name = "TestOrganization"
        };

        var organizationalUnit = new OrganizationalUnit
        {
            NodeId = Guid.NewGuid(),
            Name = "IT",
            OrganizationId = organization.NodeId
        };

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<Organization, bool>>>()))
            .ReturnsAsync([organization]);

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<OrganizationalUnit, bool>>>()))
            .ReturnsAsync([organizationalUnit]);

        _mockDatabaseService.Setup(s => s.GetChildrenByParentIdAsync<Computer>(It.IsAny<Guid>()))
            .ReturnsAsync([]);

        _mockDatabaseService.Setup(s => s.AddNodeAsync(It.IsAny<Computer>()))
            .ReturnsAsync(Guid.NewGuid());

        var mockClient = new Mock<IManagementClient>();
        var mockClients = new Mock<IHubCallerClients<IManagementClient>>();
        mockClients.Setup(clients => clients.Caller).Returns(mockClient.Object);
        _managementHub.Clients = mockClients.Object;

        // Act
        var result = await _managementHub.RegisterHostAsync(hostConfiguration);

        // Assert
        Assert.True(result);
        _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RegisterHostAsync_ShouldFail_WhenOrganizationNotFound()
    {
        // Arrange
        var hostConfiguration = new HostConfiguration
        {
            Host = new Computer
            {
                Name = "TestHost",
                MacAddress = "00:11:22:33:44:55",
                IpAddress = "192.168.1.1"
            },
            Subject = new SubjectOptions
            {
                Organization = "NonExistentOrganization",
                OrganizationalUnit = ["IT"]
            }
        };

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<Organization, bool>>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _managementHub.RegisterHostAsync(hostConfiguration);

        // Assert
        Assert.False(result);
        _mockNotificationService.Verify(n => n.SendNotificationAsync(It.Is<string>(msg => msg.Contains("Host registration failed"))), Times.Once);
    }

    [Fact]
    public async Task UnregisterHostAsync_ShouldUnregisterHostSuccessfully_WhenHostConfigurationIsValid()
    {
        // Arrange
        var hostConfiguration = new HostConfiguration
        {
            Host = new Computer
            {
                Name = "TestHost",
                MacAddress = "00:11:22:33:44:55",
                IpAddress = "192.168.1.1"
            },
            Subject = new SubjectOptions
            {
                Organization = "TestOrganization",
                OrganizationalUnit = ["IT"]
            }
        };

        var organization = new Organization
        {
            NodeId = Guid.NewGuid(),
            Name = "TestOrganization"
        };

        var organizationalUnit = new OrganizationalUnit
        {
            NodeId = Guid.NewGuid(),
            Name = "IT",
            OrganizationId = organization.NodeId
        };

        var computer = new Computer
        {
            NodeId = Guid.NewGuid(),
            Name = "TestHost",
            IpAddress = "192.168.1.1",
            MacAddress = "00:11:22:33:44:55",
            ParentId = organizationalUnit.NodeId
        };

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<Organization, bool>>>()))
            .ReturnsAsync([organization]);

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<OrganizationalUnit, bool>>>()))
            .ReturnsAsync([organizationalUnit]);

        _mockDatabaseService.Setup(s => s.GetChildrenByParentIdAsync<Computer>(It.IsAny<Guid>()))
            .ReturnsAsync([computer]);

        _mockDatabaseService.Setup(s => s.RemoveNodeAsync(It.IsAny<Computer>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _managementHub.UnregisterHostAsync(hostConfiguration);

        // Assert
        Assert.True(result);
        _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UpdateHostInformationAsync_ShouldUpdateHostSuccessfully_WhenHostConfigurationIsValid()
    {
        // Arrange
        var hostConfiguration = new HostConfiguration
        {
            Host = new Computer
            {
                Name = "UpdatedHost",
                MacAddress = "00:11:22:33:44:55",
                IpAddress = "192.168.1.2"
            },
            Subject = new SubjectOptions
            {
                Organization = "TestOrganization",
                OrganizationalUnit = ["IT"]
            }
        };

        var organization = new Organization
        {
            NodeId = Guid.NewGuid(),
            Name = "TestOrganization"
        };

        var organizationalUnit = new OrganizationalUnit
        {
            NodeId = Guid.NewGuid(),
            Name = "IT",
            OrganizationId = organization.NodeId
        };

        var computer = new Computer
        {
            NodeId = Guid.NewGuid(),
            Name = "TestHost",
            IpAddress = "192.168.1.1",
            MacAddress = "00:11:22:33:44:55",
            ParentId = organizationalUnit.NodeId
        };

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<Organization, bool>>>()))
            .ReturnsAsync([organization]);

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<OrganizationalUnit, bool>>>()))
            .ReturnsAsync([organizationalUnit]);

        _mockDatabaseService.Setup(s => s.GetChildrenByParentIdAsync<Computer>(It.IsAny<Guid>()))
            .ReturnsAsync([computer]);

        _mockDatabaseService.Setup(s => s.UpdateComputerAsync(It.IsAny<Computer>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _managementHub.UpdateHostInformationAsync(hostConfiguration);

        // Assert
        Assert.True(result);
        _mockNotificationService.Verify(n => n.SendNotificationAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task IsHostRegisteredAsync_ShouldReturnTrue_WhenHostIsRegistered()
    {
        // Arrange
        var hostConfiguration = new HostConfiguration
        {
            Host = new Computer
            {
                Name = "TestHost",
                IpAddress = "192.168.1.1",
                MacAddress = "00:11:22:33:44:55"
            }
        };

        var computer = new Computer
        {
            NodeId = Guid.NewGuid(),
            Name = "TestHost",
            IpAddress = "192.168.1.1",
            MacAddress = "00:11:22:33:44:55"
        };

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync([computer]);

        // Act
        var result = await _managementHub.IsHostRegisteredAsync(hostConfiguration);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsHostRegisteredAsync_ShouldReturnFalse_WhenHostIsNotRegistered()
    {
        // Arrange
        var hostConfiguration = new HostConfiguration
        {
            Host = new Computer
            {
                Name = "TestHost",
                IpAddress = "192.168.1.1",
                MacAddress = "00:11:22:33:44:55"
            }
        };

        _mockDatabaseService.Setup(s => s.GetNodesAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _managementHub.IsHostRegisteredAsync(hostConfiguration);

        // Assert
        Assert.False(result);
    }
}
