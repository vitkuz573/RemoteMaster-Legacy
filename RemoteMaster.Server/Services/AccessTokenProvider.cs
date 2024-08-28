// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class AccessTokenProvider(ITokenService tokenService, ITokenStorageService tokenStorageService, NavigationManager navigationManager) : IAccessTokenProvider
{
    /// <inheritdoc />
    public async Task<Result<string?>> GetAccessTokenAsync(string userId)
    {
        var accessTokenResult = await tokenStorageService.GetAccessTokenAsync(userId);

        if (accessTokenResult.IsSuccess && !string.IsNullOrEmpty(accessTokenResult.Value))
        {
            var tokenValidResult = tokenService.IsTokenValid(accessTokenResult.Value);

            if (tokenValidResult.IsSuccess)
            {
                return Result.Ok<string?>(accessTokenResult.Value);
            }
        }

        var refreshTokenResult = await tokenStorageService.GetRefreshTokenAsync(userId);

        if (refreshTokenResult.IsSuccess && !string.IsNullOrEmpty(refreshTokenResult.Value))
        {
            var refreshTokenValidResult = await tokenService.IsRefreshTokenValid(userId, refreshTokenResult.Value);

            if (refreshTokenValidResult.IsSuccess)
            {
                var tokenDataResult = await tokenService.GenerateTokensAsync(userId, refreshTokenResult.Value);

                if (tokenDataResult.IsSuccess && tokenDataResult.Value is not null)
                {
                    var tokenData = tokenDataResult.Value;

                    var storeTokensResult = await tokenStorageService.StoreTokensAsync(userId, tokenData);

                    if (storeTokensResult.IsSuccess)
                    {
                        return Result.Ok<string?>(tokenData.AccessToken);
                    }
                }
            }
        }

        await tokenStorageService.ClearTokensAsync(userId);

        navigationManager.NavigateTo("/Account/Logout", true);

        return Result.Fail<string?>("Failed to retrieve or refresh access token.");
    }
}
