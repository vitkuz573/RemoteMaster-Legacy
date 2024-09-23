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
    private readonly Mock<IApplicationUserRepository> _userRepositoryMock;
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly HostAccessHandler _handler;

    public HostAccessHandlerTests()
    {
        _userRepositoryMock = new Mock<IApplicationUserRepository>();
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _handler = new HostAccessHandler(_userRepositoryMock.Object, _organizationRepositoryMock.Object);
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

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(applicationUser);

        _organizationRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer>());

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

        organizationalUnit.AddComputer("localhost", ipAddress, macAddress);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _organizationRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer> { organizationalUnit.Computers.First() });

        _organizationRepositoryMock.Setup(repo => repo.GetOrganizationalUnitByIdAsync(organizationalUnit.Id))
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

        organizationalUnit.AddComputer("localhost", ipAddress, macAddress);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _organizationRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer> { organizationalUnit.Computers.First() });

        _organizationRepositoryMock.Setup(repo => repo.GetOrganizationalUnitByIdAsync(organizationalUnit.Id))
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

        organizationalUnit1.AddComputer("sharedHost", ipAddress1, macAddress1);

        organizationalUnit2.AddUser("user2");

        var ipAddress2 = IPAddress.Loopback;
        var macAddress2 = PhysicalAddress.Parse("00-14-22-01-23-45");

        organizationalUnit2.AddComputer("sharedHost", ipAddress2, macAddress2);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _organizationRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer> { organizationalUnit1.Computers.First() });

        _organizationRepositoryMock.Setup(repo => repo.GetOrganizationalUnitByIdAsync(organizationalUnit1.Id))
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
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

        var organization1 = new Organization("Organization 1", new Address("City", "State", new CountryCode("US")));
        var organization2 = new Organization("Organization 2", new Address("City", "State", new CountryCode("US")));

        organization1.AddOrganizationalUnit("Unit 1");
        organization2.AddOrganizationalUnit("Unit 2");

        var organizationalUnit1 = organization1.OrganizationalUnits.First();
        var organizationalUnit2 = organization2.OrganizationalUnits.First();

        organizationalUnit1.AddUser("user2");

        var ipAddress1 = IPAddress.Loopback;
        var macAddress1 = PhysicalAddress.Parse("00-14-22-01-23-45");

        organizationalUnit1.AddComputer("sharedHost", ipAddress1, macAddress1);

        organizationalUnit2.AddUser("user1");

        var ipAddress2 = IPAddress.Parse("127.0.0.2");
        var macAddress2 = PhysicalAddress.Parse("00-14-22-01-23-46");

        organizationalUnit2.AddComputer("sharedHost", ipAddress2, macAddress2);

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _organizationRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer> { organizationalUnit1.Computers.First() });

        _organizationRepositoryMock.Setup(repo => repo.GetOrganizationalUnitByIdAsync(organizationalUnit2.Id))
            .ReturnsAsync(organizationalUnit1);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
