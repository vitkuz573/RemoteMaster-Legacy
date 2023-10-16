// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Abstractions;
using TypedSignalR.Client;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Services;

public class ServiceCommandListener : IHostedService
{
    private readonly ILogger<ServiceCommandListener> _logger;

    public ServiceCommandListener(ILogger<ServiceCommandListener> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting listen service commands");

        try
        {
            var connection = new HubConnectionBuilder()
    .WithUrl("http://127.0.0.1:5076/hubs/control")
    .WithAutomaticReconnect()
    .Build();

            connection.On<string>("ReceiveCommand", command =>
            {
                if (command == "CtrlAltDel")
                {
                    SendSAS(true);
                    SendSAS(false);
                }
            });

            await connection.StartAsync();

            var proxy = connection.CreateHubProxy<IControlHub>();

            await proxy.JoinGroup("serviceGroup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection error");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Если у вас есть какая-то логика остановки, вызовите её здесь.
        return Task.CompletedTask;
    }
}
