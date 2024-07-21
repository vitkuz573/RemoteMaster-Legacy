// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostRegistrationService(IDatabaseService databaseService, IEventNotificationService eventNotificationService) : IHostRegistrationService
{
    private async Task<Organization> GetOrganizationAsync(string organizationName)
    {
        var organizationsResult = await databaseService.GetNodesAsync<Organization>(n => n.Name == organizationName);

        if (!organizationsResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to retrieve organizations: {organizationsResult.Errors.FirstOrDefault()?.Message}");
        }

        var organization = organizationsResult.Value.FirstOrDefault();

        return organization ?? throw new InvalidOperationException($"Organization '{organizationName}' not found.");
    }

    private async Task<OrganizationalUnit?> ResolveOrganizationalUnitHierarchyAsync(IEnumerable<string> ouNames, Guid organizationId)
    {
        OrganizationalUnit? parentOu = null;

        var organizationResult = await databaseService.GetNodesAsync<Organization>(n => n.NodeId == organizationId);

        if (!organizationResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to retrieve organization: {organizationResult.Errors.FirstOrDefault()?.Message}");
        }

        var organizationName = organizationResult.Value.FirstOrDefault()?.Name ?? "Unknown";

        foreach (var ouName in ouNames)
        {
            var ousResult = await databaseService.GetNodesAsync<OrganizationalUnit>(n => n.Name == ouName && n.OrganizationId == organizationId);

            if (!ousResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to retrieve organizational units: {ousResult.Errors.FirstOrDefault()?.Message}");
            }

            var ou = ousResult.Value.FirstOrDefault(o => parentOu == null || o.ParentId == parentOu.NodeId) ?? throw new InvalidOperationException($"Organizational Unit '{ouName}' not found in organization '{organizationName}'.");

            parentOu = ou;
        }

        return parentOu;
    }

    private async Task<Computer> GetComputerByMacAddressAsync(string macAddress, Guid parentOuId)
    {
        var existingComputersResult = await databaseService.GetNodesAsync<Computer>(c => c.ParentId == parentOuId);

        if (!existingComputersResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to retrieve computers: {existingComputersResult.Errors.FirstOrDefault()?.Message}");
        }

        var computer = existingComputersResult.Value.FirstOrDefault(c => c.MacAddress == macAddress) ?? throw new InvalidOperationException($"Computer with MAC address '{macAddress}' not found.");

        return computer;
    }

    public async Task<Result<bool>> IsHostRegisteredAsync(string macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        try
        {
            var computersResult = await databaseService.GetNodesAsync<Computer>(n => n.MacAddress == macAddress);

            if (!computersResult.IsSuccess)
            {
                await eventNotificationService.SendNotificationAsync($"Error while checking registration of the host with MAC address `{macAddress}`: {computersResult.Errors.FirstOrDefault()?.Message}");
                
                return Result<bool>.Failure($"Error while checking registration of the host with MAC address `{macAddress}`: {computersResult.Errors.FirstOrDefault()?.Message}");
            }

            return Result<bool>.Success(computersResult.Value.Any());
        }
        catch (Exception ex)
        {
            await eventNotificationService.SendNotificationAsync($"Error while checking registration of the host with MAC address `{macAddress}`: {ex.Message}");
            
            return Result<bool>.Failure($"Error while checking registration of the host with MAC address `{macAddress}`: {ex.Message}");
        }
    }

    public async Task<Result<bool>> RegisterHostAsync(HostConfiguration hostConfiguration)
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
            
            return Result<bool>.Failure($"Host registration failed: {ex.Message}");
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, parentOu.NodeId);

            var updateResult = await databaseService.UpdateNodeAsync(existingComputer, computer =>
            {
                computer.IpAddress = hostConfiguration.Host.IpAddress;
                computer.Name = hostConfiguration.Host.Name;
            });

            if (!updateResult.IsSuccess)
            {
                await eventNotificationService.SendNotificationAsync($"Failed to update existing computer: {updateResult.Errors.FirstOrDefault()?.Message}");
                
                return Result<bool>.Failure($"Failed to update existing computer: {updateResult.Errors.FirstOrDefault()?.Message}");
            }

            await eventNotificationService.SendNotificationAsync($"Host registration successful: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
            
            return Result<bool>.Success(true);
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

            var addResult = await databaseService.AddNodesAsync(new List<Computer> { computer });

            if (!addResult.IsSuccess)
            {
                await eventNotificationService.SendNotificationAsync($"Failed to add new computer: {addResult.Errors.FirstOrDefault()?.Message}");
                
                return Result<bool>.Failure($"Failed to add new computer: {addResult.Errors.FirstOrDefault()?.Message}");
            }

            await eventNotificationService.SendNotificationAsync($"New host registered: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'");
            
            return Result<bool>.Success(true);
        }
    }

    public async Task<Result<bool>> UnregisterHostAsync(HostUnregisterRequest request)
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
            
            return Result<bool>.Failure($"Host unregister failed: {ex.Message}");
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(request.MacAddress, lastOu.NodeId);
            var removeResult = await databaseService.RemoveNodesAsync(new List<Computer> { existingComputer });

            if (!removeResult.IsSuccess)
            {
                await eventNotificationService.SendNotificationAsync($"Failed to remove existing computer: {removeResult.Errors.FirstOrDefault()?.Message}");
                
                return Result<bool>.Failure($"Failed to remove existing computer: {removeResult.Errors.FirstOrDefault()?.Message}");
            }

            await eventNotificationService.SendNotificationAsync($"Host unregistered: {request.Name} (`{request.MacAddress}`) from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'");
            
            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            await eventNotificationService.SendNotificationAsync($"Host unregister failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'");
            
            return Result<bool>.Failure($"Host unregister failed: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UpdateHostInformationAsync(HostUpdateRequest request)
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
            
            return Result<bool>.Failure($"Host information update failed: {ex.Message}");
        }

        try
        {
            var computer = await GetComputerByMacAddressAsync(request.MacAddress, lastOu.NodeId);

            var updateResult = await databaseService.UpdateNodeAsync(computer, updatedComputer =>
            {
                updatedComputer.IpAddress = request.IpAddress;
                updatedComputer.Name = request.Name;
            });

            if (!updateResult.IsSuccess)
            {
                await eventNotificationService.SendNotificationAsync($"Failed to update existing computer: {updateResult.Errors.FirstOrDefault()?.Message}");
                
                return Result<bool>.Failure($"Failed to update existing computer: {updateResult.Errors.FirstOrDefault()?.Message}");
            }

            await eventNotificationService.SendNotificationAsync($"Host information updated: {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'");
            
            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            await eventNotificationService.SendNotificationAsync($"Host information update failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'");
            
            return Result<bool>.Failure($"Host information update failed: {ex.Message}");
        }
    }
}
