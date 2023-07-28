using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Services;

public class DatabaseService
{
    public delegate void NodeAddedEventHandler(Node node);
    public event NodeAddedEventHandler NodeAdded;

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

        NodeAdded?.Invoke(node);
    }

    public IList<Computer> GetComputersByFolderId(Guid folderId)
    {
        return _context.Nodes
            .OfType<Computer>()
            .Where(c => c.ParentId == folderId)
            .ToList();
    }
}
