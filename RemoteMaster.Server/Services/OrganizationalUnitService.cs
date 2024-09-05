// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.DTOs;

namespace RemoteMaster.Server.Services;

public class OrganizationalUnitService(IOrganizationRepository organizationRepository, IOrganizationalUnitRepository organizationalUnitRepository) : IOrganizationalUnitService
{
    public async Task<string[]> GetFullPathAsync(Guid organizationalUnitId)
    {
        var path = new List<string>();
        var unit = await organizationalUnitRepository.GetByIdAsync(organizationalUnitId);

        while (unit != null)
        {
            path.Insert(0, unit.Name);

            if (unit.ParentId == null)
            {
                break;
            }

            unit = await organizationalUnitRepository.GetByIdAsync(unit.ParentId.Value);
        }

        return [.. path];
    }

    public async Task<string> AddOrUpdateOrganizationalUnitAsync(OrganizationalUnitDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var organization = await organizationRepository.GetByIdAsync(dto.OrganizationId);

        if (organization == null)
        {
            return "Error: Organization not found.";
        }

        OrganizationalUnit? parent = null;

        if (dto.ParentId.HasValue)
        {
            parent = await organizationalUnitRepository.GetByIdAsync(dto.ParentId.Value);
        }

        if (dto.Id.HasValue)
        {
            var organizationalUnit = await organizationalUnitRepository.GetByIdAsync(dto.Id.Value);

            if (organizationalUnit == null)
            {
                return "Error: Organizational unit not found.";
            }

            organizationalUnit.SetName(dto.Name);

            if (parent != null)
            {
                organizationalUnit.SetParent(parent);
            }

            await organizationalUnitRepository.UpdateAsync(organizationalUnit);
        }
        else
        {
            var newUnit = new OrganizationalUnit(dto.Name, organization, parent);
            organization.AddOrganizationalUnit(newUnit);

            await organizationalUnitRepository.AddAsync(newUnit);
        }

        await organizationalUnitRepository.SaveChangesAsync();

        return dto.Id.HasValue ? "Organizational unit updated successfully." : "Organizational unit created successfully.";
    }

    public async Task<string> DeleteOrganizationalUnitAsync(OrganizationalUnit organizationalUnit)
    {
        ArgumentNullException.ThrowIfNull(organizationalUnit);

        var organization = await organizationRepository.GetByIdAsync(organizationalUnit.OrganizationId);
        
        if (organization == null)
        {
            return "Error: Organization not found.";
        }

        try
        {
            organization.RemoveOrganizationalUnit(organizationalUnit);

            await organizationRepository.UpdateAsync(organization);
            await organizationalUnitRepository.SaveChangesAsync();

            return "Organizational unit deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<IEnumerable<OrganizationalUnit>> GetAllOrganizationalUnitsAsync()
    {
        return await organizationalUnitRepository.GetAllAsync();
    }

    public async Task UpdateUserOrganizationalUnitsAsync(ApplicationUser user, List<Guid> selectedUnitIds)
    {
        ArgumentNullException.ThrowIfNull(user);

        foreach (var unit in user.UserOrganizationalUnits.ToList().Where(unit => !selectedUnitIds.Contains(unit.OrganizationalUnitId)))
        {
            var organizationalUnit = await organizationalUnitRepository.GetByIdAsync(unit.OrganizationalUnitId);
            organizationalUnit?.RemoveUser(user.Id);
        }

        foreach (var unitId in selectedUnitIds.Where(unitId => user.UserOrganizationalUnits.All(uou => uou.OrganizationalUnitId != unitId)))
        {
            var unit = await organizationalUnitRepository.GetByIdAsync(unitId);
            unit?.AddUser(user.Id);
        }

        await organizationalUnitRepository.SaveChangesAsync();
    }
}
