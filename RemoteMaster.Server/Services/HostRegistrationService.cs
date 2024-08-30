// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Entities;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostRegistrationService(IEventNotificationService eventNotificationService, IOrganizationRepository organizationRepository, IOrganizationalUnitRepository organizationalUnitRepository) : IHostRegistrationService
{
    private async Task<Result<Organization>> GetOrganizationAsync(string organizationName)
    {
        var organizations = await organizationRepository.FindAsync(n => n.Name == organizationName);

        var organization = organizations.FirstOrDefault();

        return organization != null
            ? Result.Ok(organization)
            : Result.Fail<Organization>($"Organization '{organizationName}' not found.");
    }

    private async Task<Result<OrganizationalUnit?>> ResolveOrganizationalUnitHierarchyAsync(IEnumerable<string> ouNames, Guid organizationId)
    {
        OrganizationalUnit? parentOu = null;

        foreach (var ouName in ouNames)
        {
            var ous = await organizationalUnitRepository.FindAsync(n => n.Name == ouName && n.OrganizationId == organizationId);

            var ou = ous.FirstOrDefault(o => parentOu == null || o.ParentId == parentOu?.Id);

            if (ou == null)
            {
                return Result.Fail<OrganizationalUnit?>($"Organizational Unit '{ouName}' not found in the specified hierarchy.");
            }

            parentOu = ou;
        }

        return Result.Ok(parentOu);
    }

    public async Task<Result> IsHostRegisteredAsync(string macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        try
        {
            var computers = await organizationalUnitRepository.FindComputersAsync(c => c.MacAddress == macAddress);

            if (computers.Any())
            {
                return Result.Ok();
            }

            var errorMessage = $"Host with MAC address `{macAddress}` is not registered.";
           
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

        var parentUnit = parentOuResult.Value ?? throw new InvalidOperationException("Parent Organizational Unit not found.");

        try
        {
            var computer = parentUnit.Computers.FirstOrDefault(c => c.MacAddress == hostConfiguration.Host.MacAddress);

            if (computer != null)
            {
                var updateResult = await UpdateComputerAsync(computer, hostConfiguration);

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

            parentUnit.AddComputer(hostConfiguration.Host.Name, hostConfiguration.Host.IpAddress, hostConfiguration.Host.MacAddress);

            await organizationalUnitRepository.UpdateAsync(parentUnit);
            await organizationalUnitRepository.SaveChangesAsync();

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

    private async Task<Result> UpdateComputerAsync(Computer computer, HostConfiguration hostConfiguration)
    {
        computer.SetName(hostConfiguration.Host.Name);
        computer.SetIpAddress(hostConfiguration.Host.IpAddress);

        var organizationalUnit = computer.Parent;

        await organizationalUnitRepository.UpdateAsync(organizationalUnit);
        await organizationalUnitRepository.SaveChangesAsync();

        return Result.Ok();
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
            var parentUnit = parentOuResult.Value;
            var computer = parentUnit.Computers.FirstOrDefault(c => c.MacAddress == request.MacAddress);

            if (computer == null)
            {
                var errorMessage = $"Failed to find the computer: Computer with MAC address '{request.MacAddress}' not found in organizational unit '{parentUnit.Name}'.";

                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            parentUnit.RemoveComputer(computer.Id);

            await organizationalUnitRepository.UpdateAsync(parentUnit);
            await organizationalUnitRepository.SaveChangesAsync();

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
            var parentUnit = parentOuResult.Value;
            var computer = parentUnit.Computers.FirstOrDefault(c => c.MacAddress == request.MacAddress);

            if (computer == null)
            {
                var errorMessage = $"Failed to find the computer: Computer with MAC address '{request.MacAddress}' not found in organizational unit '{parentUnit.Name}'.";

                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            computer.SetIpAddress(request.IpAddress);

            await organizationalUnitRepository.UpdateAsync(parentUnit);
            await organizationalUnitRepository.SaveChangesAsync();

            var successMessage = $"Host information updated: {computer.Name} (`{computer.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";

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
