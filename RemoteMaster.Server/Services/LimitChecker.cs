// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class LimitChecker(IPlanService planService, IUserPlanProvider userPlanProvider) : ILimitChecker
{
    private PlanLimits GetCurrentPlanLimits()
    {
        var userPlan = userPlanProvider.GetUserPlan();

        return planService.GetPlanLimits(userPlan);
    }

    public bool CanAddOrganization(IEnumerable<Organization> organizations)
    {
        var limits = GetCurrentPlanLimits();

        return organizations.Count() < limits.MaxOrganizations;
    }

    public bool CanAddOrganizationalUnit(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        var limits = GetCurrentPlanLimits();
        
        return organization.OrganizationalUnits.Count < limits.MaxOrganizationalUnitsPerOrganization;
    }

    public bool CanAddComputer(OrganizationalUnit organizationalUnit)
    {
        ArgumentNullException.ThrowIfNull(organizationalUnit);

        var limits = GetCurrentPlanLimits();

        return organizationalUnit.Computers.Count < limits.MaxComputersPerOrganizationalUnit;
    }

    public bool CanAddUserToOrganization(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        var limits = GetCurrentPlanLimits();

        return organization.UserOrganizations.Count < limits.MaxUsersPerOrganization;
    }

    public bool CanAddUserToOrganizationalUnit(OrganizationalUnit organizationalUnit)
    {
        ArgumentNullException.ThrowIfNull(organizationalUnit);

        var limits = GetCurrentPlanLimits();

        return organizationalUnit.UserOrganizationalUnits.Count < limits.MaxUsersPerOrganizationalUnit;
    }
}
