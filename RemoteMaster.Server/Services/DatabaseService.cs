// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class DatabaseService(ApplicationDbContext applicationDbContext) : IDatabaseService
{
    private IQueryable<T> GetQueryForType<T>() where T : class, INode
    {
        return typeof(T) switch
        {
            Type t when t == typeof(OrganizationalUnit) => applicationDbContext.OrganizationalUnits
                .Include(ou => ou.Children)
                .Include(ou => ou.Computers)
                .Cast<T>(),
            Type t when t == typeof(Computer) => applicationDbContext.Computers.Cast<T>(),
            Type t when t == typeof(Organization) => applicationDbContext.Organizations
                .Include(o => o.OrganizationalUnits)
                .ThenInclude(ou => ou.Computers)
                .Cast<T>(),
            _ => throw new InvalidOperationException($"Cannot create a DbSet for '{typeof(T)}' because this type is not included in the model for the context.")
        };
    }

    public async Task<IList<T>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode
    {
        var query = GetQueryForType<T>();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    public async Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : class, INode
    {
        return await GetQueryForType<T>()
            .Where(node => node.ParentId == parentId)
            .ToListAsync();
    }

    public async Task<Guid> AddNodeAsync<T>(T node) where T : class, INode
    {
        ArgumentNullException.ThrowIfNull(node);

        await applicationDbContext.Set<T>().AddAsync(node);
        await applicationDbContext.SaveChangesAsync();

        return node.NodeId;
    }

    public async Task RemoveNodeAsync<T>(T node) where T : class, INode
    {
        ArgumentNullException.ThrowIfNull(node);

        applicationDbContext.Set<T>().Remove(node);

        await applicationDbContext.SaveChangesAsync();
    }

    public async Task UpdateNodeAsync<T>(T node, Action<T> updateAction) where T : class, INode
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(updateAction);

        var trackedNode = await applicationDbContext.Set<T>().FindAsync(node.NodeId) ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");

        updateAction(trackedNode);

        await applicationDbContext.SaveChangesAsync();
    }

    public async Task MoveNodeAsync<T>(Guid nodeId, Guid newParentId) where T : class, INode
    {
        var node = await applicationDbContext.Set<T>().FindAsync(nodeId) ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");

        if (node.NodeId != newParentId)
        {
            node.ParentId = newParentId;
        }

        await applicationDbContext.SaveChangesAsync();
    }

    public async Task<string[]> GetFullPathAsync<T>(Guid nodeId) where T : class, INode
    {
        var nodes = await applicationDbContext.Set<T>()
            .AsNoTracking()
            .ToListAsync();

        var path = new List<string>();
        var currentNode = nodes.FirstOrDefault(node => node.NodeId == nodeId);

        while (currentNode != null)
        {
            path.Insert(0, currentNode.Name);
            currentNode = nodes.FirstOrDefault(node => node.NodeId == currentNode.ParentId);
        }

        return [.. path];
    }
}