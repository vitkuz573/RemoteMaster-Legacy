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

    public async Task UpdateUserOrganizationalUnitsAsync(ApplicationUser user, List<Guid> unitIds)
    {
        ArgumentNullException.ThrowIfNull(user);

        var currentUnitIds = user.UserOrganizationalUnits.Select(uou => uou.OrganizationalUnitId).ToHashSet();
        
        var organizationalUnits = await organizationalUnitRepository.GetByIdsAsync(unitIds);

        var unitsToRemove = currentUnitIds.Except(unitIds).ToList();
        var unitsToAdd = unitIds.Except(currentUnitIds).ToList();

        if (unitsToRemove.Any())
        {
            var unitsToRemoveEntities = await organizationalUnitRepository.GetByIdsAsync(unitsToRemove);

            foreach (var unit in unitsToRemoveEntities)
            {
                unit.RemoveUser(user.Id);
            }
        }

        if (unitsToAdd.Any())
        {
            foreach (var unit in unitsToAdd.Select(unitId => organizationalUnits.FirstOrDefault(u => u.Id == unitId)))
            {
                unit?.AddUser(user.Id);
            }
        }

        await organizationalUnitRepository.SaveChangesAsync();
    }
}
