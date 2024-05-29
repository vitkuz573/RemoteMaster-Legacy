// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Models;

public class OrganizationalUnit : Node
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; }

#pragma warning disable CA2227
    public ICollection<UserOrganizationalUnit> UserOrganizationalUnits { get; set; }
#pragma warning restore CA2227
}
