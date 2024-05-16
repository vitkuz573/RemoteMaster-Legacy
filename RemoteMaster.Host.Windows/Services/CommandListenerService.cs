// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using Serilog;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandListenerService : IHostedService
{
    private HubConnection? _connection;

    public CommandListenerService(ISessionChangeEventService sessionChangeEventService)
    {
        ArgumentNullException.ThrowIfNull(sessionChangeEventService);

        sessionChangeEventService.SessionChanged += OnSessionChanged;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("Starting listen service commands");

        await Task.Delay(5000, cancellationToken);

        await ConnectToHubAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.StopAsync(cancellationToken);
        }
    }

    private async Task ConnectToHubAsync(CancellationToken cancellationToken)
    {
        try
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://127.0.0.1:5000/hubs/control")
                .AddMessagePackProtocol()
                .Build();

            _connection.On<string>("ReceiveCommand", (command) =>
            {
                if (command == "CtrlAltDel")
                {
                    SendSAS(true);
                    SendSAS(false);
                }
            });

            await _connection.StartAsync(cancellationToken);
            await _connection.InvokeAsync("JoinGroup", "serviceGroup", cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Connection error");
        }
    }

    private async void OnSessionChanged(object? sender, SessionChangeEventArgs e)
    {
        if (e.ChangeDescription.Contains("A session was connected to the console terminal"))
        {
            Log.Information("Session changed, reconnecting to hub");
            await ConnectToHubAsync(CancellationToken.None);
        }
    }
}
