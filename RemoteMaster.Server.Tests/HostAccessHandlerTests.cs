// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Requirements;
using RemoteMaster.Server.ValueObjects;

namespace RemoteMaster.Server.Tests;

public class HostAccessHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly HostAccessHandler _handler;

    public HostAccessHandlerTests()
    {
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["ConnectionStrings:DefaultConnection"]).Returns("Data Source=:memory:");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        Mock<IServiceScopeFactory> mockScopeFactory = new();
        Mock<IServiceScope> mockScope = new();
        Mock<IServiceProvider> mockServiceProvider = new();

        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext))).Returns(_dbContext);

        _handler = new HostAccessHandler(mockScopeFactory.Object);
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

        var address = new Address("City", "State", "US");
        var organization = new Organization("Test Organization", address);

        var organizationalUnit = new OrganizationalUnit("Test OU", organization);
        organization.AddOrganizationalUnit(organizationalUnit);

        var applicationUser = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1"
        };

        organizationalUnit.AddUser(new UserOrganizationalUnit(organizationalUnit, applicationUser));

        var computer = new Computer("host", "127.0.0.1", "00-14-22-01-23-45", organizationalUnit);
        organizationalUnit.AddComputer(computer);

        _dbContext.Users.Add(applicationUser);
        _dbContext.Computers.Add(computer);
        _dbContext.OrganizationalUnits.Add(organizationalUnit);
        _dbContext.Organizations.Add(organization);

        await _dbContext.SaveChangesAsync();

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

        var address = new Address("City", "State", "US");
        var organization = new Organization("Test Organization", address);

        var organizationalUnit = new OrganizationalUnit("Test OU", organization);
        organization.AddOrganizationalUnit(organizationalUnit);

        var computer = new Computer("host", "127.0.0.1", "00-14-22-01-23-45", organizationalUnit);
        organizationalUnit.AddComputer(computer);

        var applicationUser = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2"
        };

        var userOrganizationalUnit = new UserOrganizationalUnit(organizationalUnit, applicationUser);
        organizationalUnit.AddUser(userOrganizationalUnit);

        _dbContext.Users.Add(applicationUser);
        _dbContext.Computers.Add(computer);
        _dbContext.OrganizationalUnits.Add(organizationalUnit);
        _dbContext.Organizations.Add(organization);

        await _dbContext.SaveChangesAsync();

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

        var address = new Address("City", "State", "US");
        var organization1 = new Organization("Organization 1", address);
        var organization2 = new Organization("Organization 2", address);

        var organizationalUnit1 = new OrganizationalUnit("Unit 1", organization1);
        organization1.AddOrganizationalUnit(organizationalUnit1);

        var organizationalUnit2 = new OrganizationalUnit("Unit 2", organization2);
        organization2.AddOrganizationalUnit(organizationalUnit2);

        var applicationUser = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1"
        };

        var userOrganizationalUnit1 = new UserOrganizationalUnit(organizationalUnit1, applicationUser);
        organizationalUnit1.AddUser(userOrganizationalUnit1);

        var computer1 = new Computer("sharedHost", "127.0.0.1", "00-14-22-01-23-45", organizationalUnit1);
        organizationalUnit1.AddComputer(computer1);

        var computer2 = new Computer("sharedHost", "127.0.0.2", "00-14-22-01-23-46", organizationalUnit2);
        organizationalUnit2.AddComputer(computer2);

        var anotherUser = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2"
        };

        var userOrganizationalUnit2 = new UserOrganizationalUnit(organizationalUnit2, anotherUser);
        organizationalUnit2.AddUser(userOrganizationalUnit2);

        _dbContext.Users.AddRange(applicationUser, anotherUser);
        _dbContext.Computers.AddRange(computer1, computer2);
        _dbContext.OrganizationalUnits.AddRange(organizationalUnit1, organizationalUnit2);
        _dbContext.Organizations.AddRange(organization1, organization2);
        await _dbContext.SaveChangesAsync();

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

        var address = new Address("City", "State", "US");
        var organization1 = new Organization("Organization 1", address);
        var organization2 = new Organization("Organization 2", address);

        var organizationalUnit1 = new OrganizationalUnit("Unit 1", organization1);
        organization1.AddOrganizationalUnit(organizationalUnit1);

        var organizationalUnit2 = new OrganizationalUnit("Unit 2", organization2);
        organization2.AddOrganizationalUnit(organizationalUnit2);

        var applicationUser = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1"
        };

        var anotherUser = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2"
        };

        var userOrganizationalUnit1 = new UserOrganizationalUnit(organizationalUnit1, anotherUser);
        organizationalUnit1.AddUser(userOrganizationalUnit1);

        var userOrganizationalUnit2 = new UserOrganizationalUnit(organizationalUnit2, applicationUser);
        organizationalUnit2.AddUser(userOrganizationalUnit2);

        var computer1 = new Computer("sharedHost", "127.0.0.1", "00-14-22-01-23-45", organizationalUnit1);
        organizationalUnit1.AddComputer(computer1);

        var computer2 = new Computer("sharedHost", "127.0.0.2", "00-14-22-01-23-46", organizationalUnit2);
        organizationalUnit2.AddComputer(computer2);

        _dbContext.Users.AddRange(applicationUser, anotherUser);
        _dbContext.Computers.AddRange(computer1, computer2);
        _dbContext.OrganizationalUnits.AddRange(organizationalUnit1, organizationalUnit2);
        _dbContext.Organizations.AddRange(organization1, organization2);

        await _dbContext.SaveChangesAsync();

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
