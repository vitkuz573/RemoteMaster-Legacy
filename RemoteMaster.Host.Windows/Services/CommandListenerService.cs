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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _loopTask = Task.Run(ListenLoop, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeAsync();
    }

    private async Task ListenLoop()
    {
        while (!_disposed)
        {
            using var server = new NamedPipeServerStream(PipeNames.CommandPipe, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();
            
            using var reader = new StreamReader(server, Encoding.UTF8);
            
            var cmd = await reader.ReadLineAsync();
            
            if (cmd == "CtrlAltDel")
            {
                SendSAS(true);
                SendSAS(false);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_loopTask != null)
        {
            await _loopTask;
        }
    }
}
