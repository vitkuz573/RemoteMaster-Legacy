// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using Serilog;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandListenerService : IHostedService
{
    private readonly IUserInstanceService _userInstanceService;
    private HubConnection? _connection;

    public CommandListenerService(IUserInstanceService userInstanceService)
    {
        _userInstanceService = userInstanceService ?? throw new ArgumentNullException(nameof(userInstanceService));
        _userInstanceService.UserInstanceCreated += OnUserInstanceCreated;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("Starting CommandListenerService");

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.StopAsync(cancellationToken);
            await _connection.DisposeAsync();
        }
    }

    private async void OnUserInstanceCreated(object? sender, UserInstanceCreatedEventArgs e)
    {
        await Task.Delay(3000);

        Log.Information("UserInstanceCreated event received, connecting to hub");

        try
        {
            await CreateAndStartConnectionAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Connection error");
        }
    }

    private async Task CreateAndStartConnectionAsync()
    {
#pragma warning disable CA2000
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
        };
#pragma warning restore CA2000

        Log.Information("Creating HubConnection");

        _connection = new HubConnectionBuilder()
            .WithUrl("https://127.0.0.1:5001/hubs/control", options =>
            {
                options.HttpMessageHandlerFactory = _ => httpClientHandler;
            })
            .AddMessagePackProtocol()
            .Build();

        Log.Information("HubConnection created, setting up ReceiveCommand handler");

        _connection.On<string>("ReceiveCommand", (command) =>
        {
            Log.Information("Invoked with command: {Command}", command);

            if (command == "CtrlAltDel")
            {
                SendSAS(true);
                SendSAS(false);
            }
        });

        _connection.Closed += async (error) =>
        {
            Log.Warning("Connection closed: {Error}", error?.Message);

            await Task.Delay(5000);
            await CreateAndStartConnectionAsync();
        };

        _connection.Reconnecting += (error) =>
        {
            Log.Warning("Connection reconnecting: {Error}", error?.Message);

            return Task.CompletedTask;
        };

        _connection.Reconnected += (connectionId) =>
        {
            Log.Information("Connection reconnected: {ConnectionId}", connectionId);

            return Task.CompletedTask;
        };

        Log.Information("Starting connection to the hub");

        await _connection.StartAsync();

        await Task.Delay(2000);

        if (_connection.State == HubConnectionState.Connected)
        {
            Log.Information("Connection started successfully, joining Services");

            await _connection.InvokeAsync("JoinGroup", "Services");

            Log.Information("Successfully joined Services");
        }
        else
        {
            Log.Warning("Connection did not start successfully. Current state: {State}", _connection.State);
        }
    }
}

