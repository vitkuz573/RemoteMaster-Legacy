// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class LimitChecker(IPlanService planService, IUserPlanProvider userPlanProvider, IOrganizationRepository organizationRepository) : ILimitChecker
{
    private PlanLimits GetCurrentPlanLimits()
    {
        var userPlan = userPlanProvider.GetUserPlan();

        return planService.GetPlanLimits(userPlan);
    }

    public async Task<bool> CanAddOrganization()
    {
        var limits = GetCurrentPlanLimits();
        var organizations = await organizationRepository.GetAllAsync();
        var organizationCount = organizations.Count();

        return organizationCount < limits.MaxOrganizations;
    }

    public bool CanAddOrganizationalUnit(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        var limits = GetCurrentPlanLimits();
        var organizationalUnitCount = organization.OrganizationalUnits.Count;

        return organizationalUnitCount < limits.MaxOrganizationalUnitsPerOrganization;
    }

    public bool CanAddComputer(OrganizationalUnit organizationalUnit)
    {
        ArgumentNullException.ThrowIfNull(organizationalUnit);

        var limits = GetCurrentPlanLimits();
        var computerCount = organizationalUnit.Hosts.Count;

        return computerCount < limits.MaxComputersPerOrganizationalUnit;
    }

    public bool CanAddUserToOrganization(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        var limits = GetCurrentPlanLimits();
        var userOrganizationCount = organization.UserOrganizations.Count;

        return userOrganizationCount < limits.MaxUsersPerOrganization;
    }

    public bool CanAddUserToOrganizationalUnit(OrganizationalUnit organizationalUnit)
    {
        ArgumentNullException.ThrowIfNull(organizationalUnit);

        var limits = GetCurrentPlanLimits();
        var userOrganizationalUnitCount = organizationalUnit.UserOrganizationalUnits.Count;

        return userOrganizationalUnitCount < limits.MaxUsersPerOrganizationalUnit;
    }
}
