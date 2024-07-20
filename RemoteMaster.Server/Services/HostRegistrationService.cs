// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostRegistrationService(IDatabaseService databaseService, IEventNotificationService eventNotificationService) : IHostRegistrationService
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
        var existingComputers = await databaseService.GetNodesAsync<Computer>(c => c.ParentId == parentOuId);
        var computer = existingComputers.FirstOrDefault(c => c.MacAddress == macAddress) ?? throw new InvalidOperationException($"Computer with MAC address '{macAddress}' not found.");

        return computer;
    }

    /// <summary>
    /// Checks if a host with the specified MAC address is registered.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host.</param>
    /// <returns>True if the host is registered, otherwise false.</returns>
    public async Task<bool> IsHostRegisteredAsync(string macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        try
        {
            var computers = await databaseService.GetNodesAsync<Computer>(n => n.MacAddress == macAddress);
            
            return computers.Any();
        }
        catch (Exception ex)
        {
            await eventNotificationService.SendNotificationAsync($"Error while checking registration of the host with MAC address `{macAddress}`: {ex.Message}");
            
            return false;
        }
    }

    /// <summary>
    /// Registers a host with the specified configuration.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <returns>True if registration is successful, otherwise false.</returns>
    public async Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        OrganizationalUnit? parentOu;

        try
        {
            var organization = await GetOrganizationAsync(hostConfiguration.Subject.Organization);
            parentOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organization.NodeId);
        }
        catch (InvalidOperationException ex)
        {
            await eventNotificationService.SendNotificationAsync($"Host registration failed: {ex.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");

            return false;
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, parentOu.NodeId);

            await databaseService.UpdateNodeAsync(existingComputer, computer =>
            {
                computer.IpAddress = hostConfiguration.Host.IpAddress;
                computer.Name = hostConfiguration.Host.Name;
            });

            await eventNotificationService.SendNotificationAsync($"Host registration successful: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");

            return true;
        }
        catch (InvalidOperationException)
        {
            var computer = new Computer
            {
                Name = hostConfiguration.Host.Name,
                IpAddress = hostConfiguration.Host.IpAddress,
                MacAddress = hostConfiguration.Host.MacAddress,
                ParentId = parentOu.NodeId
            };

            await eventNotificationService.SendNotificationAsync($"New host registered: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");

            await databaseService.AddNodeAsync(computer);

            return true;
        }
    }

    /// <summary>
    /// Unregisters a host with the specified request.
    /// </summary>
    /// <param name="request">The host unregister request containing the necessary configuration.</param>
    /// <returns>True if unregister is successful, otherwise false.</returns>
    /// <exception cref="ArgumentException">Thrown if the host unregister request is invalid.</exception>
    public async Task<bool> UnregisterHostAsync(HostUnregisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.MacAddress))
        {
            throw new ArgumentException("Request must have a valid MAC address.", nameof(request));
        }

        OrganizationalUnit? lastOu;

        try
        {
            var organizationEntity = await GetOrganizationAsync(request.Organization);
            lastOu = await ResolveOrganizationalUnitHierarchyAsync(request.OrganizationalUnit, organizationEntity.NodeId);
        }
        catch (InvalidOperationException ex)
        {
            await eventNotificationService.SendNotificationAsync($"Host unregister failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'");
            
            return false;
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(request.MacAddress, lastOu.NodeId);
            await databaseService.RemoveNodeAsync(existingComputer);

            await eventNotificationService.SendNotificationAsync($"Host unregistered: {request.Name} (`{request.MacAddress}`) from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'");
            
            return true;
        }
        catch (InvalidOperationException ex)
        {
            await eventNotificationService.SendNotificationAsync($"Host unregister failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'");
            
            return false;
        }
    }

    /// <summary>
    /// Updates the information of a host with the specified request.
    /// </summary>
    /// <param name="request">The host update request containing the necessary configuration.</param>
    /// <returns>True if the update is successful, otherwise false.</returns>
    public async Task<bool> UpdateHostInformationAsync(HostUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        OrganizationalUnit? lastOu;

        try
        {
            var organization = await GetOrganizationAsync(request.Organization);
            lastOu = await ResolveOrganizationalUnitHierarchyAsync(request.OrganizationalUnit, organization.NodeId);
        }
        catch (InvalidOperationException ex)
        {
            await eventNotificationService.SendNotificationAsync($"Host information update failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'");

            return false;
        }

        try
        {
            var computer = await GetComputerByMacAddressAsync(request.MacAddress, lastOu.NodeId);
            
            await databaseService.UpdateNodeAsync(computer, updatedComputer =>
            {
                updatedComputer.IpAddress = request.IpAddress;
                updatedComputer.Name = request.Name;
            });

            await eventNotificationService.SendNotificationAsync($"Host information updated: {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'");

            return true;
        }
        catch (InvalidOperationException ex)
        {
            await eventNotificationService.SendNotificationAsync($"Host information update failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'");

            return false;
        }
    }
}
