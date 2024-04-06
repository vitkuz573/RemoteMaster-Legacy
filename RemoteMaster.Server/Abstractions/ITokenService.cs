// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Abstractions;

public interface ITokenService
{
    string GenerateAccessToken(string email);

    string GenerateRefreshToken(string userId, string ipAddress);

    Task<(string? AccessToken, string? RefreshToken)> RefreshTokensAsync(string oldRefreshToken, string ipAddress);

    bool RequiresTokenUpdate(string accessToken);
}
