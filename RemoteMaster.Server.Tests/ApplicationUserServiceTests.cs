// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.Http;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class ApplicationUserServiceTests
{
    private readonly Mock<IApplicationUnitOfWork> _applicationUnitOfWorkMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IApplicationUserRepository> _applicationUserRepositoryMock;
    private readonly ApplicationUserService _applicationUserService;

    public ApplicationUserServiceTests()
    {
        _applicationUnitOfWorkMock = new Mock<IApplicationUnitOfWork>();
        _applicationUserRepositoryMock = new Mock<IApplicationUserRepository>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _applicationUnitOfWorkMock.Setup(uow => uow.ApplicationUsers).Returns(_applicationUserRepositoryMock.Object);

        _applicationUserService = new ApplicationUserService(
            _applicationUnitOfWorkMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    [Fact]
    public async Task AddSignInEntry_ShouldAddSignInEntry_WhenUserIsValid()
    {
        // Arrange
        var user = new ApplicationUser();
        var httpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Loopback
            }
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _applicationUserService.AddSignInEntry(user, true);

        // Assert
        _applicationUserRepositoryMock.Verify(x => x.AddSignInEntryAsync(It.IsAny<string>(), true, IPAddress.Loopback), Times.Once);
        _applicationUnitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSignInEntry_ShouldThrowArgumentNullException_WhenUserIsNull()
    {
        // Arrange
        ApplicationUser user = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _applicationUserService.AddSignInEntry(user, true));
    }

    [Fact]
    public async Task AddSignInEntry_ShouldThrowInvalidOperationException_WhenHttpContextIsNull()
    {
        // Arrange
        var user = new ApplicationUser();

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _applicationUserService.AddSignInEntry(user, true));
    }

    [Fact]
    public async Task AddSignInEntry_ShouldUseNoneIpAddress_WhenIpAddressIsNull()
    {
        // Arrange
        var user = new ApplicationUser();
        var httpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = null
            }
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _applicationUserService.AddSignInEntry(user, true);

        // Assert
        _applicationUserRepositoryMock.Verify(x => x.AddSignInEntryAsync(It.IsAny<string>(), true, IPAddress.None), Times.Once);
        _applicationUnitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSignInEntry_ShouldCallRepositoryWithCorrectParameters_WhenSignInFails()
    {
        // Arrange
        var user = new ApplicationUser();
        var httpContext = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = IPAddress.Parse("192.168.1.1")
            }
        };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _applicationUserService.AddSignInEntry(user, false);

        // Assert
        _applicationUserRepositoryMock.Verify(x => x.AddSignInEntryAsync(It.IsAny<string>(), false, IPAddress.Parse("192.168.1.1")), Times.Once);
        _applicationUnitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}