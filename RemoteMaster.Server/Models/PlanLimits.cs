// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class PlanLimits
{
    public int MaxOrganizations { get; set; }

    public int MaxOrganizationalUnitsPerOrganization { get; set; }

    public int MaxHostsPerOrganizationalUnit { get; set; }

    public int MaxUsersPerOrganization { get; set; }

    public int MaxUsersPerOrganizationalUnit { get; set; }
}
