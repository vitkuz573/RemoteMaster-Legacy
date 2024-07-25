// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Interface for managing network drive operations such as mapping and canceling network drives.
/// </summary>
public interface INetworkDriveService
{
    /// <summary>
    /// Maps a network drive to a specified remote path.
    /// </summary>
    /// <param name="remotePath">The remote path to map the network drive to.</param>
    /// <param name="username">The username for authentication, if required.</param>
    /// <param name="password">The password for authentication, if required.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Result MapNetworkDrive(string remotePath, string? username, string? password);

    /// <summary>
    /// Cancels a mapped network drive for a specified remote path.
    /// </summary>
    /// <param name="remotePath">The remote path to cancel the network drive mapping for.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Result CancelNetworkDrive(string remotePath);
}
