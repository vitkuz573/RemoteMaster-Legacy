// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Entities;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostRegistrationService(INodesService nodesService, IEventNotificationService eventNotificationService) : IHostRegistrationService
{
    private async Task<Result<Organization>> GetOrganizationAsync(string organizationName)
    {
        var organizationsResult = await nodesService.GetNodesAsync<Organization>(n => n.Name == organizationName);

        if (organizationsResult.IsFailed)
        {
            return Result.Fail<Organization>($"Failed to retrieve organizations: {organizationsResult.Errors.FirstOrDefault()?.Message}");
        }

        var organization = organizationsResult.Value.FirstOrDefault();

        return organization != null
            ? Result.Ok(organization)
            : Result.Fail<Organization>($"Organization '{organizationName}' not found.");
    }

    private async Task<Result<OrganizationalUnit?>> ResolveOrganizationalUnitHierarchyAsync(IEnumerable<string> ouNames, Guid organizationId)
    {
        OrganizationalUnit? parentOu = null;

        foreach (var ouName in ouNames)
        {
            var ousResult = await nodesService.GetNodesAsync<OrganizationalUnit>(n => n.Name == ouName && n.OrganizationId == organizationId);

            if (ousResult.IsFailed)
            {
                return Result.Fail<OrganizationalUnit?>($"Failed to retrieve organizational units: {ousResult.Errors.FirstOrDefault()?.Message}");
            }

            var ou = ousResult.Value.FirstOrDefault(o => parentOu == null || o.ParentId == parentOu.Id);

            if (ou == null)
            {
                return Result.Fail<OrganizationalUnit?>($"Organizational Unit '{ouName}' not found in the specified hierarchy.");
            }

            parentOu = ou;
        }

        return Result.Ok(parentOu);
    }

    private async Task<Result<Computer>> GetComputerByMacAddressAsync(string macAddress, Guid parentOuId)
    {
        var existingComputersResult = await nodesService.GetNodesAsync<Computer>(c => c.ParentId == parentOuId);

        if (existingComputersResult.IsFailed)
        {
            return Result.Fail<Computer>($"Failed to retrieve computers: {existingComputersResult.Errors.FirstOrDefault()?.Message}");
        }

        var computer = existingComputersResult.Value.FirstOrDefault(c => c.MacAddress == macAddress);

        return computer != null
            ? Result.Ok(computer)
            : Result.Fail<Computer>($"Computer with MAC address '{macAddress}' not found.");
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
                    ? Result.Ok()
                    : Result.Fail($"Host with MAC address `{macAddress}` is not registered.");
            }

            var errorMessage = $"Error while checking registration of the host with MAC address `{macAddress}`: {computersResult.Errors.FirstOrDefault()?.Message}";
            
            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error while checking registration of the host with MAC address `{macAddress}`: {ex.Message}";
            
            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }
    }

    public async Task<Result> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);

        var organizationResult = await GetOrganizationAsync(hostConfiguration.Subject.Organization);

        if (organizationResult.IsFailed)
        {
            var errorMessage = $"Host registration failed: {organizationResult.Errors.FirstOrDefault()?.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";

            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }

        var parentOuResult = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organizationResult.Value.Id);

        if (parentOuResult.IsFailed)
        {
            var errorMessage = $"Host registration failed: {parentOuResult.Errors.FirstOrDefault()?.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";

            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }

        try
        {
            var computerResult = await GetComputerByMacAddressAsync(hostConfiguration.Host.MacAddress, parentOuResult.Value.Id);

            if (computerResult.IsSuccess)
            {
                var updateResult = await nodesService.UpdateNodeAsync(computerResult.Value, computer =>
                {
                    computer.Name = hostConfiguration.Host.Name;
                    computer.IpAddress = hostConfiguration.Host.IpAddress;
                });

                if (updateResult.IsFailed)
                {
                    var errorMessage = $"Failed to update existing computer: {updateResult.Errors.FirstOrDefault()?.Message}";

                    await eventNotificationService.SendNotificationAsync(errorMessage);

                    return Result.Fail(errorMessage);
                }

                var updateSuccessMessage = $"Host registration successful: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";

                await eventNotificationService.SendNotificationAsync(updateSuccessMessage);

                return Result.Ok();
            }

            var computer = new Computer
            {
                Name = hostConfiguration.Host.Name,
                IpAddress = hostConfiguration.Host.IpAddress,
                MacAddress = hostConfiguration.Host.MacAddress,
                ParentId = parentOuResult.Value.Id
            };

            var addResult = await nodesService.AddNodesAsync(new List<Computer> { computer });

            if (addResult.IsFailed)
            {
                var errorMessage = $"Failed to add new computer: {addResult.Errors.FirstOrDefault()?.Message}";
                
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Fail(errorMessage);
            }

            var successMessage = $"New host registered: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";

            await eventNotificationService.SendNotificationAsync(successMessage);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Host registration failed: {ex.Message} for host {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";

            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }
    }

    public async Task<Result> UnregisterHostAsync(HostUnregisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.MacAddress))
        {
            return Result.Fail("Request must have a valid MAC address.");
        }

        var organizationResult = await GetOrganizationAsync(request.Organization);
        
        if (organizationResult.IsFailed)
        {
            var errorMessage = $"Host unregister failed: {organizationResult.Errors.FirstOrDefault()?.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Fail(errorMessage);
        }

        var parentOuResult = await ResolveOrganizationalUnitHierarchyAsync(request.OrganizationalUnit, organizationResult.Value.Id);
        
        if (parentOuResult.IsFailed)
        {
            var errorMessage = $"Host unregister failed: {parentOuResult.Errors.FirstOrDefault()?.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Fail(errorMessage);
        }

        try
        {
            var computerResult = await GetComputerByMacAddressAsync(request.MacAddress, parentOuResult.Value.Id);

            if (computerResult.IsFailed)
            {
                var errorMessage = $"Failed to find the computer: {computerResult.Errors.FirstOrDefault()?.Message}";
                
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Fail(errorMessage);
            }

            var removeResult = await nodesService.RemoveNodesAsync(new List<Computer> { computerResult.Value });

            if (removeResult.IsFailed)
            {
                var errorMessage = $"Failed to remove existing computer: {removeResult.Errors.FirstOrDefault()?.Message}";
                
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Fail(errorMessage);
            }

            var successMessage = $"Host unregistered: {request.Name} (`{request.MacAddress}`) from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'";
            
            await eventNotificationService.SendNotificationAsync(successMessage);
            
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Host unregister failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'";
            
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Fail(errorMessage);
        }
    }

    public async Task<Result> UpdateHostInformationAsync(HostUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var organizationResult = await GetOrganizationAsync(request.Organization);
        
        if (organizationResult.IsFailed)
        {
            var errorMessage = $"Host information update failed: {organizationResult.Errors.FirstOrDefault()?.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Fail(errorMessage);
        }

        var parentOuResult = await ResolveOrganizationalUnitHierarchyAsync(request.OrganizationalUnit, organizationResult.Value.Id);
        
        if (parentOuResult.IsFailed)
        {
            var errorMessage = $"Host information update failed: {parentOuResult.Errors.FirstOrDefault()?.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            
            await eventNotificationService.SendNotificationAsync(errorMessage);
            
            return Result.Fail(errorMessage);
        }

        try
        {
            var computerResult = await GetComputerByMacAddressAsync(request.MacAddress, parentOuResult.Value.Id);

            if (computerResult.IsFailed)
            {
                var errorMessage = $"Failed to find the computer: {computerResult.Errors.FirstOrDefault()?.Message}";
                
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Fail(errorMessage);
            }

            var updateResult = await nodesService.UpdateNodeAsync(computerResult.Value, updatedComputer =>
            {
                updatedComputer.Name = request.Name;
                updatedComputer.IpAddress = request.IpAddress;
            });

            if (updateResult.IsFailed)
            {
                var errorMessage = $"Failed to update existing computer: {updateResult.Errors.FirstOrDefault()?.Message}";
                
                await eventNotificationService.SendNotificationAsync(errorMessage);
                
                return Result.Fail(errorMessage);
            }

            var successMessage = $"Host information updated: {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            
            await eventNotificationService.SendNotificationAsync(successMessage);
            
            return Result.Ok();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Host information update failed: {ex.Message} for host {request.Name} (`{request.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";
            
            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }
    }
}
