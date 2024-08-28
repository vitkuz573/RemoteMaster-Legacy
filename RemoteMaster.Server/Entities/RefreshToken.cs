// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.ValueObjects;

namespace RemoteMaster.Server.Entities;

public class RefreshToken
{
    private RefreshToken() { }

    public RefreshToken(string userId, DateTime expires, string ipAddress)
    {
        UserId = userId;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        TokenValue = new TokenValue(token, expires, DateTime.UtcNow, ipAddress);
    }

    public int Id { get; private set; }
    
    public string UserId { get; private set; }
    
    public TokenValue TokenValue { get; private set; }
    
    public TokenRevocationInfo? RevocationInfo { get; private set; }
    
    public RefreshToken? ReplacedByToken { get; private set; }
    
    public ApplicationUser User { get; private set; }

    public bool IsActive => RevocationInfo == null && !TokenValue.IsExpired;

    public void Revoke(TokenRevocationReason reason, string ipAddress)
    {
        var revocationInfo = new TokenRevocationInfo(DateTime.UtcNow, ipAddress, reason);

        RevocationInfo = revocationInfo;
    }

    public RefreshToken Replace(string userId, DateTime expires, string ipAddress)
    {
        var refreshToken = new RefreshToken(userId, expires, ipAddress);

        Revoke(TokenRevocationReason.Replaced, ipAddress);
        ReplacedByToken = refreshToken;

        return refreshToken;
    }
}
