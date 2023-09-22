// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Abstractions;

public interface IComponentUpdater
{
    Task<UpdateResponse> IsUpdateAvailableAsync();

    Task UpdateAsync();

    string ComponentName { get; }
}
