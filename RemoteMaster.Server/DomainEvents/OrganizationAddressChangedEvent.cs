// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;

namespace RemoteMaster.Server.DomainEvents;

public class OrganizationAddressChangedEvent(Guid organizationId, Address newAddress) : IDomainEvent
{
    public Guid OrganizationId { get; } = organizationId;
    
    public Address NewAddress { get; } = newAddress;
    
    public DateTime OccurredOn { get; private set; } = DateTime.UtcNow;
}
