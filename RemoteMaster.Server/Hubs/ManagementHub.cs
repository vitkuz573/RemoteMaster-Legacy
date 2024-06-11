// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Hubs;

public class ManagementHub(ICertificateService certificateService, ICaCertificateService caCertificateService, IDatabaseService databaseService) : Hub<IManagementClient>
{
    private async Task<Organization> GetOrganizationAsync(string organizationName)
    {
        var organizations = await databaseService.GetNodesAsync<Organization>(n => n.Name == organizationName);
        var organization = organizations.FirstOrDefault();
        
        return organization ?? throw new InvalidOperationException($"Organization '{organizationName}' not found.");
    }

    private async Task<OrganizationalUnit?> ResolveOrganizationalUnitHierarchyAsync(IEnumerable<string> ouNames, Guid organizationId)
    {
        OrganizationalUnit? parentOu = null;

        foreach (var ouName in ouNames)
        {
            var ous = await databaseService.GetNodesAsync<OrganizationalUnit>(n => n.Name == ouName && n.OrganizationId == organizationId);
            var ou = ous.FirstOrDefault(o => parentOu == null || o.ParentId == parentOu.NodeId) ?? throw new InvalidOperationException($"Organizational Unit '{ouName}' not found.");
            
            parentOu = ou;
        }

        return parentOu;
    }

    public async Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var organizationEntity = await GetOrganizationAsync(hostConfiguration.Subject.Organization);
        var organizationId = organizationEntity.NodeId;

        var parentOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organizationId);

        var existingComputers = await databaseService.GetChildrenByParentIdAsync<Computer>(parentOu.NodeId);
        var existingComputer = existingComputers.FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

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

        if (string.IsNullOrWhiteSpace(hostConfiguration.Host.MacAddress))
        {
            throw new ArgumentException("Host configuration must have a non-null Host property with a valid MAC address.", nameof(hostConfiguration));
        }

        var organizationEntity = await GetOrganizationAsync(hostConfiguration.Subject.Organization);

        var organizationId = organizationEntity.NodeId;
        var lastOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organizationId);

        var existingComputers = await databaseService.GetChildrenByParentIdAsync<Computer>(lastOu.NodeId);
        var existingComputer = existingComputers.FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

        if (existingComputer != null)
        {
            await databaseService.RemoveNodeAsync(existingComputer);
            
            return true;
        }

        Log.Warning("Unregistration failed: Computer with MAC address '{MACAddress}' not found in the last organizational unit.", hostConfiguration.Host.MacAddress);
        
        return false;
    }

    public async Task<bool> UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var lastOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, Guid.Empty);

        var computers = await databaseService.GetChildrenByParentIdAsync<Computer>(lastOu.NodeId);
        var computer = computers.FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

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

        if (string.IsNullOrWhiteSpace(hostConfiguration.Host.MacAddress))
        {
            throw new ArgumentNullException(hostConfiguration.Host.MacAddress);
        }

        try
        {
            var computers = await databaseService.GetNodesAsync<Computer>(n => n.MacAddress == hostConfiguration.Host.MacAddress);
            
            return computers.Any();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while checking registration of the host '{HostName}'.", hostConfiguration.Host.Name);
            
            return false;
        }
    }

    public async Task<byte[]?> GetPublicKey()
    {
        try
        {
            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");

            if (File.Exists(publicKeyPath))
            {
                return await File.ReadAllBytesAsync(publicKeyPath);
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

    public async Task<HostMoveRequest?> GetHostMoveRequest(string macAddress)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var ouChangeRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");

        if (File.Exists(ouChangeRequestsFilePath))
        {
            var json = await File.ReadAllTextAsync(ouChangeRequestsFilePath);
            var hostMoveRequests = JsonSerializer.Deserialize<List<HostMoveRequest>>(json) ?? [];
           
            return hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
        }

        Log.Information("No host move request found for MAC address {MACAddress}.", macAddress);
        
        return null;
    }

    public async Task AcknowledgeMoveRequest(string macAddress)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var hostMoveRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");

        if (File.Exists(hostMoveRequestsFilePath))
        {
            var json = await File.ReadAllTextAsync(hostMoveRequestsFilePath);
            var hostMoveRequests = JsonSerializer.Deserialize<List<HostMoveRequest>>(json) ?? [];

            var requestToRemove = hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
            
            if (requestToRemove != null)
            {
                hostMoveRequests.Remove(requestToRemove);
                var updatedJson = JsonSerializer.Serialize(hostMoveRequests);
                
                await File.WriteAllTextAsync(hostMoveRequestsFilePath, updatedJson);

                Log.Information("Acknowledged and removed host move request for MAC address {MACAddress}.", macAddress);
            }
            else
            {
                Log.Information("No host move request found for MAC address {MACAddress} to acknowledge.", macAddress);
            }
        }
        else
        {
            Log.Warning("Host move requests file not found at '{Path}'. Unable to acknowledge change request.", hostMoveRequestsFilePath);
        }
    }

    public async Task<bool> IssueCertificateAsync(byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(csrBytes, nameof(csrBytes));

        try
        {
            var certificate = certificateService.IssueCertificate(csrBytes);
            await Clients.Caller.ReceiveCertificate(certificate.Export(X509ContentType.Pfx));
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while issuing certificate.");
            
            return false;
        }
    }

    public async Task<bool> GetCaCertificateAsync()
    {
        try
        {
            var caCertificatePublicPart = caCertificateService.GetCaCertificate(X509ContentType.Cert);

            if (caCertificatePublicPart != null)
            {
                await Clients.Caller.ReceiveCertificate(caCertificatePublicPart.RawData);
                
                return true;
            }
            else
            {
                Log.Warning("CA certificate public part could not be retrieved.");
                
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while sending CA certificate public part.");
            
            return false;
        }
    }
}
