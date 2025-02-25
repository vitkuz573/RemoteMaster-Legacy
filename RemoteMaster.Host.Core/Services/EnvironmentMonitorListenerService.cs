// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Services;

public class EnvironmentMonitorListenerService(IUserInstanceService userInstanceService, ILogger<EnvironmentMonitorListenerService> logger) : IHostedService, IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(() => ListenLoopAsync(_cts.Token), cancellationToken);

        return Task.CompletedTask;
    }

    private async Task ListenLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(PipeNames.EnvironmentMonitorPipe, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                logger.LogInformation("Waiting for connection on pipe {Pipe}...", PipeNames.EnvironmentMonitorPipe);
                await server.WaitForConnectionAsync(token);

                using var reader = new StreamReader(server, Encoding.UTF8);
                var messageJson = await reader.ReadLineAsync(token);

                if (string.IsNullOrWhiteSpace(messageJson))
                {
                    continue;
                }

                try
                {
                    var message = JsonSerializer.Deserialize<EnvironmentMismatchMessage>(messageJson);
                    
                    if (message is not null && message.Command.Equals("restart", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation("Received restart command from environment monitor. Display: {Display}, XAuthority: {XAuthority}", message.Display, message.XAuthority);

                        await userInstanceService.RestartAsync();
                    }
                    else
                    {
                        logger.LogWarning("Received unknown command: {Message}", messageJson);
                    }
                }
                catch (JsonException jsonEx)
                {
                    logger.LogError(jsonEx, "Error deserializing message: {Message}", messageJson);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in environment monitor listener loop.");
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cts.CancelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        _cts.Dispose();

        await Task.CompletedTask;
    }
}
