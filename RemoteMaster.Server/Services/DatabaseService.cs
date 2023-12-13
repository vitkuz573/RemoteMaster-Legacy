// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class DatabaseService(NodesDbContext context) : IDatabaseService
{
    public async Task<IList<Node>> GetNodesAsync(Expression<Func<Node, bool>>? predicate = null)
    {
        var query = context.Nodes.AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.Include(node => node.Nodes).ToListAsync();
    }

    public async Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : Node
    {
        return await context.Nodes.OfType<T>().Where(node => node.ParentId == parentId).ToListAsync();
    }

    public async Task AddNodeAsync(Node node)
    {
        await context.Nodes.AddAsync(node);
        await context.SaveChangesAsync();
    }

    public async Task RemoveNodeAsync(Node node)
    {
        context.Nodes.Remove(node);
        await context.SaveChangesAsync();
    }

    public async Task UpdateComputerAsync(Computer computer, string ipAddress, string hostName)
    {
        if (computer != null)
        {
            computer.IPAddress = ipAddress;
            computer.Name = hostName;
            context.Nodes.Update(computer);
        }

        await context.SaveChangesAsync();
    }

    public async Task<bool> HasChildrenAsync(Node node)
    {
        return await context.Nodes.AnyAsync(n => n.ParentId == node.NodeId);
    }
}
