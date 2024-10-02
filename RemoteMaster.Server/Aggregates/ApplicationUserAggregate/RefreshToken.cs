// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate.ValueObjects;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

public class RefreshToken
{
    private RefreshToken() { }

    internal RefreshToken(string userId, DateTime expires, IPAddress ipAddress, string token)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        if (expires <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration date must be in the future.", nameof(expires));
        }

        UserId = userId;
        TokenValue = new Token(token, expires, DateTime.UtcNow, ipAddress);
    }

    public int Id { get; private set; }

    public string UserId { get; private set; }

    public Token TokenValue { get; private set; }

    public TokenRevocationInfo? RevocationInfo { get; private set; }

    public RefreshToken? ReplacedByToken { get; private set; }

    public ApplicationUser User { get; private set; }

    public bool IsActive => RevocationInfo == null && !TokenValue.IsExpired;

    public static RefreshToken Create(string userId, DateTime expires, IPAddress ipAddress)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new RefreshToken(userId, expires, ipAddress, token);
    }

    public void Revoke(TokenRevocationReason reason, IPAddress ipAddress)
    {
        if (RevocationInfo != null)
        {
            throw new InvalidOperationException("Token has already been revoked.");
        }

        var revocationInfo = new TokenRevocationInfo(DateTime.UtcNow, ipAddress, reason);

        RevocationInfo = revocationInfo;
    }

    public RefreshToken Replace(IPAddress ipAddress, Func<string, DateTime, IPAddress, RefreshToken> tokenFactory)
    {
        ArgumentNullException.ThrowIfNull(tokenFactory);

        var newToken = tokenFactory(UserId, TokenValue.Expires, ipAddress);

        Revoke(TokenRevocationReason.Replaced, ipAddress);
        ReplacedByToken = newToken;

        return newToken;
    }

    public bool IsValid()
    {
        return RevocationInfo == null && !TokenValue.IsExpired;
    }
}
