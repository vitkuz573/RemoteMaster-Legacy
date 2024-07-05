// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Interop;
using MudBlazor.Services;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Components.Pages;

namespace RemoteMaster.Server.Tests;

public class AccessTests
{
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly TestContext _ctx;

    public AccessTests()
    {
        _ctx = new TestContext();
        SetupServices(_ctx, out _mockAuthStateProvider, out _mockAuthorizationService);
    }

    private static void SetupServices(TestContext ctx, out Mock<AuthenticationStateProvider> mockAuthStateProvider, out Mock<IAuthorizationService> mockAuthorizationService)
    {
        var mockAccessTokenProvider = new Mock<IAccessTokenProvider>();
        var mockQueryParameterService = new Mock<IQueryParameterService>();
        mockAuthorizationService = new Mock<IAuthorizationService>();
        var mockSnackbar = new Mock<ISnackbar>();
        var mockAuthorizationPolicyProvider = new Mock<IAuthorizationPolicyProvider>();

        mockAuthStateProvider = new Mock<AuthenticationStateProvider>();

        ctx.Services.AddSingleton(mockAccessTokenProvider.Object);
        ctx.Services.AddSingleton(mockQueryParameterService.Object);
        ctx.Services.AddSingleton(mockAuthorizationService.Object);
        ctx.Services.AddSingleton(mockSnackbar.Object);
        ctx.Services.AddSingleton(mockAuthorizationPolicyProvider.Object);
        ctx.Services.AddSingleton(mockAuthStateProvider.Object);
        ctx.Services.AddMudServices();

        ctx.JSInterop.SetupVoid("mudPopover.initialize", "mudblazor-main-content", 0);
        ctx.JSInterop.Setup<BoundingClientRect>("mudElementRef.getBoundingClientRect", _ => true);
        ctx.JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);

        var module = ctx.JSInterop.SetupModule("./js/eventListeners.js");
        module.SetupVoid("addPreventCtrlSListener");
        module.SetupVoid("addBeforeUnloadListener", _ => true);
        module.SetupVoid("addKeyDownEventListener", _ => true);
        module.SetupVoid("addKeyUpEventListener", _ => true);
        module.SetupVoid("preventDefaultForKeydownWhenDrawerClosed", _ => true);
    }

    [Fact]
    public void ComponentRendersCorrectly()
    {
        // Arrange
        var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);

        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        // Act
        var cut = _ctx.RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<Access>(0);
                builder.AddAttribute(1, "Host", "test-host");
                builder.CloseComponent();
            })
        );

        // Assert
        Assert.Contains("Establishing connection...", cut.Markup);
    }

    [Fact]
    public void AccessDenied_ShowsErrorMessage()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(user));
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);

        _mockAuthorizationService
            .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        // Act
        var cut = _ctx.RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<Access>(0);
                builder.AddAttribute(1, "Host", "test-host");
                builder.CloseComponent();
            })
        );

        // Assert
        Assert.Contains("Access Denied. Please contact the administrator.", cut.Markup);
    }
}
