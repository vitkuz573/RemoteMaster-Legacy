// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.Requirements;

namespace RemoteMaster.Server.Tests;

public class HostAccessHandlerTests
{
    private readonly Mock<IApplicationUnitOfWork> _unitOfWorkMock;
    private readonly HostAccessHandler _handler;

    public HostAccessHandlerTests()
    {
        _unitOfWorkMock = new Mock<IApplicationUnitOfWork>();

        var userRepositoryMock = new Mock<IApplicationUserRepository>();
        var organizationRepositoryMock = new Mock<IOrganizationRepository>();

        _unitOfWorkMock.Setup(uow => uow.ApplicationUsers).Returns(userRepositoryMock.Object);
        _unitOfWorkMock.Setup(uow => uow.Organizations).Returns(organizationRepositoryMock.Object);

        _handler = new HostAccessHandler(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserNotAuthenticated_Fails()
    {
        // Arrange
        var requirement = new HostAccessRequirement("host");
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new AuthorizationHandlerContext([requirement], user, null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_HostNotFound_Fails()
    {
        // Arrange
        var requirement = new HostAccessRequirement("nonexistentHost");
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user1")]));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _unitOfWorkMock.Setup(uow => uow.ApplicationUsers.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(applicationUser);

        _unitOfWorkMock.Setup(uow => uow.Organizations.FindHostsAsync(It.IsAny<Expression<Func<Host, bool>>>()))
            .ReturnsAsync([]);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserHasAccess_Succeeds()
    {
        // Arrange
        var requirement = new HostAccessRequirement("host");
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user1")]));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        var organization = new Organization("Test Organization", new Address("City", "State", new CountryCode("US")));
        organization.AddOrganizationalUnit("Test Unit");

        var organizationalUnit = organization.OrganizationalUnits.First();
        organizationalUnit.AddUser("user1");

        var ipAddress = IPAddress.Loopback;
        var macAddress = PhysicalAddress.Parse("00-14-22-01-23-45");

        organizationalUnit.AddHost("localhost", ipAddress, macAddress);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _unitOfWorkMock.Setup(uow => uow.ApplicationUsers.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _unitOfWorkMock.Setup(uow => uow.Organizations.FindHostsAsync(It.IsAny<Expression<Func<Host, bool>>>()))
            .ReturnsAsync([organizationalUnit.Hosts.First()]);

        _unitOfWorkMock.Setup(uow => uow.Organizations.GetOrganizationalUnitByIdAsync(organizationalUnit.Id))
            .ReturnsAsync(organizationalUnit);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserHasNoAccess_Fails()
    {
        // Arrange
        var requirement = new HostAccessRequirement("host");
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user1")]));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        var organization = new Organization("Test Organization", new Address("City", "State", new CountryCode("US")));
        organization.AddOrganizationalUnit("Test Unit");

        var organizationalUnit = organization.OrganizationalUnits.First();

        organizationalUnit.AddUser("user2");

        var ipAddress = IPAddress.Loopback;
        var macAddress = PhysicalAddress.Parse("00-14-22-01-23-45");

        organizationalUnit.AddHost("localhost", ipAddress, macAddress);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _unitOfWorkMock.Setup(uow => uow.ApplicationUsers.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _unitOfWorkMock.Setup(uow => uow.Organizations.FindHostsAsync(It.IsAny<Expression<Func<Host, bool>>>()))
            .ReturnsAsync([organizationalUnit.Hosts.First()]);

        _unitOfWorkMock.Setup(uow => uow.Organizations.GetOrganizationalUnitByIdAsync(organizationalUnit.Id))
            .ReturnsAsync(organizationalUnit);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_HostInMultipleUnits_UserHasAccessToSpecificHost_Succeeds()
    {
        // Arrange
        var requirement = new HostAccessRequirement("sharedHost");
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user1")]));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        var organization1 = new Organization("Organization 1", new Address("City", "State", new CountryCode("US")));
        var organization2 = new Organization("Organization 2", new Address("City", "State", new CountryCode("US")));

        organization1.AddOrganizationalUnit("Unit 1");
        organization2.AddOrganizationalUnit("Unit 2");

        var organizationalUnit1 = organization1.OrganizationalUnits.First();
        var organizationalUnit2 = organization2.OrganizationalUnits.First();

        organizationalUnit1.AddUser("user1");

        var ipAddress1 = IPAddress.Loopback;
        var macAddress1 = PhysicalAddress.Parse("00-14-22-01-23-45");

        organizationalUnit1.AddHost("sharedHost", ipAddress1, macAddress1);

        organizationalUnit2.AddUser("user2");

        var ipAddress2 = IPAddress.Loopback;
        var macAddress2 = PhysicalAddress.Parse("00-14-22-01-23-45");

        organizationalUnit2.AddHost("sharedHost", ipAddress2, macAddress2);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _unitOfWorkMock.Setup(uow => uow.ApplicationUsers.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _unitOfWorkMock.Setup(uow => uow.Organizations.FindHostsAsync(It.IsAny<Expression<Func<Host, bool>>>()))
            .ReturnsAsync([organizationalUnit1.Hosts.First()]);

        _unitOfWorkMock.Setup(uow => uow.Organizations.GetOrganizationalUnitByIdAsync(organizationalUnit1.Id))
            .ReturnsAsync(organizationalUnit1);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_HostInMultipleUnits_UserHasNoAccessToSpecificHost_Fails()
    {
        // Arrange
        var requirement = new HostAccessRequirement("sharedHost");
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user1")]));
        var context = new AuthorizationHandlerContext([requirement], user, null);

        var organization1 = new Organization("Organization 1", new Address("City", "State", new CountryCode("US")));
        var organization2 = new Organization("Organization 2", new Address("City", "State", new CountryCode("US")));

        organization1.AddOrganizationalUnit("Unit 1");
        organization2.AddOrganizationalUnit("Unit 2");

        var organizationalUnit1 = organization1.OrganizationalUnits.First();
        var organizationalUnit2 = organization2.OrganizationalUnits.First();

        organizationalUnit1.AddUser("user2");

        var ipAddress1 = IPAddress.Loopback;
        var macAddress1 = PhysicalAddress.Parse("00-14-22-01-23-45");

        organizationalUnit1.AddHost("sharedHost", ipAddress1, macAddress1);

        organizationalUnit2.AddUser("user1");

        var ipAddress2 = IPAddress.Parse("127.0.0.2");
        var macAddress2 = PhysicalAddress.Parse("00-14-22-01-23-46");

        organizationalUnit2.AddHost("sharedHost", ipAddress2, macAddress2);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _unitOfWorkMock.Setup(uow => uow.ApplicationUsers.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _unitOfWorkMock.Setup(uow => uow.Organizations.FindHostsAsync(It.IsAny<Expression<Func<Host, bool>>>()))
            .ReturnsAsync([organizationalUnit1.Hosts.First()]);

        _unitOfWorkMock.Setup(uow => uow.Organizations.GetOrganizationalUnitByIdAsync(organizationalUnit2.Id))
            .ReturnsAsync(organizationalUnit1);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
