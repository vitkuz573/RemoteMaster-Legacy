// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Agent.Core.Abstractions;

namespace RemoteMaster.Agent.Core.Hubs;

public class MaintenanceHub : Hub
{
    private readonly IUpdateService _updateService;

    public MaintenanceHub(IUpdateService updateService)
    {
        _updateService = updateService;
    }

    public async Task SendClientUpdate()
    {
        _updateService.UpdateClient();
    }
}
