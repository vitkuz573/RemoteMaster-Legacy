// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Repositories;

namespace RemoteMaster.Server.Tests;

public class OrganizationRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly OrganizationRepository _repository;

    public OrganizationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new OrganizationRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsOrganization_WhenOrganizationExists()
    {
        // Arrange
        var organization = new Organization("Test Org", new Address("City", "State", new CountryCode("US")));
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(organization.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Org", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenOrganizationDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrganizations()
    {
        // Arrange
        var org1 = new Organization("Org 1", new Address("City1", "State1", new CountryCode("US")));
        var org2 = new Organization("Org 2", new Address("City2", "State2", new CountryCode("CA")));
        _context.Organizations.AddRange(org1, org2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AddAsync_AddsOrganizationToContext()
    {
        // Arrange
        var organization = new Organization("New Org", new Address("City", "State", new CountryCode("US")));

        // Act
        await _repository.AddAsync(organization);
        await _repository.SaveChangesAsync();

        // Assert
        Assert.Contains(_context.Organizations, o => o.Name == "New Org");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOrganizationInContext()
    {
        // Arrange
        var organization = new Organization("Old Org", new Address("City", "State", new CountryCode("US")));
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Act
        organization.SetName("Updated Org");
        await _repository.UpdateAsync(organization);
        await _repository.SaveChangesAsync();

        // Assert
        var updatedOrganization = await _repository.GetByIdAsync(organization.Id);
        Assert.Equal("Updated Org", updatedOrganization.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOrganizationFromContext()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(organization);
        await _repository.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByIdAsync(organization.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveHostAsync_RemovesHost()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        organization.AddOrganizationalUnit("Unit 1");
        var unit = organization.OrganizationalUnits.First();

        var ipAddress = IPAddress.Parse("192.168.1.1");
        var macAddress = PhysicalAddress.Parse("00:11:22:33:44:55");

        unit.AddHost("Comp1", ipAddress, macAddress);
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        var hostId = unit.Hosts.First().Id;

        // Act
        await _repository.RemoveHostAsync(organization.Id, unit.Id, hostId);
        await _repository.SaveChangesAsync();

        // Assert
        var updatedOrganization = await _repository.GetByIdAsync(organization.Id);
        Assert.Empty(updatedOrganization.OrganizationalUnits.First().Hosts);
    }

    [Fact]
    public async Task FindAsync_ReturnsFilteredResults()
    {
        // Arrange
        var org1 = new Organization("Test1", new Address("City1", "State1", new CountryCode("US")));
        var org2 = new Organization("Test2", new Address("City2", "State2", new CountryCode("CA")));
        _context.Organizations.AddRange(org1, org2);
        await _context.SaveChangesAsync();

        Expression<Func<Organization, bool>> predicate = o => o.Name.Contains("Test1");

        // Act
        var result = await _repository.FindAsync(predicate);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test1", result.First().Name);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsOrganizations_WhenTheyExist()
    {
        // Arrange
        var org1 = new Organization("Org 1", new Address("City1", "State1", new CountryCode("US")));
        var org2 = new Organization("Org 2", new Address("City2", "State2", new CountryCode("CA")));
        _context.Organizations.AddRange(org1, org2);
        await _context.SaveChangesAsync();

        var ids = new List<Guid> { org1.Id, org2.Id };

        // Act
        var result = await _repository.GetByIdsAsync(ids);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, o => o.Id == org1.Id);
        Assert.Contains(result, o => o.Id == org2.Id);
    }

    [Fact]
    public async Task FindHostsAsync_ReturnsFilteredHosts()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        organization.AddOrganizationalUnit("Unit 1");
        var unit = organization.OrganizationalUnits.First();

        var ipAddress1 = IPAddress.Parse("192.168.1.1");
        var ipAddress2 = IPAddress.Parse("192.168.1.2");

        var macAddress1 = PhysicalAddress.Parse("00-11-22-33-44-55");
        var macAddress2 = PhysicalAddress.Parse("00-11-22-33-44-66");

        unit.AddHost("Comp1", ipAddress1, macAddress1);
        unit.AddHost("Comp2", ipAddress2, macAddress2);

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Act
        var macAddress = PhysicalAddress.Parse("00-11-22-33-44-66");
        var result = await _repository.FindHostsAsync(c => c.MacAddress.Equals(macAddress));

        // Assert
        Assert.Single(result);
        Assert.Equal("Comp2", result.First().Name);
    }

    [Fact]
    public async Task GetOrganizationByUnitIdAsync_ReturnsOrganization()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        organization.AddOrganizationalUnit("Unit 1");
        var unit = organization.OrganizationalUnits.First();
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetOrganizationByUnitIdAsync(unit.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(organization.Id, result.Id);
    }

    [Fact]
    public async Task MoveHostAsync_MovesHostBetweenUnits()
    {
        // Arrange
        var org1 = new Organization("Org 1", new Address("City1", "State1", new CountryCode("US")));
        var org2 = new Organization("Org 2", new Address("City2", "State2", new CountryCode("CA")));
        org1.AddOrganizationalUnit("Unit 1");
        org2.AddOrganizationalUnit("Unit 2");

        var unit1 = org1.OrganizationalUnits.First();
        var unit2 = org2.OrganizationalUnits.First();

        var ipAddress = IPAddress.Parse("192.168.1.1");
        var macAddress = PhysicalAddress.Parse("00-11-22-33-44-55");

        unit1.AddHost("Comp1", ipAddress, macAddress);

        _context.Organizations.AddRange(org1, org2);
        await _context.SaveChangesAsync();

        var hostId = unit1.Hosts.First().Id;

        // Act
        await _repository.MoveHostAsync(org1.Id, org2.Id, hostId, unit1.Id, unit2.Id);
        await _context.SaveChangesAsync();

        // Assert
        var updatedOrg1 = await _repository.GetByIdAsync(org1.Id);
        var updatedOrg2 = await _repository.GetByIdAsync(org2.Id);
        Assert.Empty(updatedOrg1.OrganizationalUnits.First().Hosts);
        Assert.Single(updatedOrg2.OrganizationalUnits.First().Hosts);
    }

    [Fact]
    public async Task CreateCertificateRenewalTaskAsync_CreatesTask()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        organization.AddOrganizationalUnit("Unit 1");
        var unit = organization.OrganizationalUnits.First();

        var ipAddress = IPAddress.Parse("192.168.1.1");
        var macAddress = PhysicalAddress.Parse("00-11-22-33-44-55");

        unit.AddHost("Comp1", ipAddress, macAddress);
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        var hostId = unit.Hosts.First().Id;
        var plannedDate = DateTimeOffset.Now.AddDays(1);

        // Act
        await _repository.CreateCertificateRenewalTaskAsync(organization.Id, hostId, plannedDate);
        await _repository.SaveChangesAsync();

        // Assert
        var tasks = await _repository.GetAllCertificateRenewalTasksAsync();
        Assert.Single(tasks);
        Assert.Equal(plannedDate, tasks.First().PlannedDate);
    }
}
