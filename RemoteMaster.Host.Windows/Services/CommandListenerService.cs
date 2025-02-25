// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Models;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandListenerService : IHostedService, IAsyncDisposable
{
    private Task? _loopTask;
    private bool _disposed;
    private readonly CancellationTokenSource _cts = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _loopTask = Task.Run(() => ListenLoopAsync(_cts.Token), cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cts.CancelAsync();

        if (_loopTask is not null)
        {
            await _loopTask;
        }

        await DisposeAsync();
    }

    private static async Task ListenLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await using var server = new NamedPipeServerStream(PipeNames.CommandPipe, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            try
            {
                await server.WaitForConnectionAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            using var reader = new StreamReader(server, Encoding.UTF8);
            var cmd = await reader.ReadLineAsync(token);

            if (cmd != "CtrlAltDel")
            {
                continue;
            }

            SendSAS(true);
            SendSAS(false);
        }
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
