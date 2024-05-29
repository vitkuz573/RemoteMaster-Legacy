// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.Models;

public class UserOrganizationalUnit
{
    public string UserId { get; set; }

    public ApplicationUser User { get; set; }

    public Guid OrganizationalUnitId { get; set; }

    public OrganizationalUnit OrganizationalUnit { get; set; }
}
