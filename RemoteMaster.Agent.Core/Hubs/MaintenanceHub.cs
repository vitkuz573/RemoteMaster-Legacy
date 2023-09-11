// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Agent.Core.Abstractions;

namespace RemoteMaster.Agent.Core.Hubs;

public class MaintenanceHub : Hub<IMaintenanceClient>
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly IUpdateService _updateService;

    public MaintenanceHub(IConfigurationProvider configurationProvider, IUpdateService updateService)
    {
        _configurationProvider = configurationProvider;
        _updateService = updateService;
    }

    public async override Task OnConnectedAsync()
    {
        var configuration = _configurationProvider.Fetch();

        await Clients.Caller.ReceiveAgentConfiguration(configuration);
    }

    public async Task SendClientUpdate()
    {
        _updateService.UpdateClient();
    }
}
