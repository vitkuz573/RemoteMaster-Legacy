// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Hubs;

public class UpdaterHub(IUpdaterService updaterService) : Hub<IUpdaterClient>
{
    public void SendUpdate(string folderPath, string username, string password)
    {
        updaterService.Execute(folderPath, username, password);
    }
}
