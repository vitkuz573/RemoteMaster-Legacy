// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class InMemoryTokenStorageService : ITokenStorageService
{
    private readonly ConcurrentDictionary<string, TokenResponseData> _tokenStorage = new();

    public Task<string?> GetAccessTokenAsync(string userId)
    {
        Log.Information("Attempting to retrieve access token for user {UserId}", userId);

        if (_tokenStorage.TryGetValue(userId, out var tokens))
        {
            Log.Information("Access token found for user {UserId}", userId);

            return Task.FromResult<string?>(tokens.AccessToken);
        }

        Log.Warning("Access token not found for user {UserId}", userId);

        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetRefreshTokenAsync(string userId)
    {
        Log.Information("Attempting to retrieve refresh token for user {UserId}", userId);

        if (_tokenStorage.TryGetValue(userId, out var tokens))
        {
            Log.Information("Refresh token found for user {UserId}", userId);

            return Task.FromResult<string?>(tokens.RefreshToken);
        }

        Log.Warning("Refresh token not found for user {UserId}", userId);

        return Task.FromResult<string?>(null);
    }

    public Task StoreTokensAsync(string userId, string accessToken, string refreshToken)
    {
        Log.Information("Storing tokens for user {UserId}", userId);

        var tokenData = new TokenResponseData
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        _tokenStorage[userId] = tokenData;

        Log.Information("Tokens stored successfully for user {UserId}", userId);

        return Task.CompletedTask;
    }

    public Task ClearTokensAsync(string userId)
    {
        Log.Information("Attempting to clear tokens for user {UserId}", userId);

        _tokenStorage.TryRemove(userId, out _);

        Log.Information("Tokens cleared successfully for user {UserId}", userId);

        return Task.CompletedTask;
    }
}
