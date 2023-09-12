// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Agent.Core.Abstractions;
using Windows.Win32;

namespace RemoteMaster.Agent.Core.Hubs;

public class MaintenanceHub : Hub<IMaintenanceClient>
{
    private readonly IConfigurationProvider _configurationProvider;
    private readonly IClientUpdater _updateService;

    public MaintenanceHub(IConfigurationProvider configurationProvider, IClientUpdater updateService)
    {
        _configurationProvider = configurationProvider;
        _updateService = updateService;
    }

    public async override Task OnConnectedAsync()
    {
        var configuration = _configurationProvider.Fetch();

        await Clients.Caller.ReceiveAgentConfiguration(configuration);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Пометьте члены как статические", Justification = "<Ожидание>")]
    public async Task SendCtrlAltDel()
    {
        PInvoke.SendSAS(true);
        PInvoke.SendSAS(false);
    }

    public async Task SendClientUpdate()
    {
        _updateService.Update();
    }
}
