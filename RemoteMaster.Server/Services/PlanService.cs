// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class PlanService : IPlanService
{
    private readonly Dictionary<string, PlanLimits> _plans;

    public PlanService()
    {
        _plans = new Dictionary<string, PlanLimits>
        {
            { "Basic", new PlanLimits { MaxOrganizations = 5, MaxOrganizationalUnitsPerOrganization = 3, MaxComputersPerOrganizationalUnit = 10, MaxUsersPerOrganization = 20, MaxUsersPerOrganizationalUnit = 5 } },
            { "Pro", new PlanLimits { MaxOrganizations = 10, MaxOrganizationalUnitsPerOrganization = 10, MaxComputersPerOrganizationalUnit = 50, MaxUsersPerOrganization = 50, MaxUsersPerOrganizationalUnit = 10 } },
            { "Enterprise", new PlanLimits { MaxOrganizations = 50, MaxOrganizationalUnitsPerOrganization = 50, MaxComputersPerOrganizationalUnit = 200, MaxUsersPerOrganization = 200, MaxUsersPerOrganizationalUnit = 50 } }
        };
    }

    public PlanLimits GetPlanLimits(string plan)
    {
        if (_plans.TryGetValue(plan, out var limits))
        {
            return limits;
        }

        throw new ArgumentException("Unknown plan", nameof(plan));
    }
}
