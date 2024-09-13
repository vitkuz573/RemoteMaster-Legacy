// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
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
    public async Task RemoveComputerAsync_RemovesComputer()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        organization.AddOrganizationalUnit("Unit 1");
        var unit = organization.OrganizationalUnits.First();
        unit.AddComputer("Comp1", "192.168.1.1", "00:11:22:33:44:55");
        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        var computerId = unit.Computers.First().Id;

        // Act
        await _repository.RemoveComputerAsync(organization.Id, unit.Id, computerId);
        await _repository.SaveChangesAsync();

        // Assert
        var updatedOrganization = await _repository.GetByIdAsync(organization.Id);
        Assert.Empty(updatedOrganization.OrganizationalUnits.First().Computers);
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
}
