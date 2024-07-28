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

public class NodesServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public NodesServiceTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        serviceCollection.AddScoped<INodesService, NodesService>();

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
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        context.OrganizationalUnits.RemoveRange(context.OrganizationalUnits);
        var nodes = new List<OrganizationalUnit>
        {
            new() { Id = Guid.NewGuid(), Name = "OU1" },
            new() { Id = Guid.NewGuid(), Name = "OU2" }
        };
        context.OrganizationalUnits.AddRange(nodes);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.GetNodesAsync<OrganizationalUnit>();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task AddNodesAsync_AddsNodes()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var organizationalUnits = new List<OrganizationalUnit>
        {
            new() { Id = Guid.NewGuid(), Name = "OU1" },
            new() { Id = Guid.NewGuid(), Name = "OU2" }
        };

        // Act
        var result = await databaseService.AddNodesAsync(organizationalUnits);

        // Assert
        Assert.True(result.IsSuccess);
        foreach (var addedNode in result.Value)
        {
            var fetchedNode = await context.OrganizationalUnits.FindAsync(addedNode.Id);
            Assert.NotNull(fetchedNode);
            Assert.Equal(addedNode.Name, fetchedNode.Name);
        }
    }

    [Fact]
    public async Task AddNodesAsync_ThrowsInvalidOperationException_ForUnknownNodeType()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var unknownNodes = new List<UnknownNode>
        {
            new() { Id = Guid.NewGuid(), Name = "Unknown1" },
            new() { Id = Guid.NewGuid(), Name = "Unknown2" }
        };

        // Act
        var result = await databaseService.AddNodesAsync(unknownNodes);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Error: Failed to add UnknownNode nodes.", result.Errors.First().Message);
    }

    [Fact]
    public async Task RemoveNodesAsync_RemovesNodes()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var organizationalUnits = new List<OrganizationalUnit>
        {
            new() { Id = Guid.NewGuid(), Name = "OU1" },
            new() { Id = Guid.NewGuid(), Name = "OU2" }
        };
        context.OrganizationalUnits.AddRange(organizationalUnits);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.RemoveNodesAsync(organizationalUnits);

        // Assert
        Assert.True(result.IsSuccess);
        foreach (var organizationalUnit in organizationalUnits)
        {
            var removedNode = await context.OrganizationalUnits.FindAsync(organizationalUnit.Id);
            Assert.Null(removedNode);
        }
    }

    [Fact]
    public async Task RemoveNodesAsync_ThrowsInvalidOperationException_ForUnknownNodeType()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var unknownNodes = new List<UnknownNode>
        {
            new() { Id = Guid.NewGuid(), Name = "Unknown1" },
            new() { Id = Guid.NewGuid(), Name = "Unknown2" }
        };

        // Act
        var result = await databaseService.RemoveNodesAsync(unknownNodes);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Error: Failed to remove the UnknownNode nodes.", result.Errors.First().Message);
    }

    [Fact]
    public async Task UpdateNodeAsync_UpdatesComputer()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var computer = new Computer("OldName", "127.0.0.1", "00:00:00:00:00:00")
        {
            Id = Guid.NewGuid(),
            ParentId = Guid.NewGuid()
        };
        context.Computers.Add(computer);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.UpdateNodeAsync(computer, updatedComputer => updatedComputer.With("NewName", "192.168.0.1"));

        // Assert
        Assert.True(result.IsSuccess);
        var updatedComputer = await context.Computers.FindAsync(computer.Id);
        Assert.NotNull(updatedComputer);
        Assert.Equal("192.168.0.1", updatedComputer.IpAddress);
        Assert.Equal("NewName", updatedComputer.Name);
    }

    [Fact]
    public async Task GetFullPathAsync_ReturnsFullPath()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        context.OrganizationalUnits.RemoveRange(context.OrganizationalUnits);
        var parentOu = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "ParentOU", ParentId = null };
        var childOu = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "ChildOU", ParentId = parentOu.Id };
        context.OrganizationalUnits.AddRange(parentOu, childOu);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.GetFullPathAsync(childOu);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "ParentOU", "ChildOU" }, result.Value);
    }

    [Fact]
    public async Task MoveNodeAsync_MovesNodeToNewParent()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var oldParent = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "OldParent" };
        var newParent = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "NewParent" };
        var childOu = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "ChildOU", ParentId = oldParent.Id };
        context.OrganizationalUnits.AddRange(oldParent, newParent, childOu);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.MoveNodeAsync(childOu, newParent);

        // Assert
        Assert.True(result.IsSuccess);
        var movedNode = await context.OrganizationalUnits.FindAsync(childOu.Id);
        Assert.Equal(newParent.Id, movedNode.ParentId);
    }

    [Fact]
    public async Task GetNodesAsync_WithPredicate_ReturnsFilteredNodes()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        context.OrganizationalUnits.RemoveRange(context.OrganizationalUnits);
        var nodes = new List<OrganizationalUnit>
        {
            new() { Id = Guid.NewGuid(), Name = "OU1" },
            new() { Id = Guid.NewGuid(), Name = "OU2" }
        };
        context.OrganizationalUnits.AddRange(nodes);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.GetNodesAsync<OrganizationalUnit>(ou => ou.Name == "OU1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("OU1", result.Value.First().Name);
    }

    [Fact]
    public async Task MoveNodeAsync_ThrowsException_IfNodeIsOrganization()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Org",
            Country = "Country",
            Locality = "Locality",
            State = "State"
        };
        var parent = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "Parent" };
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Organizations.Add(organization);
        context.OrganizationalUnits.Add(parent);
        await context.SaveChangesAsync();

        // Act & Assert
        var result = await databaseService.MoveNodeAsync(organization, parent);
        Assert.False(result.IsSuccess);
        Assert.Contains("Organizations cannot be moved.", result.Errors.First().Exception.Message);
    }

    [Fact]
    public async Task MoveNodeAsync_ThrowsException_IfNewParentNotFound()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var node = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "Node" };
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.OrganizationalUnits.Add(node);
        await context.SaveChangesAsync();

        var nonExistentParent = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "NonExistentParent" };

        // Act & Assert
        var result = await databaseService.MoveNodeAsync(node, nonExistentParent);
        Assert.False(result.IsSuccess);
        Assert.Contains("New parent not found or is invalid.", result.Errors.First().Exception.Message);
    }

    [Fact]
    public async Task MoveNodeAsync_DoesNotChangeParentId_ForSameNodeAndParent()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var parentNode = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "Parent" };
        var childNode = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "Child", ParentId = parentNode.Id };
        context.OrganizationalUnits.AddRange(parentNode, childNode);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.MoveNodeAsync(childNode, parentNode);

        // Assert
        Assert.True(result.IsSuccess);
        var unchangedNode = await context.OrganizationalUnits.FindAsync(childNode.Id);
        Assert.Equal(parentNode.Id, unchangedNode.ParentId);
    }

    [Fact]
    public async Task UpdateNodeAsync_ThrowsException_IfNodeNotFound()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var nonExistentComputer = new Computer("NonExistent", "127.0.0.1", "00:00:00:00:00:00")
        {
            Id = Guid.NewGuid(),
            ParentId = Guid.NewGuid()
        };

        // Act & Assert
        var result = await databaseService.UpdateNodeAsync(nonExistentComputer, updatedComputer => updatedComputer.With("NewName", "192.168.0.1"));

        Assert.False(result.IsSuccess);
        Assert.Contains("Error: The Computer with the name 'NonExistent' not found.", result.Errors.First().Message);
    }

    [Fact]
    public async Task MoveNodeAsync_ThrowsException_IfMovingNodeToItself()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var node = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "Node" };
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.OrganizationalUnits.Add(node);
        await context.SaveChangesAsync();

        // Act & Assert
        var result = await databaseService.MoveNodeAsync(node, node);
        Assert.False(result.IsSuccess);
        Assert.Contains("Cannot move a node to itself.", result.Errors.First().Exception.Message);
    }

    [Fact]
    public async Task GetFullPathAsync_ThrowsException_IfNodeNotFound()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var nonExistentNode = new OrganizationalUnit { Id = Guid.NewGuid(), Name = "NonExistentNode" };

        // Act & Assert
        var result = await databaseService.GetFullPathAsync(nonExistentNode);
        Assert.False(result.IsSuccess);
        Assert.Contains("Error: Failed to get full path for OrganizationalUnit node.", result.Errors.First().Message);
    }

    [Fact]
    public async Task AddNodesAsync_AddsOrganizations()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var organizations = new List<Organization>
        {
            new() { Id = Guid.NewGuid(), Name = "Org1", Country = "Country1", Locality = "Locality1", State = "State1" },
            new() { Id = Guid.NewGuid(), Name = "Org2", Country = "Country2", Locality = "Locality2", State = "State2" }
        };

        // Act
        var result = await databaseService.AddNodesAsync(organizations);

        // Assert
        Assert.True(result.IsSuccess);
        foreach (var addedNode in result.Value)
        {
            var fetchedNode = await context.Organizations.FindAsync(addedNode.Id);
            Assert.NotNull(fetchedNode);
            Assert.Equal(addedNode.Name, fetchedNode.Name);
        }
    }

    [Fact]
    public async Task AddNodesAsync_AddsComputers()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var computers = new List<Computer>
        {
            new("Comp1", "192.168.0.1", "00:00:00:00:00:01") { Id = Guid.NewGuid(), ParentId = Guid.NewGuid() },
            new("Comp2", "192.168.0.2", "00:00:00:00:00:02") { Id = Guid.NewGuid(), ParentId = Guid.NewGuid() }
        };

        // Act
        var result = await databaseService.AddNodesAsync(computers);

        // Assert
        Assert.True(result.IsSuccess);
        foreach (var addedNode in result.Value)
        {
            var fetchedNode = await context.Computers.FindAsync(addedNode.Id);
            Assert.NotNull(fetchedNode);
            Assert.Equal(addedNode.Name, fetchedNode.Name);
        }
    }

    [Fact]
    public async Task GetNodesAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        using var scope = CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Act
        var result = await databaseService.GetNodesAsync<OrganizationalUnit>();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetNodesAsync_WithLargeDataSet_ReturnsAllNodes()
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var databaseService = scope.ServiceProvider.GetRequiredService<INodesService>();

        // Arrange
        var largeNumberOfNodes = new List<OrganizationalUnit>();
        for (int i = 0; i < 1000; i++)
        {
            largeNumberOfNodes.Add(new OrganizationalUnit { Id = Guid.NewGuid(), Name = $"OU{i}" });
        }
        context.OrganizationalUnits.AddRange(largeNumberOfNodes);
        await context.SaveChangesAsync();

        // Act
        var result = await databaseService.GetNodesAsync<OrganizationalUnit>();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000, result.Value.Count);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private class UnknownNode : INode
    {
        public Guid Id { get; init; }

        public string Name { get; set; }

        public Guid? ParentId { get; set; }

        public INode? Parent { get; set; }
    }
}
