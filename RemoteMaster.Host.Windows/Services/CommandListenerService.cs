// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.EventArguments;
using RemoteMaster.Shared.Extensions;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandListenerService : IHostedService
{
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IInstanceManagerService _instanceManagerService;
    private readonly ILogger<CommandListenerService> _logger;

    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly Timer _connectionCheckTimer;

    public CommandListenerService(IHostConfigurationService hostConfigurationService, IInstanceManagerService instanceManagerService, ILogger<CommandListenerService> logger)
    {
        _hostConfigurationService = hostConfigurationService;
        _instanceManagerService = instanceManagerService;
        _logger = logger;

        _instanceManagerService.InstanceStarted += OnInstanceStarted;

        _connectionCheckTimer = new Timer(CheckConnectionAsync, null, Timeout.Infinite, 0);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CommandListenerService started.");

        _connectionCheckTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping CommandListenerService.");

        await _connectionCheckTimer.DisposeAsync();
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

    private async void CheckConnectionAsync(object? state)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            return;
        }

        _logger.LogWarning("Connection is not active. Attempting to reconnect...");

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

            _logger.LogDebug("Creating HubConnection.");

            var hostConfiguration = await _hostConfigurationService.LoadAsync();

            _connection = new HubConnectionBuilder()
                .WithUrl($"https://{hostConfiguration.Host.IpAddress}:5001/hubs/service", options =>
                {
                    options.Headers.Add("Service-Flag", "true");
                })
                .AddMessagePackProtocol(options => options.Configure())
                .Build();

            _logger.LogDebug("HubConnection created, setting up ReceiveCommand handler.");

            _connection.On<string>("ReceiveCommand", command =>
            {
                _logger.LogDebug("Received command: {Command}.", command);

                if (command != "CtrlAltDel")
                {
                    return;
                }

                SendSAS(true);
                SendSAS(false);
            });

            _connection.Closed += async error =>
            {
                _logger.LogWarning("Connection closed: {Error}.", error?.Message);

                await Task.Delay(5000);
                await StartConnectionAsync();
            };

            _connection.Reconnecting += error =>
            {
                _logger.LogWarning("Connection reconnecting: {Error}.", error?.Message);

                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                _logger.LogInformation("Connection reconnected: {ConnectionId}.", connectionId);

                return Task.CompletedTask;
            };

            _logger.LogInformation("Starting connection to the hub.");

            await _connection.StartAsync();

            if (_connection.State == HubConnectionState.Connected)
            {
                _logger.LogInformation("Connection started successfully.");
            }
            else
            {
                _logger.LogWarning("Connection did not start successfully. Current state: {State}.", _connection.State);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async void OnInstanceStarted(object? sender, InstanceStartedEventArgs e)
    {
        if (!string.Equals(e.CommandName, "user", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _logger.LogInformation("User instance started with Process ID: {ProcessId}.", e.ProcessId);

        await StartConnectionAsync();
    }
}
