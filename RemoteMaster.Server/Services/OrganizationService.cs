// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Services;

public class OrganizationService(IApplicationUnitOfWork applicationUnitOfWork) : IOrganizationService
{
    public async Task<IEnumerable<OrganizationDto>> GetAllOrganizationsAsync()
    {
        var organizations = await applicationUnitOfWork.Organizations.GetAllAsync();

        return organizations.Select(o =>
        {
            var addressDto = new AddressDto(o.Address.Locality, o.Address.State, o.Address.Country.Code);
            var organizationDto = new OrganizationDto(o.Id, o.Name, addressDto);

            foreach (var organizationalUnit in o.OrganizationalUnits)
            {
                var organizationalUnitDto = new OrganizationalUnitDto(organizationalUnit.Id, organizationalUnit.Name, o.Id, organizationalUnit.ParentId);
                
                organizationDto.OrganizationalUnits.Add(organizationalUnitDto);
            }

            return organizationDto;
        });
    }

    public async Task<OrganizationDto?> GetOrganization(string organizationName)
    {
        var organization = (await applicationUnitOfWork.Organizations.FindAsync(o => o.Name == organizationName)).FirstOrDefault();

        if (organization == null)
        {
            return null;
        }

        var addressDto = new AddressDto(organization.Address.Locality, organization.Address.State, organization.Address.Country.Code);
        var organizationDto = new OrganizationDto(organization.Id, organization.Name, addressDto);

        foreach (var organizationalUnit in organization.OrganizationalUnits)
        {
            var organizationalUnitDto = new OrganizationalUnitDto(organizationalUnit.Id, organizationalUnit.Name, organization.Id, organizationalUnit.ParentId);

            organizationDto.OrganizationalUnits.Add(organizationalUnitDto);
        }

        return organizationDto;
    }

    public async Task<OrganizationDto?> GetOrganizationById(Guid organizationId)
    {
        var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(organizationId);

        if (organization == null)
        {
            return null;
        }

        var addressDto = new AddressDto(organization.Address.Locality, organization.Address.State, organization.Address.Country.Code);
        var organizationDto = new OrganizationDto(organization.Id, organization.Name, addressDto);

        foreach (var organizationalUnit in organization.OrganizationalUnits)
        {
            var organizationalUnitDto = new OrganizationalUnitDto(organizationalUnit.Id, organizationalUnit.Name, organization.Id, organizationalUnit.ParentId);

            organizationDto.OrganizationalUnits.Add(organizationalUnitDto);
        }

        return organizationDto;
    }

    public async Task<string> AddOrUpdateOrganizationAsync(OrganizationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var countryCode = new CountryCode(dto.Address.Country);
        var address = new Address(dto.Address.Locality, dto.Address.State, countryCode);

        Organization? organization;

        if (dto.Id.HasValue)
        {
            organization = await applicationUnitOfWork.Organizations.GetByIdAsync(dto.Id.Value);

            if (organization == null)
            {
                return "Error: Organization not found.";
            }

            organization.SetName(dto.Name);
            organization.SetAddress(address);

            applicationUnitOfWork.Organizations.Update(organization);
        }
        else
        {
            organization = new Organization(dto.Name, address);

            await applicationUnitOfWork.Organizations.AddAsync(organization);
        }

        await applicationUnitOfWork.CommitAsync();

        return dto.Id.HasValue ? "Organization updated successfully." : "Organization created successfully.";
    }

    public async Task<string> DeleteOrganizationAsync(string organizationName)
    {
        var organization = (await applicationUnitOfWork.Organizations.FindAsync(o => o.Name == organizationName)).FirstOrDefault();

        if (organization == null)
        {
            return $"Organization with name {organizationName} not found.";
        }

        applicationUnitOfWork.Organizations.Delete(organization);

        await applicationUnitOfWork.CommitAsync();

        return "Organization deleted successfully.";
    }

    public async Task UpdateUserOrganizationsAsync(ApplicationUser user, List<Guid> selectedOrganizationIds)
    {
        ArgumentNullException.ThrowIfNull(user);

        foreach (var org in user.UserOrganizations.ToList().Where(org => !selectedOrganizationIds.Contains(org.OrganizationId)))
        {
            var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(org.OrganizationId);
            organization?.RemoveUser(user.Id);
        }

        foreach (var orgId in selectedOrganizationIds.Where(orgId => user.UserOrganizations.All(uo => uo.OrganizationId != orgId)))
        {
            var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(orgId);
            organization?.AddUser(user.Id);
        }

        await applicationUnitOfWork.CommitAsync();
    }

    public async Task<IEnumerable<Organization>> GetOrganizationsWithAccessibleUnitsAsync(string userId)
    {
        var user = await applicationUnitOfWork.ApplicationUsers.GetByIdAsync(userId) ?? throw new InvalidOperationException($"User with ID '{userId}' not found.");
        
        var accessibleOrganizationIds = user.UserOrganizations.Select(uo => uo.OrganizationId);
        var accessibleOrganizationalUnitIds = user.UserOrganizationalUnits.Select(uou => uou.OrganizationalUnitId);

        return await applicationUnitOfWork.Organizations.GetOrganizationsWithAccessibleUnitsAsync(accessibleOrganizationIds, accessibleOrganizationalUnitIds);

    }

    public async Task RemoveHostAsync(Guid organizationId, Guid organizationalUnitId, Guid hostId)
    {
        var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(organizationId) ?? throw new InvalidOperationException("Organization not found");
        var organizationalUnit = organization.OrganizationalUnits.FirstOrDefault(u => u.Id == organizationalUnitId) ?? throw new InvalidOperationException("Organizational Unit not found");

        if (organizationalUnit.Hosts.All(h => h.Id != hostId))
        {
            throw new InvalidOperationException("Host not found");
        }

        await applicationUnitOfWork.Organizations.RemoveHostAsync(organizationId, organizationalUnitId, hostId);
        
        applicationUnitOfWork.Organizations.Update(organization);
        
        await applicationUnitOfWork.CommitAsync();
    }
}
