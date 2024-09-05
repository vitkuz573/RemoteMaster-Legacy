// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Requirements;

namespace RemoteMaster.Server.Tests;

public class HostAccessHandlerTests
{
    private readonly Mock<IApplicationUserRepository> _userRepositoryMock;
    private readonly Mock<IOrganizationalUnitRepository> _organizationalUnitRepositoryMock;
    private readonly HostAccessHandler _handler;

    public HostAccessHandlerTests()
    {
        _userRepositoryMock = new Mock<IApplicationUserRepository>();
        _organizationalUnitRepositoryMock = new Mock<IOrganizationalUnitRepository>();
        _handler = new HostAccessHandler(_userRepositoryMock.Object, _organizationalUnitRepositoryMock.Object);
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

        _organizationalUnitRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
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

        var organization = new Organization("Test Organization", new Address("City", "State", "US"));
        var organizationalUnit = new OrganizationalUnit("Test Unit", organization);

        organizationalUnit.AddUser("user1");
        organizationalUnit.AddComputer("host", "127.0.0.1", "00-14-22-01-23-45");

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _organizationalUnitRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer> { new("host", "127.0.0.1", "00-14-22-01-23-45") });

        _organizationalUnitRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
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

        var organization = new Organization("Test Organization", new Address("City", "State", "US"));
        var organizationalUnit = new OrganizationalUnit("Test Unit", organization);

        organizationalUnit.AddUser("user2");
        organizationalUnit.AddComputer("host", "127.0.0.1", "00-14-22-01-23-45");

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _organizationalUnitRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer> { new("host", "127.0.0.1", "00-14-22-01-23-45") });

        _organizationalUnitRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>()))
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

        var organization1 = new Organization("Organization 1", new Address("City", "State", "US"));
        var organization2 = new Organization("Organization 2", new Address("City", "State", "US"));

        var organizationalUnit1 = new OrganizationalUnit("Unit 1", organization1);
        var organizationalUnit2 = new OrganizationalUnit("Unit 2", organization2);

        organizationalUnit1.AddUser("user1");
        organizationalUnit1.AddComputer("sharedHost", "127.0.0.1", "00-14-22-01-23-45");
        organizationalUnit2.AddUser("user2");
        organizationalUnit2.AddComputer("sharedHost", "127.0.0.2", "00-14-22-01-23-46");

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _organizationalUnitRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer> { new("sharedHost", "127.0.0.1", "00-14-22-01-23-45") });

        _organizationalUnitRepositoryMock.Setup(repo => repo.GetByIdAsync(organizationalUnit1.Id))
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

        var organization1 = new Organization("Organization 1", new Address("City", "State", "US"));
        var organization2 = new Organization("Organization 2", new Address("City", "State", "US"));

        var organizationalUnit1 = new OrganizationalUnit("Unit 1", organization1);
        var organizationalUnit2 = new OrganizationalUnit("Unit 2", organization2);

        organizationalUnit1.AddUser("user2");
        organizationalUnit1.AddComputer("sharedHost", "127.0.0.1", "00-14-22-01-23-45");
        organizationalUnit2.AddUser("user1");
        organizationalUnit2.AddComputer("sharedHost", "127.0.0.2", "00-14-22-01-23-46");

        var applicationUser = new ApplicationUser();
        applicationUser.RevokeAccessToUnregisteredHosts();

        _userRepositoryMock.Setup(repo => repo.GetByIdAsync("user1"))
            .ReturnsAsync(applicationUser);

        _organizationalUnitRepositoryMock.Setup(repo => repo.FindComputersAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer> { new("sharedHost", "127.0.0.1", "00-14-22-01-23-45") });

        _organizationalUnitRepositoryMock.Setup(repo => repo.GetByIdAsync(organizationalUnit1.Id))
            .ReturnsAsync(organizationalUnit1);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
