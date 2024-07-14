// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using RemoteMaster.Host.Core.AuthorizationHandlers;
using RemoteMaster.Host.Core.Requirements;

namespace RemoteMaster.Host.Core.Tests;

public class LocalhostOrAuthenticatedHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly DefaultHttpContext _httpContext;
    private readonly LocalhostOrAuthenticatedRequirement _requirement;
    private readonly LocalhostOrAuthenticatedHandler _handler;

    public LocalhostOrAuthenticatedHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(_ => _.HttpContext).Returns(_httpContext);

        _requirement = new LocalhostOrAuthenticatedRequirement();
        _handler = new LocalhostOrAuthenticatedHandler(_httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromLocalhostWithServiceFlag_Succeeds()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        _httpContext.Request.Headers["X-Service-Flag"] = "true";

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.True(authorizationHandlerContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserIsAuthenticated_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Role, "User"),
        ], "TestAuthType"));

        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.True(authorizationHandlerContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromLocalhostWithoutServiceFlag_Fails()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.False(authorizationHandlerContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromNonLocalhostAndUserNotAuthenticated_Fails()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.False(authorizationHandlerContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromLocalhostAddsServiceClaim_Succeeds()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        _httpContext.Request.Headers["X-Service-Flag"] = "true";

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.True(authorizationHandlerContext.HasSucceeded);
        var identity = authorizationHandlerContext.User?.Identities.FirstOrDefault(i => i.AuthenticationType == "RemoteMaster Security");
        Assert.NotNull(identity);
        Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Name && c.Value == "RCHost");
        Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Windows Service");
    }
}