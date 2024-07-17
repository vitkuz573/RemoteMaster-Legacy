// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class AccessTokenProviderTests
{
    private readonly Mock<ITokenService> _mockTokenService = new();
    private readonly Mock<ITokenStorageService> _mockTokenStorageService = new();
    private readonly FakeNavigationManager _fakeNavigationManager = new("http://localhost/", "http://localhost/");

    [Fact]
    public async Task GetAccessTokenAsync_ValidAccessToken_ReturnsAccessToken()
    {
        // Arrange
        const string userId = "user1";
        const string accessToken = "validAccessToken";

        _mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId)).ReturnsAsync(accessToken);
        _mockTokenService.Setup(s => s.IsTokenValid(accessToken)).Returns(true);

        var provider = new AccessTokenProvider(_mockTokenService.Object, _mockTokenStorageService.Object, _fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.Equal(accessToken, result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_InvalidAccessToken_ValidRefreshToken_ReturnsNewAccessToken()
    {
        // Arrange
        const string userId = "user1";
        const string invalidAccessToken = "invalidAccessToken";
        const string validRefreshToken = "validRefreshToken";
        const string newAccessToken = "newAccessToken";
        var tokenData = new TokenData { AccessToken = newAccessToken };

        _mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId)).ReturnsAsync(invalidAccessToken);
        _mockTokenService.Setup(s => s.IsTokenValid(invalidAccessToken)).Returns(false);
        _mockTokenStorageService.Setup(s => s.GetRefreshTokenAsync(userId)).ReturnsAsync(validRefreshToken);
        _mockTokenService.Setup(s => s.IsRefreshTokenValid(validRefreshToken)).Returns(true);
        _mockTokenService.Setup(s => s.GenerateTokensAsync(userId, validRefreshToken)).ReturnsAsync(tokenData);
        _mockTokenStorageService.Setup(s => s.StoreTokensAsync(userId, tokenData)).Returns(Task.CompletedTask);

        var provider = new AccessTokenProvider(_mockTokenService.Object, _mockTokenStorageService.Object, _fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.Equal(newAccessToken, result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_InvalidAccessToken_InvalidRefreshToken_ClearsTokensAndRedirects()
    {
        // Arrange
        const string userId = "user1";
        const string invalidAccessToken = "invalidAccessToken";
        const string invalidRefreshToken = "invalidRefreshToken";

        _mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId)).ReturnsAsync(invalidAccessToken);
        _mockTokenService.Setup(s => s.IsTokenValid(invalidAccessToken)).Returns(false);
        _mockTokenStorageService.Setup(s => s.GetRefreshTokenAsync(userId)).ReturnsAsync(invalidRefreshToken);
        _mockTokenService.Setup(s => s.IsRefreshTokenValid(invalidRefreshToken)).Returns(false);
        _mockTokenStorageService.Setup(s => s.ClearTokensAsync(userId)).Returns(Task.CompletedTask);

        var provider = new AccessTokenProvider(_mockTokenService.Object, _mockTokenStorageService.Object, _fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.Null(result);
        _mockTokenStorageService.Verify(s => s.ClearTokensAsync(userId), Times.Once);
        Assert.Equal("http://localhost/Account/Logout", _fakeNavigationManager.Uri);
        Assert.True(_fakeNavigationManager.ForceLoad);
    }
}
