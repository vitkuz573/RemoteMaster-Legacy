﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Services;

public class OrganizationalUnitService(IApplicationUnitOfWork applicationUnitOfWork) : IOrganizationalUnitService
{
    public async Task<List<string>> GetFullPathAsync(Guid organizationalUnitId)
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

        return path;
    }

    public async Task<string> AddOrUpdateOrganizationalUnitAsync(OrganizationalUnitDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(dto.OrganizationId);

        if (organization == null)
        {
            return "Error: Organization not found.";
        }

        if (dto.Id.HasValue)
        {
            var organizationalUnit = organization.OrganizationalUnits.FirstOrDefault(u => u.Id == dto.Id.Value);

            if (organizationalUnit == null)
            {
                return "Error: Organizational unit not found.";
            }

            organizationalUnit.SetName(dto.Name);

            if (dto.ParentId.HasValue)
            {
                var parentUnit = organization.OrganizationalUnits.FirstOrDefault(u => u.Id == dto.ParentId.Value);

                if (parentUnit == null)
                {
                    return "Error: Parent organizational unit not found.";
                }

                organizationalUnit.SetParent(dto.ParentId);
            }
            else
            {
                organizationalUnit.SetParent(null);
            }
        }
        else
        {
            if (dto.ParentId.HasValue)
            {
                var parentUnit = organization.OrganizationalUnits.FirstOrDefault(u => u.Id == dto.ParentId.Value);

                if (parentUnit == null)
                {
                    return "Error: Parent organizational unit not found.";
                }
            }

            organization.AddOrganizationalUnit(dto.Name, dto.ParentId);
        }

        applicationUnitOfWork.Organizations.Update(organization);

        await applicationUnitOfWork.CommitAsync();

        return dto.Id.HasValue ? "Organizational unit updated successfully." : "Organizational unit created successfully.";
    }

    public async Task<string> DeleteOrganizationalUnitAsync(OrganizationalUnitDto organizationalUnit)
    {
        ArgumentNullException.ThrowIfNull(organizationalUnit);

        var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(organizationalUnit.OrganizationId);

        if (organization == null)
        {
            return "Error: Organization not found.";
        }

        try
        {
            organization.RemoveOrganizationalUnit(organizationalUnit.Id.Value);

            applicationUnitOfWork.Organizations.Update(organization);
            await applicationUnitOfWork.CommitAsync();

            return "Organizational unit deleted successfully.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<IEnumerable<OrganizationalUnitDto>> GetAllOrganizationalUnitsAsync()
    {
        var organizations = await applicationUnitOfWork.Organizations.GetAllAsync();

        return organizations
            .SelectMany(o => o.OrganizationalUnits)
            .Select(ou => new OrganizationalUnitDto(ou.Id, ou.Name, ou.OrganizationId, ou.ParentId));
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

        await applicationUnitOfWork.CommitAsync();
    }
}
