// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class DatabaseServiceTests : IDisposable
{
    private readonly DatabaseService _databaseService;
    private readonly ApplicationDbContext _context;

    public DatabaseServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new ApplicationDbContext(options);
        _databaseService = new DatabaseService(_context);
    }

    [Fact]
    public async Task GetNodesAsync_ReturnsNodes()
    {
        // Arrange
        _context.OrganizationalUnits.RemoveRange(_context.OrganizationalUnits);
        var nodes = new List<OrganizationalUnit>
        {
            new() { NodeId = Guid.NewGuid(), Name = "OU1" },
            new() { NodeId = Guid.NewGuid(), Name = "OU2" }
        };
        _context.OrganizationalUnits.AddRange(nodes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _databaseService.GetNodesAsync<OrganizationalUnit>();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddNodeAsync_AddsNode()
    {
        // Arrange
        var organizationalUnit = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "OU" };

        // Act
        var nodeId = await _databaseService.AddNodeAsync(organizationalUnit);

        // Assert
        var addedNode = await _context.OrganizationalUnits.FindAsync(nodeId);
        Assert.NotNull(addedNode);
        Assert.Equal(organizationalUnit.Name, addedNode.Name);
    }

    [Fact]
    public async Task RemoveNodeAsync_RemovesNode()
    {
        // Arrange
        var organizationalUnit = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "OU" };
        _context.OrganizationalUnits.Add(organizationalUnit);
        await _context.SaveChangesAsync();

        // Act
        await _databaseService.RemoveNodeAsync(organizationalUnit);

        // Assert
        var removedNode = await _context.OrganizationalUnits.FindAsync(organizationalUnit.NodeId);
        Assert.Null(removedNode);
    }

    [Fact]
    public async Task UpdateComputerAsync_UpdatesComputer()
    {
        // Arrange
        var computer = new Computer
        {
            NodeId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            Name = "OldName",
            MacAddress = "00:00:00:00:00:00",
            ParentId = Guid.NewGuid()
        };
        _context.Computers.Add(computer);
        await _context.SaveChangesAsync();

        // Act
        await _databaseService.UpdateComputerAsync(computer, "192.168.0.1", "NewName");

        // Assert
        var updatedComputer = await _context.Computers.FindAsync(computer.NodeId);
        Assert.NotNull(updatedComputer);
        Assert.Equal("192.168.0.1", updatedComputer.IpAddress);
        Assert.Equal("NewName", updatedComputer.Name);
    }

    [Fact]
    public async Task GetFullPathForOrganizationalUnitAsync_ReturnsFullPath()
    {
        // Arrange
        _context.OrganizationalUnits.RemoveRange(_context.OrganizationalUnits);
        var parentOu = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "ParentOU", ParentId = null };
        var childOu = new OrganizationalUnit { NodeId = Guid.NewGuid(), Name = "ChildOU", ParentId = parentOu.NodeId };
        _context.OrganizationalUnits.AddRange(parentOu, childOu);
        await _context.SaveChangesAsync();

        // Act
        var result = await _databaseService.GetFullPathForOrganizationalUnitAsync(childOu.NodeId);

        // Assert
        Assert.Equal(new[] { "ParentOU", "ChildOU" }, result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
