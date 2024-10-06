// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Models;

public class TokenData(string accessToken, string refreshToken, DateTime accessTokenExpiresAt, DateTime refreshTokenExpiresAt)
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; } = accessToken;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; } = refreshToken;

    [JsonPropertyName("accessTokenExpiresAt")]
    public DateTime AccessTokenExpiresAt { get; } = accessTokenExpiresAt;

    [JsonPropertyName("refreshTokenExpiresAt")]
    public DateTime RefreshTokenExpiresAt { get; } = refreshTokenExpiresAt;
}
