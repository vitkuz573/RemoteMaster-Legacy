// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandListenerService : IHostedService
{
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IUserInstanceService _userInstanceService;
    private readonly ILogger<CommandListenerService> _logger;

    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public CommandListenerService(IHostConfigurationService hostConfigurationService, IUserInstanceService userInstanceService, ILogger<CommandListenerService> logger)
    {
        _hostConfigurationService = hostConfigurationService;
        _userInstanceService = userInstanceService;
        _userInstanceService.UserInstanceCreated += OnUserInstanceCreated;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CommandListenerService started.");
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping CommandListenerService.");

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
        _logger.LogInformation("UserInstanceCreated event received.");
        
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

            var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync();

            _connection = new HubConnectionBuilder()
                .WithUrl($"https://{hostConfiguration.Host.IpAddress}:5001/hubs/control", options =>
                {
                    options.Headers.Add("X-Service-Flag", "true");
                })
                .AddMessagePackProtocol()
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
}
