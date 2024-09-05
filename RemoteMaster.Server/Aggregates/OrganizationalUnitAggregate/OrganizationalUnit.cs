// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;

public class OrganizationalUnit : IAggregateRoot
{
    private readonly List<OrganizationalUnit> _children = [];
    private readonly List<Computer> _computers = [];
    private readonly List<UserOrganizationalUnit> _userOrganizationalUnits = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Guid? ParentId { get; private set; }

    public OrganizationalUnit? Parent { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; }

    public IReadOnlyCollection<OrganizationalUnit> Children => _children.AsReadOnly();

    public IReadOnlyCollection<Computer> Computers => _computers.AsReadOnly();

    public IReadOnlyCollection<UserOrganizationalUnit> UserOrganizationalUnits => _userOrganizationalUnits.AsReadOnly();

    private OrganizationalUnit() { }

    public OrganizationalUnit(string name, Organization organization, OrganizationalUnit? parent = null)
    {
        Name = name;
        Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        OrganizationId = organization.Id;
        Parent = parent;
        ParentId = parent?.Id;

        parent?.AddChildUnit(this);
    }

    public void SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void SetParent(OrganizationalUnit? parent)
    {
        Parent = parent;
        ParentId = parent?.Id;
    }

    public void AssignToOrganization(Organization organization)
    {
        Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        OrganizationId = organization.Id;
    }

    private void AddChildUnit(OrganizationalUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (_children.Any(c => c.Name == unit.Name))
        {
            throw new InvalidOperationException("A child unit with the same name already exists.");
        }

        unit.AssignToParent(this);

        _children.Add(unit);
    }

    public void RemoveChildUnit(OrganizationalUnit unit)
    {
        if (!_children.Contains(unit))
        {
            throw new InvalidOperationException("Child unit not found.");
        }

        _children.Remove(unit);
    }

    public void AddComputer(string name, string ipAddress, string macAddress)
    {
        var computer = new Computer(name, ipAddress, macAddress);
        computer.SetOrganizationalUnit(Id);

        _computers.Add(computer);
    }

    public void RemoveComputer(Guid computerId)
    {
        var computer = _computers.SingleOrDefault(c => c.Id == computerId);

        _computers.Remove(computer);
    }

    public void MoveComputerToUnit(Guid computerId, OrganizationalUnit newUnit)
    {
        ArgumentNullException.ThrowIfNull(newUnit);

        var computer = _computers.SingleOrDefault(c => c.Id == computerId);

        if (computer == null)
        {
            throw new InvalidOperationException("Computer not found in the current unit.");
        }

        _computers.Remove(computer);
        computer.SetOrganizationalUnit(newUnit.Id);
        newUnit._computers.Add(computer);
    }

    public void AddUser(string userId)
    {
        if (_userOrganizationalUnits.Any(u => u.UserId == userId))
        {
            throw new InvalidOperationException("User is already part of this unit.");
        }

        var userOrganizationalUnit = new UserOrganizationalUnit(Id, userId);

        _userOrganizationalUnits.Add(userOrganizationalUnit);
    }

    public void RemoveUser(string userId)
    {
        var userOrganizationalUnit = _userOrganizationalUnits.SingleOrDefault(u => u.UserId == userId);

        if (userOrganizationalUnit == null)
        {
            throw new InvalidOperationException("User not found in this unit.");
        }

        _userOrganizationalUnits.Remove(userOrganizationalUnit);
    }

    private void AssignToParent(OrganizationalUnit parent)
    {
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        ParentId = parent.Id;
    }
}
