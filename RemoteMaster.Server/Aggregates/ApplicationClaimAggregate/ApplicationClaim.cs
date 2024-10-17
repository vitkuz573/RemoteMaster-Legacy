// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Aggregates.ApplicationClaimAggregate;

public class ApplicationClaim : IAggregateRoot
{
    private ApplicationClaim() { }

    public ApplicationClaim(string type, string value, string displayName, string description)
    {
        ClaimType = type;
        ClaimValue = value;
        DisplayName = displayName;
        Description = description;
    }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public int Id { get; private set; }

    public string ClaimType { get; private set; } = null!;

    public string ClaimValue { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
