// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Server.Aggregates.ApplicationUserAggregate.ValueObjects;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.ApplicationUserAggregate;

public class RefreshToken
{
    private RefreshToken() { }

    internal RefreshToken(string userId, DateTime expires, string ipAddress, string token)
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

    public void Revoke(TokenRevocationReason reason, string ipAddress)
    {
        if (RevocationInfo != null)
        {
            throw new InvalidOperationException("Token has already been revoked.");
        }

        var revocationInfo = new TokenRevocationInfo(DateTime.UtcNow, ipAddress, reason);

        RevocationInfo = revocationInfo;
    }

    public RefreshToken Replace(string ipAddress, Func<string, DateTime, string, RefreshToken> tokenFactory)
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

    public static RefreshToken Create(string userId, DateTime expires, string ipAddress)
    {
        var token = GenerateToken();

        return new RefreshToken(userId, expires, ipAddress, token);
    }

    private static string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
