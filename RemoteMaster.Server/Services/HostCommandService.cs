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

namespace RemoteMaster.Server.Services;

public class HostCommandService(IJSRuntime jsRuntime, [FromKeyedServices("Resilience-Pipeline")] ResiliencePipeline<string> resiliencePipeline, ILogger<HostCommandService> logger) : IHostCommandService
{
    /// <inheritdoc />
    public async Task<Result> Execute(ConcurrentDictionary<HostDto, HubConnection?> hosts, Func<HostDto, HubConnection?, Task> action)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(hosts);
            ArgumentNullException.ThrowIfNull(action);

            foreach (var (host, connection) in hosts)
            {
                if (connection == null)
                {
                    await action(host, null);

                    continue;
                }

                var result = await resiliencePipeline.ExecuteAsync(async _ =>
                {
                    if (connection is not { State: HubConnectionState.Connected })
                    {
                        throw new InvalidOperationException("Connection is not active");
                    }

                    await action(host, connection);

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
            logger.LogError(ex, "An error occurred while executing a command on hosts.");

            return Result.Fail("An error occurred while executing a command on hosts.").WithError(ex.Message);
        }
    }
}
