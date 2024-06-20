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
    private readonly Mock<ITokenService> mockTokenService;
    private readonly Mock<ITokenStorageService> mockTokenStorageService;
    private readonly FakeNavigationManager fakeNavigationManager;

    public AccessTokenProviderTests()
    {
        mockTokenService = new Mock<ITokenService>();
        mockTokenStorageService = new Mock<ITokenStorageService>();
        fakeNavigationManager = new FakeNavigationManager("http://localhost/", "http://localhost/");
    }

    [Fact]
    public async Task GetAccessTokenAsync_ValidAccessToken_ReturnsAccessToken()
    {
        // Arrange
        var userId = "user1";
        var accessToken = "validAccessToken";

        mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId)).ReturnsAsync(accessToken);
        mockTokenService.Setup(s => s.IsTokenValid(accessToken)).Returns(true);

        var provider = new AccessTokenProvider(mockTokenService.Object, mockTokenStorageService.Object, fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.Equal(accessToken, result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_InvalidAccessToken_ValidRefreshToken_ReturnsNewAccessToken()
    {
        // Arrange
        var userId = "user1";
        var invalidAccessToken = "invalidAccessToken";
        var validRefreshToken = "validRefreshToken";
        var newAccessToken = "newAccessToken";
        var tokenData = new TokenData { AccessToken = newAccessToken };

        mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId)).ReturnsAsync(invalidAccessToken);
        mockTokenService.Setup(s => s.IsTokenValid(invalidAccessToken)).Returns(false);
        mockTokenStorageService.Setup(s => s.GetRefreshTokenAsync(userId)).ReturnsAsync(validRefreshToken);
        mockTokenService.Setup(s => s.IsRefreshTokenValid(validRefreshToken)).Returns(true);
        mockTokenService.Setup(s => s.GenerateTokensAsync(userId, validRefreshToken)).ReturnsAsync(tokenData);
        mockTokenStorageService.Setup(s => s.StoreTokensAsync(userId, tokenData)).Returns(Task.CompletedTask);

        var provider = new AccessTokenProvider(mockTokenService.Object, mockTokenStorageService.Object, fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.Equal(newAccessToken, result);
    }

    [Fact]
    public async Task GetAccessTokenAsync_InvalidAccessToken_InvalidRefreshToken_ClearsTokensAndRedirects()
    {
        // Arrange
        var userId = "user1";
        var invalidAccessToken = "invalidAccessToken";
        var invalidRefreshToken = "invalidRefreshToken";

        mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId)).ReturnsAsync(invalidAccessToken);
        mockTokenService.Setup(s => s.IsTokenValid(invalidAccessToken)).Returns(false);
        mockTokenStorageService.Setup(s => s.GetRefreshTokenAsync(userId)).ReturnsAsync(invalidRefreshToken);
        mockTokenService.Setup(s => s.IsRefreshTokenValid(invalidRefreshToken)).Returns(false);
        mockTokenStorageService.Setup(s => s.ClearTokensAsync(userId)).Returns(Task.CompletedTask);

        var provider = new AccessTokenProvider(mockTokenService.Object, mockTokenStorageService.Object, fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.Null(result);
        mockTokenStorageService.Verify(s => s.ClearTokensAsync(userId), Times.Once);
        Assert.Equal("http://localhost/Account/Logout", fakeNavigationManager.Uri);
        Assert.True(fakeNavigationManager.ForceLoad);
    }
}
