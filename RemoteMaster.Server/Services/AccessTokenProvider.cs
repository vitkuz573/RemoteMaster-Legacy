// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using Microsoft.AspNetCore.Components;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class AccessTokenProvider(ITokenService tokenService, ITokenStorageService tokenStorageService, ITokenValidationService tokenValidationService, NavigationManager navigationManager) : IAccessTokenProvider
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Result<string?>> GetAccessTokenAsync(string userId)
    {
        await _semaphore.WaitAsync();
       
        try
        {
            var accessTokenResult = await tokenStorageService.GetAccessTokenAsync(userId);

            if (accessTokenResult.IsSuccess && !string.IsNullOrEmpty(accessTokenResult.Value))
            {
                var tokenValidResult = tokenValidationService.ValidateToken(accessTokenResult.Value);
                
                if (tokenValidResult.IsSuccess)
                {
                    return Result.Ok(accessTokenResult.Value);
                }
            }

            var refreshTokenResult = await tokenStorageService.GetRefreshTokenAsync(userId);
            
            if (refreshTokenResult.IsFailed || string.IsNullOrEmpty(refreshTokenResult.Value))
            {
                await SafeLogout(userId);
                
                return Result.Fail("Refresh token not found");
            }

            var refreshValidResult = await tokenService.IsRefreshTokenValid(userId, refreshTokenResult.Value);
            
            if (refreshValidResult.IsFailed)
            {
                await SafeLogout(userId);
                
                return Result.Fail("Invalid refresh token");
            }

            var tokenDataResult = await tokenService.GenerateTokensAsync(userId, refreshTokenResult.Value);
           
            if (tokenDataResult.IsFailed)
            {
                await SafeLogout(userId);

                return Result.Fail("Token generation failed");
            }

            var storeResult = await tokenStorageService.StoreTokensAsync(userId, tokenDataResult.Value);

            if (!storeResult.IsFailed)
            {
                return Result.Ok(tokenDataResult.Value.AccessToken);
            }

            await SafeLogout(userId);

            return Result.Fail("Token storage failed");

        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task SafeLogout(string userId)
    {
        await tokenStorageService.ClearTokensAsync(userId);
        navigationManager.NavigateTo("/Account/Logout", true);
    }
}
