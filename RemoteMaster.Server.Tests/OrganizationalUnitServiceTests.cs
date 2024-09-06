// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate.ValueObjects;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class OrganizationalUnitServiceTests
{
    private readonly Mock<IOrganizationalUnitRepository> _organizationalUnitRepositoryMock;
    private readonly OrganizationalUnitService _service;

    public OrganizationalUnitServiceTests()
    {
        Mock<IOrganizationRepository> organizationRepositoryMock = new();
        _organizationalUnitRepositoryMock = new Mock<IOrganizationalUnitRepository>();
        _service = new OrganizationalUnitService(organizationRepositoryMock.Object, _organizationalUnitRepositoryMock.Object);
    }

    [Fact]
    public async Task UpdateUserOrganizationalUnitsAsync_ShouldAddAndRemoveUsersCorrectly()
    {
        // Arrange
        const string userId = "User1";
        var user = new ApplicationUser { Id = userId };

        var organization = new Organization("Test Organization", new Address("New York", "NY", "US"));

        var unitIdToRemove = Guid.NewGuid();
        var unitIdToAdd = Guid.NewGuid();

        var unitToRemove = new OrganizationalUnit("Unit to Remove", organization);
        var unitToAdd = new OrganizationalUnit("Unit to Add", organization);

        organization.AddOrganizationalUnit(unitToRemove);
        organization.AddOrganizationalUnit(unitToAdd);

        unitToRemove.AddUser(userId);

        _organizationalUnitRepositoryMock
            .Setup(repo => repo.GetByIdAsync(unitIdToRemove))
            .ReturnsAsync(unitToRemove);

        _organizationalUnitRepositoryMock
            .Setup(repo => repo.GetByIdAsync(unitIdToAdd))
            .ReturnsAsync(unitToAdd);

        // Act
        await _service.UpdateUserOrganizationalUnitsAsync(user, new List<Guid> { unitIdToAdd });

        // Assert
        Assert.DoesNotContain(unitToRemove.UserOrganizationalUnits, uou => uou.UserId == userId);
        Assert.Contains(unitToAdd.UserOrganizationalUnits, uou => uou.UserId == userId);

        // Проверяем, что изменения были сохранены в репозитории
        _organizationalUnitRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }
}
