// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.DomainEvents;

public class OrganizationalUnitCreatedEvent(Guid organizationId, Guid organizationalUnitId, string name) : DomainEventBase
{
    public Guid OrganizationId { get; } = organizationId;

    public Guid OrganizationalUnitId { get; } = organizationalUnitId;

    public string Name { get; } = name;
}
