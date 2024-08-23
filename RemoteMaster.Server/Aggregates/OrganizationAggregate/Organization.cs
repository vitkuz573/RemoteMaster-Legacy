// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.ValueObjects;

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class Organization : IAggregateRoot
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public Address Address { get; set; }

    public ICollection<OrganizationalUnit> OrganizationalUnits { get; } = [];

    public ICollection<UserOrganization> UserOrganizations { get; } = [];
}
