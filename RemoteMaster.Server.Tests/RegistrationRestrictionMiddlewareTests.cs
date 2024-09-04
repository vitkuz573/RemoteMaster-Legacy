// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Middlewares;

namespace RemoteMaster.Server.Tests;

public class RegistrationRestrictionMiddlewareTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly RequestDelegate _next;

    public RegistrationRestrictionMiddlewareTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(roleStore.Object, null!, null!, null!, null!);

        _next = new RequestDelegate((innerHttpContext) => Task.CompletedTask);
    }

    private HttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = path
            }
        };

        var services = new ServiceCollection();
        services.AddSingleton(_mockUserManager.Object);
        services.AddSingleton(_mockRoleManager.Object);
        context.RequestServices = services.BuildServiceProvider();

        return context;
    }

    [Fact]
    public async Task InvokeAsync_RootAdministratorExists_RedirectsToHome()
    {
        // Arrange
        var middleware = new RegistrationRestrictionMiddleware(_next);

        var context = CreateHttpContext("/account/register");

        _mockRoleManager.Setup(r => r.RoleExistsAsync("RootAdministrator")).ReturnsAsync(true);
        _mockUserManager.Setup(u => u.GetUsersInRoleAsync("RootAdministrator")).ReturnsAsync([new ApplicationUser()]);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("/", context.Response.Headers.Location);
    }

    [Fact]
    public async Task InvokeAsync_NoRootAdministrator_AllowsRegistration()
    {
        // Arrange
        var middleware = new RegistrationRestrictionMiddleware(_next);

        var context = CreateHttpContext("/account/register");

        _mockRoleManager.Setup(r => r.RoleExistsAsync("RootAdministrator")).ReturnsAsync(false);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task InvokeAsync_NotRegistrationRoute_ContinuesPipeline()
    {
        // Arrange
        var middleware = new RegistrationRestrictionMiddleware(_next);

        var context = CreateHttpContext("/some/other/path");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        // If the next middleware is called, the response status code should remain 200 (default).
        Assert.Equal(200, context.Response.StatusCode);
    }
}