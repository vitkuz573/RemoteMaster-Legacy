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

        var countryCode = new CountryCode(dto.Country);
        var address = new Address(dto.Locality, dto.State, countryCode);

        if (dto.Id.HasValue)
        {
            var organization = await organizationRepository.GetByIdAsync(dto.Id.Value);

            if (organization == null)
            {
                return "Error: Organization not found.";
            }

            var addressChanged = !organization.Address.Equals(address);

            organization.SetName(dto.Name);
            organization.SetAddress(address);

            await organizationRepository.UpdateAsync(organization);

            if (addressChanged)
            {
                foreach (var unit in organization.OrganizationalUnits)
                {
                    foreach (var computer in unit.Computers)
                    {
                        await organizationRepository.CreateCertificateRenewalTaskAsync(organization.Id, computer.Id, DateTime.UtcNow.AddHours(1));
                    }
                }
            }
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

    public async Task UpdateUserOrganizationsAsync(ApplicationUser user, List<Guid> organizationIds)
    {
        ArgumentNullException.ThrowIfNull(user);

        var currentOrganizationIds = user.UserOrganizations.Select(uo => uo.OrganizationId).ToHashSet();

        var organizations = await organizationRepository.GetByIdsAsync(organizationIds);

        var organizationsToRemove = currentOrganizationIds.Except(organizationIds).ToList();
        var organizationsToAdd = organizationIds.Except(currentOrganizationIds).ToList();

        if (organizationsToRemove.Any())
        {
            var organizationsToRemoveEntities = await organizationRepository.GetByIdsAsync(organizationsToRemove);

            foreach (var organization in organizationsToRemoveEntities)
            {
                organization.RemoveUser(user.Id);
            }
        }

        if (organizationsToAdd.Any())
        {
            foreach (var orgId in organizationsToAdd.Select(orgId => organizations.FirstOrDefault(o => o.Id == orgId)))
            {
                orgId?.AddUser(user.Id);
            }
        }

        await organizationRepository.SaveChangesAsync();
    }
}
