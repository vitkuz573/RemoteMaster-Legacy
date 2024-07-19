// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class DatabaseServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public DatabaseServiceTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        serviceCollection.AddScoped<IDatabaseService, DatabaseService>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    private IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }

    [Fact]
    public async Task GetNodesAsync_ReturnsNodes()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        context.OrganizationalUnits.RemoveRange(context.OrganizationalUnits);
        var nodes = new List<OrganizationalUnit>
        {
            new() { NodeId = Guid.NewGuid(), Name = "OU1" },
            new() { NodeId = Guid.NewGuid(), Name = "OU2" }
        };
        context.OrganizationalUnits.AddRange(nodes);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.GetNodesAsync<OrganizationalUnit>();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddNodeAsync_AddsNode()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var organizationalUnit = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "OU" };

        // Act
        var nodeId = await databaseService.AddNodeAsync(organizationalUnit);

        // Assert
        var addedNode = await context.OrganizationalUnits.FindAsync(nodeId);
        Assert.NotNull(addedNode);
        Assert.Equal(organizationalUnit.Name, addedNode.Name);
    }

    [Fact]
    public async Task AddNodeAsync_ThrowsInvalidOperationException_ForUnknownNodeType()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var unknownNode = new UnknownNode { NodeId = Guid.NewGuid(), Name = "Unknown" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => databaseService.AddNodeAsync(unknownNode));
    }

    [Fact]
    public async Task RemoveNodeAsync_RemovesNode()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var organizationalUnit = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "OU" };
        context.OrganizationalUnits.Add(organizationalUnit);
        await context.SaveChangesAsync();

        // Act
        await databaseService.RemoveNodeAsync(organizationalUnit);

        // Assert
        var removedNode = await context.OrganizationalUnits.FindAsync(organizationalUnit.NodeId);
        Assert.Null(removedNode);
    }

    [Fact]
    public async Task RemoveNodeAsync_ThrowsInvalidOperationException_ForUnknownNodeType()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var unknownNode = new UnknownNode { NodeId = Guid.NewGuid(), Name = "Unknown" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => databaseService.RemoveNodeAsync(unknownNode));
    }

    [Fact]
    public async Task UpdateComputerAsync_UpdatesComputer()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var computer = new Computer
        {
            NodeId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            Name = "OldName",
            MacAddress = "00:00:00:00:00:00",
            ParentId = Guid.NewGuid()
        };
        context.Computers.Add(computer);
        await context.SaveChangesAsync();

        // Act
        await databaseService.UpdateComputerAsync(computer, "192.168.0.1", "NewName");

        // Assert
        var updatedComputer = await context.Computers.FindAsync(computer.NodeId);
        Assert.NotNull(updatedComputer);
        Assert.Equal("192.168.0.1", updatedComputer.IpAddress);
        Assert.Equal("NewName", updatedComputer.Name);
    }

    [Fact]
    public async Task GetFullPathForOrganizationalUnitAsync_ReturnsFullPath()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        context.OrganizationalUnits.RemoveRange(context.OrganizationalUnits);
        var parentOu = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "ParentOU", ParentId = null };
        var childOu = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "ChildOU", ParentId = parentOu.NodeId };
        context.OrganizationalUnits.AddRange(parentOu, childOu);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.GetFullPathForOrganizationalUnitAsync(childOu.NodeId);

        // Assert
        Assert.Equal(new[] { "ParentOU", "ChildOU" }, result);
    }

    [Fact]
    public async Task GetChildrenByParentIdAsync_ReturnsChildren()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var parentOu = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "ParentOU" };
        var childOu = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "ChildOU", ParentId = parentOu.NodeId };
        context.OrganizationalUnits.AddRange(parentOu, childOu);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.GetChildrenByParentIdAsync<OrganizationalUnit>(parentOu.NodeId);

        // Assert
        Assert.Single(result);
        Assert.Equal(childOu.NodeId, result[0].NodeId);
    }

    [Fact]
    public async Task GetChildrenByParentIdAsync_ThrowsInvalidOperationException_ForUnknownNodeType()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var parentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => databaseService.GetChildrenByParentIdAsync<UnknownNode>(parentId));
    }

    [Fact]
    public async Task MoveNodesAsync_MovesNodesToNewParent()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var oldParent = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "OldParent" };
        var newParent = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "NewParent" };
        var childOu = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "ChildOU", ParentId = oldParent.NodeId };
        context.OrganizationalUnits.AddRange(oldParent, newParent, childOu);
        await context.SaveChangesAsync();

        // Act
        await databaseService.MoveNodesAsync([childOu.NodeId], newParent.NodeId);

        // Assert
        var movedNode = await context.OrganizationalUnits.FindAsync(childOu.NodeId);
        Assert.Equal(newParent.NodeId, movedNode.ParentId);
    }

    [Fact]
    public async Task GetNodesAsync_WithPredicate_ReturnsFilteredNodes()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        context.OrganizationalUnits.RemoveRange(context.OrganizationalUnits);
        var nodes = new List<OrganizationalUnit>
        {
            new() { NodeId = Guid.NewGuid(), Name = "OU1" },
            new() { NodeId = Guid.NewGuid(), Name = "OU2" }
        };
        context.OrganizationalUnits.AddRange(nodes);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.GetNodesAsync<OrganizationalUnit>(ou => ou.Name == "OU1");

        // Assert
        Assert.Single(result);
        Assert.Equal("OU1", result[0].Name);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private class UnknownNode : INode
    {
        public Guid NodeId { get; init; }

        public string Name { get; set; }

        public Guid? ParentId { get; set; }

        public INode? Parent { get; set; }
    }
}
