// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Polly;
using Polly.Retry;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class ComputerCommandService(IJSRuntime jsRuntime) : IComputerCommandService
{
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
        [
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        ]);

    /// <inheritdoc />
    public async Task<Result> Execute(ConcurrentDictionary<Computer, HubConnection?> hosts, Func<Computer, HubConnection, Task> action)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(hosts);

            foreach (var (computer, connection) in hosts)
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (connection?.State == HubConnectionState.Connected)
                    {
                        try
                        {
                            await action(computer, connection);
                        }
                        catch (HubException ex) when (ex.Message.Contains("Method does not exist"))
                        {
                            await jsRuntime.InvokeVoidAsync("alert", $"Host: {computer.Name}.\nThis function is not available in the current host version. Please update your host.");
                        }
                    }
                });
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while executing a command on computers.");
            
            return Result.Failure("An error occurred while executing a command on computers.", exception: ex);
        }
    }
}
