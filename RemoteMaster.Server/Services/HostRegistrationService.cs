// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Entities;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostRegistrationService(INodesService nodesService, IEventNotificationService eventNotificationService) : IHostRegistrationService
{
    private async Task<Organization> GetOrganizationAsync(string organizationName)
    {
        var organizationsResult = await nodesService.GetNodesAsync<Organization>(n => n.Name == organizationName);

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

        var organizationResult = await nodesService.GetNodesAsync<Organization>(n => n.Id == organizationId);

        if (!organizationResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to retrieve organization: {organizationResult.Errors.FirstOrDefault()?.Message}");
        }

        var organizationName = organizationResult.Value.FirstOrDefault()?.Name ?? "Unknown";

        foreach (var ouName in ouNames)
        {
            var ousResult = await nodesService.GetNodesAsync<OrganizationalUnit>(n => n.Name == ouName && n.OrganizationId == organizationId);

            if (!ousResult.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to retrieve organizational units: {ousResult.Errors.FirstOrDefault()?.Message}");
            }

            var ou = ousResult.Value.FirstOrDefault(o => parentOu == null || o.ParentId == parentOu.Id) ?? throw new InvalidOperationException($"Organizational Unit '{ouName}' not found in organization '{organizationName}'.");

            parentOu = ou;
        }

        return parentOu;
    }

    private async Task<Computer> GetComputerByMacAddressAsync(string macAddress, Guid parentOuId)
    {
        var existingComputersResult = await nodesService.GetNodesAsync<Computer>(c => c.ParentId == parentOuId);

        if (!existingComputersResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to retrieve computers: {existingComputersResult.Errors.FirstOrDefault()?.Message}");
        }

        var computer = existingComputersResult.Value.FirstOrDefault(c => c.MacAddress == macAddress) ?? throw new InvalidOperationException($"Computer with MAC address '{macAddress}' not found.");

        return computer;
    }

    public async Task<Result> IsHostRegisteredAsync(string macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        try
        {
            var computersResult = await nodesService.GetNodesAsync<Computer>(n => n.MacAddress == macAddress);

            if (computersResult.IsSuccess)
            {
                return computersResult.Value.Any()
                    ? Result.Success()
                    : Result.Failure($"Host with MAC address `{macAddress}` is not registered.");
            }

            var errorMessage = $"Error while checking registration of the host with MAC address `{macAddress}`: {computersResult.Errors.FirstOrDefault()?.Message}";
            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Failure(errorMessage);

        }
        catch (Exception ex)
        {
            var errorMessage = $"Error while checking registration of the host with MAC address `{macAddress}`: {ex.Message}";
            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Failure(errorMessage);
        }
    }

    public async Task<Result> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        OrganizationalUnit? parentOu;

        try
        {
            var organization = await GetOrganizationAsync(hostConfiguration.Subject.Organization);
            parentOu = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organization.Id);
        }
        catch (InvalidOperationException ex)
        {
            var errorMessage = $"Host registration failed: {ex.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Failure(errorMessage);
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, parentOu.Id);

            var updateResult = await nodesService.UpdateNodeAsync(existingComputer, computer =>
            {
                computer.Name = hostConfiguration.Host.Name;
                computer.IpAddress = hostConfiguration.Host.IpAddress;
            });

            if (!updateResult.IsSuccess)
            {
                var errorMessage = $"Failed to update existing computer: {updateResult.Errors.FirstOrDefault()?.Message}";
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Failure(errorMessage);
            }

            var successMessage = $"Host registration successful: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";
            await eventNotificationService.SendNotificationAsync(successMessage);
            
            return Result.Success();
        }
        catch (InvalidOperationException)
        {
            var computer = new Computer
            {
                Name = hostConfiguration.Host.Name,
                IpAddress = hostConfiguration.Host.IpAddress,
                MacAddress = hostConfiguration.Host.MacAddress,
                ParentId = parentOu.Id
            };

            var addResult = await nodesService.AddNodesAsync(new List<Computer> { computer });

            if (!addResult.IsSuccess)
            {
                var errorMessage = $"Failed to add new computer: {addResult.Errors.FirstOrDefault()?.Message}";
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Failure(errorMessage);
            }

            var successMessage = $"New host registered: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";
            await eventNotificationService.SendNotificationAsync(successMessage);
            
            return Result.Success();
        }
    }

    public async Task<Result> UnregisterHostAsync(HostUnregisterRequest request)
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
            lastOu = await ResolveOrganizationalUnitHierarchyAsync(request.OrganizationalUnit, organizationEntity.Id);
        }
        catch (InvalidOperationException ex)
        {
            var errorMessage = $"Host unregister failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Failure(errorMessage);
        }

        try
        {
            var existingComputer = await GetComputerByMacAddressAsync(request.MacAddress, lastOu.Id);
            var removeResult = await nodesService.RemoveNodesAsync(new List<Computer> { existingComputer });

            if (!removeResult.IsSuccess)
            {
                var errorMessage = $"Failed to remove existing computer: {removeResult.Errors.FirstOrDefault()?.Message}";
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Failure(errorMessage);
            }

            var successMessage = $"Host unregistered: {request.Name} (`{request.MacAddress}`) from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'";
            await eventNotificationService.SendNotificationAsync(successMessage);
            
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            var errorMessage = $"Host unregister failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'";
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Failure(errorMessage);
        }
    }

    public async Task<Result> UpdateHostInformationAsync(HostUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        OrganizationalUnit? lastOu;

        try
        {
            var organization = await GetOrganizationAsync(request.Organization);
            lastOu = await ResolveOrganizationalUnitHierarchyAsync(request.OrganizationalUnit, organization.Id);
        }
        catch (InvalidOperationException ex)
        {
            var errorMessage = $"Host information update failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Failure(errorMessage);
        }

        try
        {
            var computer = await GetComputerByMacAddressAsync(request.MacAddress, lastOu.Id);

            var updateResult = await nodesService.UpdateNodeAsync(computer, updatedComputer =>
            {
                updatedComputer.Name = request.Name;
                updatedComputer.IpAddress = request.IpAddress;
            });

            if (!updateResult.IsSuccess)
            {
                var errorMessage = $"Failed to update existing computer: {updateResult.Errors.FirstOrDefault()?.Message}";
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Failure(errorMessage);
            }

            var successMessage = $"Host information updated: {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            await eventNotificationService.SendNotificationAsync(successMessage);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            var errorMessage = $"Host information update failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Failure(errorMessage);
        }
    }
}
