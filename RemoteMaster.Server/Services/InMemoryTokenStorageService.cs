// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class InMemoryTokenStorageService : ITokenStorageService
{
    private readonly ConcurrentDictionary<string, TokenData> _tokenStorage = new();

    public Task<Result<string?>> GetAccessTokenAsync(string userId)
    {
        if (_tokenStorage.TryGetValue(userId, out var tokens) && tokens.AccessTokenExpiresAt > DateTime.UtcNow)
        {
            return Task.FromResult(Result<string?>.Success(tokens.AccessToken));
        }

        return Task.FromResult(Result<string?>.Success(null));
    }

    public Task<Result<string?>> GetRefreshTokenAsync(string userId)
    {
        if (_tokenStorage.TryGetValue(userId, out var tokens) && tokens.RefreshTokenExpiresAt > DateTime.UtcNow)
        {
            return Task.FromResult(Result<string?>.Success(tokens.RefreshToken));
        }

        return Task.FromResult(Result<string?>.Success(null));
    }

    public Task<Result> StoreTokensAsync(string userId, TokenData tokenData)
    {
        _tokenStorage[userId] = tokenData;

        return Task.FromResult(Result.Success());
    }

    public Task<Result> ClearTokensAsync(string userId)
    {
        _tokenStorage.TryRemove(userId, out _);

        return Task.FromResult(Result.Success());
    }
}
