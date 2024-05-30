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
    public async Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        ArgumentNullException.ThrowIfNull(hostConfiguration.Subject);
        ArgumentNullException.ThrowIfNull(hostConfiguration.Subject.OrganizationalUnit);

        if (hostConfiguration.Host == null)
        {
            throw new ArgumentException("Host configuration must have a non-null Host property.", nameof(hostConfiguration));
        }

        var organizationEntity = (await databaseService.GetNodesAsync<Organization>(n => n.Name == hostConfiguration.Subject.Organization)).FirstOrDefault() ?? throw new InvalidOperationException($"Organization '{hostConfiguration.Subject.Organization}' not found.");
        var organizationId = organizationEntity.OrganizationId;

        OrganizationalUnit? parentOu = null;

        foreach (var ouName in hostConfiguration.Subject.OrganizationalUnit)
        {
            var ous = await databaseService.GetNodesAsync<OrganizationalUnit>(n => n.Name == ouName && n.OrganizationId == organizationId);
            var ou = ous.FirstOrDefault(o => parentOu == null || o.ParentId == parentOu.NodeId) ?? throw new InvalidOperationException($"Organizational Unit '{ouName}' not found.");
            parentOu = ou;
        }

        if (parentOu == null)
        {
            throw new InvalidOperationException("Failed to resolve organizational unit for registration.");
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

        var organizationEntity = (await databaseService.GetNodesAsync<Organization>(n => n.Name == hostConfiguration.Subject.Organization)).FirstOrDefault();

        if (organizationEntity == null)
        {
            Log.Warning("Unregistration failed: Organization '{Organization}' not found.", hostConfiguration.Subject.Organization);

            return false;
        }

        var organizationId = organizationEntity.OrganizationId;

        OrganizationalUnit? lastOu = null;

        foreach (var ouName in hostConfiguration.Subject.OrganizationalUnit)
        {
            var ous = await databaseService.GetNodesAsync<OrganizationalUnit>(n => n.Name == ouName && n.OrganizationId == organizationId);
            var ou = ous.FirstOrDefault(o => lastOu == null || o.ParentId == lastOu.NodeId);

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

        var existingComputer = (await databaseService.GetChildrenByParentIdAsync<Computer>(lastOu.NodeId))
            .FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

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

        if (hostConfiguration.Host == null)
        {
            throw new ArgumentException("Host configuration must have a non-null Host property.", nameof(hostConfiguration));
        }

        OrganizationalUnit? lastOu = null;

        foreach (var ouName in hostConfiguration.Subject.OrganizationalUnit)
        {
            var ous = await databaseService.GetNodesAsync<OrganizationalUnit>(n => n.Name == ouName);
            var ou = ous.FirstOrDefault(o => lastOu == null || o.ParentId == lastOu.NodeId);

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
            var computers = await databaseService.GetNodesAsync<Computer>(n => n.MacAddress == hostConfiguration.Host.MacAddress);
            var isRegistered = computers.Any();

            return isRegistered;
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

    public async Task<string[]> GetNewOrganizationalUnitIfChangeRequested(string macAddress)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var ouChangeRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "OrganizationalUnitChangeRequests.json");

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
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var ouChangeRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "OrganizationalUnitChangeRequests.json");

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
