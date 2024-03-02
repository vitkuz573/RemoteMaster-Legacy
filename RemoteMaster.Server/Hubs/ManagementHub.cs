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
        ArgumentNullException.ThrowIfNull(hostConfiguration.Subject);
        ArgumentNullException.ThrowIfNull(hostConfiguration.Subject.OrganizationalUnit);

        if (hostConfiguration.Host == null)
        {
            throw new ArgumentException("Host configuration must have a non-null Host property.", nameof(hostConfiguration));
        }

        var certificate = certificateService.IssueCertificate(csrBytes);
        await Clients.Caller.ReceiveCertificate(certificate.Export(X509ContentType.Pfx));

        OrganizationalUnit? parentOu = null;

        foreach (var ouName in hostConfiguration.Subject.OrganizationalUnit)
        {
            var ou = (await databaseService.GetNodesAsync(n => n.Name == ouName && n is OrganizationalUnit && n.Parent == parentOu))
                     .OfType<OrganizationalUnit>()
                     .FirstOrDefault();

            if (ou == null)
            {
                ou = new OrganizationalUnit
                {
                    Name = ouName,
                    Parent = parentOu
                };

                await databaseService.AddNodeAsync(ou);
            }

            parentOu = ou;
        }

        if (parentOu == null)
        {
            throw new InvalidOperationException("Failed to resolve or create organizational unit for registration.");
        }

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(parentOu.NodeId))
                               .FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

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
                Parent = parentOu
            };

            var hostGuid = await databaseService.AddNodeAsync(computer);

            await Clients.Caller.ReceiveHostGuid(hostGuid);
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

        OrganizationalUnit? lastOu = null;

        foreach (var ouName in hostConfiguration.Subject.OrganizationalUnit)
        {
            var ous = await databaseService.GetNodesAsync(n => n.Name == ouName && n is OrganizationalUnit && (lastOu == null || n.ParentId == lastOu.NodeId));
            var ou = ous.OfType<OrganizationalUnit>().FirstOrDefault();
            
            if (ou != null)
            {
                lastOu = ou;
            }
            else
            {
                Log.Warning("Unregistration failed: OrganizationalUnit '{OUName}' not found.", ouName);
                
                return false;
            }
        }

        if (lastOu == null)
        {
            Log.Warning("Unregistration failed: Specified OrganizationalUnit hierarchy not found.");
            
            return false;
        }

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(lastOu.NodeId)).FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);
        
        if (existingComputer != null)
        {
            await databaseService.RemoveNodeAsync(existingComputer);

            var currentOu = lastOu;

            while (currentOu != null)
            {
                var children = await databaseService.GetChildrenByParentIdAsync<Node>(currentOu.NodeId);
                
                if (!children.Any())
                {
                    var parentOu = currentOu.Parent;
                    await databaseService.RemoveNodeAsync(currentOu);
                    currentOu = parentOu as OrganizationalUnit;
                }
                else
                {
                    break;
                }
            }

            return true;
        }

        Log.Warning("Unregistration failed: Computer with MAC address '{MACAddress}' not found in the last organizational unit.", hostConfiguration.Host.MacAddress);
        
        return false;
    }

    public async Task<bool> UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        if (hostConfiguration.Host == null)
        {
            throw new ArgumentException("Host configuration must have a non-null Host property.", nameof(hostConfiguration));
        }

        OrganizationalUnit? lastOu = null;

        foreach (var ouName in hostConfiguration.Subject.OrganizationalUnit)
        {
            var ous = await databaseService.GetNodesAsync(n => n.Name == ouName && n is OrganizationalUnit && (lastOu == null || n.ParentId == lastOu.NodeId));
            var ou = ous.OfType<OrganizationalUnit>().FirstOrDefault();

            if (ou != null)
            {
                lastOu = ou;
            }
            else
            {
                Log.Warning("Update failed: OrganizationalUnit '{OUName}' not found.", ouName);
                
                return false;
            }
        }

        if (lastOu == null)
        {
            Log.Warning("Update failed: Specified OrganizationalUnit hierarchy not found.");
            
            return false;
        }

        var computer = (await databaseService.GetChildrenByParentIdAsync<Computer>(lastOu.NodeId))
            .FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

        if (computer == null)
        {
            Log.Warning("Update failed: Computer with MAC address '{MACAddress}' not found in the last organizational unit.", hostConfiguration.Host.MacAddress);
            
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

            Log.Warning("Public key file not found at '{Path}'", publicKeyPath);

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while reading public key file.");

            return null;
        }
    }

    public async Task<string[]> GetNewOrganizationalUnitIfChangeRequested(string macAddress)
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var ouChangeRequestsFilePath = Path.Combine(programData, "RemoteMaster", "Server", "OrganizationalUnitChangeRequests.json");

        if (File.Exists(ouChangeRequestsFilePath))
        {
            var json = await File.ReadAllTextAsync(ouChangeRequestsFilePath);
            var changeRequests = JsonSerializer.Deserialize<List<OrganizationalUnitChangeRequest>>(json) ?? [];
            var request = changeRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

            if (request != null)
            {
                return request.NewOrganizationalUnit;
            }
        }

        Log.Information("No organizational unit change request found for MAC address {MACAddress}.", macAddress);

        return [];
    }

    public async Task AcknowledgeOrganizationalUnitChange(string macAddress)
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var ouChangeRequestsFilePath = Path.Combine(programData, "RemoteMaster", "Server", "OrganizationalUnitChangeRequests.json");

        if (File.Exists(ouChangeRequestsFilePath))
        {
            var json = await File.ReadAllTextAsync(ouChangeRequestsFilePath);
            var changeRequests = JsonSerializer.Deserialize<List<OrganizationalUnitChangeRequest>>(json) ?? [];

            var requestToRemove = changeRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

            if (requestToRemove != null)
            {
                changeRequests.Remove(requestToRemove);
                var updatedJson = JsonSerializer.Serialize(changeRequests);
                await File.WriteAllTextAsync(ouChangeRequestsFilePath, updatedJson);

                Log.Information("Acknowledged and removed organizational unit change request for MAC address {MACAddress}.", macAddress);
            }
            else
            {
                Log.Information("No organizational unit change request found for MAC address {MACAddress} to acknowledge.", macAddress);
            }
        }
        else
        {
            Log.Warning("Organizational unit change requests file not found at '{Path}'. Unable to acknowledge change request.", ouChangeRequestsFilePath);
        }
    }
}