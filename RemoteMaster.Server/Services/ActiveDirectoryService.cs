// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Net.Sockets;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class ActiveDirectoryService
{
    public async Task<IDictionary<string, List<Computer>>> FetchComputers()
    {
        using var domainContext = new PrincipalContext(ContextType.Domain);
        using var computerPrincipal = new ComputerPrincipal(domainContext);
        using var searcher = new PrincipalSearcher(computerPrincipal);

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
