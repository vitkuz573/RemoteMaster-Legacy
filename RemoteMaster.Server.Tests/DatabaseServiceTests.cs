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
        var addedNode = await databaseService.AddNodeAsync(organizationalUnit);

        // Assert
        var fetchedNode = await context.OrganizationalUnits.FindAsync(addedNode.NodeId);
        Assert.NotNull(fetchedNode);
        Assert.Equal(organizationalUnit.Name, fetchedNode.Name);
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
    public async Task UpdateNodeAsync_UpdatesComputer()
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
        await databaseService.UpdateNodeAsync(computer, updatedComputer =>
        {
            updatedComputer.IpAddress = "192.168.0.1";
            updatedComputer.Name = "NewName";
        });

        // Assert
        var updatedComputer = await context.Computers.FindAsync(computer.NodeId);
        Assert.NotNull(updatedComputer);
        Assert.Equal("192.168.0.1", updatedComputer.IpAddress);
        Assert.Equal("NewName", updatedComputer.Name);
    }

    [Fact]
    public async Task GetFullPathAsync_ReturnsFullPath()
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
        var result = await databaseService.GetFullPathAsync(childOu);

        // Assert
        Assert.Equal(new[] { "ParentOU", "ChildOU" }, result);
    }

    [Fact]
    public async Task MoveNodeAsync_MovesNodeToNewParent()
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
        await databaseService.MoveNodeAsync(childOu, newParent);

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

    [Fact]
    public async Task MoveNodeAsync_ThrowsException_IfNodeIsOrganization()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var organization = new Organization
        {
            NodeId = Guid.NewGuid(),
            Name = "Org",
            Country = "Country",
            Locality = "Locality",
            State = "State"
        };
        var parent = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "Parent" };
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Organizations.Add(organization);
        context.OrganizationalUnits.Add(parent);
        await context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await databaseService.MoveNodeAsync(organization, parent));
    }

    [Fact]
    public async Task MoveNodeAsync_ThrowsException_IfNewParentNotFound()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var node = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "Node" };
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.OrganizationalUnits.Add(node);
        await context.SaveChangesAsync();

        var nonExistentParent = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "NonExistentParent" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await databaseService.MoveNodeAsync(node, nonExistentParent));
    }

    [Fact]
    public async Task MoveNodeAsync_DoesNotChangeParentId_ForSameNodeAndParent()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var parentNode = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "Parent" };
        var childNode = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "Child", ParentId = parentNode.NodeId };
        context.OrganizationalUnits.AddRange(parentNode, childNode);
        await context.SaveChangesAsync();

        // Act
        await databaseService.MoveNodeAsync(childNode, parentNode);

        // Assert
        var unchangedNode = await context.OrganizationalUnits.FindAsync(childNode.NodeId);
        Assert.Equal(parentNode.NodeId, unchangedNode.ParentId);
    }

    [Fact]
    public async Task UpdateNodeAsync_ThrowsException_IfNodeNotFound()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();

        // Arrange
        var nonExistentComputer = new Computer
        {
            NodeId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            Name = "NonExistent",
            MacAddress = "00:00:00:00:00:00",
            ParentId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await databaseService.UpdateNodeAsync(nonExistentComputer, updatedComputer =>
            {
                updatedComputer.IpAddress = "192.168.0.1";
                updatedComputer.Name = "NewName";
            }));
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

