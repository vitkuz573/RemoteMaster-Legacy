// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.DTOs;

namespace RemoteMaster.Server.Services;

public class OrganizationalUnitService(IOrganizationRepository organizationRepository) : IOrganizationalUnitService
{
    public async Task<string[]> GetFullPathAsync(Guid organizationalUnitId)
    {
        var path = new List<string>();

        var organization = await organizationRepository.GetOrganizationByUnitIdAsync(organizationalUnitId);
        
        if (organization == null)
        {
            return [];
        }

        var unit = organization.OrganizationalUnits.FirstOrDefault(u => u.Id == organizationalUnitId);

        while (unit != null)
        {
            path.Insert(0, unit.Name);

            if (unit.ParentId == null)
            {
                break;
            }

            unit = organization.OrganizationalUnits.FirstOrDefault(u => u.Id == unit.ParentId);
        }

        return path.ToArray();
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
            parent = organization.OrganizationalUnits.FirstOrDefault(u => u.Id == dto.ParentId.Value);
        }

        if (dto.Id.HasValue)
        {
            var organizationalUnit = organization.OrganizationalUnits.FirstOrDefault(u => u.Id == dto.Id.Value);

            if (organizationalUnit == null)
            {
                return "Error: Organizational unit not found.";
            }

            organizationalUnit.SetName(dto.Name);

            if (parent != null)
            {
                organizationalUnit.SetParent(parent);
            }
        }
        else
        {
            organization.AddOrganizationalUnit(dto.Name);
        }

        await organizationRepository.UpdateAsync(organization);
        await organizationRepository.SaveChangesAsync();

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
            organization.RemoveOrganizationalUnit(organizationalUnit.Id);

            await organizationRepository.UpdateAsync(organization);
            await organizationRepository.SaveChangesAsync();

            return "Organizational unit deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<IEnumerable<OrganizationalUnit>> GetAllOrganizationalUnitsAsync()
    {
        var organizations = await organizationRepository.GetAllAsync();

        return organizations.SelectMany(o => o.OrganizationalUnits);
    }

    public async Task UpdateUserOrganizationalUnitsAsync(ApplicationUser user, List<Guid> unitIds)
    {
        ArgumentNullException.ThrowIfNull(user);

        var currentUnitIds = user.UserOrganizationalUnits.Select(uou => uou.OrganizationalUnitId).ToHashSet();

        var organizations = await organizationRepository.GetAllAsync();
        var organizationalUnits = organizations.SelectMany(o => o.OrganizationalUnits).Where(u => unitIds.Contains(u.Id)).ToList();

        var unitsToRemove = currentUnitIds.Except(unitIds).ToList();
        var unitsToAdd = unitIds.Except(currentUnitIds).ToList();

        if (unitsToRemove.Any())
        {
            var unitsToRemoveEntities = organizationalUnits.Where(u => unitsToRemove.Contains(u.Id)).ToList();

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

        await organizationRepository.SaveChangesAsync();
    }
}
