// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using FluentResults;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Polly;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.DTOs;
using Serilog;

namespace RemoteMaster.Server.Services;

public class ComputerCommandService(IJSRuntime jsRuntime, [FromKeyedServices("Resilience-Pipeline")] ResiliencePipeline<string> resiliencePipeline) : IComputerCommandService
{
    /// <inheritdoc />
    public async Task<Result> Execute(ConcurrentDictionary<ComputerDto, HubConnection?> hosts, Func<ComputerDto, HubConnection?, Task> action)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(hosts);
            ArgumentNullException.ThrowIfNull(action);

            foreach (var (computer, connection) in hosts)
            {
                if (connection == null)
                {
                    await action(computer, null);

                    continue;
                }

                var result = await resiliencePipeline.ExecuteAsync(async _ =>
                {
                    if (connection is not { State: HubConnectionState.Connected })
                    {
                        throw new InvalidOperationException("Connection is not active");
                    }

                    await action(computer, connection);

                    await Task.CompletedTask;

                    return "Success";
                }, CancellationToken.None);

                if (result == "This function is not available in the current host version. Please update your host.")
                {
                    await jsRuntime.InvokeVoidAsync("alert", result);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while executing a command on computers.");

            return Result.Fail("An error occurred while executing a command on computers.").WithError(ex.Message);
        }
    }
}
