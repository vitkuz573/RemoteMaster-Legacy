// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.AuthorizationHandlers;
using RemoteMaster.Host.Core.Requirements;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Tests;

public class LocalhostOrAuthenticatedHandlerTests
{
    private readonly DefaultHttpContext _httpContext;
    private readonly LocalhostOrAuthenticatedRequirement _requirement;
    private readonly LocalhostOrAuthenticatedHandler _handler;

    public LocalhostOrAuthenticatedHandlerTests()
    {
        _httpContext = new DefaultHttpContext();
        _requirement = new LocalhostOrAuthenticatedRequirement();

        Mock<IHostConfigurationService> hostConfigurationServiceMock = new();
        var host = new HostDto("TestHost", IPAddress.Parse("192.168.1.1"), PhysicalAddress.Parse("00-14-22-01-23-45"));
        hostConfigurationServiceMock
            .Setup(h => h.LoadAsync())
            .ReturnsAsync(new HostConfiguration(It.IsAny<string>(), It.IsAny<SubjectDto>(), host));

        Mock<IHttpContextAccessor> httpContextAccessorMock = new();
        httpContextAccessorMock.Setup(h => h.HttpContext).Returns(_httpContext);

        _handler = new LocalhostOrAuthenticatedHandler(httpContextAccessorMock.Object, hostConfigurationServiceMock.Object);
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromConfiguredIp_Succeeds()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        _httpContext.Request.Headers["Service-Flag"] = true.ToString();

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.True(authorizationHandlerContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromMappedIPv6_Succeeds()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.168.1.1");
        _httpContext.Request.Headers["Service-Flag"] = true.ToString();

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.True(authorizationHandlerContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromNonLocalhostAndUserNotAuthenticated_Fails()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.10.10");

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
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        _httpContext.Request.Headers["Service-Flag"] = true.ToString();

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.True(authorizationHandlerContext.HasSucceeded);
        
        var identity = authorizationHandlerContext.User.Identities.FirstOrDefault(i => i.AuthenticationType == "RemoteMaster Security");

        Assert.NotNull(identity);
        Assert.Contains(identity.Claims, c => c is { Type: ClaimTypes.Name, Value: "RCHost" });
        Assert.Contains(identity.Claims, c => c is { Type: ClaimTypes.Role, Value: "System Service" });
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromNonLocalhostAndUserAuthenticated_Succeeds()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.10.10");

        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "User"),
        ], "TestAuthType"));

        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.True(authorizationHandlerContext.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_RequestFromNonLocalhostWithServiceFlag_Fails()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.168.10.10");
        _httpContext.Request.Headers["Service-Flag"] = true.ToString();

        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var authorizationHandlerContext = new AuthorizationHandlerContext([_requirement], user, null);

        // Act
        await _handler.HandleAsync(authorizationHandlerContext);

        // Assert
        Assert.False(authorizationHandlerContext.HasSucceeded);
    }
}
