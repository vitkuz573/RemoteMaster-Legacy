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
    public async Task<IList<T>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode
    {
        var query = nodesDbContext.Set<T>().AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    public async Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : INode
    {
        if (typeof(T) == typeof(OrganizationalUnit))
        {
            return (await nodesDbContext.OrganizationalUnits.Where(node => node.ParentId == parentId).ToListAsync()).Cast<T>().ToList();
        }

        if (typeof(T) == typeof(Computer))
        {
            return (await nodesDbContext.Computers.Where(node => node.ParentId == parentId).ToListAsync()).Cast<T>().ToList();
        }

        return [];
    }

    public async Task<Guid> AddNodeAsync(INode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node is OrganizationalUnit ou)
        {
            await nodesDbContext.OrganizationalUnits.AddAsync(ou);
        }
        else if (node is Computer computer)
        {
            await nodesDbContext.Computers.AddAsync(computer);
        }
        else
        {
            throw new InvalidOperationException("Unknown node type");
        }

        await nodesDbContext.SaveChangesAsync();

        return node.NodeId;
    }

    public async Task RemoveNodeAsync(INode node)
    {
        if (node is OrganizationalUnit ou)
        {
            nodesDbContext.OrganizationalUnits.Remove(ou);
        }
        else if (node is Computer computer)
        {
            nodesDbContext.Computers.Remove(computer);
        }
        else
        {
            throw new InvalidOperationException("Unknown node type");
        }

        await nodesDbContext.SaveChangesAsync();
    }

    public async Task UpdateComputerAsync(Computer computer, string ipAddress, string hostName)
    {
        ArgumentNullException.ThrowIfNull(computer);

        var trackedComputer = nodesDbContext.Computers.Local.FirstOrDefault(c => c.NodeId == computer.NodeId);

        if (trackedComputer == null)
        {
            nodesDbContext.Computers.Attach(computer);
        }

        nodesDbContext.Entry(computer).Property("IpAddress").CurrentValue = ipAddress;
        nodesDbContext.Entry(computer).Property("Name").CurrentValue = hostName;

        nodesDbContext.Entry(computer).Property("IpAddress").IsModified = true;
        nodesDbContext.Entry(computer).Property("Name").IsModified = true;

        await nodesDbContext.SaveChangesAsync();
    }

    public async Task MoveNodesAsync(IEnumerable<Guid> nodeIds, Guid newParentId)
    {
        var organizationalUnits = await nodesDbContext.OrganizationalUnits.Where(node => nodeIds.Contains(node.NodeId)).ToListAsync();
        var computers = await nodesDbContext.Computers.Where(node => nodeIds.Contains(node.NodeId)).ToListAsync();

        foreach (var node in organizationalUnits.Cast<INode>().Concat(computers))
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
        var currentOu = await nodesDbContext.OrganizationalUnits.FirstOrDefaultAsync(ou => ou.NodeId == ouId);

        while (currentOu != null)
        {
            path.Insert(0, currentOu.Name);

            if (currentOu.ParentId.HasValue)
            {
                currentOu = await nodesDbContext.OrganizationalUnits.FirstOrDefaultAsync(ou => ou.NodeId == currentOu.ParentId.Value);
            }
            else
            {
                break;
            }
        }

        return path.ToArray();
    }

    public async Task<List<Guid>> GetAllowedOrganizationalUnitsForViewerAsync(string userName)
    {
        var userOrganizationalUnits = await (from user in applicationDbContext.Users
                                             join uou in applicationDbContext.UserOrganizationalUnits on user.Id equals uou.UserId
                                             where user.UserName == userName
                                             select uou.OrganizationalUnitId)
                                            .ToListAsync();

        if (userOrganizationalUnits.Count == 0)
        {
            Log.Warning($"User not found or no organizational units: {userName}");

            return [];
        }

        return userOrganizationalUnits;
    }
}
