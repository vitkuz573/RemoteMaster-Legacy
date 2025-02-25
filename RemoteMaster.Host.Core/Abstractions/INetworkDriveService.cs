// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface INetworkDriveService
{
    Task<bool> MapNetworkDriveAsync(string remotePath, string? username, string? password);

    Task<bool> CancelNetworkDriveAsync(string remotePath);

    string GetEffectivePath(string remotePath);
}
