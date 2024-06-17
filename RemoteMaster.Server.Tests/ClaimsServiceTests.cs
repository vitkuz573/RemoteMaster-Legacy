// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Moq;
using RemoteMaster.Server.Data;
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
        _mockRoleManager.Setup(rm => rm.GetClaimsAsync(It.IsAny<IdentityRole>())).ReturnsAsync([new Claim("Permission", "CanView")]);

        // Act
        var claims = await _claimsService.GetClaimsForUserAsync(user);

        // Assert
        Assert.Contains(claims, c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        Assert.Contains(claims, c => c.Type == "Permission" && c.Value == "CanView");
    }

    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();

        return new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
    }

    private static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
    {
        var store = new Mock<IRoleStore<TRole>>();
        var roles = new List<IRoleValidator<TRole>> { new RoleValidator<TRole>() };

        return new Mock<RoleManager<TRole>>(store.Object, roles, null, null, null);
    }
}

