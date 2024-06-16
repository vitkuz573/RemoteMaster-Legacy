// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

public interface ITokenStorageService
{
    Task<string?> GetAccessTokenAsync(string userId);

    Task<string?> GetRefreshTokenAsync(string userId);

    Task StoreTokensAsync(string userId, TokenData tokenData);

    Task ClearTokensAsync(string userId);
}