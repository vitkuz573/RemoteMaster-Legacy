// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;

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
    private readonly List<CertificateRenewalTask> _certificateRenewalTasks = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Address Address { get; private set; }

    public IReadOnlyCollection<OrganizationalUnit> OrganizationalUnits => _organizationalUnits.AsReadOnly();
    
    public IReadOnlyCollection<UserOrganization> UserOrganizations => _userOrganizations.AsReadOnly();

    public IReadOnlyCollection<CertificateRenewalTask> CertificateRenewalTasks => _certificateRenewalTasks.AsReadOnly();

    public void SetName(string newName)
    {
        Name = newName ?? throw new ArgumentNullException(nameof(newName));
    }

    public void SetAddress(Address address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));

        foreach (var unit in _organizationalUnits)
        {
            foreach (var computer in unit.Computers)
            {
                CreateCertificateRenewalTask(computer.Id, DateTime.UtcNow.AddHours(1));
            }
        }
    }

    public void AddOrganizationalUnit(string unitName, Guid? parentId = null)
    {
        OrganizationalUnit? parentUnit = null;

        if (parentId.HasValue)
        {
            parentUnit = _organizationalUnits.SingleOrDefault(u => u.Id == parentId.Value);

            if (parentUnit == null)
            {
                throw new InvalidOperationException("Parent unit not found.");
            }
        }

        var unit = new OrganizationalUnit(unitName, parentUnit);

        unit.AssignToOrganization(this);

        _organizationalUnits.Add(unit);
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

        unit.ClearComputers();
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

    public CertificateRenewalTask CreateCertificateRenewalTask(Guid computerId, DateTime plannedDate)
    {
        var unit = _organizationalUnits.SingleOrDefault(u => u.Computers.Any(c => c.Id == computerId)) ?? throw new InvalidOperationException("Computer not found in this organization.");
        var computer = unit.Computers.Single(c => c.Id == computerId);

        var task = new CertificateRenewalTask(computer, this, plannedDate);

        _certificateRenewalTasks.Add(task);

        return task;
    }

    public void RemoveCertificateRenewalTask(Guid taskId)
    {
        var task = _certificateRenewalTasks.SingleOrDefault(t => t.Id == taskId) ?? throw new InvalidOperationException("Certificate renewal task not found.");
        
        _certificateRenewalTasks.Remove(task);
    }
}
