// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class DatabaseService
{
    public event EventHandler<Node> NodeAdded;

    private readonly AppDbContext _context;

    public DatabaseService(AppDbContext context)
    {
        _context = context;
    }

    public IList<Folder> GetFolders()
    {
        return _context.Nodes.OfType<Folder>().Include(f => f.Children).ToList();
    }

    public void AddNode(Node node)
    {
        _context.Nodes.Add(node);
        _context.SaveChanges();

        NodeAdded?.Invoke(this, node);
    }

    public IList<Computer> GetComputersByFolderId(Guid folderId)
    {
        return _context.Nodes
            .OfType<Computer>()
            .Where(c => c.ParentId == folderId)
            .ToList();
    }
}
