// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class EnvironmentMonitorService(IEnvironmentProvider environmentProvider, ILogger<EnvironmentMonitorService> logger) : IHostedService, IAsyncDisposable
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var currentDisplay = Environment.GetEnvironmentVariable("DISPLAY") ?? string.Empty;
            var currentXAuthority = Environment.GetEnvironmentVariable("XAUTHORITY") ?? string.Empty;

            var expectedDisplay = await environmentProvider.GetDisplayAsync();
            var expectedXAuthority = await environmentProvider.GetXAuthorityAsync();

            if (!string.Equals(currentDisplay, expectedDisplay, StringComparison.Ordinal) || !string.Equals(currentXAuthority, expectedXAuthority, StringComparison.Ordinal))
            {
                logger.LogWarning("Environment variable mismatch detected. Current: DISPLAY={CurrentDisplay}, XAUTHORITY={CurrentXAuthority}. Expected: DISPLAY={ExpectedDisplay}, XAUTHORITY={ExpectedXAuthority}", currentDisplay, currentXAuthority, expectedDisplay, expectedXAuthority);

                await SendEnvironmentMismatchNotificationAsync(currentDisplay, currentXAuthority, cancellationToken);
            }
            else
            {
                logger.LogInformation("Environment variables match. No action needed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during environment variable check.");
        }

        await Task.CompletedTask;
    }

    private async Task SendEnvironmentMismatchNotificationAsync(string currentDisplay, string currentXAuthority, CancellationToken cancellationToken)
    {
        try
        {
            await using var client = new NamedPipeClientStream(".", PipeNames.EnvironmentMonitorPipe, PipeDirection.Out, PipeOptions.Asynchronous);
            await client.ConnectAsync(5000, cancellationToken);

            await using var writer = new StreamWriter(client, Encoding.UTF8);
            writer.AutoFlush = true;

            var messageData = new EnvironmentMismatchMessage
            {
                Display = currentDisplay,
                XAuthority = currentXAuthority
            };

            var messageJson = JsonSerializer.Serialize(messageData);

            await writer.WriteLineAsync(messageJson.AsMemory(), cancellationToken);

            logger.LogInformation("Sent environment mismatch notification via pipe {Pipe}.", PipeNames.EnvironmentMonitorPipe);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending notification via pipe {Pipe}.", PipeNames.EnvironmentMonitorPipe);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }
}
