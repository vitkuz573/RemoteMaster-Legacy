// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.DTOs;

namespace RemoteMaster.Server.Services;

public class OrganizationService(IOrganizationRepository organizationRepository) : IOrganizationService
{
    public async Task<IEnumerable<Organization>> GetAllOrganizationsAsync()
    {
        return await organizationRepository.GetAllAsync();
    }

    public async Task<string> AddOrUpdateOrganizationAsync(OrganizationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var address = new Address(dto.Locality, dto.State, dto.Country);

        if (dto.Id.HasValue)
        {
            var organization = await organizationRepository.GetByIdAsync(dto.Id.Value);
            
            if (organization == null)
            {
                return "Error: Organization not found.";
            }

            organization.SetName(dto.Name);
            organization.SetAddress(address);

            await organizationRepository.UpdateAsync(organization);
        }
        else
        {
            var newOrganization = new Organization(dto.Name, address);
            await organizationRepository.AddAsync(newOrganization);
        }

        await organizationRepository.SaveChangesAsync();

        return dto.Id.HasValue ? "Organization updated successfully." : "Organization created successfully.";
    }

    public async Task<string> DeleteOrganizationAsync(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        await organizationRepository.DeleteAsync(organization);
        await organizationRepository.SaveChangesAsync();

        return "Organization deleted successfully.";
    }

    public async Task UpdateUserOrganizationsAsync(ApplicationUser user, List<Guid> selectedOrganizationIds)
    {
        ArgumentNullException.ThrowIfNull(user);

        foreach (var org in user.UserOrganizations.ToList().Where(org => !selectedOrganizationIds.Contains(org.OrganizationId)))
        {
            var organization = await organizationRepository.GetByIdAsync(org.OrganizationId);
            organization?.RemoveUser(user.Id);
        }

        foreach (var orgId in selectedOrganizationIds.Where(orgId => user.UserOrganizations.All(uo => uo.OrganizationId != orgId)))
        {
            var organization = await organizationRepository.GetByIdAsync(orgId);
            organization?.AddUser(user.Id);
        }

        await organizationRepository.SaveChangesAsync();
    }
}
