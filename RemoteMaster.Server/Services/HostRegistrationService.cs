// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class HostRegistrationService(IEventNotificationService eventNotificationService, IApplicationUnitOfWork applicationUnitOfWork) : IHostRegistrationService
{
    private async Task<Result<Organization>> GetOrganizationAsync(string organizationName)
    {
        var organizations = await applicationUnitOfWork.Organizations.FindAsync(n => n.Name == organizationName);

        var organization = organizations.FirstOrDefault();

        return organization != null
            ? Result.Ok(organization)
            : Result.Fail<Organization>($"Organization '{organizationName}' not found.");
    }

    private async Task<Result<OrganizationalUnit?>> ResolveOrganizationalUnitHierarchyAsync(IEnumerable<string> ouNames, Guid organizationId)
    {
        var ouNamesList = ouNames.ToList();

        OrganizationalUnit? parentOu = null;
        OrganizationalUnit? currentOu = null;

        var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(organizationId);

        if (organization == null)
        {
            return Result.Fail<OrganizationalUnit?>($"Organization with ID '{organizationId}' not found.");
        }

        foreach (var ouName in ouNamesList)
        {
            currentOu = organization.OrganizationalUnits
                .FirstOrDefault(n => DoesOrganizationalUnitMatch(n, ouName, parentOu));

            if (currentOu == null)
            {
                var existingPath = ouNamesList.TakeWhile(IsMatchingOu);
                var existingPathString = string.Join(" > ", existingPath);
                var fullPath = string.Join(" > ", ouNamesList);

                return Result.Fail<OrganizationalUnit?>(
                    $"Organizational Unit '{ouName}' not found in the hierarchy '{fullPath}'. " +
                    $"Existing path: '{existingPathString}'."
                );
            }

            parentOu = currentOu;
        }

        return Result.Ok(currentOu);

        bool IsMatchingOu(string name) => organization.OrganizationalUnits.Any(ou => DoesOrganizationalUnitMatch(ou, name, parentOu));
    }

    private static bool DoesOrganizationalUnitMatch(OrganizationalUnit ou, string name, OrganizationalUnit? parentOu)
    {
        return ou.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && (parentOu == null || ou.ParentId == parentOu.Id);
    }

    public async Task<Result> IsHostRegisteredAsync(PhysicalAddress macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        try
        {
            var hosts = await applicationUnitOfWork.Organizations.FindHostsAsync(h => h.MacAddress.Equals(macAddress));

            if (hosts.Any())
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

        var existingHost = await applicationUnitOfWork.Organizations.FindHostsAsync(h => h.MacAddress.Equals(hostConfiguration.Host.MacAddress));

        if (existingHost.Any())
        {
            var errorMessage = $"Host with MAC address '{hostConfiguration.Host.MacAddress}' is already registered in the system.";
            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }

        var organizationResult = await GetOrganizationAsync(hostConfiguration.Subject.Organization);

        if (organizationResult.IsFailed)
        {
            var errorMessage = $"Host registration failed: {organizationResult.Errors.FirstOrDefault()?.Message} " +
                               $"for host '{hostConfiguration.Host.Name}' ('{hostConfiguration.Host.MacAddress}') " +
                               $"in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' " +
                               $"of organization '{hostConfiguration.Subject.Organization}'";

            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }

        var ouResult = await ResolveOrganizationalUnitHierarchyAsync(hostConfiguration.Subject.OrganizationalUnit, organizationResult.Value.Id);

        if (ouResult.IsFailed)
        {
            var errorMessage = $"Host registration failed: {ouResult.Errors.FirstOrDefault()?.Message} " +
                               $"for host '{hostConfiguration.Host.Name}' ('{hostConfiguration.Host.MacAddress}') " +
                               $"in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' " +
                               $"of organization '{hostConfiguration.Subject.Organization}'";

            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }

        var targetOu = ouResult.Value;

        try
        {
            targetOu.AddHost(hostConfiguration.Host.Name, hostConfiguration.Host.IpAddress, hostConfiguration.Host.MacAddress);

            applicationUnitOfWork.Organizations.Update(organizationResult.Value);
            await applicationUnitOfWork.CommitAsync();

            var successMessage = $"New host registered: '{hostConfiguration.Host.Name}' ('{hostConfiguration.Host.MacAddress}') " +
                                 $"in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' " +
                                 $"of organization '{hostConfiguration.Subject.Organization}'";

            await eventNotificationService.SendNotificationAsync(successMessage);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Host registration failed: {ex.Message} for host '{hostConfiguration.Host.Name}' " +
                               $"('{hostConfiguration.Host.MacAddress}') in organizational unit '{string.Join(" > ", hostConfiguration.Subject.OrganizationalUnit)}' " +
                               $"of organization '{hostConfiguration.Subject.Organization}'";

            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }
    }

    public async Task<Result> UnregisterHostAsync(HostUnregisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var organizationResult = await GetOrganizationAsync(request.Organization);

        if (organizationResult.IsFailed)
        {
            var errorMessage = $"Host unregister failed: {organizationResult.Errors.FirstOrDefault()?.Message} " +
                               $"for host '{request.MacAddress}' in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' " +
                               $"of organization '{request.Organization}'";

            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }

        var ouResult = await ResolveOrganizationalUnitHierarchyAsync(request.OrganizationalUnit, organizationResult.Value.Id);

        if (ouResult.IsFailed)
        {
            var errorMessage = $"Host unregister failed: {ouResult.Errors.FirstOrDefault()?.Message} " +
                               $"for host '{request.MacAddress}' in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' " +
                               $"of organization '{request.Organization}'";

            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }

        try
        {
            var targetOu = ouResult.Value ?? throw new InvalidOperationException("Target Organizational Unit not found.");
            var host = targetOu.Hosts.FirstOrDefault(h => h.MacAddress.Equals(request.MacAddress));

            if (host == null)
            {
                var errorMessage = $"Failed to find the host: Host with MAC address '{request.MacAddress}' not found in organizational unit '{targetOu.Name}'.";

                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            await applicationUnitOfWork.Organizations.RemoveHostAsync(organizationResult.Value.Id, targetOu.Id, host.Id);
            await applicationUnitOfWork.CommitAsync();

            var successMessage = $"Host unregistered: '{request.MacAddress}' from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' in organization '{request.Organization}'";

            await eventNotificationService.SendNotificationAsync(successMessage);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Host unregister failed: {ex.Message} for host '{request.MacAddress}' " +
                               $"from organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' " +
                               $"in organization '{request.Organization}'";

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
            var host = parentUnit.Hosts.FirstOrDefault(h => h.MacAddress.Equals(request.MacAddress));

            if (host == null)
            {
                var errorMessage = $"Failed to find the host: Host with MAC address '{request.MacAddress}' not found in organizational unit '{parentUnit.Name}'.";

                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            host.SetName(request.Name);
            host.SetIpAddress(request.IpAddress);

            applicationUnitOfWork.Organizations.Update(organizationResult.Value);
            await applicationUnitOfWork.CommitAsync();

            var successMessage = $"Host information updated: {host.Name} (`{host.MacAddress}`) in organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'";

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

    public async Task<Result> ForceUpdateHostAsync(HostUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var macAddress = request.MacAddress;

        try
        {
            var hosts = await applicationUnitOfWork.Organizations.FindHostsAsync(h => h.MacAddress.Equals(macAddress));
            var host = hosts.FirstOrDefault();

            if (host == null)
            {
                var errorMessage = $"Host with MAC address '{macAddress}' not found.";
                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            var currentOrganization = host.Parent.Organization;
            var currentOu = host.Parent;

            var organizationResult = await GetOrganizationAsync(request.Organization);

            if (organizationResult.IsFailed)
            {
                var errorMessage = $"Organization '{request.Organization}' not found.";
                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            var targetOrganization = organizationResult.Value;
            var targetOrganizationId = targetOrganization.Id;

            var newOuResult = await ResolveOrganizationalUnitHierarchyAsync(request.OrganizationalUnit, targetOrganizationId);

            if (newOuResult.IsFailed)
            {
                var errorMessage = newOuResult.Errors.FirstOrDefault()?.Message;
                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            var targetOu = newOuResult.Value;

            if (targetOu == null)
            {
                const string errorMessage = "Target Organizational Unit is null.";
                await eventNotificationService.SendNotificationAsync(errorMessage);

                return Result.Fail(errorMessage);
            }

            var needsMove = currentOu.Id != targetOu.Id || currentOrganization.Id != targetOrganizationId;

            if (needsMove)
            {
                await applicationUnitOfWork.Organizations.MoveHostAsync(currentOrganization.Id, targetOrganizationId, host.Id, currentOu.Id, targetOu.Id);
            }

            host.SetName(request.Name);
            host.SetIpAddress(request.IpAddress);

            applicationUnitOfWork.Organizations.Update(targetOrganization);

            if (needsMove)
            {
                applicationUnitOfWork.Organizations.Update(currentOrganization);
            }

            await applicationUnitOfWork.CommitAsync();

            var successMessage = $"Host '{host.Name}' (`{host.MacAddress}`) updated and moved to organizational unit '{string.Join(" > ", request.OrganizationalUnit)}' of organization '{request.Organization}'.";
            await eventNotificationService.SendNotificationAsync(successMessage);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Force update failed: {ex.Message}";
            await eventNotificationService.SendNotificationAsync(errorMessage);

            return Result.Fail(errorMessage);
        }
    }
}
