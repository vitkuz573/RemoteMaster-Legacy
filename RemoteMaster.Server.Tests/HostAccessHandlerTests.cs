// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Requirements;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class HostAccessHandlerTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly HostAccessHandler _handler;

    public HostAccessHandlerTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext))).Returns(_dbContext);

        _handler = new HostAccessHandler(_mockScopeFactory.Object);
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

        var computer = new Computer
        {
            NodeId = Guid.NewGuid(),
            Name = "host",
            IpAddress = "127.0.0.1",
            MacAddress = "00-14-22-01-23-45",
            ParentId = Guid.NewGuid()
        };

        var organizationalUnit = new OrganizationalUnit
        {
            NodeId = computer.ParentId.Value,
            Name = "Test OU",
            OrganizationId = Guid.NewGuid(),
            AccessibleUsers = [new() { Id = "user1" }]
        };

        _dbContext.Computers.Add(computer);
        _dbContext.OrganizationalUnits.Add(organizationalUnit);
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

        var computer = new Computer
        {
            NodeId = Guid.NewGuid(),
            Name = "host",
            IpAddress = "127.0.0.1",
            MacAddress = "00-14-22-01-23-45",
            ParentId = Guid.NewGuid()
        };

        var organizationalUnit = new OrganizationalUnit
        {
            NodeId = computer.ParentId.Value,
            Name = "Test OU",
            OrganizationId = Guid.NewGuid(),
            AccessibleUsers = [new() { Id = "user2" }]
        };

        _dbContext.Computers.Add(computer);
        _dbContext.OrganizationalUnits.Add(organizationalUnit);
        await _dbContext.SaveChangesAsync();

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}