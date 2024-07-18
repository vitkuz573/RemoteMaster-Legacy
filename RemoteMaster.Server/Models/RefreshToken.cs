// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Models;

/// <summary>
/// Represents a refresh token used for authentication in the RemoteMaster project.
/// </summary>
public class RefreshToken : IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the refresh token.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID associated with the refresh token.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets the token string.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the expiration date and time of the token.
    /// </summary>
    public DateTime Expires { get; set; }

    /// <summary>
    /// Gets a value indicating whether the token is expired.
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow >= Expires;

    /// <summary>
    /// Gets or sets the date and time when the token was created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the token was created.
    /// </summary>
    public string CreatedByIp { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the token was revoked, if applicable.
    /// </summary>
    public DateTime? Revoked { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the token was revoked, if applicable.
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// Gets or sets the reason for token revocation.
    /// </summary>
    public TokenRevocationReason RevocationReason { get; set; }

    /// <summary>
    /// Gets or sets the token that replaced this token, if applicable.
    /// </summary>
    public RefreshToken? ReplacedByToken { get; set; }

    /// <summary>
    /// Gets a value indicating whether the token is currently active.
    /// </summary>
    [JsonIgnore]
    public bool IsActive => Revoked == null && !IsExpired;

    /// <summary>
    /// Gets or sets the user associated with the refresh token.
    /// </summary>
    public ApplicationUser User { get; set; }

    /// <summary>
    /// Validates the current refresh token instance.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RevocationReason == TokenRevocationReason.Replaced && ReplacedByToken == null)
        {
            yield return new ValidationResult("ReplacedByToken must be specified if the RevocationReason is Replaced.", new[] { "ReplacedByToken" });
        }
    }
}
