// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class AccessTokenProviderTests
{
    private readonly Mock<ITokenService> _mockTokenService = new();
    private readonly Mock<ITokenStorageService> _mockTokenStorageService = new();
    private readonly Mock<ITokenValidationService> _mockTokenValidationService = new();
    private readonly FakeNavigationManager _fakeNavigationManager = new("http://localhost/", "http://localhost/");

    [Fact]
    public async Task GetAccessTokenAsync_ValidAccessToken_ReturnsAccessToken()
    {
        // Arrange
        const string userId = "user1";
        const string accessToken = "validAccessToken";

        _mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId))
            .ReturnsAsync(Result.Ok<string?>(accessToken));
        _mockTokenValidationService.Setup(s => s.ValidateTokenAsync(accessToken))
            .ReturnsAsync(Result.Ok());

        var provider = new AccessTokenProvider(_mockTokenService.Object, _mockTokenStorageService.Object, _mockTokenValidationService.Object, _fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(accessToken, result.ValueOrDefault);
    }

    [Fact]
    public async Task GetAccessTokenAsync_InvalidAccessToken_ValidRefreshToken_ReturnsNewAccessToken()
    {
        // Arrange
        const string userId = "user1";
        const string invalidAccessToken = "invalidAccessToken";
        const string validRefreshToken = "validRefreshToken";
        const string newAccessToken = "newAccessToken";
        var tokenData = new TokenData(newAccessToken, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>());

        _mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId))
            .ReturnsAsync(Result.Ok<string?>(invalidAccessToken));
        _mockTokenValidationService.Setup(s => s.ValidateTokenAsync(invalidAccessToken))
            .ReturnsAsync(Result.Fail("Invalid token"));
        _mockTokenStorageService.Setup(s => s.GetRefreshTokenAsync(userId))
            .ReturnsAsync(Result.Ok<string?>(validRefreshToken));
        _mockTokenService.Setup(s => s.IsRefreshTokenValidAsync(userId, validRefreshToken))
            .Returns(Task.FromResult(Result.Ok()));
        _mockTokenService.Setup(s => s.GenerateTokensAsync(userId, validRefreshToken))
            .ReturnsAsync(Result.Ok(tokenData));
        _mockTokenStorageService.Setup(s => s.StoreTokensAsync(userId, tokenData))
            .ReturnsAsync(Result.Ok());

        var provider = new AccessTokenProvider(_mockTokenService.Object, _mockTokenStorageService.Object, _mockTokenValidationService.Object, _fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newAccessToken, result.ValueOrDefault);
    }

    [Fact]
    public async Task GetAccessTokenAsync_InvalidAccessToken_InvalidRefreshToken_ClearsTokensAndRedirects()
    {
        // Arrange
        const string userId = "user1";
        const string invalidAccessToken = "invalidAccessToken";
        const string invalidRefreshToken = "invalidRefreshToken";

        _mockTokenStorageService.Setup(s => s.GetAccessTokenAsync(userId))
            .ReturnsAsync(Result.Ok<string?>(invalidAccessToken));
        _mockTokenValidationService.Setup(s => s.ValidateTokenAsync(invalidAccessToken))
            .ReturnsAsync(Result.Fail("Invalid token"));
        _mockTokenStorageService.Setup(s => s.GetRefreshTokenAsync(userId))
            .ReturnsAsync(Result.Ok<string?>(invalidRefreshToken));
        _mockTokenService.Setup(s => s.IsRefreshTokenValidAsync(userId, invalidRefreshToken))
            .Returns(Task.FromResult(Result.Fail("Invalid refresh token")));
        _mockTokenStorageService.Setup(s => s.ClearTokensAsync(userId))
            .ReturnsAsync(Result.Ok());

        var provider = new AccessTokenProvider(_mockTokenService.Object, _mockTokenStorageService.Object, _mockTokenValidationService.Object, _fakeNavigationManager);

        // Act
        var result = await provider.GetAccessTokenAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        _mockTokenStorageService.Verify(s => s.ClearTokensAsync(userId), Times.Once);
        Assert.Equal("http://localhost/Account/Logout", _fakeNavigationManager.Uri);
        Assert.True(_fakeNavigationManager.ForceLoad);
    }
}
