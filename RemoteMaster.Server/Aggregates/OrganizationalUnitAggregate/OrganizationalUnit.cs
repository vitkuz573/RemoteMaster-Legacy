// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Entities;

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

    public void ChangeName(string newName)
    {
        Name = newName ?? throw new ArgumentNullException(nameof(newName));
    }

    public void ChangeParent(OrganizationalUnit newParent)
    {
        Parent = newParent ?? throw new ArgumentNullException(nameof(newParent));
        ParentId = newParent.Id;
    }

    public void AssignToOrganization(Organization organization)
    {
        Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        OrganizationId = organization.Id;
    }

    public void AddChildUnit(OrganizationalUnit unit)
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
    public void ClearChildren()
    {
        _children.Clear();
    }

    public void AddComputer(Computer computer)
    {
        ArgumentNullException.ThrowIfNull(computer);

        if (computer.Parent != this)
        {
            throw new InvalidOperationException("This computer is assigned to a different organizational unit.");
        }

        _computers.Add(computer);
    }

    public void RemoveComputer(Computer computer)
    {
        if (!_computers.Contains(computer))
        {
            throw new InvalidOperationException("Computer not found in this unit.");
        }

        _computers.Remove(computer);
    }

    public void MoveComputerToUnit(Computer computer, OrganizationalUnit newUnit)
    {
        ArgumentNullException.ThrowIfNull(computer);

        if (!_computers.Contains(computer))
        {
            throw new InvalidOperationException("Computer does not belong to this unit.");
        }

        computer.SetParent(newUnit);
        RemoveComputer(computer);
        newUnit.AddComputer(computer);
    }

    public void ClearComputers()
    {
        _computers.Clear();
    }

    public void AddUser(UserOrganizationalUnit userOrganizationalUnit)
    {
        if (_userOrganizationalUnits.Any(u => u.UserId == userOrganizationalUnit.UserId))
        {
            throw new InvalidOperationException("User is already part of this unit.");
        }

        _userOrganizationalUnits.Add(userOrganizationalUnit);
    }

    public void RemoveUser(UserOrganizationalUnit userOrganizationalUnit)
    {
        if (!_userOrganizationalUnits.Contains(userOrganizationalUnit))
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
