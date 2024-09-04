// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Requirements;

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

        organizationalUnit.AddUser(applicationUser.Id);

        organizationalUnit.AddComputer("host", "127.0.0.1", "00-14-22-01-23-45");

        _dbContext.Users.Add(applicationUser);
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

        organizationalUnit.AddComputer("host", "127.0.0.1", "00-14-22-01-23-45");

        var applicationUser = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2"
        };

        organizationalUnit.AddUser(applicationUser.Id);

        _dbContext.Users.Add(applicationUser);
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

        organizationalUnit1.AddUser(applicationUser.Id);

        organizationalUnit1.AddComputer("sharedHost", "127.0.0.1", "00-14-22-01-23-45");
        organizationalUnit2.AddComputer("sharedHost", "127.0.0.2", "00-14-22-01-23-46");

        var anotherUser = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2"
        };

        organizationalUnit2.AddUser(anotherUser.Id);

        _dbContext.Users.AddRange(applicationUser, anotherUser);
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

        organizationalUnit1.AddUser(anotherUser.Id);
        organizationalUnit2.AddUser(applicationUser.Id);

        organizationalUnit1.AddComputer("sharedHost", "127.0.0.1", "00-14-22-01-23-45");
        organizationalUnit2.AddComputer("sharedHost", "127.0.0.2", "00-14-22-01-23-46");

        _dbContext.Users.AddRange(applicationUser, anotherUser);
        _dbContext.OrganizationalUnits.AddRange(organizationalUnit1, organizationalUnit2);
        _dbContext.Organizations.AddRange(organization1, organization2);

        await _dbContext.SaveChangesAsync();

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
