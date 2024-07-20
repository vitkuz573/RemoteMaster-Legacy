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
        if (typeof(T) == typeof(OrganizationalUnit))
        {
            return applicationDbContext.OrganizationalUnits
                                        .Include(ou => ou.Children)
                                        .Include(ou => ou.Computers)
                                        .Cast<T>();
        }
        if (typeof(T) == typeof(Computer))
        {
            return applicationDbContext.Computers.Cast<T>();
        }
        if (typeof(T) == typeof(Organization))
        {
            return applicationDbContext.Organizations
                                        .Include(o => o.OrganizationalUnits)
                                        .ThenInclude(ou => ou.Computers)
                                        .Cast<T>();
        }

        throw new InvalidOperationException($"Cannot create a DbSet for '{typeof(T)}' because this type is not included in the model for the context.");
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
        var query = GetQueryForType<T>().Where(node => node.ParentId == parentId);

        return await query.ToListAsync();
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

    public async Task UpdateComputerAsync(Computer computer, string ipAddress, string hostName)
    {
        ArgumentNullException.ThrowIfNull(computer);

        var trackedComputer = await applicationDbContext.Computers.FindAsync(computer.NodeId) ?? throw new InvalidOperationException("Computer not found.");
        
        trackedComputer.IpAddress = ipAddress;
        trackedComputer.Name = hostName;

        await applicationDbContext.SaveChangesAsync();
    }

    public async Task MoveNodesAsync(IEnumerable<Guid> nodeIds, Guid newParentId)
    {
        var organizationalUnits = await applicationDbContext.OrganizationalUnits
            .Where(ou => nodeIds.Contains(ou.NodeId))
            .ToListAsync();

        var computers = await applicationDbContext.Computers
            .Where(c => nodeIds.Contains(c.NodeId))
            .ToListAsync();

        foreach (var node in organizationalUnits.Cast<INode>().Concat(computers))
        {
            if (node.NodeId != newParentId)
            {
                node.ParentId = newParentId;
            }
        }

        await applicationDbContext.SaveChangesAsync();
    }

    public async Task<string[]> GetFullPathForOrganizationalUnitAsync(Guid ouId)
    {
        var path = new List<string>();
        var currentOu = await applicationDbContext.OrganizationalUnits
            .AsNoTracking()
            .FirstOrDefaultAsync(ou => ou.NodeId == ouId);

        while (currentOu != null)
        {
            path.Insert(0, currentOu.Name);

            if (currentOu.ParentId.HasValue)
            {
                currentOu = await applicationDbContext.OrganizationalUnits
                    .AsNoTracking()
                    .FirstOrDefaultAsync(ou => ou.NodeId == currentOu.ParentId.Value);
            }
            else
            {
                break;
            }
        }

        return [.. path];
    }
}
