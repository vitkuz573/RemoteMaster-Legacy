// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Hubs;

public class ManagementHub(ICertificateService certificateService, IDatabaseService databaseService) : Hub
{
    public async Task<bool> RegisterHostAsync(string hostName, string ipAddress, string macAddress, HostConfiguration config, byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(config);

        var certificate = certificateService.GenerateCertificateFromCSR(csrBytes);

        await Clients.Caller.SendAsync("ReceiveCertificate", certificate.Export(X509ContentType.Pfx));

        var folder = (await databaseService.GetNodesAsync(f => f.Name == config.Group && f is Folder)).OfType<Folder>().FirstOrDefault();

        if (folder == null)
        {
            folder = new Folder(config.Group);
            await databaseService.AddNodeAsync(folder);
        }

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId)).FirstOrDefault(c => c.Name == hostName);

        if (existingComputer != null)
        {
            existingComputer.IPAddress = ipAddress;
        }
        else
        {
            var computer = new Computer
            {
                Name = hostName,
                IPAddress = ipAddress,
                MACAddress = macAddress,
                Parent = folder
            };

            await databaseService.AddNodeAsync(computer);
        }

        return true;
    }

    public async Task<bool> UnregisterHostAsync(string hostName, HostConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var folder = (await databaseService.GetNodesAsync(f => f.Name == config.Group && f is Folder)).OfType<Folder>().FirstOrDefault();

        if (folder == null)
        {
            Log.Warning("Unregistration failed: Folder '{Group}' not found.", config.Group);

            return false;
        }

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId))
                               .FirstOrDefault(c => c.Name == hostName);

        if (existingComputer != null)
        {
            await databaseService.RemoveNodeAsync(existingComputer);

            var remainingComputers = await databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId);

            if (!remainingComputers.Any())
            {
                await databaseService.RemoveNodeAsync(folder);
            }

            return true;
        }

        Log.Warning("Unregistration failed: Computer '{HostName}' not found in folder '{Group}'.", hostName, config.Group);

        return false;
    }

    public async Task<bool> UpdateHostInformationAsync(string hostName, string group, string ipAddress)
    {
        var folder = (await databaseService.GetNodesAsync(f => f.Name == group && f is Folder)).OfType<Folder>().FirstOrDefault();

        if (folder == null)
        {
            return false;
        }

        var computer = (await databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId))
                       .FirstOrDefault(c => c.Name == hostName);

        if (computer != null)
        {
            await databaseService.UpdateComputerAsync(computer, ipAddress);

            return true;
        }

        return false;
    }
}