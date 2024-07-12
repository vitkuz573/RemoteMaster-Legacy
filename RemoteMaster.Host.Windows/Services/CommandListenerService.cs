// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

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
    private readonly object _connectionLock = new();

    public CommandListenerService(IUserInstanceService userInstanceService)
    {
        _userInstanceService = userInstanceService;
        _userInstanceService.UserInstanceCreated += OnUserInstanceCreated;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("CommandListenerService started at {Time}", DateTimeOffset.Now);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping CommandListenerService at {Time}", DateTimeOffset.Now);

        if (_connection != null)
        {
            await _connection.StopAsync(cancellationToken);
            await _connection.DisposeAsync();

            _connection = null;
        }
    }

    private async void OnUserInstanceCreated(object? sender, UserInstanceCreatedEventArgs e)
    {
        Log.Information("UserInstanceCreated event received at {Time}", DateTimeOffset.Now);

        await StartConnectionAsync();
    }

    private async Task StartConnectionAsync()
    {
        lock (_connectionLock)
        {
            if (_connection != null && _connection.State != HubConnectionState.Disconnected)
            {
                Log.Information("Connection is already in a valid state: {State} at {Time}", _connection.State, DateTimeOffset.Now);
                return;
            }
        }

        await Task.Delay(3000);

#pragma warning disable CA2000
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
#pragma warning restore CA2000

        Log.Information("Creating HubConnection at {Time}", DateTimeOffset.Now);

        lock (_connectionLock)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://127.0.0.1:5001/hubs/control", options =>
                {
                    options.HttpMessageHandlerFactory = _ => httpClientHandler;
                })
                .AddMessagePackProtocol()
                .Build();
        }

        Log.Information("HubConnection created, setting up ReceiveCommand handler at {Time}", DateTimeOffset.Now);

        _connection.On<string>("ReceiveCommand", (command) =>
        {
            Log.Information("Invoked with command: {Command} at {Time}", command, DateTimeOffset.Now);

            if (command != "CtrlAltDel")
            {
                return;
            }

            SendSAS(true);
            SendSAS(false);
        });

        _connection.Closed += async (error) =>
        {
            Log.Warning("Connection closed: {Error} at {Time}", error?.Message, DateTimeOffset.Now);

            await Task.Delay(5000);
            await StartConnectionAsync();
        };

        _connection.Reconnecting += (error) =>
        {
            Log.Warning("Connection reconnecting: {Error} at {Time}", error?.Message, DateTimeOffset.Now);

            return Task.CompletedTask;
        };

        _connection.Reconnected += (connectionId) =>
        {
            Log.Information("Connection reconnected: {ConnectionId} at {Time}", connectionId, DateTimeOffset.Now);

            return Task.CompletedTask;
        };

        Log.Information("Starting connection to the hub at {Time}", DateTimeOffset.Now);

        await _connection.StartAsync();

        await Task.Delay(2000);

        if (_connection.State == HubConnectionState.Connected)
        {
            Log.Information("Connection started successfully, joining Services at {Time}", DateTimeOffset.Now);

            await _connection.InvokeAsync("JoinGroup", "Services");

            Log.Information("Successfully joined Services at {Time}", DateTimeOffset.Now);
        }
        else
        {
            Log.Warning("Connection did not start successfully. Current state: {State} at {Time}", _connection.State, DateTimeOffset.Now);
        }
    }
}
