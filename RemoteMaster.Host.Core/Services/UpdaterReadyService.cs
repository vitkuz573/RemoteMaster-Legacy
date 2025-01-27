// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Pipes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Shared.Models;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Core.Services;

public class UpdaterReadyService(IHubContext<UpdaterHub, IUpdaterClient> hubContext) : IHostedService, IAsyncDisposable
{
    private Task? _loopTask;
    private bool _disposed;
    private readonly CancellationTokenSource _cts = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _loopTask = Task.Run(() => ListenLoop(_cts.Token), cancellationToken);

        return Task.CompletedTask;
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(PipeNames.UpdaterReadyPipe, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            try
            {
                await server.WaitForConnectionAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            using var reader = new StreamReader(server);
            var line = await reader.ReadLineAsync(token);

            if (!string.IsNullOrEmpty(line) && line.Contains("ready", StringComparison.OrdinalIgnoreCase))
            {
                await hubContext.Clients.All.ReceiveMessage(new Message("Updater instance on port 6001 is ready.", MessageSeverity.Information));
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cts.CancelAsync();

        if (_loopTask != null)
        {
            await _loopTask;
        }

        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Dispose();

        if (_loopTask != null)
        {
            await _loopTask;
        }
    }
}
