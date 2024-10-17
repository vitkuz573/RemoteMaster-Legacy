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
    private readonly Mock<IApplicationUnitOfWork> _applicationUnitOfWorkMock;
    private readonly OrganizationService _organizationService;

    public OrganizationServiceTests()
    {
        _applicationUnitOfWorkMock = new Mock<IApplicationUnitOfWork>();
        Mock<IDomainEventDispatcher> domainEventDispatcherMock = new();

        var organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _applicationUnitOfWorkMock.Setup(uow => uow.Organizations).Returns(organizationRepositoryMock.Object);

        _organizationService = new OrganizationService(_applicationUnitOfWorkMock.Object, domainEventDispatcherMock.Object);
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

        _applicationUnitOfWorkMock.Setup(uow => uow.Organizations.GetAllAsync()).ReturnsAsync(organizations);

        // Act
        var result = await _organizationService.GetAllOrganizationsAsync();

        // Assert
        Assert.Equal(2, result.Count());
        _applicationUnitOfWorkMock.Verify(uow => uow.Organizations.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateOrganizationAsync_ShouldCreateOrganization_WhenDtoDoesNotHaveId()
    {
        // Arrange
        var dto = new OrganizationDto(null, "New Org", new AddressDto("City1", "State1", "US"));
        _applicationUnitOfWorkMock.Setup(uow => uow.Organizations.AddAsync(It.IsAny<Organization>())).Returns(Task.CompletedTask);

        // Act
        var result = await _organizationService.AddOrUpdateOrganizationAsync(dto);

        // Assert
        Assert.Equal("Organization created successfully.", result);
        _applicationUnitOfWorkMock.Verify(uow => uow.Organizations.AddAsync(It.IsAny<Organization>()), Times.Once);
        _applicationUnitOfWorkMock.Verify(uow => uow.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateOrganizationAsync_ShouldUpdateOrganization_WhenDtoHasId()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));
        var dto = new OrganizationDto(organization.Id, "Updated Org", new AddressDto("City", "State", "US"));

        _applicationUnitOfWorkMock.Setup(uow => uow.Organizations.GetByIdAsync(organization.Id)).ReturnsAsync(organization);
        _applicationUnitOfWorkMock.Setup(uow => uow.Organizations.Update(organization));

        // Act
        var result = await _organizationService.AddOrUpdateOrganizationAsync(dto);

        // Assert
        Assert.Equal("Organization updated successfully.", result);
        _applicationUnitOfWorkMock.Verify(uow => uow.Organizations.Update(organization), Times.Once);
        _applicationUnitOfWorkMock.Verify(uow => uow.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddOrUpdateOrganizationAsync_ShouldReturnError_WhenOrganizationNotFound()
    {
        // Arrange
        var dto = new OrganizationDto(Guid.NewGuid(), "Nonexistent Org", new AddressDto("City", "State", "US"));
        _applicationUnitOfWorkMock.Setup(uow => uow.Organizations.GetByIdAsync(dto.Id!.Value)).ReturnsAsync((Organization)null!);

        // Act
        var result = await _organizationService.AddOrUpdateOrganizationAsync(dto);

        // Assert
        Assert.Equal("Error: Organization not found.", result);
        _applicationUnitOfWorkMock.Verify(uow => uow.Organizations.Update(It.IsAny<Organization>()), Times.Never);
        _applicationUnitOfWorkMock.Verify(uow => uow.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteOrganizationAsync_ShouldDeleteOrganization()
    {
        // Arrange
        var organization = new Organization("Org", new Address("City", "State", new CountryCode("US")));

        _applicationUnitOfWorkMock.Setup(uow => uow.Organizations.Delete(organization));

        // Act
        var result = await _organizationService.DeleteOrganizationAsync(organization);

        // Assert
        Assert.Equal("Organization deleted successfully.", result);
        _applicationUnitOfWorkMock.Verify(uow => uow.Organizations.Delete(organization), Times.Once);
        _applicationUnitOfWorkMock.Verify(uow => uow.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
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

        _applicationUnitOfWorkMock.Setup(uow => uow.Organizations.GetByIdAsync(organization.Id)).ReturnsAsync(organization);

        // Act
        await _organizationService.RemoveHostAsync(organization.Id, unit.Id, host.Id);

        // Assert
        _applicationUnitOfWorkMock.Verify(uow => uow.Organizations.RemoveHostAsync(organization.Id, unit.Id, host.Id), Times.Once);
        _applicationUnitOfWorkMock.Verify(uow => uow.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
