// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;

namespace RemoteMaster.Server.DomainEvents;

public class OrganizationCreatedEvent(Guid organizationId, string name, Address address) : DomainEventBase
{
    public Guid OrganizationId { get; } = organizationId;

    public string Name { get; } = name;

    public Address Address { get; } = address;
}
