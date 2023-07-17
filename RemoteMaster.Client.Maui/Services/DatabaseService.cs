using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client.Maui.Models;

namespace RemoteMaster.Client.Maui.Services;

public class DatabaseService
{
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
    }
}
