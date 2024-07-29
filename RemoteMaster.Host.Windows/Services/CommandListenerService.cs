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
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public CommandListenerService(IUserInstanceService userInstanceService)
    {
        _userInstanceService = userInstanceService;
        _userInstanceService.UserInstanceCreated += OnUserInstanceCreated;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("CommandListenerService started.");

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping CommandListenerService.");

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection != null)
            {
                await _connection.StopAsync(cancellationToken);
                await _connection.DisposeAsync();

                _connection = null;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async void OnUserInstanceCreated(object? sender, UserInstanceCreatedEventArgs e)
    {
        Log.Information("UserInstanceCreated event received.");

        await StartConnectionAsync();
    }

    private async Task StartConnectionAsync()
    {
        await _connectionLock.WaitAsync();

        try
        {
            if (_connection is { State: HubConnectionState.Connected })
            {
                return;
            }

            await Task.Delay(5000);

#pragma warning disable CA2000
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
#pragma warning restore CA2000

            Log.Information("Creating HubConnection.");

            _connection = new HubConnectionBuilder()
                .WithUrl("https://127.0.0.1:5001/hubs/control", options =>
                {
                    options.HttpMessageHandlerFactory = _ => httpClientHandler;
                    options.Headers.Add("X-Service-Flag", "true");
                })
                .AddMessagePackProtocol()
                .Build();

            Log.Information("HubConnection created, setting up ReceiveCommand handler.");

            _connection.On<string>("ReceiveCommand", command =>
            {
                Log.Information("Invoked with command: {Command}.", command);

                if (command != "CtrlAltDel")
                {
                    return;
                }

                SendSAS(true);
                SendSAS(false);
            });

            _connection.Closed += async error =>
            {
                Log.Warning("Connection closed: {Error}.", error?.Message);

                await Task.Delay(5000);
                await StartConnectionAsync();
            };

            _connection.Reconnecting += error =>
            {
                Log.Warning("Connection reconnecting: {Error}.", error?.Message);

                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                Log.Information("Connection reconnected: {ConnectionId}.", connectionId);

                return Task.CompletedTask;
            };

            Log.Information("Starting connection to the hub.");

            await _connection.StartAsync();

            await Task.Delay(2000);

            if (_connection.State == HubConnectionState.Connected)
            {
                Log.Information("Connection started successfully.");
            }
            else
            {
                Log.Warning("Connection did not start successfully. Current state: {State}.", _connection.State);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}
