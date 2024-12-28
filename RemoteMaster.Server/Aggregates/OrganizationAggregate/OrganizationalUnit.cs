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

    public string Name { get; private set; } = null!;

    public Guid? ParentId { get; private set; }

    public OrganizationalUnit? Parent { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public IReadOnlyCollection<OrganizationalUnit> Children => _children.AsReadOnly();

    public IReadOnlyCollection<Host> Hosts => _hosts.AsReadOnly();

    public IReadOnlyCollection<UserOrganizationalUnit> UserOrganizationalUnits => _userOrganizationalUnits.AsReadOnly();

    private OrganizationalUnit() { }

    internal OrganizationalUnit(string name, Guid? parentId)
    {
        Name = name;
        ParentId = parentId;
    }

    public void SetName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public void SetParent(Guid? parentId)
    {
        ParentId = parentId;
    }

    public void AssignToOrganization(Guid organizationId)
    {
        OrganizationId = organizationId;
    }

    public void AddHost(string name, IPAddress ipAddress, PhysicalAddress macAddress)
    {
        var host = new Host(name, ipAddress, macAddress);
        host.SetOrganizationalUnit(Id);

        _hosts.Add(host);
    }

    internal void AddExistingHost(Host host)
    {
        if (_hosts.Any(h => h.Id == host.Id))
        {
            throw new InvalidOperationException("Host already exists in this unit.");
        }

        _hosts.Add(host);
    }

    public void RemoveHost(Guid hostId)
    {
        var host = _hosts.SingleOrDefault(h => h.Id == hostId) ?? throw new InvalidOperationException("Host not found in this unit.");
        
        _hosts.Remove(host);
    }

    public void ClearHosts()
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
