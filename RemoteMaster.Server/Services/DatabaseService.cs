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
            { } t when t == typeof(Organization) => applicationDbContext.Organizations
                .Include(o => o.OrganizationalUnits)
                .ThenInclude(ou => ou.Computers)
                .Cast<T>(),
            { } t when t == typeof(OrganizationalUnit) => applicationDbContext.OrganizationalUnits
                .Include(ou => ou.Children)
                .Include(ou => ou.Computers)
                .Cast<T>(),
            { } t when t == typeof(Computer) => applicationDbContext.Computers
                .Cast<T>(),
            _ => throw new InvalidOperationException($"Cannot create a DbSet for '{typeof(T).Name}' because this type is not included in the model for the context.")
        };
    }

    /// <inheritdoc />
    public async Task<Result<IList<T>>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode
    {
        try
        {
            var query = GetQueryForType<T>();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var nodes = await query.ToListAsync();

            return Result<IList<T>>.Success(nodes);
        }
        catch (Exception ex)
        {
            return Result<IList<T>>.Failure($"Failed to retrieve nodes of type {typeof(T).Name}.", exception: ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<IList<T>>> AddNodesAsync<T>(IEnumerable<T> nodes) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(nodes);

            var nodesList = nodes.ToList();

            await applicationDbContext.Set<T>().AddRangeAsync(nodesList);
            await applicationDbContext.SaveChangesAsync();

            return Result<IList<T>>.Success(nodesList);
        }
        catch (Exception ex)
        {
            return Result<IList<T>>.Failure($"Failed to add nodes of type {typeof(T).Name}.", exception: ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> RemoveNodesAsync<T>(IEnumerable<T> nodes) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(nodes);

            applicationDbContext.Set<T>().RemoveRange(nodes);
            await applicationDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to remove nodes of type {typeof(T).Name}.", exception: ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateNodeAsync<T>(T node, Func<T, T> updateFunction) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(updateFunction);

            var trackedNode = await applicationDbContext.Set<T>().FindAsync(node.NodeId) ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");

            var updatedNode = updateFunction(trackedNode);

            applicationDbContext.Entry(trackedNode).CurrentValues.SetValues(updatedNode);

            await applicationDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to update node of type {typeof(T).Name}.", exception: ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result> MoveNodeAsync<TNode, TParent>(TNode node, TParent newParent) where TNode : class, INode where TParent : class, INode
    {
        try
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

            if (node.NodeId == newParent.NodeId)
            {
                throw new InvalidOperationException("Cannot move a node to itself.");
            }

            var trackedNode = await applicationDbContext.Set<TNode>().FindAsync(node.NodeId) ?? throw new InvalidOperationException($"{typeof(TNode).Name} not found.");

            var trackedParentExists = await applicationDbContext.Set<TParent>().FindAsync(newParent.NodeId) != null;

            if (!trackedParentExists)
            {
                throw new InvalidOperationException("New parent not found or is invalid.");
            }

            if (trackedNode.ParentId != newParent.NodeId)
            {
                trackedNode.ParentId = newParent.NodeId;
            }

            await applicationDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to move node of type {typeof(TNode).Name}.", exception: ex);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string[]>> GetFullPathAsync<T>(T node) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(node);

            var nodes = await applicationDbContext.Set<T>()
                .AsNoTracking()
                .ToListAsync();

            var path = new List<string>();
            var currentNode = nodes.FirstOrDefault(n => n.NodeId == node.NodeId) ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");

            while (currentNode != null)
            {
                path.Insert(0, currentNode.Name);
                currentNode = nodes.FirstOrDefault(n => n.NodeId == currentNode.ParentId);
            }

            return Result<string[]>.Success([.. path]);
        }
        catch (Exception ex)
        {
            return Result<string[]>.Failure($"Failed to get full path for node of type {typeof(T).Name}.", exception: ex);
        }
    }
}
