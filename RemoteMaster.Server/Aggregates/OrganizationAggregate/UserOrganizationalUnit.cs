﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class UserOrganizationalUnit
{
    private UserOrganizationalUnit() { }

    internal UserOrganizationalUnit(Guid unitId, string userId)
    {
        OrganizationalUnitId = unitId;
        UserId = userId;
    }

    public Guid OrganizationalUnitId { get; private set; }

    public OrganizationalUnit OrganizationalUnit { get; private set; } = null!;

    public string UserId { get; private set; } = null!;
}
