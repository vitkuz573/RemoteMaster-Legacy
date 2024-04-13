// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize]
public class UpdaterHub(IUpdaterInstanceService updaterInstanceService) : Hub<IUpdaterClient>
{
    public void SendStartUpdater(UpdateDto updateRequest)
    {
        updaterInstanceService.Start(updateRequest);
    }
}
