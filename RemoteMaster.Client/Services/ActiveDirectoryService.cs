using RemoteMaster.Client.Models;
using System.Collections.Concurrent;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Net.Sockets;

namespace RemoteMaster.Client.Services;

public class ActiveDirectoryService
{
    public async Task<IDictionary<string, List<Computer>>> FetchComputers()
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

                    domainComputers.AddOrUpdate(
                        cp.DistinguishedName.Split(',').Skip(1).First().Replace("OU=", ""),
                        new List<Computer> { new Computer(cp.Name, ipAddress) },
                        (key, oldValue) =>
                        {
                            if (!oldValue.Any(c => c.Name == cp.Name && c.IPAddress == ipAddress))
                            {
                                oldValue.Add(new Computer(cp.Name, ipAddress));
                            }

                            return oldValue;
                        }
                    );
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
