// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

public class ApplicationUser : IdentityUser, IAggregateRoot
{
    private readonly List<UserOrganization> _userOrganizations = [];
    private readonly List<UserOrganizationalUnit> _userOrganizationalUnits = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    public IReadOnlyCollection<UserOrganization> UserOrganizations => _userOrganizations.AsReadOnly();

    public IReadOnlyCollection<UserOrganizationalUnit> UserOrganizationalUnits => _userOrganizationalUnits.AsReadOnly();

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public bool CanAccessUnregisteredHosts { get; private set; }

    public void GrantAccessToUnregisteredHosts()
    {
        CanAccessUnregisteredHosts = true;
    }

    public void RevokeAccessToUnregisteredHosts()
    {
        CanAccessUnregisteredHosts = false;
    }

    public SignInEntry AddSignInEntry(bool isSuccessful, string ipAddress)
    {
        return new SignInEntry(Id, isSuccessful, ipAddress);
    }

    public RefreshToken AddRefreshToken(DateTime expires, string ipAddress)
    {
        var refreshToken = RefreshToken.Create(Id, expires, ipAddress);

        _refreshTokens.Add(refreshToken);

        return refreshToken;
    }

    public void RevokeRefreshToken(string token, TokenRevocationReason reason, string ipAddress)
    {
        var refreshToken = _refreshTokens.SingleOrDefault(rt => rt.TokenValue.Token == token);

        refreshToken.Revoke(reason, ipAddress);
    }

    public RefreshToken ReplaceRefreshToken(string token, string ipAddress)
    {
        var refreshToken = _refreshTokens.SingleOrDefault(rt => rt.TokenValue.Token == token) ?? throw new InvalidOperationException("The specified token does not exist.");
        var newRefreshToken = refreshToken.Replace(ipAddress, RefreshToken.Create);

        _refreshTokens.Add(newRefreshToken);

        return newRefreshToken;
    }

    public void RemoveRefreshToken(RefreshToken refreshToken)
    {
        if (_refreshTokens.Contains(refreshToken))
        {
            _refreshTokens.Remove(refreshToken);
        }
        else
        {
            throw new InvalidOperationException("The specified token does not exist.");
        }
    }

    public bool IsRefreshTokenValid(string token)
    {
        var refreshToken = _refreshTokens.SingleOrDefault(rt => rt.TokenValue.Token == token);

        return refreshToken?.IsValid() ?? false;
    }
}
