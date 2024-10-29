// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class UserOrganization
{
    private UserOrganization() { }

    internal UserOrganization(Guid organizationId, string userId)
    {
        OrganizationId = organizationId;
        UserId = userId;
    }

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public string UserId { get; private set; } = null!;
}
