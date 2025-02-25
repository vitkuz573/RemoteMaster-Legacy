// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Aggregates.HostMoveRequestAggregate;

public class HostMoveRequest : IAggregateRoot
{
    protected HostMoveRequest() { }

    internal HostMoveRequest(PhysicalAddress macAddress, string organization, List<string> organizationalUnit)
    {
        MacAddress = macAddress ?? throw new ArgumentNullException(nameof(macAddress));
        Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        OrganizationalUnit = organizationalUnit ?? throw new ArgumentNullException(nameof(organizationalUnit));
    }

    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }

    public PhysicalAddress MacAddress { get; } = null!;

    public string Organization { get; private set; } = null!;

    public List<string> OrganizationalUnit { get; private set; } = null!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void SetOrganization(string organizationName)
    {
        Organization = organizationName;
    }

    public void SetOrganizationalUnit(List<string> organizationalUnit)
    {
        OrganizationalUnit = organizationalUnit;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
