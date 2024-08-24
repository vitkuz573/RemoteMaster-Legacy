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
    private Organization() { }

    public Organization(string name, Address address)
    {
        Name = name;
        Address = address;
    }

    private readonly List<OrganizationalUnit> _organizationalUnits = [];
    private readonly List<UserOrganization> _userOrganizations = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Address Address { get; private set; }

    public IReadOnlyCollection<OrganizationalUnit> OrganizationalUnits => _organizationalUnits.AsReadOnly();
    
    public IReadOnlyCollection<UserOrganization> UserOrganizations => _userOrganizations.AsReadOnly();

    public void ChangeName(string newName)
    {
        Name = newName ?? throw new ArgumentNullException(nameof(newName));
    }

    public void ChangeAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public void AddOrganizationalUnit(OrganizationalUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (_organizationalUnits.Any(u => u.Name == unit.Name))
        {
            throw new InvalidOperationException("Organizational unit with the same name already exists.");
        }

        unit.AssignToOrganization(this);

        _organizationalUnits.Add(unit);
    }

    public void RemoveOrganizationalUnit(OrganizationalUnit unit)
    {
        if (!_organizationalUnits.Contains(unit))
        {
            throw new InvalidOperationException("Organizational unit not found in this organization.");
        }

        _organizationalUnits.Remove(unit);
    }

    public void ClearOrganizationalUnits()
    {
        _organizationalUnits.Clear();
    }

    public void UpdateAddress(Address newAddress)
    {
        Address = newAddress;
    }

    public void AddUser(UserOrganization userOrganization)
    {
        if (_userOrganizations.Any(u => u.UserId == userOrganization.UserId))
        {
            throw new InvalidOperationException("User is already part of this organization.");
        }

        _userOrganizations.Add(userOrganization);
    }

    public void RemoveUser(UserOrganization userOrganization)
    {
        if (!_userOrganizations.Contains(userOrganization))
        {
            throw new InvalidOperationException("User not found in this organization.");
        }

        _userOrganizations.Remove(userOrganization);
    }
}
