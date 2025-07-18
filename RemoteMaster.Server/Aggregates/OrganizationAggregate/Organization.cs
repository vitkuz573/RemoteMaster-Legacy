﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.DomainEvents;

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class Organization : IAggregateRoot
{
    private Organization() { }

    public Organization(string name, Address address)
    {
        Name = name;
        Address = address;
    }

    private readonly List<IDomainEvent> _domainEvents = [];

    private readonly List<OrganizationalUnit> _organizationalUnits = [];
    private readonly List<UserOrganization> _userOrganizations = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyCollection<OrganizationalUnit> OrganizationalUnits => _organizationalUnits.AsReadOnly();
    
    public IReadOnlyCollection<UserOrganization> UserOrganizations => _userOrganizations.AsReadOnly();

    public void SetName(string newName)
    {
        Name = newName ?? throw new ArgumentNullException(nameof(newName));
    }

    public void SetAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));

        AddDomainEvent(new OrganizationAddressChangedEvent(Id, address));
    }

    public void AddOrganizationalUnit(string unitName, Guid? parentId = null)
    {
        if (parentId.HasValue)
        {
            var parentUnit = _organizationalUnits.SingleOrDefault(u => u.Id == parentId.Value) ?? throw new InvalidOperationException("Parent unit not found.");
        }

        var unit = new OrganizationalUnit(unitName, parentId);

        unit.AssignToOrganization(Id);

        _organizationalUnits.Add(unit);

        AddDomainEvent(new OrganizationalUnitCreatedEvent(Id, unit.Id, unit.Name));
    }

    public void RemoveOrganizationalUnit(Guid unitId)
    {
        var unit = _organizationalUnits.SingleOrDefault(u => u.Id == unitId) ?? throw new InvalidOperationException("Organizational unit not found in this organization.");
        
        RemoveChildUnits(unit);

        _organizationalUnits.Remove(unit);
    }

    private void RemoveChildUnits(OrganizationalUnit unit)
    {
        foreach (var child in unit.Children.ToList())
        {
            RemoveChildUnits(child);

            _organizationalUnits.Remove(child);
        }

        unit.ClearHosts();
        unit.ClearUsers();
    }

    public void AddUser(string userId)
    {
        if (_userOrganizations.Any(u => u.UserId == userId))
        {
            throw new InvalidOperationException("User is already part of this organization.");
        }

        var userOrganization = new UserOrganization(Id, userId);

        _userOrganizations.Add(userOrganization);
    }

    public void RemoveUser(string userId)
    {
        var userOrganization = _userOrganizations.SingleOrDefault(u => u.UserId == userId) ?? throw new InvalidOperationException("User not found in this organization.");
        
        _userOrganizations.Remove(userOrganization);
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
