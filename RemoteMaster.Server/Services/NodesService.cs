// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class NodesService(ApplicationDbContext applicationDbContext) : INodesService
{
    private Result<IQueryable<T>> GetQueryForType<T>() where T : class, INode
    {
        try
        {
            var query = typeof(T) switch
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

            return Result<IQueryable<T>>.Success(query);
        }
        catch (Exception ex)
        {
            return Result<IQueryable<T>>.Failure("Error: Failed to create query.", exception: ex);
        }
    }

    private async Task<Result> CheckForConflictsAsync<T>(T node, Guid? nodeId = null) where T : class, INode
    {
        var query = applicationDbContext.Set<T>().AsQueryable();

        Expression<Func<T, bool>> predicate = node switch
        {
            OrganizationalUnit ouNode => n => ((OrganizationalUnit)(object)n).Name == ouNode.Name &&
                                               ((OrganizationalUnit)(object)n).OrganizationId == ouNode.OrganizationId &&
                                               (!nodeId.HasValue || ((OrganizationalUnit)(object)n).Id != nodeId.Value),
            Computer compNode => n => ((Computer)(object)n).MacAddress == compNode.MacAddress &&
                                      (!nodeId.HasValue || ((Computer)(object)n).Id != nodeId.Value),
            Organization orgNode => n => ((Organization)(object)n).Name == orgNode.Name &&
                                         (!nodeId.HasValue || ((Organization)(object)n).Id != nodeId.Value),
            _ => _ => false
        };

        if (!await query.AnyAsync(predicate))
        {
            return Result.Success();
        }

        string conflictMessage;

        switch (node)
        {
            case OrganizationalUnit ouNode:
                var organization = await applicationDbContext.Organizations
                    .FirstOrDefaultAsync(o => o.Id == ouNode.OrganizationId);

                var organizationName = organization?.Name ?? "Unknown Organization";
                conflictMessage = $"Error: An Organizational Unit with the name '{ouNode.Name}' already exists in organization '{organizationName}'.";
                break;

            case Computer compNode:
                conflictMessage = $"Error: A Computer with the MAC address '{compNode.MacAddress}' already exists.";
                break;

            case Organization orgNode:
                conflictMessage = $"Error: An Organization with the name '{orgNode.Name}' already exists.";
                break;

            default:
                throw new InvalidOperationException("Unknown node type.");
        }

        return Result.Failure(conflictMessage);
    }

    private static void ValidateMoveOperation<TNode, TParent>(TNode node, TParent newParent) where TNode : class, INode where TParent : class, INode
    {
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

        if (node.Id == newParent.Id)
        {
            throw new InvalidOperationException("Cannot move a node to itself.");
        }
    }

    public async Task<Result<IList<T>>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null) where T : class, INode
    {
        try
        {
            var queryResult = GetQueryForType<T>();

            if (!queryResult.IsSuccess)
            {
                return Result<IList<T>>.Failure([.. queryResult.Errors]);
            }

            var query = queryResult.Value;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            var nodes = await query.ToListAsync();

            return Result<IList<T>>.Success(nodes);
        }
        catch (Exception ex)
        {
            return Result<IList<T>>.Failure($"Error: Failed to retrieve {typeof(T).Name} nodes.", exception: ex);
        }
    }

    public async Task<Result<IList<T>>> AddNodesAsync<T>(IEnumerable<T> nodes) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(nodes);

            var nodesList = nodes.ToList();

            foreach (var node in nodesList)
            {
                var conflict = await CheckForConflictsAsync(node);
                
                if (!conflict.IsSuccess)
                {
                    return Result<IList<T>>.Failure([.. conflict.Errors]);
                }
            }

            await applicationDbContext.Set<T>().AddRangeAsync(nodesList);
            await applicationDbContext.SaveChangesAsync();

            return Result<IList<T>>.Success(nodesList);
        }
        catch (Exception ex)
        {
            return Result<IList<T>>.Failure($"Error: Failed to add {typeof(T).Name} nodes.", exception: ex);
        }
    }

    public async Task<Result> RemoveNodesAsync<T>(IEnumerable<T> nodes) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(nodes);

            var nodeIds = nodes.Select(n => n.Id).ToList();
            var existingNodes = await applicationDbContext.Set<T>().Where(n => nodeIds.Contains(n.Id)).ToListAsync();

            if (existingNodes.Count != nodeIds.Count)
            {
                return Result.Failure($"Error: Some {typeof(T).Name} nodes do not exist.");
            }

            applicationDbContext.Set<T>().RemoveRange(existingNodes);
            
            await applicationDbContext.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error: Failed to remove the {typeof(T).Name} nodes.", exception: ex);
        }
    }

    public async Task<Result> UpdateNodeAsync<T>(T node, Action<T> updateAction) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(updateAction);

            var trackedNode = await applicationDbContext.Set<T>().FindAsync(node.Id);

            if (trackedNode == null)
            {
                return Result.Failure($"Error: The {typeof(T).Name} with the name '{node.Name}' not found.");
            }

            updateAction(trackedNode);

            var conflict = await CheckForConflictsAsync(trackedNode, node.Id);

            if (!conflict.IsSuccess)
            {
                return Result.Failure([.. conflict.Errors]);
            }

            await applicationDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error: Failed to update the {typeof(T).Name} node.", exception: ex);
        }
    }

    public async Task<Result> MoveNodeAsync<TNode, TParent>(TNode node, TParent newParent) where TNode : class, INode where TParent : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(newParent);

            ValidateMoveOperation(node, newParent);

            var trackedNode = await applicationDbContext.Set<TNode>().FindAsync(node.Id) ?? throw new InvalidOperationException($"{typeof(TNode).Name} not found.");
            var trackedParentExists = await applicationDbContext.Set<TParent>().FindAsync(newParent.Id) != null;
            
            if (!trackedParentExists)
            {
                throw new InvalidOperationException("New parent not found or is invalid.");
            }

            if (trackedNode.ParentId != newParent.Id)
            {
                trackedNode.ParentId = newParent.Id;
            }

            await applicationDbContext.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error: Failed to move {typeof(TNode).Name} node to new parent.", exception: ex);
        }
    }

    public async Task<Result<string[]>> GetFullPathAsync<T>(T node) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(node);

            var nodes = await applicationDbContext.Set<T>()
                .AsNoTracking()
                .ToListAsync();

            var path = new List<string>();
            var currentNode = nodes.FirstOrDefault(n => n.Id == node.Id) ?? throw new InvalidOperationException($"{typeof(T).Name} not found.");

            while (currentNode != null)
            {
                path.Insert(0, currentNode.Name);
                currentNode = nodes.FirstOrDefault(n => n.Id == currentNode.ParentId);
            }

            return Result<string[]>.Success([.. path]);
        }
        catch (Exception ex)
        {
            return Result<string[]>.Failure($"Error: Failed to get full path for {typeof(T).Name} node.", exception: ex);
        }
    }
}
