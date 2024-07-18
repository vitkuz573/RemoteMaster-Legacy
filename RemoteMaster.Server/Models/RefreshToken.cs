// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Models;

public class RefreshToken : IValidatableObject
{
    public int Id { get; set; }

    public string UserId { get; set; }

    public string Token { get; set; }

    public DateTime Expires { get; set; }

    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow >= Expires;

    public DateTime Created { get; set; }

    public string CreatedByIp { get; set; }

    public DateTime? Revoked { get; set; }

    public string? RevokedByIp { get; set; }

    public TokenRevocationReason RevocationReason { get; set; } = TokenRevocationReason.None;

    public int? ReplacedByTokenId { get; set; }

    public RefreshToken? ReplacedByToken { get; set; }

    [JsonIgnore]
    public bool IsActive => Revoked == null && !IsExpired;

    public ApplicationUser User { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RevocationReason == TokenRevocationReason.Replaced && ReplacedByTokenId == null)
        {
            yield return new ValidationResult("ReplacedByToken must be specified if the RevocationReason is ReplacedDuringRefresh.", new[] { "ReplacedByTokenId" });
        }
    }
}
