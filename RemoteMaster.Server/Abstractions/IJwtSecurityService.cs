// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines the contract for managing JWT security keys.
/// </summary>
public interface IJwtSecurityService
{
    /// <summary>
    /// Retrieves the public key asynchronously.
    /// </summary>
    /// <returns>A result containing the public key bytes or an error.</returns>
    Task<Result<byte[]?>> GetPublicKeyAsync();

    /// <summary>
    /// Ensures that the JWT keys exist asynchronously, generating them if necessary.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> EnsureKeysExistAsync();
}
