// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Retry;
using Serilog;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandListenerService : IHostedService
{
    private HubConnection? _connection;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        });

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Information("Starting listen service commands");

        try
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("http://127.0.0.1:5000/hubs/control")
                .Build();

            _connection.On<string>("ReceiveCommand", command =>
            {
                if (command == "CtrlAltDel")
                {
                    SendSAS(true);
                    SendSAS(false);
                }
            });

            _connection.Closed += async (error) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                await _connection.StartAsync();
                await SafeInvokeAsync(async () => await _connection.InvokeAsync("JoinGroup", "serviceGroup"));
            };

            await _connection.StartAsync();

            await _connection.InvokeAsync("JoinGroup", "serviceGroup");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Connection error");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
        }
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await action();
            }
            else
            {
                throw new InvalidOperationException("Connection is not active");
            }
        });
    }
}
