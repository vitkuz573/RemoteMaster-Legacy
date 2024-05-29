// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class DatabaseService(ApplicationDbContext applicationDbContext, NodesDbContext nodesDbContext) : IDatabaseService
{
    public async Task<IList<Node>> GetNodesAsync(Expression<Func<Node, bool>>? predicate = null)
    {
        var query = nodesDbContext.Nodes.AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.Include(node => node.Nodes).ToListAsync();
    }

    public async Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : Node
    {
        return await nodesDbContext.Nodes.OfType<T>().Where(node => node.ParentId == parentId).ToListAsync();
    }

    public async Task<Guid> AddNodeAsync(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);

        await nodesDbContext.Nodes.AddAsync(node);
        await nodesDbContext.SaveChangesAsync();

        return node.NodeId;
    }

    public async Task RemoveNodeAsync(Node node)
    {
        nodesDbContext.Nodes.Remove(node);
        await nodesDbContext.SaveChangesAsync();
    }

    public async Task UpdateComputerAsync(Computer computer, string ipAddress, string hostName)
    {
        ArgumentNullException.ThrowIfNull(computer);

        var trackedComputer = nodesDbContext.Nodes.Local
            .OfType<Computer>()
            .FirstOrDefault(c => c.NodeId == computer.NodeId);

        if (trackedComputer == null)
        {
            nodesDbContext.Nodes.Attach(computer);
        }

        nodesDbContext.Entry(computer).Property("IpAddress").CurrentValue = ipAddress;
        nodesDbContext.Entry(computer).Property("Name").CurrentValue = hostName;

        nodesDbContext.Entry(computer).Property("IpAddress").IsModified = true;
        nodesDbContext.Entry(computer).Property("Name").IsModified = true;

        await nodesDbContext.SaveChangesAsync();
    }

    public async Task MoveNodesAsync(IEnumerable<Guid> nodeIds, Guid newParentId)
    {
        var nodes = await nodesDbContext.Nodes.Where(node => nodeIds.Contains(node.NodeId)).ToListAsync();

        if (nodes.Count == 0)
        {
            Log.Warning($"No nodes found with the provided IDs: {string.Join(", ", nodeIds)}");
            return;
        }

        foreach (var node in nodes)
        {
            if (node.NodeId == newParentId)
            {
                Log.Error($"Attempting to move a node into itself. Node ID: {node.NodeId}");
                continue;
            }

            Log.Information($"Moving node {node.NodeId} to new parent {newParentId}");

            node.ParentId = newParentId;
        }

        await nodesDbContext.SaveChangesAsync();
    }

    public async Task<string[]> GetFullPathForOrganizationalUnitAsync(Guid ouId)
    {
        var path = new List<string>();
        var currentOu = await nodesDbContext.Nodes.OfType<OrganizationalUnit>().FirstOrDefaultAsync(ou => ou.NodeId == ouId);

        while (currentOu != null)
        {
            path.Insert(0, currentOu.Name);

            if (currentOu.ParentId.HasValue)
            {
                currentOu = await nodesDbContext.Nodes.OfType<OrganizationalUnit>().FirstOrDefaultAsync(ou => ou.NodeId == currentOu.ParentId.Value);
            }
            else
            {
                break;
            }
        }

        return [.. path];
    }

    public async Task<List<Guid>> GetAllowedOrganizationalUnitsForViewerAsync(string userName)
    {
        var userOrganizationalUnits = await (from user in applicationDbContext.Users
                                             join uou in applicationDbContext.UserOrganizationalUnits on user.Id equals uou.UserId
                                             where user.UserName == userName
                                             select uou.OrganizationalUnitId)
                                            .ToListAsync();

        if (!userOrganizationalUnits.Any())
        {
            Log.Warning($"User not found or no organizational units: {userName}");
            return new List<Guid>();
        }

        return userOrganizationalUnits;
    }
}
