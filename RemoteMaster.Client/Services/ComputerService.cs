using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client.Models;
using System.Collections.Concurrent;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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

    public void AddNode(Node node)
    {
        _context.Nodes.Add(node);
        _context.SaveChanges();
    }

    public async Task<IDictionary<string, List<Computer>>> SyncComputersFromActiveDirectory()
    {
        using var domainContext = new PrincipalContext(ContextType.Domain);
        using var searcher = new PrincipalSearcher(new ComputerPrincipal(domainContext));

        var domainComputers = new ConcurrentDictionary<string, List<Computer>>();

        var tasks = searcher.FindAll().OfType<ComputerPrincipal>()
            .Select(cp => Task.Run(async () =>
            {
                try
                {
                    var ipAddress = (await Dns.GetHostEntryAsync(cp.Name)).AddressList.FirstOrDefault()?.ToString();

                    domainComputers.AddOrUpdate(cp.DistinguishedName.Split(',').Skip(1).First().Replace("OU=", ""),
                        new List<Computer> { new Computer(cp.Name, ipAddress) },
                        (key, oldValue) =>
                        {
                            oldValue.Add(new Computer(cp.Name, ipAddress));

                            return oldValue;
                        });
                }
                catch (SocketException)
                {
                    // Хост недоступен, пропустите его и продолжите со следующим.
                }
                catch (Exception)
                {
                    // Обработка других исключений, если требуется
                }
            }));

        await Task.WhenAll(tasks);

        return domainComputers;
    }
}
