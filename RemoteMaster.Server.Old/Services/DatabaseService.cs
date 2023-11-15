// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class DatabaseService : IDatabaseService
{
    private readonly NodesDataContext _context;

    public DatabaseService(NodesDataContext context)
    {
        _context = context;
    }

    public async Task<IList<Node>> GetNodesAsync(Expression<Func<Node, bool>>? predicate = null)
    {
        var query = _context.Nodes.AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.Include(node => node.Nodes).ToListAsync();
    }

    public async Task<IList<T>> GetChildrenByParentIdAsync<T>(Guid parentId) where T : Node
    {
        return await _context.Nodes.OfType<T>().Where(node => node.ParentId == parentId).ToListAsync();
    }

    public async Task AddNodeAsync(Node node)
    {
        await _context.Nodes.AddAsync(node);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveNodeAsync(Node node)
    {
        _context.Nodes.Remove(node);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateComputerAsync(Computer computer, string ipAddress)
    {
        if (computer != null)
        {
            computer.IPAddress = ipAddress;
            _context.Nodes.Update(computer);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasChildrenAsync(Node node)
    {
        return await _context.Nodes.AnyAsync(n => n.ParentId == node.NodeId);
    }
}
