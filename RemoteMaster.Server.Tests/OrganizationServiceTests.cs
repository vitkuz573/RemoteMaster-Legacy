// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.DTOs;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Tests;

public class OrganizationServiceTests
{
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly Mock<IApplicationUserRepository> _applicationUserRepositoryMock;
    private readonly OrganizationService _organizationService;

    public OrganizationServiceTests()
    {
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _applicationUserRepositoryMock = new Mock<IApplicationUserRepository>();
        _organizationService = new OrganizationService(_organizationRepositoryMock.Object, _applicationUserRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllOrganizationsAsync_ShouldReturnAllOrganizations()
    {
        // Arrange
        var organizations = new List<Organization>
        {
            new("Org1", new Address("City1", "State1", new CountryCode("US"))),
            new("Org2", new Address("City2", "State2", new CountryCode("GB")))
        };
        _organizationRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(organizations);

        // Act
        var result = await _organizationService.GetAllOrganizationsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        _organizationRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateOrganizationAsync_ShouldCreateOrganization_WhenDtoDoesNotHaveId()
    {
        // Arrange
        var dto = new OrganizationDto(null, "New Org", new AddressDto("City1", "State1", "US"));
        _organizationRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Organization>())).Returns(Task.CompletedTask);

        // Act
        var result = await _organizationService.AddOrUpdateOrganizationAsync(dto);

        // Assert
        Assert.Equal("Organization created successfully.", result);
        _organizationRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Organization>()), Times.Once);
        _organizationRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateOrganizationAsync_ShouldUpdateOrganization_WhenDtoHasId()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        var dto = new OrganizationDto(organization.Id, "Updated Org", new AddressDto("City", "State", "US"));
        _organizationRepositoryMock.Setup(repo => repo.GetByIdAsync(organization.Id)).ReturnsAsync(organization);
        _organizationRepositoryMock.Setup(repo => repo.Update(organization));

        // Act
        var result = await _organizationService.AddOrUpdateOrganizationAsync(dto);

        // Assert
        Assert.Equal("Organization updated successfully.", result);
        _organizationRepositoryMock.Verify(repo => repo.Update(organization), Times.Once);
        _organizationRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateOrganizationAsync_ShouldReturnError_WhenOrganizationNotFound()
    {
        // Arrange
        var dto = new OrganizationDto(Guid.NewGuid(), "Nonexistent Org", new AddressDto("City", "State", "US"));
        _organizationRepositoryMock.Setup(repo => repo.GetByIdAsync(dto.Id.Value)).ReturnsAsync((Organization)null);

        // Act
        var result = await _organizationService.AddOrUpdateOrganizationAsync(dto);

        // Assert
        Assert.Equal("Error: Organization not found.", result);
        _organizationRepositoryMock.Verify(repo => repo.Update(It.IsAny<Organization>()), Times.Never);
        _organizationRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteOrganizationAsync_ShouldDeleteOrganization()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        _organizationRepositoryMock.Setup(repo => repo.Delete(organization));

        // Act
        var result = await _organizationService.DeleteOrganizationAsync(organization);

        // Assert
        Assert.Equal("Organization deleted successfully.", result);
        _organizationRepositoryMock.Verify(repo => repo.Delete(organization), Times.Once);
        _organizationRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveHostAsync_ShouldRemoveHostFromUnit()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));

        organization.AddOrganizationalUnit("Unit");
        var unit = organization.OrganizationalUnits.First();

        unit.AddHost("Host1", IPAddress.Loopback, PhysicalAddress.Parse("001122334455"));
        var host = unit.Hosts.First();

        _organizationRepositoryMock.Setup(repo => repo.GetByIdAsync(organization.Id)).ReturnsAsync(organization);

        // Act
        await _organizationService.RemoveHostAsync(organization.Id, unit.Id, host.Id);

        // Assert
        _organizationRepositoryMock.Verify(repo => repo.RemoveHostAsync(organization.Id, unit.Id, host.Id), Times.Once);
        _organizationRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }
}
