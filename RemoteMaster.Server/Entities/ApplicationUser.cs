// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Entities;

public class ApplicationUser : IdentityUser, IAggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = [];

    public ICollection<UserOrganization> UserOrganizations { get; } = [];

    public ICollection<UserOrganizationalUnit> UserOrganizationalUnits { get; } = [];

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public bool CanAccessUnregisteredHosts { get; set; }

    public SignInEntry AddSignInEntry(bool isSuccessful, string ipAddress)
    {
        return new SignInEntry(Id, DateTime.UtcNow, isSuccessful, ipAddress);
    }

    public RefreshToken AddRefreshToken(DateTime expires, string ipAddress)
    {
        var refreshToken = new RefreshToken(Id, expires, ipAddress);

        _refreshTokens.Add(refreshToken);

        return refreshToken;
    }

    public void RevokeRefreshToken(string token, TokenRevocationReason reason, string ipAddress)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.TokenValue.Token == token);

        refreshToken.Revoke(reason, ipAddress);
    }

    public RefreshToken ReplaceRefreshToken(string token, DateTime expires, string ipAddress)
    {
        var refreshToken = _refreshTokens.FirstOrDefault(rt => rt.TokenValue.Token == token);
        var newRefreshToken = refreshToken.Replace(Id, expires, ipAddress);

        _refreshTokens.Add(newRefreshToken);

        return newRefreshToken;
    }

    public bool IsRefreshTokenValid(string refreshToken)
    {
        var token = _refreshTokens.SingleOrDefault(rt => rt.TokenValue.Token == refreshToken);

        return token?.IsValid() ?? false;
    }
}
