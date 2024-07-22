// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class AccessTokenProvider(ITokenService tokenService, ITokenStorageService tokenStorageService, NavigationManager navigationManager) : IAccessTokenProvider
{
    public async Task<Result<string?>> GetAccessTokenAsync(string userId)
    {
        var accessTokenResult = await tokenStorageService.GetAccessTokenAsync(userId);

        if (accessTokenResult.IsSuccess && !string.IsNullOrEmpty(accessTokenResult.Value))
        {
            var tokenValidResult = tokenService.IsTokenValid(accessTokenResult.Value);

            if (tokenValidResult is { IsSuccess: true, Value: true })
            {
                return Result<string?>.Success(accessTokenResult.Value);
            }
        }

        var refreshTokenResult = await tokenStorageService.GetRefreshTokenAsync(userId);

        if (refreshTokenResult.IsSuccess && !string.IsNullOrEmpty(refreshTokenResult.Value))
        {
            var refreshTokenValidResult = tokenService.IsRefreshTokenValid(refreshTokenResult.Value);

            if (refreshTokenValidResult is { IsSuccess: true, Value: true })
            {
                var tokenDataResult = await tokenService.GenerateTokensAsync(userId, refreshTokenResult.Value);

                if (tokenDataResult.IsSuccess && !string.IsNullOrEmpty(tokenDataResult.Value.AccessToken))
                {
                    var storeTokensResult = await tokenStorageService.StoreTokensAsync(userId, tokenDataResult.Value);

                    if (storeTokensResult.IsSuccess)
                    {
                        return Result<string?>.Success(tokenDataResult.Value.AccessToken);
                    }
                }
            }
        }

        await tokenStorageService.ClearTokensAsync(userId);
        navigationManager.NavigateTo("/Account/Logout", true);

        return Result<string?>.Failure("Failed to retrieve or refresh access token.");
    }
}
