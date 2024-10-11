// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class TokenData(string accessToken, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt)
{
    public string AccessToken { get; } = accessToken;

    public string RefreshToken { get; } = refreshToken;

    public DateTime AccessTokenExpiresAt { get; } = accessTokenExpiresAt;

    public DateTime RefreshTokenExpiresAt { get; } = refreshTokenExpiresAt;
}
