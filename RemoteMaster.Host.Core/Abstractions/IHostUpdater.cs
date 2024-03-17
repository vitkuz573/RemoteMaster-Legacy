// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IHostUpdater
{
    Task UpdateAsync(string folderPath, string? username, string? password, bool force = false, bool allowDowngrade = false);
}
