// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class DbTokenStorageService(TokenDbContext context) : ITokenStorageService
{
    public async Task<string?> GetAccessTokenAsync(string userId)
    {
        Log.Information("Attempting to retrieve access token for user {UserId}", userId);

        var tokenEntity = await context.Tokens.FindAsync(userId);

        if (tokenEntity != null)
        {
            Log.Information("Access token found for user {UserId}", userId);

            return tokenEntity.AccessToken;
        }

        Log.Warning("Access token not found for user {UserId}", userId);

        return null;
    }

    public async Task<string?> GetRefreshTokenAsync(string userId)
    {
        Log.Information("Attempting to retrieve refresh token for user {UserId}", userId);

        var tokenEntity = await context.Tokens.FindAsync(userId);

        if (tokenEntity != null)
        {
            Log.Information("Refresh token found for user {UserId}", userId);

            return tokenEntity.RefreshToken;
        }

        Log.Warning("Refresh token not found for user {UserId}", userId);

        return null;
    }

    public async Task StoreTokensAsync(string userId, string accessToken, string refreshToken)
    {
        Log.Information("Storing tokens for user {UserId}", userId);

        var tokenEntity = await context.Tokens.FindAsync(userId);

        if (tokenEntity == null)
        {
            tokenEntity = new TokenEntity
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            context.Tokens.Add(tokenEntity);
        }
        else
        {
            tokenEntity.AccessToken = accessToken;
            tokenEntity.RefreshToken = refreshToken;
            context.Tokens.Update(tokenEntity);
        }

        await context.SaveChangesAsync();

        Log.Information("Tokens stored successfully for user {UserId}", userId);
    }

    public async Task ClearTokensAsync(string userId)
    {
        Log.Information("Attempting to clear tokens for user {UserId}", userId);

        var tokenEntity = await context.Tokens.FindAsync(userId);

        if (tokenEntity != null)
        {
            context.Tokens.Remove(tokenEntity);
            await context.SaveChangesAsync();

            Log.Information("Tokens cleared successfully for user {UserId}", userId);
        }
    }
}