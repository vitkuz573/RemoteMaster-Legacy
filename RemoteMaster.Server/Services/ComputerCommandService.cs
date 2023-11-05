// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Polly;
using Polly.Retry;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class ComputerCommandService : IComputerCommandService
{
    private static readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        });

    private readonly IJSRuntime _jsRuntime;

    public ComputerCommandService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task Execute(Dictionary<Computer, HubConnection> computers, Func<Computer, HubConnection, Task> actionOnComputer)
    {
        if (computers == null)
        {
            throw new ArgumentNullException(nameof(computers));
        }

        foreach (var (computer, connection) in computers)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await actionOnComputer(computer, connection);
                    }
                    catch (HubException ex) when (ex.Message.Contains("Method does not exist"))
                    {
                        await _jsRuntime.InvokeVoidAsync("showAlert", $"Host: {computer.Name}.\nThis function is not available in the current host version. Please update your host.");
                    }
                }
            });
        }
    }
}
