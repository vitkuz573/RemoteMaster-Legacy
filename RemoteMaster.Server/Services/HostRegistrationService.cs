// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostRegistrationService(IDatabaseService databaseService, INotificationService notificationService) : IHostRegistrationService
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
            await notificationService.SendNotificationAsync($"Error while checking registration of the host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`): {ex.Message}");

            return false;
        }
    }

    /// <summary>
    /// Registers a host with the specified configuration.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <returns>True if registration is successful, otherwise false.</returns>
    public async Task<Guid?> RegisterHostAsync(HostConfiguration hostConfiguration)
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
            await notificationService.SendNotificationAsync($"Host registration failed: {ex.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
            
            return null;
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, parentOu.NodeId);
            
            await databaseService.UpdateComputerAsync(existingComputer, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);
            await notificationService.SendNotificationAsync($"Host registration successful: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");

            return existingComputer.NodeId;
        }
        catch (InvalidOperationException)
        {
            var computer = new Computer
            {
                Name = hostConfiguration.Host.Name,
                IpAddress = hostConfiguration.Host.IpAddress,
                MacAddress = hostConfiguration.Host.MacAddress,
                Parent = parentOu
            };

            await notificationService.SendNotificationAsync($"New host registered: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");

            return await databaseService.AddNodeAsync(computer);
        }
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
            await notificationService.SendNotificationAsync($"Host unregistration failed: {ex.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
            
            return false;
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, lastOu.NodeId);
            await databaseService.RemoveNodeAsync(existingComputer);

            await notificationService.SendNotificationAsync($"Host unregistered: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) from organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' in organization '{hostConfiguration.Subject.Organization}'");

            return true;
        }
        catch (InvalidOperationException ex)
        {
            await notificationService.SendNotificationAsync($"Host unregistration failed: {ex.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) from organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' in organization '{hostConfiguration.Subject.Organization}'");

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
            await notificationService.SendNotificationAsync($"Host information update failed: {ex.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
            
            return false;
        }

        try
        {
            var computer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, lastOu.NodeId);
            await databaseService.UpdateComputerAsync(computer, hostConfiguration.Host.IpAddress, hostConfiguration.Host.Name);

            await notificationService.SendNotificationAsync($"Host information updated: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");

            return true;
        }
        catch (InvalidOperationException ex)
        {
            await notificationService.SendNotificationAsync($"Host information update failed: {ex.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");

            return false;
        }
    }
}
