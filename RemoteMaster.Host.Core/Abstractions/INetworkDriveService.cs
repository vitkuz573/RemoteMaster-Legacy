// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface INetworkDriveService
{
    bool MapNetworkDrive(string remotePath, string? username, string? password);

    bool CancelNetworkDrive(string remotePath);

    string GetEffectivePath(string remotePath);
}
