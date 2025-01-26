// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Pipes;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class CommandListenerService(ILogger<CommandListenerService> logger) : IHostedService, IAsyncDisposable
{
    private NamedPipeServerStream? _pipeServer;
    private Task? _listeningTask;
    private bool _disposed;

    /// <summary>
    /// Starts the CommandListenerService and begins listening for IPC commands.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("CommandListenerService started.");

        await StartListeningAsync();
    }

    /// <summary>
    /// Stops the CommandListenerService and closes the IPC connection.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping CommandListenerService.");

        await DisposeAsyncCore();
    }

    /// <summary>
    /// Starts listening for IPC commands via Named Pipes.
    /// </summary>
    private async Task StartListeningAsync()
    {
        if (_pipeServer != null)
        {
            logger.LogWarning("Pipe server is already running.");

            return;
        }

        var pipeName = "CommandPipe";

        _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);

        _listeningTask = Task.Run(async () =>
        {
            while (_pipeServer != null && !_disposed)
            {
                try
                {
                    logger.LogInformation("Waiting for client connection...");

                    await _pipeServer.WaitForConnectionAsync();

                    logger.LogInformation("Client connected.");

                    using var reader = new StreamReader(_pipeServer, Encoding.UTF8);

                    string? command;

                    while ((command = await reader.ReadLineAsync()) != null)
                    {
                        logger.LogInformation("Received command: {Command}", command);

                        HandleCommand(command);
                    }

                    logger.LogInformation("Client disconnected.");

                    _pipeServer.Disconnect();
                }
                catch (IOException ex)
                {
                    logger.LogError(ex, "Pipe connection error.");

                    _pipeServer.Dispose();
                    _pipeServer = null;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error in pipe listening.");

                    _pipeServer.Dispose();
                    _pipeServer = null;
                }

                if (!_disposed && _pipeServer == null)
                {
                    _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                }
            }
        });

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles received commands.
    /// </summary>
    /// <param name="command">The command received.</param>
    private void HandleCommand(string command)
    {
        if (command == "CtrlAltDel")
        {
            try
            {
                SendSAS(true);
                SendSAS(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing SendSAS.");
            }
        }
    }

    /// <summary>
    /// Disposes the service resources asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisposeAsyncCore();

        _disposed = true;
    }

    /// <summary>
    /// Asynchronously disposes the service resources.
    /// </summary>
    private async Task DisposeAsyncCore()
    {
        if (_pipeServer != null)
        {
            try
            {
                _pipeServer.Close();
                _pipeServer.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing pipe server.");
            }
            finally
            {
                _pipeServer = null;
            }
        }

        if (_listeningTask != null)
        {
            try
            {
                await _listeningTask;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in listening task during disposal.");
            }
        }
    }
}
