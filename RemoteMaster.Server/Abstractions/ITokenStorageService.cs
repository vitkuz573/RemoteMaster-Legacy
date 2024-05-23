// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Abstractions;

public interface ITokenStorageService
{
    Task<string?> GetAccessTokenAsync(string userId);

    Task<string?> GetRefreshTokenAsync(string userId);

    Task StoreTokensAsync(string userId, string accessToken, string refreshToken);

    Task ClearTokensAsync(string userId);
}