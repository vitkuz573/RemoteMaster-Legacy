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
        }
    }

    private async void OnUserInstanceCreated(object? sender, UserInstanceCreatedEventArgs e)
    {
        Log.Information("UserInstanceCreated event received, connecting to hub");

        try
        {
            using var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            };

            _connection = new HubConnectionBuilder()
                .WithUrl("https://127.0.0.1:5001/hubs/control", options =>
                {
                    options.HttpMessageHandlerFactory = _ => httpClientHandler;
                })
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

            await _connection.StartAsync();
            await _connection.InvokeAsync("JoinGroup", "serviceGroup");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Connection error");
        }
    }
}
