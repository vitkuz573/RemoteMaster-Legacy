// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Shared.Models;
using Host = RemoteMaster.Server.Aggregates.OrganizationAggregate.Host;

namespace RemoteMaster.Server.Services;

public class HostRegistrationService(IEventNotificationService eventNotificationService, IOrganizationRepository organizationRepository) : IHostRegistrationService
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

        var organization = await organizationRepository.GetByIdAsync(organizationId);

        if (organization == null)
        {
            return Result.Fail<OrganizationalUnit?>($"Organization with ID '{organizationId}' not found.");
        }

        foreach (var ouName in ouNames)
        {
            var ou = organization.OrganizationalUnits.FirstOrDefault(n => n.Name == ouName && (parentOu == null || n.ParentId == parentOu.Id));

            if (ou == null)
            {
                return Result.Fail<OrganizationalUnit?>($"Organizational Unit '{ouName}' not found in the specified hierarchy.");
            }

            parentOu = ou;
        }

        return Result.Ok(parentOu);
    }

    public async Task<Result> IsHostRegisteredAsync(PhysicalAddress macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        try
        {
            var computers = await organizationRepository.FindComputersAsync(c => c.MacAddress.GetAddressBytes().SequenceEqual(macAddress.GetAddressBytes()));

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

        if (hostConfiguration.Host == null)
        {
            throw new InvalidOperationException("Host information must be provided during the registration process.");
        }

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
            var computer = parentUnit.Hosts.FirstOrDefault(c => c.MacAddress.GetAddressBytes().SequenceEqual(hostConfiguration.Host.MacAddress.GetAddressBytes()));

            if (computer != null)
            {
                var updateResult = await UpdateComputerAsync(computer, hostConfiguration);
                
                if (updateResult.IsFailed)
                {
                    var errorMessage = $"Failed to update existing host: {updateResult.Errors.FirstOrDefault()?.Message}";
                    
                    await eventNotificationService.SendNotificationAsync(errorMessage);
                    
                    return Result.Fail(errorMessage);
                }

                var updateSuccessMessage = $"Host registration successful: {hostConfiguration.Host.Name} (`{hostConfiguration.Host.MacAddress}`) in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' of organization '{hostConfiguration.Subject.Organization}'";
                
                await eventNotificationService.SendNotificationAsync(updateSuccessMessage);
                
                return Result.Ok();
            }

            parentUnit.AddComputer(hostConfiguration.Host.Name, hostConfiguration.Host.IpAddress, hostConfiguration.Host.MacAddress);

            await organizationRepository.UpdateAsync(organizationResult.Value);
            await organizationRepository.SaveChangesAsync();

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

    private async Task<Result> UpdateComputerAsync(Host host, HostConfiguration hostConfiguration)
    {
        if (hostConfiguration.Host == null)
        {
            throw new InvalidOperationException("Host information must be provided during the update process.");
        }

        host.SetName(hostConfiguration.Host.Name);
        host.SetIpAddress(hostConfiguration.Host.IpAddress);

        var organization = host.Parent.Organization;

        await organizationRepository.UpdateAsync(organization);
        await organizationRepository.SaveChangesAsync();

        return Result.Ok();
    }

    public async Task<Result> UnregisterHostAsync(HostUnregisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

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
            var parentUnit = parentOuResult.Value ?? throw new InvalidOperationException("Parent Organizational Unit not found.");
            var computer = parentUnit.Hosts.FirstOrDefault(c => c.MacAddress.GetAddressBytes().SequenceEqual(request.MacAddress.GetAddressBytes()));

            if (computer == null)
            {
                var errorMessage = $"Failed to find the host: Host with MAC address '{request.MacAddress}' not found in organizational unit '{parentUnit.Name}'.";

                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            await organizationRepository.RemoveComputerAsync(organizationResult.Value.Id, parentUnit.Id, computer.Id);
            await organizationRepository.SaveChangesAsync();

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
            var parentUnit = parentOuResult.Value ?? throw new InvalidOperationException("Parent Organizational Unit not found.");
            var computer = parentUnit.Hosts.FirstOrDefault(c => c.MacAddress.GetAddressBytes().SequenceEqual(request.MacAddress.GetAddressBytes()));

            if (computer == null)
            {
                var errorMessage = $"Failed to find the host: Host with MAC address '{request.MacAddress}' not found in organizational unit '{parentUnit.Name}'.";

                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            computer.SetName(request.Name);
            computer.SetIpAddress(request.IpAddress);

            await organizationRepository.UpdateAsync(organizationResult.Value);
            await organizationRepository.SaveChangesAsync();

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
