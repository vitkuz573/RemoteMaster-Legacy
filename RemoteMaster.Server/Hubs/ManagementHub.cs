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

    private async Task<Computer> GetComputerByMacAddressAsync(string macAddress, Guid parentOuId)
    {
        var existingComputers = await databaseService.GetChildrenByParentIdAsync<Computer>(parentOuId);
        var computer = existingComputers.FirstOrDefault(c => c.MacAddress == macAddress) ?? throw new InvalidOperationException($"Computer with MAC address '{macAddress}' not found.");
        
        return computer;
    }

    public async Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var organization = await GetOrganizationAsync(hostConfiguration.Subject.Organization);
        var parentOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organization.NodeId);

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, parentOu.NodeId);
            await databaseService.UpdateComputerAsync(existingComputer, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);

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
        var lastOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organizationEntity.NodeId);

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, lastOu.NodeId);
            await databaseService.RemoveNodeAsync(existingComputer);
            
            return true;
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);
            
            return false;
        }
    }

    public async Task<bool> UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var lastOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, Guid.Empty);

        try
        {
            var computer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, lastOu.NodeId);
            await databaseService.UpdateComputerAsync(computer, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);
            
            return true;
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);
            
            return false;
        }
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

            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while reading public key file.");
            
            return null;
        }
    }

    private static async Task<List<HostMoveRequest>> GetHostMoveRequestsAsync()
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var hostMoveRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");

        if (File.Exists(hostMoveRequestsFilePath))
        {
            var json = await File.ReadAllTextAsync(hostMoveRequestsFilePath);
            
            return JsonSerializer.Deserialize<List<HostMoveRequest>>(json) ?? [];
        }

        return [];
    }

    private static async Task SaveHostMoveRequestsAsync(List<HostMoveRequest> hostMoveRequests)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var hostMoveRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");
        var updatedJson = JsonSerializer.Serialize(hostMoveRequests);

        await File.WriteAllTextAsync(hostMoveRequestsFilePath, updatedJson);
    }

    public async Task<HostMoveRequest?> GetHostMoveRequest(string macAddress)
    {
        var hostMoveRequests = await GetHostMoveRequestsAsync();
        var hostMoveRequest = hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

        return hostMoveRequest;
    }

    public async Task AcknowledgeMoveRequest(string macAddress)
    {
        var hostMoveRequests = await GetHostMoveRequestsAsync();
        var requestToRemove = hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

        if (requestToRemove != null)
        {
            hostMoveRequests.Remove(requestToRemove);
            await SaveHostMoveRequestsAsync(hostMoveRequests);
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
