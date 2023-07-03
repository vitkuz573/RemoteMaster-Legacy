using Microsoft.EntityFrameworkCore;
using RemoteMaster.Client.Models;
using System.Collections.Concurrent;
using System.DirectoryServices.AccountManagement;
using System.Net;
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

    public async Task<List<Computer>> SyncComputersFromActiveDirectory()
    {
        using var domainContext = new PrincipalContext(ContextType.Domain);
        using var searcher = new PrincipalSearcher(new ComputerPrincipal(domainContext));

        var domainComputers = new ConcurrentBag<Computer>();  // Используем потокобезопасную коллекцию

        var tasks = searcher.FindAll().OfType<ComputerPrincipal>()
            .Select(cp => Task.Run(async () =>  // Запуск задачи в фоновом потоке
            {
                try
                {
                    var ipAddress = (await Dns.GetHostEntryAsync(cp.Name)).AddressList.FirstOrDefault()?.ToString();

                    domainComputers.Add(new Computer
                    {
                        Name = cp.Name,
                        IPAddress = ipAddress
                    });
                }
                catch (SocketException)
                {
                    // Хост недоступен, пропустите его и продолжите со следующим.
                }
            }));

        await Task.WhenAll(tasks);  // Ожидание завершения всех задач

        return domainComputers.ToList();  // Возвращает список найденных компьютеров
    }
}
