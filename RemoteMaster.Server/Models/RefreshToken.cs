// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Models;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; }

    [Required]
    [StringLength(256)]
    public string Token { get; set; }

    [Required]
    public DateTime Expires { get; set; }

    [NotMapped]
    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow >= Expires;

    [Required]
    public DateTime Created { get; set; }

    [Required]
    [StringLength(45)]
    public string CreatedByIp { get; set; }

    public DateTime? Revoked { get; set; }

    [StringLength(45)]
    public string? RevokedByIp { get; set; }

    [EnumDataType(typeof(TokenRevocationReason))]
    public TokenRevocationReason RevocationReason { get; set; } = TokenRevocationReason.None;

    public int? ReplacedByTokenId { get; set; }

    [ForeignKey("ReplacedByTokenId")]
    public RefreshToken? ReplacedByToken { get; set; }

    [NotMapped]
    [JsonIgnore]
    public bool IsActive => Revoked == null && !IsExpired;

    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; }

    public override string ToString()
    {
        return $"RefreshToken [TokenId={Id}, Expires={Expires}, IsActive={IsActive}]";
    }
}
