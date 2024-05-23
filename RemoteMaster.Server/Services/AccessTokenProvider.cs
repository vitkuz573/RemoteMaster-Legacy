// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class AccessTokenProvider(ITokenService tokenService, ITokenStorageService tokenStorageService) : IAccessTokenProvider
{
    public async Task<string?> GetAccessTokenAsync(string userId)
    {
        var accessToken = await tokenStorageService.GetAccessTokenAsync(userId);

        if (!string.IsNullOrEmpty(accessToken) && tokenService.IsTokenValid(accessToken))
        {
            return accessToken;
        }

        var refreshToken = await tokenStorageService.GetRefreshTokenAsync(userId);

        if (!string.IsNullOrEmpty(refreshToken) && tokenService.IsRefreshTokenValid(refreshToken))
        {
            var newTokens = await tokenService.RefreshAccessToken(refreshToken);

            if (newTokens != null && !string.IsNullOrEmpty(newTokens.AccessToken))
            {
                await tokenStorageService.StoreTokensAsync(userId, newTokens.AccessToken, newTokens.RefreshToken);

                return newTokens.AccessToken;
            }
        }

        await tokenStorageService.ClearTokensAsync(userId);

        return null;
    }
}