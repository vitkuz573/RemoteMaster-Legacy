// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Linq.Expressions;
using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using MudBlazor;
using MudBlazor.Interop;
using MudBlazor.Services;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Components.Pages;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class HomeTests
{
    private readonly TestContext _testContext;
    private readonly Mock<IDatabaseService> _mockDatabaseService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IAccessTokenProvider> _mockAccessTokenProvider;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IStringLocalizer<Home>> _mockLocalizer;
    private readonly Mock<ICrlService> _mockCrlService;
    private readonly Mock<ISnackbar> _mockSnackbar;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<IBrandingService> _mockBrandingService;
    private readonly Mock<IAuthorizationPolicyProvider> _mockAuthorizationPolicyProvider;
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;

    public HomeTests()
    {
        _testContext = new TestContext();
        _mockDatabaseService = new Mock<IDatabaseService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockAccessTokenProvider = new Mock<IAccessTokenProvider>();
        _mockUserManager = MockUserManager<ApplicationUser>();
        _mockLocalizer = new Mock<IStringLocalizer<Home>>();
        _mockCrlService = new Mock<ICrlService>();
        _mockSnackbar = new Mock<ISnackbar>();
        _mockDialogService = new Mock<IDialogService>();
        _mockBrandingService = new Mock<IBrandingService>();
        _mockAuthorizationPolicyProvider = new Mock<IAuthorizationPolicyProvider>();
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        _mockAuthorizationService = new Mock<IAuthorizationService>();

        SetupServices();
    }

    private void SetupServices()
    {
        _testContext.Services.AddMudServices();
        _testContext.Services.AddSingleton(_mockDatabaseService.Object);
        _testContext.Services.AddSingleton(_mockHttpContextAccessor.Object);
        _testContext.Services.AddSingleton(_mockAccessTokenProvider.Object);
        _testContext.Services.AddSingleton(_mockUserManager.Object);
        _testContext.Services.AddSingleton(_mockLocalizer.Object);
        _testContext.Services.AddSingleton(_mockCrlService.Object);
        _testContext.Services.AddSingleton(_mockSnackbar.Object);
        _testContext.Services.AddSingleton(_mockDialogService.Object);
        _testContext.Services.AddSingleton(_mockBrandingService.Object);
        _testContext.Services.AddSingleton(_mockAuthorizationPolicyProvider.Object);
        _testContext.Services.AddSingleton(_mockAuthStateProvider.Object);
        _testContext.Services.AddSingleton(_mockAuthorizationService.Object);

        _testContext.JSInterop.Mode = JSRuntimeMode.Loose;
        _testContext.JSInterop.SetupVoid("mudPopover.initialize", "mudblazor-main-content", 0);
        _testContext.JSInterop.Setup<BoundingClientRect>("mudElementRef.getBoundingClientRect", _ => true);
        _testContext.JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        _testContext.JSInterop.Setup<IEnumerable<BoundingClientRect>>("mudResizeObserver.connect", _ => true).SetResult(Enumerable.Empty<BoundingClientRect>());
        _testContext.JSInterop.SetupVoid("mudResizeObserver.disconnect", _ => true);

        // Set up the default authorization policy
        _mockAuthorizationPolicyProvider
            .Setup(x => x.GetPolicyAsync(It.IsAny<string>()))
            .ReturnsAsync(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        _mockAuthorizationPolicyProvider
            .Setup(x => x.GetDefaultPolicyAsync())
            .ReturnsAsync(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // Mock HttpContextAccessor
        var context = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);
    }

    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        var userManager = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);
        return userManager;
    }

    [Fact]
    public async Task HomeComponent_InitializesCorrectly()
    {
        // Arrange
        var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);
        _mockAuthorizationService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                                 .ReturnsAsync(AuthorizationResult.Success);

        var cut = _testContext.RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<Home>(0);
                builder.CloseComponent();
            })
        );

        var homeComponent = cut.FindComponent<Home>();

        _mockDatabaseService.Setup(service => service.GetNodesAsync(It.IsAny<Expression<Func<Organization, bool>>>()))
            .ReturnsAsync(new List<Organization>());
        _mockDatabaseService.Setup(service => service.GetNodesAsync(It.IsAny<Expression<Func<OrganizationalUnit, bool>>>()))
            .ReturnsAsync(new List<OrganizationalUnit>());
        _mockDatabaseService.Setup(service => service.GetNodesAsync(It.IsAny<Expression<Func<Computer, bool>>>()))
            .ReturnsAsync(new List<Computer>());

        // Act
        await homeComponent.Instance.InitializeAsync();

        // Assert
        Assert.NotNull(homeComponent.Instance.GetTreeItems());
        Assert.Empty(homeComponent.Instance.GetTreeItems());
    }

    [Fact]
    public void HomeComponent_TogglesDrawer()
    {
        // Arrange
        var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);
        _mockAuthorizationService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                                 .ReturnsAsync(AuthorizationResult.Success);

        var cut = _testContext.RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<Home>(0);
                builder.CloseComponent();
            })
        );

        var homeComponent = cut.FindComponent<Home>();

        // Act
        homeComponent.Instance.ToggleDrawer();

        // Assert
        Assert.True(homeComponent.Instance.DrawerOpen);

        // Act
        homeComponent.Instance.ToggleDrawer();

        // Assert
        Assert.False(homeComponent.Instance.DrawerOpen);
    }
}
