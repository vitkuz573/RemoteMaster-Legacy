// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.DTOs;

namespace RemoteMaster.Server.Services;

public class OrganizationalUnitService(IApplicationUnitOfWork applicationUnitOfWork) : IOrganizationalUnitService
{
    public async Task<string[]> GetFullPathAsync(Guid organizationalUnitId)
    {
        var path = new List<string>();

        var organization = await applicationUnitOfWork.Organizations.GetOrganizationByUnitIdAsync(organizationalUnitId);
        
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

        return [.. path];
    }

    public async Task<string> AddOrUpdateOrganizationalUnitAsync(OrganizationalUnitDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(dto.OrganizationId);

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

        applicationUnitOfWork.Organizations.Update(organization);
        await applicationUnitOfWork.SaveChangesAsync();

        return dto.Id.HasValue ? "Organizational unit updated successfully." : "Organizational unit created successfully.";
    }


    public async Task<string> DeleteOrganizationalUnitAsync(OrganizationalUnit organizationalUnit)
    {
        ArgumentNullException.ThrowIfNull(organizationalUnit);

        var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(organizationalUnit.OrganizationId);

        if (organization == null)
        {
            return "Error: Organization not found.";
        }

        try
        {
            organization.RemoveOrganizationalUnit(organizationalUnit.Id);

            applicationUnitOfWork.Organizations.Update(organization);
            await applicationUnitOfWork.SaveChangesAsync();

            return "Organizational unit deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<IEnumerable<OrganizationalUnit>> GetAllOrganizationalUnitsAsync()
    {
        var organizations = await applicationUnitOfWork.Organizations.GetAllAsync();

        return organizations.SelectMany(o => o.OrganizationalUnits);
    }

    public async Task UpdateUserOrganizationalUnitsAsync(ApplicationUser user, List<Guid> selectedUnitIds)
    {
        ArgumentNullException.ThrowIfNull(user);

        var organizations = (await applicationUnitOfWork.Organizations.GetAllAsync()).ToList();

        foreach (var unit in user.UserOrganizationalUnits.ToList().Where(unit => !selectedUnitIds.Contains(unit.OrganizationalUnitId)))
        {
            var organization = organizations.FirstOrDefault(o => o.OrganizationalUnits.Any(u => u.Id == unit.OrganizationalUnitId));
            var organizationalUnit = organization?.OrganizationalUnits.FirstOrDefault(u => u.Id == unit.OrganizationalUnitId);

            organizationalUnit?.RemoveUser(user.Id);
        }

        foreach (var unitId in selectedUnitIds.Where(unitId => user.UserOrganizationalUnits.All(uou => uou.OrganizationalUnitId != unitId)))
        {
            var organization = organizations.FirstOrDefault(o => o.OrganizationalUnits.Any(u => u.Id == unitId));
            var unit = organization?.OrganizationalUnits.FirstOrDefault(u => u.Id == unitId);

            unit?.AddUser(user.Id);
        }

        await applicationUnitOfWork.SaveChangesAsync();
    }
}
