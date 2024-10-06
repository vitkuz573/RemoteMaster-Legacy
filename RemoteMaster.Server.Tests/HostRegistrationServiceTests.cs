// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class HostRegistrationServiceTests
{
    private readonly Mock<IEventNotificationService> _eventNotificationServiceMock;
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly IHostRegistrationService _hostRegistrationService;

    public HostRegistrationServiceTests()
    {
        _eventNotificationServiceMock = new Mock<IEventNotificationService>();
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _hostRegistrationService = new HostRegistrationService(_eventNotificationServiceMock.Object, _organizationRepositoryMock.Object);
    }

    [Fact]
    public async Task RegisterHostAsync_ShouldRegisterNewHost_WhenHostIsNotAlreadyRegistered()
    {
        // Arrange
        var countryCode = new CountryCode("US");
        var address = new Address("New York", "NY", countryCode);
        var organization = new Organization("TestOrg", address);
        organization.AddOrganizationalUnit("OU1");

        var ipAddress = IPAddress.Parse("192.168.0.1");
        var macAddress = PhysicalAddress.Parse("00:11:22:33:44:55");

        var subject = new SubjectDto("TestOrg", ["OU1"]);

        var hostConfig = new HostConfiguration(It.IsAny<string>(), subject, It.IsAny<HostDto>())
        {
            Host = new HostDto("Host1", ipAddress, macAddress),
        };

        _organizationRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<Organization, bool>>>()))
            .ReturnsAsync([organization]);

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(organization.Id))
            .ReturnsAsync(organization);

        // Act
        var result = await _hostRegistrationService.RegisterHostAsync(hostConfig);

        // Assert
        Assert.True(result.IsSuccess);
        _eventNotificationServiceMock.Verify(x => x.SendNotificationAsync(It.IsAny<string>()), Times.Once);
        _organizationRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterHostAsync_ShouldFail_WhenOrganizationIsNotFound()
    {
        // Arrange

        var ipAddress = IPAddress.Parse("192.168.0.1");
        var macAddress = PhysicalAddress.Parse("00:11:22:33:44:55");

        var subject = new SubjectDto("UnknownOrg", ["OU1"]);
        var host = new HostDto("Host1", ipAddress, macAddress);

        var hostConfig = new HostConfiguration(It.IsAny<string>(), subject, host);

        _organizationRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<Organization, bool>>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _hostRegistrationService.RegisterHostAsync(hostConfig);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Organization 'UnknownOrg' not found.", result.Errors.First().Message);
        _eventNotificationServiceMock.Verify(x => x.SendNotificationAsync(It.IsAny<string>()), Times.Once);
    }
}
