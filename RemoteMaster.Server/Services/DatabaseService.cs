// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class DatabaseService(ApplicationDbContext applicationDbContext) : IDatabaseService
{
    public async Task<IList<T>> GetNodesAsync<T>(Expression<Func<T, bool>>? predicate = null, List<Guid>? accessibleIds = null) where T : class, INode
    {
        IQueryable<T> query;

        if (typeof(T) == typeof(OrganizationalUnit))
        {
            query = applicationDbContext.OrganizationalUnits
                                        .Include(ou => ou.Children)
                                        .Include(ou => ou.Computers)
                                        .AsQueryable().Cast<T>();
        }
        else if (typeof(T) == typeof(Computer))
        {
            query = applicationDbContext.Computers.AsQueryable().Cast<T>();
        }
        else if (typeof(T) == typeof(Organization))
        {
            query = applicationDbContext.Organizations
                                        .Include(o => o.OrganizationalUnits)
                                        .ThenInclude(ou => ou.Computers)
                                        .AsQueryable().Cast<T>();
        }
        else
        {
            throw new InvalidOperationException($"Cannot create a DbSet for '{typeof(T)}' because this type is not included in the model for the context.");
        }

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (accessibleIds != null && accessibleIds.Count > 0)
        {
            if (typeof(T) == typeof(Organization))
            {
                query = query.Cast<Organization>()
                             .Where(node => accessibleIds.Contains(node.OrganizationId))
                             .Cast<T>();
            }
            else
            {
                query = query.Where(node => accessibleIds.Contains(node.NodeId));
            }
        }

        return await query.ToListAsync();
    }

    public async Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : INode
    {
        if (typeof(T) == typeof(OrganizationalUnit))
        {
            return (await applicationDbContext.OrganizationalUnits
                .Where(node => node.ParentId == parentId)
                .Include(ou => ou.Children)
                .Include(ou => ou.Computers)
                .ToListAsync()).Cast<T>().ToList();
        }

        if (typeof(T) == typeof(Computer))
        {
            return (await applicationDbContext.Computers
                .Where(node => node.ParentId == parentId)
                .ToListAsync()).Cast<T>().ToList();
        }

        throw new InvalidOperationException($"Cannot get children for '{typeof(T)}' because this type is not included in the model for the context.");
    }

    public async Task<Guid> AddNodeAsync(INode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node is OrganizationalUnit ou)
        {
            await applicationDbContext.OrganizationalUnits.AddAsync(ou);
        }
        else if (node is Computer computer)
        {
            await applicationDbContext.Computers.AddAsync(computer);
        }
        else
        {
            throw new InvalidOperationException("Unknown node type");
        }

        await applicationDbContext.SaveChangesAsync();

        return node.NodeId;
    }

    public async Task RemoveNodeAsync(INode node)
    {
        if (node is OrganizationalUnit ou)
        {
            applicationDbContext.OrganizationalUnits.Remove(ou);
        }
        else if (node is Computer computer)
        {
            applicationDbContext.Computers.Remove(computer);
        }
        else
        {
            throw new InvalidOperationException("Unknown node type");
        }

        await applicationDbContext.SaveChangesAsync();
    }

    public async Task UpdateComputerAsync(Computer computer, string ipAddress, string hostName)
    {
        ArgumentNullException.ThrowIfNull(computer);

        var trackedComputer = applicationDbContext.Computers.Local.FirstOrDefault(c => c.NodeId == computer.NodeId);

        if (trackedComputer == null)
        {
            applicationDbContext.Computers.Attach(computer);
        }

        applicationDbContext.Entry(computer).Property("IpAddress").CurrentValue = ipAddress;
        applicationDbContext.Entry(computer).Property("Name").CurrentValue = hostName;

        applicationDbContext.Entry(computer).Property("IpAddress").IsModified = true;
        applicationDbContext.Entry(computer).Property("Name").IsModified = true;

        await applicationDbContext.SaveChangesAsync();
    }

    public async Task MoveNodesAsync(IEnumerable<Guid> nodeIds, Guid newParentId)
    {
        var organizationalUnits = await applicationDbContext.OrganizationalUnits.Where(node => nodeIds.Contains(node.NodeId)).ToListAsync();
        var computers = await applicationDbContext.Computers.Where(node => nodeIds.Contains(node.NodeId)).ToListAsync();

        foreach (var node in organizationalUnits.Cast<INode>().Concat(computers))
        {
            if (node.NodeId == newParentId)
            {
                continue;
            }

            node.ParentId = newParentId;
        }

        await applicationDbContext.SaveChangesAsync();
    }

    public async Task<string[]> GetFullPathForOrganizationalUnitAsync(Guid ouId)
    {
        var path = new List<string>();
        var currentOu = await applicationDbContext.OrganizationalUnits.FirstOrDefaultAsync(ou => ou.NodeId == ouId);

        while (currentOu != null)
        {
            path.Insert(0, currentOu.Name);

            if (currentOu.ParentId.HasValue)
            {
                currentOu = await applicationDbContext.OrganizationalUnits.FirstOrDefaultAsync(ou => ou.NodeId == currentOu.ParentId.Value);
            }
            else
            {
                break;
            }
        }

        return [.. path];
    }
}
