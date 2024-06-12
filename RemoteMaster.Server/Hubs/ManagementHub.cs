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

/// <summary>
/// Hub for managing various operations related to hosts and certificates.
/// </summary>
public class ManagementHub(ICertificateService certificateService, ICaCertificateService caCertificateService, IDatabaseService databaseService, INotificationService notificationService) : Hub<IManagementClient>
{
    /// <summary>
    /// Retrieves an organization by name from the database.
    /// </summary>
    /// <param name="organizationName">The name of the organization.</param>
    /// <returns>The organization object if found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the organization is not found.</exception>
    private async Task<Organization> GetOrganizationAsync(string organizationName)
    {
        var organizations = await databaseService.GetNodesAsync<Organization>(n => n.Name == organizationName);
        var organization = organizations.FirstOrDefault();

        return organization ?? throw new InvalidOperationException($"Organization '{organizationName}' not found.");
    }

    /// <summary>
    /// Resolves the hierarchy of organizational units.
    /// </summary>
    /// <param name="ouNames">The list of organizational unit names.</param>
    /// <param name="organizationId">The ID of the organization.</param>
    /// <returns>The resolved organizational unit.</returns>
    /// <exception cref="InvalidOperationException">Thrown if any organizational unit is not found.</exception>
    private async Task<OrganizationalUnit?> ResolveOrganizationalUnitHierarchyAsync(IEnumerable<string> ouNames, Guid organizationId)
    {
        OrganizationalUnit? parentOu = null;

        var organizations = await databaseService.GetNodesAsync<Organization>(n => n.NodeId == organizationId);
        var organizationName = organizations.FirstOrDefault()?.Name ?? "Unknown";

        foreach (var ouName in ouNames)
        {
            var ous = await databaseService.GetNodesAsync<OrganizationalUnit>(n => n.Name == ouName && n.OrganizationId == organizationId);
            var ou = ous.FirstOrDefault(o => parentOu == null || o.ParentId == parentOu.NodeId) ?? throw new InvalidOperationException($"Organizational Unit '{ouName}' not found in organization '{organizationName}'.");
            
            parentOu = ou;
        }

        return parentOu;
    }

    /// <summary>
    /// Retrieves a computer by its MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address of the computer.</param>
    /// <param name="parentOuId">The ID of the parent organizational unit.</param>
    /// <returns>The computer object if found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the computer is not found.</exception>
    private async Task<Computer> GetComputerByMacAddressAsync(string macAddress, Guid parentOuId)
    {
        var existingComputers = await databaseService.GetChildrenByParentIdAsync<Computer>(parentOuId);
        var computer = existingComputers.FirstOrDefault(c => c.MacAddress == macAddress) ?? throw new InvalidOperationException($"Computer with MAC address '{macAddress}' not found.");

        return computer;
    }

    /// <summary>
    /// Registers a host with the specified configuration.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <returns>True if registration is successful, otherwise false.</returns>
    public async Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        Organization organization;
        OrganizationalUnit? parentOu;

        try
        {
            organization = await GetOrganizationAsync(hostConfiguration.Subject.Organization);
            parentOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organization.NodeId);
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);
            await notificationService.SendNotificationAsync($"Host registration failed: {ex.Message} for host {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress}) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
            return false;
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, parentOu.NodeId);
            await databaseService.UpdateComputerAsync(existingComputer, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);
            Log.Information("Host registration successful: {HostName} ({MacAddress})", hostConfiguration.Host.Name, hostConfiguration.Host.MacAddress);
            await notificationService.SendNotificationAsync($"Host registration successful: {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress}) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
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

            Log.Information("New host registered: {HostName} ({MacAddress})", hostConfiguration.Host.Name, hostConfiguration.Host.MacAddress);
            await notificationService.SendNotificationAsync($"New host registered: {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress}) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
        }

        return true;
    }

    /// <summary>
    /// Unregisters a host with the specified configuration.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <returns>True if unregistration is successful, otherwise false.</returns>
    /// <exception cref="ArgumentException">Thrown if the host configuration is invalid.</exception>
    public async Task<bool> UnregisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        if (string.IsNullOrWhiteSpace(hostConfiguration.Host.MacAddress))
        {
            throw new ArgumentException("Host configuration must have a non-null Host property with a valid MAC address.", nameof(hostConfiguration));
        }

        Organization organizationEntity;
        OrganizationalUnit? lastOu;

        try
        {
            organizationEntity = await GetOrganizationAsync(hostConfiguration.Subject.Organization);
            lastOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organizationEntity.NodeId);
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);
            await notificationService.SendNotificationAsync($"Host unregistration failed: {ex.Message} for host {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress}) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
            return false;
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, lastOu.NodeId);
            await databaseService.RemoveNodeAsync(existingComputer);

            Log.Information("Host unregistered: {HostName} ({MacAddress})", hostConfiguration.Host.Name, hostConfiguration.Host.MacAddress);
            await notificationService.SendNotificationAsync($"Host unregistered: {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress}) from organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' in organization '{hostConfiguration.Subject.Organization}'");

            return true;
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);
            await notificationService.SendNotificationAsync($"Host unregistration failed: {ex.Message} for host {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress}) from organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' in organization '{hostConfiguration.Subject.Organization}'");

            return false;
        }
    }

    /// <summary>
    /// Updates the information of a host with the specified configuration.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <returns>True if the update is successful, otherwise false.</returns>
    public async Task<bool> UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        Organization organization;
        OrganizationalUnit? lastOu;

        try
        {
            organization = await GetOrganizationAsync(hostConfiguration.Subject.Organization);
            lastOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organization.NodeId);
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);
            await notificationService.SendNotificationAsync($"Host information update failed: {ex.Message} for host {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress})");
            return false;
        }

        try
        {
            var computer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, lastOu.NodeId);
            await databaseService.UpdateComputerAsync(computer, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);

            Log.Information("Host information updated: {HostName} ({MacAddress})", hostConfiguration.Host.Name, hostConfiguration.Host.MacAddress);
            await notificationService.SendNotificationAsync($"Host information updated: {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress})");

            return true;
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);
            await notificationService.SendNotificationAsync($"Host information update failed: {ex.Message} for host {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress})");

            return false;
        }
    }

    /// <summary>
    /// Checks if a host with the specified configuration is registered.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <returns>True if the host is registered, otherwise false.</returns>
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
            await notificationService.SendNotificationAsync($"Error while checking registration of the host {hostConfiguration.Host.Name} ({hostConfiguration.Host.MacAddress}): {ex.Message}");

            return false;
        }
    }

    /// <summary>
    /// Retrieves the public key.
    /// </summary>
    /// <returns>The public key as a byte array if found, otherwise null.</returns>
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
            await notificationService.SendNotificationAsync($"Error while reading public key file: {ex.Message}");

            return null;
        }
    }

    /// <summary>
    /// Retrieves the list of host move requests.
    /// </summary>
    /// <returns>The list of host move requests.</returns>
    private static async Task<List<HostMoveRequest>> GetHostMoveRequestsAsync()
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var hostMoveRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");

        if (File.Exists(hostMoveRequestsFilePath))
        {
            var json = await File.ReadAllTextAsync(hostMoveRequestsFilePath);

            return JsonSerializer.Deserialize<List<HostMoveRequest>>(json) ?? new List<HostMoveRequest>();
        }

        return [];
    }

    /// <summary>
    /// Saves the list of host move requests.
    /// </summary>
    /// <param name="hostMoveRequests">The list of host move requests.</param>
    private static async Task SaveHostMoveRequestsAsync(List<HostMoveRequest> hostMoveRequests)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var hostMoveRequestsFilePath = Path.Combine(programDataPath, "RemoteMaster", "Server", "HostMoveRequests.json");
        var updatedJson = JsonSerializer.Serialize(hostMoveRequests);

        await File.WriteAllTextAsync(hostMoveRequestsFilePath, updatedJson);
    }

    /// <summary>
    /// Retrieves a host move request by MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host.</param>
    /// <returns>The host move request if found, otherwise null.</returns>
    public async Task<HostMoveRequest?> GetHostMoveRequest(string macAddress)
    {
        var hostMoveRequests = await GetHostMoveRequestsAsync();

        return hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Acknowledges a host move request by removing it from the list.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host.</param>
    public async Task AcknowledgeMoveRequest(string macAddress)
    {
        var hostMoveRequests = await GetHostMoveRequestsAsync();
        var requestToRemove = hostMoveRequests.FirstOrDefault(r => r.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));

        if (requestToRemove != null)
        {
            hostMoveRequests.Remove(requestToRemove);
            await SaveHostMoveRequestsAsync(hostMoveRequests);

            Log.Information("Acknowledged move request for host with MAC address: {MacAddress}", macAddress);
            await notificationService.SendNotificationAsync($"Acknowledged move request for host with MAC address: {macAddress}");
        }
    }

    /// <summary>
    /// Issues a certificate based on the provided CSR bytes.
    /// </summary>
    /// <param name="csrBytes">The CSR bytes.</param>
    /// <returns>True if the certificate is issued successfully, otherwise false.</returns>
    public async Task<bool> IssueCertificateAsync(byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(csrBytes, nameof(csrBytes));

        try
        {
            var certificate = certificateService.IssueCertificate(csrBytes);
            await Clients.Caller.ReceiveCertificate(certificate.Export(X509ContentType.Pfx));

            Log.Information("Certificate issued successfully.");
            await notificationService.SendNotificationAsync($"Certificate issued successfully.");

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while issuing certificate.");
            await notificationService.SendNotificationAsync($"Error while issuing certificate: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    /// Retrieves the CA certificate.
    /// </summary>
    /// <returns>True if the CA certificate is retrieved successfully, otherwise false.</returns>
    public async Task<bool> GetCaCertificateAsync()
    {
        try
        {
            var caCertificatePublicPart = caCertificateService.GetCaCertificate(X509ContentType.Cert);

            if (caCertificatePublicPart != null)
            {
                await Clients.Caller.ReceiveCertificate(caCertificatePublicPart.RawData);

                Log.Information("CA certificate retrieved successfully.");
                await notificationService.SendNotificationAsync("CA certificate retrieved successfully.");

                return true;
            }
            else
            {
                Log.Warning("CA certificate retrieval failed.");
                await notificationService.SendNotificationAsync("CA certificate retrieval failed.");

                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while sending CA certificate public part.");
            await notificationService.SendNotificationAsync($"Error while sending CA certificate public part: {ex.Message}");

            return false;
        }
    }
}
