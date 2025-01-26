// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CommandSender(ILogger<CommandSender> logger) : ICommandSender
{
    private readonly string _pipeName = "CommandPipe";
    private NamedPipeClientStream? _pipeClient;

    private async Task ConnectAsync()
    {
        try
        {
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

            logger.LogInformation("Attempting to connect to pipe server...");

            await _pipeClient.ConnectAsync(5000);

            if (_pipeClient.IsConnected)
            {
                logger.LogInformation("Successfully connected to pipe server.");
            }
            else
            {
                logger.LogWarning("Failed to connect to pipe server.");

                _pipeClient.Dispose();
                _pipeClient = null;
            }
        }
        catch (TimeoutException)
        {
            logger.LogError("Connection to pipe server timed out.");

            _pipeClient = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error connecting to pipe server.");

            _pipeClient = null;
        }
    }

    /// <summary>
    /// Sends a command through the pipe.
    /// </summary>
    /// <param name="command">The command to send.</param>
    public async Task SendCommandAsync(string command)
    {
        if (_pipeClient == null || !_pipeClient.IsConnected)
        {
            logger.LogWarning("Pipe client not connected. Attempting to reconnect...");

            await ConnectAsync();

            if (_pipeClient == null || !_pipeClient.IsConnected)
            {
                logger.LogError("Unable to connect to pipe server. Command not sent.");

                return;
            }
        }

        try
        {
            using var writer = new StreamWriter(_pipeClient, Encoding.UTF8, leaveOpen: true)
            {
                AutoFlush = true
            };

            await writer.WriteLineAsync(command);

            logger.LogInformation("Command '{Command}' sent through pipe.", command);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending command through pipe.");
        }
    }

    /// <summary>
    /// Asynchronously disposes the pipe client.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_pipeClient != null)
        {
            try
            {
                _pipeClient.Close();
                _pipeClient.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing pipe client.");
            }
            finally
            {
                _pipeClient = null;
            }
        }

        await Task.CompletedTask;
    }
}
