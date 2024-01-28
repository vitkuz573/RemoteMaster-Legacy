// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Hubs;

public class ManagementHub(ICertificateService certificateService, IDatabaseService databaseService) : Hub<IManagementClient>
{
    public async Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration, byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var certificate = certificateService.IssueCertificate(csrBytes);

        await Clients.Caller.ReceiveCertificate(certificate.Export(X509ContentType.Pfx));

        var folder = (await databaseService.GetNodesAsync(f => f.Name == hostConfiguration.Group && f is Group)).OfType<Group>().FirstOrDefault();

        if (folder == null)
        {
            folder = new Group
            {
                Name = hostConfiguration.Group
            };

            await databaseService.AddNodeAsync(folder);
        }

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId)).FirstOrDefault(c => c.Name == hostConfiguration.Host.Name);

        if (existingComputer != null)
        {
            existingComputer.IPAddress = hostConfiguration.Host.IPAddress;
        }
        else
        {
            var computer = new Computer
            {
                Name = hostConfiguration.Host.Name,
                IPAddress = hostConfiguration.Host.IPAddress,
                MACAddress = hostConfiguration.Host.MACAddress,
                Parent = folder
            };

            await databaseService.AddNodeAsync(computer);
        }

        return true;
    }

    public async Task<bool> UnregisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var folder = (await databaseService.GetNodesAsync(f => f.Name == hostConfiguration.Group && f is Group)).OfType<Group>().FirstOrDefault();

        if (folder == null)
        {
            Log.Warning("Unregistration failed: Folder '{Group}' not found.", hostConfiguration.Group);

            return false;
        }

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId))
                               .FirstOrDefault(c => c.Name == hostConfiguration.Host.Name);

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

        Log.Warning("Unregistration failed: Computer '{HostName}' not found in folder '{Group}'.", hostConfiguration.Host.Name, hostConfiguration.Group);

        return false;
    }

    public async Task<bool> UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var folder = (await databaseService.GetNodesAsync(f => f.Name == hostConfiguration.Group && f is Group)).OfType<Group>().FirstOrDefault();

        if (folder == null)
        {
            return false;
        }

        var computer = (await databaseService.GetChildrenByParentIdAsync<Computer>(folder.NodeId))
                       .FirstOrDefault(c => c.MACAddress == hostConfiguration.Host.MACAddress);

        if (computer != null)
        {
            await databaseService.UpdateComputerAsync(computer, hostConfiguration.Host.IPAddress, hostConfiguration.Host.Name);

            return true;
        }

        return false;
    }

    public async Task<bool> IsHostRegisteredAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        if (string.IsNullOrWhiteSpace(hostConfiguration.Host.Name))
        {
            throw new ArgumentNullException(hostConfiguration.Host.Name);
        }

        try
        {
            var nodes = await databaseService.GetNodesAsync(n => n is Computer && ((Computer)n).Name == hostConfiguration.Host.Name);
            var computers = nodes.OfType<Computer>();

            var isRegistered = computers.Any();

            return isRegistered;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while checking registration of the host '{HostName}'.", hostConfiguration.Host.Name);

            return false;
        }
    }

    public async Task<string> GetPublicKey()
    {
        try
        {
            var publicKeyPath = @"C:\ProgramData\RemoteMaster\Security\JWT\public_key.pem";
            
            if (File.Exists(publicKeyPath))
            {
                var publicKey = await File.ReadAllTextAsync(publicKeyPath);
                
                return publicKey;
            }
            else
            {
                Log.Warning("Public key file not found at '{Path}'", publicKeyPath);
                
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while reading public key file.");

            return null;
        }
    }
}