// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class OrganizationalUnit
{
    private readonly List<OrganizationalUnit> _children = [];
    private readonly List<Host> _hosts = [];
    private readonly List<UserOrganizationalUnit> _userOrganizationalUnits = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Guid? ParentId { get; private set; }

    public OrganizationalUnit? Parent { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; }

    public IReadOnlyCollection<OrganizationalUnit> Children => _children.AsReadOnly();

    public IReadOnlyCollection<Host> Hosts => _hosts.AsReadOnly();

    public IReadOnlyCollection<UserOrganizationalUnit> UserOrganizationalUnits => _userOrganizationalUnits.AsReadOnly();

    private OrganizationalUnit() { }

    internal OrganizationalUnit(string name, OrganizationalUnit? parent = null)
    {
        Name = name;
        Parent = parent;
        ParentId = parent?.Id;
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

    public void AddComputer(string name, IPAddress ipAddress, PhysicalAddress macAddress)
    {
        var computer = new Host(name, ipAddress, macAddress);
        computer.SetOrganizationalUnit(Id);

        _hosts.Add(computer);
    }

    internal void AddExistingComputer(Host host)
    {
        if (_hosts.Any(c => c.Id == host.Id))
        {
            throw new InvalidOperationException("Host already exists in this unit.");
        }

        _hosts.Add(host);
    }

    public void RemoveComputer(Guid computerId)
    {
        var computer = _hosts.SingleOrDefault(c => c.Id == computerId);

        _hosts.Remove(computer);
    }

    public void ClearComputers()
    {
        _hosts.Clear();
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
        var userOrganizationalUnit = _userOrganizationalUnits.SingleOrDefault(u => u.UserId == userId) ?? throw new InvalidOperationException("User not found in this unit.");
        _userOrganizationalUnits.Remove(userOrganizationalUnit);
    }

    public void ClearUsers()
    {
        _userOrganizationalUnits.Clear();
    }
}
