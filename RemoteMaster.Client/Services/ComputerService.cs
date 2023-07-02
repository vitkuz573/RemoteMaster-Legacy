using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client.Models;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Linq;

namespace RemoteMaster.Client.Services;

public class ComputerService
{
    private readonly AppDbContext _context;

    public ComputerService(AppDbContext context)
    {
        _context = context;
    }

    public IList<Folder> GetFolders()
    {
        return _context.Nodes.OfType<Folder>().Include(f => f.Children).ToList();
    }


    public void SyncComputersFromActiveDirectory()
    {
        using var domainContext = new PrincipalContext(ContextType.Domain);
        using var searcher = new PrincipalSearcher(new ComputerPrincipal(domainContext));

        var domainComputers = searcher.FindAll()
            .OfType<ComputerPrincipal>()
            .Select(cp => new Computer
            {
                Name = cp.Name,
                IPAddress = Dns.GetHostAddresses(cp.Name).FirstOrDefault()?.ToString()
            })
            .ToList();

        foreach (var domainComputer in domainComputers)
        {
            var localComputer = _context.Nodes.OfType<Computer>().FirstOrDefault(c => c.Name == domainComputer.Name);

            if (localComputer == null)
            {
                _context.Nodes.Add(domainComputer);
            }
            else
            {
                localComputer.IPAddress = domainComputer.IPAddress;
            }
        }

        _context.SaveChanges();
    }
}
