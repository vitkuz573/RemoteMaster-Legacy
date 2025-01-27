// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Hubs;

public class UpdaterHub(IUpdaterInstanceService updaterInstanceService, IHostUpdater hostUpdater, ILogger<UpdaterHub> logger) : Hub<IUpdaterClient>
{
    public async override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();

        logger.LogInformation("New connection detected. Connection ID: {ConnectionId}, Local Port: {Port}", Context.ConnectionId, httpContext?.Connection.LocalPort);

        var localPort = httpContext?.Connection.LocalPort;

        if (localPort == 6001)
        {
            logger.LogInformation("Notifying HostUpdater about client connection on port 6001.");

            hostUpdater.NotifyClientConnected();
        }

        await base.OnConnectedAsync();
    }

    [Authorize(Policy = "StartUpdaterPolicy")]
    public void SendStartUpdater(UpdateRequest updateRequest)
    {
        updaterInstanceService.Start(updateRequest);
    }
}
