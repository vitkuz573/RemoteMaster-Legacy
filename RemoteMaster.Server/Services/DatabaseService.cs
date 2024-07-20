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

    /// <summary>
    /// Gets the list of nodes matching the specified predicate.
    /// </summary>
    public async Task<IList<T>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode
    {
        var query = GetQueryForType<T>();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets the list of children nodes for the specified parent ID.
    /// </summary>
    public async Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : class, INode
    {
        return await GetQueryForType<T>()
            .Where(node => node.ParentId == parentId)
            .ToListAsync();
    }

    /// <summary>
    /// Adds a new node to the database.
    /// </summary>
    public async Task<Guid> AddNodeAsync<T>(T node) where T : class, INode
    {
        ArgumentNullException.ThrowIfNull(node);

        await applicationDbContext.Set<T>().AddAsync(node);
        await applicationDbContext.SaveChangesAsync();

        return node.NodeId;
    }

    /// <summary>
    /// Removes the specified node from the database.
    /// </summary>
    public async Task RemoveNodeAsync<T>(T node) where T : class, INode
    {
        ArgumentNullException.ThrowIfNull(node);

        applicationDbContext.Set<T>().Remove(node);

        await applicationDbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Updates the specified node with the given update action.
    /// </summary>
    public async Task UpdateNodeAsync<T>(T node, Action<T> updateAction) where T : class, INode
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(updateAction);

        var trackedNode = await applicationDbContext.Set<T>().FindAsync(node.NodeId) ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");

        updateAction(trackedNode);

        await applicationDbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Moves the specified node to a new parent.
    /// </summary>
    public async Task MoveNodeAsync<TNode, TParent>(TNode node, TParent newParent) where TNode : class, INode where TParent : class, INode
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(newParent);

        if (node is Organization)
        {
            throw new InvalidOperationException("Organizations cannot be moved.");
        }

        if (newParent is Computer)
        {
            throw new InvalidOperationException("Cannot move a node to a Computer as the new parent.");
        }

        if (node is Computer && newParent is not OrganizationalUnit)
        {
            throw new InvalidOperationException("Computers can only be moved to OrganizationalUnits.");
        }

        var trackedNode = await applicationDbContext.Set<TNode>().FindAsync(node.NodeId) ?? throw new InvalidOperationException($"{typeof(TNode).Name} not found.");

        var trackedParentExists = await applicationDbContext.Set<TParent>().FindAsync(newParent.NodeId) != null;
        
        if (!trackedParentExists)
        {
            throw new InvalidOperationException("New parent not found or is invalid.");
        }

        if (trackedNode.NodeId != newParent.NodeId)
        {
            trackedNode.ParentId = newParent.NodeId;
        }

        await applicationDbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the full path for the specified node.
    /// </summary>
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
