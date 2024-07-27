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
                if (typeof(T) == typeof(OrganizationalUnit))
                {
                    var ouNode = (OrganizationalUnit)(object)node;
                    
                    if (await applicationDbContext.Set<T>().AnyAsync(n => ((OrganizationalUnit)(object)n!).Name == ouNode.Name && ((OrganizationalUnit)(object)n!).OrganizationId == ouNode.OrganizationId))
                    {
                        var organization = await applicationDbContext.Organizations.FindAsync(ouNode.OrganizationId);
                        
                        return Result<IList<T>>.Failure($"Error: An Organizational Unit with the name '{ouNode.Name}' already exists in the organization '{organization?.Name}'.");
                    }
                }
                else if (typeof(T) == typeof(Computer))
                {
                    var compNode = (Computer)(object)node;
                    
                    if (await applicationDbContext.Set<T>().AnyAsync(n => ((Computer)(object)n!).Name == compNode.Name))
                    {
                        return Result<IList<T>>.Failure($"Error: A Computer with the name '{compNode.Name}' already exists.");
                    }
                }
                else if (typeof(T) == typeof(Organization))
                {
                    var orgNode = (Organization)(object)node;
                    
                    if (await applicationDbContext.Set<T>().AnyAsync(n => ((Organization)(object)n!).Name == orgNode.Name))
                    {
                        return Result<IList<T>>.Failure($"Error: An Organization with the name '{orgNode.Name}' already exists.");
                    }
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

            foreach (var node in nodes)
            {
                if (!await applicationDbContext.Set<T>().AnyAsync(n => n.Id == node.Id))
                {
                    return Result.Failure($"Error: {typeof(T).Name} with the name '{node.Name}' does not exist.");
                }
            }

            applicationDbContext.Set<T>().RemoveRange(nodes);
            await applicationDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error: Failed to remove {typeof(T).Name} nodes.", exception: ex);
        }
    }

    public async Task<Result> UpdateNodeAsync<T>(T node, Func<T, T> updateFunction) where T : class, INode
    {
        try
        {
            ArgumentNullException.ThrowIfNull(node);
            ArgumentNullException.ThrowIfNull(updateFunction);

            var trackedNode = await applicationDbContext.Set<T>().FindAsync(node.Id);
            
            if (trackedNode == null)
            {
                return Result.Failure($"Error: {typeof(T).Name} with the name '{node.Name}' not found.");
            }

            var updatedNode = updateFunction(trackedNode);

            if (typeof(T) == typeof(OrganizationalUnit))
            {
                var ouNode = (OrganizationalUnit)(object)updatedNode;
                
                if (await applicationDbContext.Set<T>().AnyAsync(n => ((OrganizationalUnit)(object)n!).Name == ouNode.Name && ((OrganizationalUnit)(object)n!).OrganizationId == ouNode.OrganizationId && ((OrganizationalUnit)(object)n!).Id != ouNode.Id))
                {
                    var organization = await applicationDbContext.Organizations.FindAsync(ouNode.OrganizationId);
                    
                    return Result.Failure($"Error: An Organizational Unit with the name '{ouNode.Name}' already exists in the organization '{organization?.Name}'.");
                }
            }
            else if (typeof(T) == typeof(Computer))
            {
                var compNode = (Computer)(object)updatedNode;
                
                if (await applicationDbContext.Set<T>().AnyAsync(n => ((Computer)(object)n!).Name == compNode.Name && ((Computer)(object)n!).Id != compNode.Id))
                {
                    return Result.Failure($"Error: A Computer with the name '{compNode.Name}' already exists.");
                }
            }
            else if (typeof(T) == typeof(Organization))
            {
                var orgNode = (Organization)(object)updatedNode;
                
                if (await applicationDbContext.Set<T>().AnyAsync(n => ((Organization)(object)n!).Name == orgNode.Name && ((Organization)(object)n!).Id != orgNode.Id))
                {
                    return Result.Failure($"Error: An Organization with the name '{orgNode.Name}' already exists.");
                }
            }

            applicationDbContext.Entry(trackedNode).CurrentValues.SetValues(updatedNode);

            await applicationDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error: Failed to update {typeof(T).Name} node.", exception: ex);
        }
    }

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

            if (node.Id == newParent.Id)
            {
                throw new InvalidOperationException("Cannot move a node to itself.");
            }

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
