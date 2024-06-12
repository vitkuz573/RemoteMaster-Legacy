// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class InMemoryTokenStorageService : ITokenStorageService
{
    private readonly ConcurrentDictionary<string, TokenData> _tokenStorage = new();

    public Task<string?> GetAccessTokenAsync(string userId)
    {
        if (_tokenStorage.TryGetValue(userId, out var tokens) && tokens.AccessTokenExpiresAt > DateTime.UtcNow)
        {
            return Task.FromResult<string?>(tokens.AccessToken);
        }
        
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetRefreshTokenAsync(string userId)
    {
        if (_tokenStorage.TryGetValue(userId, out var tokens) && tokens.RefreshTokenExpiresAt > DateTime.UtcNow)
        {            
            return Task.FromResult<string?>(tokens.RefreshToken);
        }
        
        return Task.FromResult<string?>(null);
    }

    public Task StoreTokensAsync(string userId, TokenData tokenData)
    {
        _tokenStorage[userId] = tokenData;

        return Task.CompletedTask;
    }

    public Task ClearTokensAsync(string userId)
    {
        _tokenStorage.TryRemove(userId, out _);

        return Task.CompletedTask;
    }
}
