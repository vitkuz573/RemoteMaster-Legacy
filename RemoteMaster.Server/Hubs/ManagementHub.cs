// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
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

        if (hostConfiguration.Host == null)
        {
            throw new ArgumentException("Host configuration must have a non-null Host property.", nameof(hostConfiguration));
        }

        var certificate = certificateService.IssueCertificate(csrBytes);

        await Clients.Caller.ReceiveCertificate(certificate.Export(X509ContentType.Pfx));

        var group = (await databaseService.GetNodesAsync(g => g.Name == hostConfiguration.Subject.OrganizationalUnit && g is Group)).OfType<Group>().FirstOrDefault();

        if (group == null)
        {
            group = new Group
            {
                Name = hostConfiguration.Subject.OrganizationalUnit
            };

            await databaseService.AddNodeAsync(group);
        }

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(group.NodeId)).FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

        if (existingComputer != null)
        {
            await databaseService.UpdateComputerAsync(existingComputer, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);
        }
        else
        {
            var computer = new Computer
            {
                Name = hostConfiguration.Host.Name,
                IpAddress = hostConfiguration.Host.IpAddress,
                MacAddress = hostConfiguration.Host.MacAddress,
                Parent = group
            };

            await databaseService.AddNodeAsync(computer);
        }

        return true;
    }

    public async Task<bool> UnregisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        if (hostConfiguration.Host == null || string.IsNullOrWhiteSpace(hostConfiguration.Host.MacAddress))
        {
            throw new ArgumentException("Host configuration must have a non-null Host property with a valid MAC address.", nameof(hostConfiguration));
        }

        var group = (await databaseService.GetNodesAsync(g => g.Name == hostConfiguration.Subject.OrganizationalUnit && g is Group)).OfType<Group>().FirstOrDefault();

        if (group == null)
        {
            Log.Warning("Unregistration failed: Group '{Group}' not found.", hostConfiguration.Subject.OrganizationalUnit);
            
            return false;
        }

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(group.NodeId)).FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

        if (existingComputer != null)
        {
            await databaseService.RemoveNodeAsync(existingComputer);

            var remainingComputers = await databaseService.GetChildrenByParentIdAsync<Computer>(group.NodeId);

            if (!remainingComputers.Any())
            {
                await databaseService.RemoveNodeAsync(group);
            }

            return true;
        }
        else
        {
            Log.Warning("Unregistration failed: Computer with MAC address '{MACAddress}' not found in group '{Group}'.", hostConfiguration.Host.MacAddress, hostConfiguration.Subject.OrganizationalUnit);
            
            return false;
        }
    }

    public async Task<bool> UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        if (hostConfiguration.Host == null)
        {
            throw new ArgumentException("Host configuration must have a non-null Host property.", nameof(hostConfiguration));
        }

        var group = (await databaseService.GetNodesAsync(g => g.Name == hostConfiguration.Subject.OrganizationalUnit && g is Group)).OfType<Group>().FirstOrDefault();

        if (group == null)
        {
            return false;
        }

        var computer = (await databaseService.GetChildrenByParentIdAsync<Computer>(group.NodeId)).FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

        if (computer == null)
        {
            return false;
        }

        await databaseService.UpdateComputerAsync(computer, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);

        return true;
    }

    public async Task<bool> IsHostRegisteredAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        if (hostConfiguration.Host == null)
        {
            throw new ArgumentException("Host configuration must have a non-null Host property.", nameof(hostConfiguration));
        }

        if (string.IsNullOrWhiteSpace(hostConfiguration.Host.MacAddress))
        {
            throw new ArgumentNullException(hostConfiguration.Host.MacAddress);
        }

        try
        {
            var nodes = await databaseService.GetNodesAsync(n => n is Computer && ((Computer)n).MacAddress == hostConfiguration.Host.MacAddress);
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

    public async Task<string?> GetPublicKey()
    {
        try
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var publicKeyPath = Path.Combine(programData, "RemoteMaster", "Security", "JWT", "public_key.pem");

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

    public async Task<string?> GetNewOrganizationalUnitIfChangeRequested(string macAddress)
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var groupChangeRequestsPath = Path.Combine(programData, "RemoteMaster", "Server", "OrganizationalUnitChangeRequests.json");

        if (File.Exists(groupChangeRequestsPath))
        {
            var json = await File.ReadAllTextAsync(groupChangeRequestsPath);
            var changeRequests = JsonSerializer.Deserialize<List<OrganizationalUnitChangeRequest>>(json) ?? [];
            var request = changeRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

            if (request != null)
            {
                return request.NewOrganizationalUnit;
            }
        }

        Log.Information("No organizational unit change request found for MAC address {MACAddress}.", macAddress);

        return null;
    }

    public async Task AcknowledgeOrganizationalUnitChange(string macAddress)
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var organizationalUnitChangeRequestsPath = Path.Combine(programData, "RemoteMaster", "Server", "OrganizationalUnitChangeRequests.json");

        if (File.Exists(organizationalUnitChangeRequestsPath))
        {
            var json = await File.ReadAllTextAsync(organizationalUnitChangeRequestsPath);
            var changeRequests = JsonSerializer.Deserialize<List<OrganizationalUnitChangeRequest>>(json) ?? [];

            var requestToRemove = changeRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

            if (requestToRemove != null)
            {
                changeRequests.Remove(requestToRemove);
                var updatedJson = JsonSerializer.Serialize(changeRequests, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(organizationalUnitChangeRequestsPath, updatedJson);

                Log.Information("Acknowledged and removed organizational unit change request for MAC address {MACAddress}.", macAddress);
            }
            else
            {
                Log.Information("No organizational unit change request found for MAC address {MACAddress} to acknowledge.", macAddress);
            }
        }
        else
        {
            Log.Warning("Organizational unit change requests file not found at '{Path}'. Unable to acknowledge change request.", organizationalUnitChangeRequestsPath);
        }
    }
}