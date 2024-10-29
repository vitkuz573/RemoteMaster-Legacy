// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.Identity;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

public class ApplicationUser : IdentityUser, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private readonly List<UserOrganization> _userOrganizations = [];
    private readonly List<UserOrganizationalUnit> _userOrganizationalUnits = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

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

    public SignInEntry AddSignInEntry(bool isSuccessful, IPAddress ipAddress)
    {
        return new SignInEntry(Id, isSuccessful, ipAddress);
    }

    public RefreshToken AddRefreshToken(DateTime expires, IPAddress ipAddress)
    {
        var refreshToken = RefreshToken.Create(Id, expires, ipAddress);

        _refreshTokens.Add(refreshToken);

        return refreshToken;
    }

    public void RevokeRefreshToken(string token, TokenRevocationReason reason, IPAddress ipAddress)
    {
        var refreshToken = _refreshTokens.SingleOrDefault(rt => rt.TokenValue.Value == token) ?? throw new InvalidOperationException("Token does not exist.");

        refreshToken.Revoke(reason, ipAddress);
    }

    public RefreshToken ReplaceRefreshToken(string token, IPAddress ipAddress)
    {
        var oldToken = _refreshTokens.SingleOrDefault(rt => rt.TokenValue.Value == token) ?? throw new InvalidOperationException("Token does not exist.");
        var newToken = oldToken.Replace(ipAddress, RefreshToken.Create);

        _refreshTokens.Add(newToken);

        return newToken;
    }

    public void RemoveRefreshToken(RefreshToken refreshToken)
    {
        if (!_refreshTokens.Remove(refreshToken))
        {
            throw new InvalidOperationException("The specified token does not exist.");
        }
    }

    public bool IsRefreshTokenValid(string token)
    {
        var refreshToken = _refreshTokens.SingleOrDefault(rt => rt.TokenValue.Value == token);

        return refreshToken?.IsValid() ?? false;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
