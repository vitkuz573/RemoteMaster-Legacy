// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Moq;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class ClaimsServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly ClaimsService _claimsService;

    public ClaimsServiceTests()
    {
        _mockUserManager = MockUserManager<ApplicationUser>();
        _mockRoleManager = MockRoleManager<IdentityRole>();

        _claimsService = new ClaimsService(_mockUserManager.Object, _mockRoleManager.Object);
    }

    [Fact]
    public async Task GetClaimsForUserAsync_GeneratesCorrectClaims()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user-id", UserName = "testuser" };
        _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(["Admin"]);
        _mockRoleManager.Setup(rm => rm.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new IdentityRole("Admin"));
        _mockRoleManager.Setup(rm => rm.GetClaimsAsync(It.IsAny<IdentityRole>())).ReturnsAsync([new("Permission", "CanView")]);

        // Act
        var result = await _claimsService.GetClaimsForUserAsync(user);

        // Assert
        Assert.True(result.IsSuccess);
        var claims = result.Value;
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        Assert.Contains(claims, c => c is { Type: ClaimTypes.Role, Value: "Admin" });
        Assert.Contains(claims, c => c is { Type: "Permission", Value: "CanView" });
    }

    [Fact]
    public async Task GetClaimsForUserAsync_UserIsNull_ThrowsArgumentNullException()
    {
        // Act
        var result = await _claimsService.GetClaimsForUserAsync(null!);

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.First();
        Assert.Equal("Failed to retrieve claims for user.", errorDetails.Message);

        var exceptionError = errorDetails.Reasons.OfType<ExceptionalError>().FirstOrDefault();
        Assert.NotNull(exceptionError);
        Assert.IsType<ArgumentNullException>(exceptionError.Exception);
    }

    [Fact]
    public async Task GetClaimsForUserAsync_UserHasNoRoles_ReturnsBasicClaims()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user-id", UserName = "testuser" };
        _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);

        // Act
        var result = await _claimsService.GetClaimsForUserAsync(user);

        // Assert
        Assert.True(result.IsSuccess);
        var claims = result.Value;
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        Assert.DoesNotContain(claims, c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public async Task GetClaimsForUserAsync_RoleDoesNotExist_ReturnsBasicClaims()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user-id", UserName = "testuser" };
        _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(["NonExistentRole"]);
        _mockRoleManager.Setup(rm => rm.FindByNameAsync("NonExistentRole")).ReturnsAsync((IdentityRole)null!);
        _mockRoleManager.Setup(rm => rm.GetClaimsAsync(It.IsAny<IdentityRole>())).ReturnsAsync([]);

        // Act
        var result = await _claimsService.GetClaimsForUserAsync(user);

        // Assert
        Assert.True(result.IsSuccess);
        var claims = result.Value;
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        Assert.DoesNotContain(claims, c => c is { Type: ClaimTypes.Role, Value: "NonExistentRole" });
    }

    [Fact]
    public async Task GetClaimsForUserAsync_RoleHasNoClaims_ReturnsBasicClaimsWithRole()
    {
        // Arrange
        var user = new ApplicationUser { Id = "test-user-id", UserName = "testuser" };
        _mockUserManager.Setup(um => um.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(["Admin"]);
        _mockRoleManager.Setup(rm => rm.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new IdentityRole("Admin"));
        _mockRoleManager.Setup(rm => rm.GetClaimsAsync(It.IsAny<IdentityRole>())).ReturnsAsync([]);

        // Act
        var result = await _claimsService.GetClaimsForUserAsync(user);

        // Assert
        Assert.True(result.IsSuccess);
        var claims = result.Value;
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        Assert.Contains(claims, c => c is { Type: ClaimTypes.Role, Value: "Admin" });
        Assert.DoesNotContain(claims, c => c.Type == "Permission");
    }

    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();

        return new Mock<UserManager<TUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
    {
        var store = new Mock<IRoleStore<TRole>>();
        var roles = new List<IRoleValidator<TRole>> { new RoleValidator<TRole>() };

        return new Mock<RoleManager<TRole>>(store.Object, roles, null!, null!, null!);
    }
}
