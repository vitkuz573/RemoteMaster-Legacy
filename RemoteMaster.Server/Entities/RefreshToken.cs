// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.ValueObjects;

namespace RemoteMaster.Server.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    
    public string UserId { get; set; }
    
    public TokenValue TokenValue { get; set; }
    
    public TokenRevocationInfo RevocationInfo { get; set; }
    
    public RefreshToken? ReplacedByToken { get; set; }
    
    public ApplicationUser User { get; set; }

    public bool IsActive => RevocationInfo.Revoked == null && !TokenValue.IsExpired;
}
