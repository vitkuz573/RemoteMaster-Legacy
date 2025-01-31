// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Abstractions;

public abstract class AbstractDaemon(ILogger<AbstractDaemon> logger) : IService
{
    private readonly CancellationTokenSource _cts = new();

    private Task? _daemonTask;

    public abstract string Name { get; }
    
    public abstract string ExecutablePath { get; }
    
    public abstract string[] Arguments { get; }
    
    public abstract bool IsInstalled { get; }

    public bool IsRunning { get; private set; }

    public virtual void Create()
    {
        // In Linux, service creation is typically handled by systemd unit files.
        // This method can be used to generate necessary configurations if needed.
        logger.LogInformation($"{Name} daemon creation logic (if any) goes here.");
    }

    public virtual void Delete()
    {
        // Similarly, service deletion would be handled by systemd.
        logger.LogInformation($"{Name} daemon deletion logic (if any) goes here.");
    }

    public virtual void Start()
    {
        if (IsRunning)
        {
            logger.LogWarning($"{Name} daemon is already running.");

            return;
        }

        logger.LogInformation($"Starting {Name} daemon...");

        _daemonTask = Task.Run(() => RunDaemonAsync(_cts.Token));

        IsRunning = true;
    }

    public virtual void Stop()
    {
        if (!IsRunning)
        {
            logger.LogWarning($"{Name} daemon is not running.");

            return;
        }

        logger.LogInformation($"Stopping {Name} daemon...");

        _cts.Cancel();

        try
        {
            _daemonTask?.Wait();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
        {
            // Expected when the task is canceled
        }
        finally
        {
            IsRunning = false;

            logger.LogInformation($"{Name} daemon has stopped.");
        }
    }

    public virtual void Restart()
    {
        logger.LogInformation($"Restarting {Name} daemon...");

        Stop();
        Start();
    }

    protected abstract Task ExecuteDaemonAsync(CancellationToken cancellationToken);

    private async Task RunDaemonAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation($"{Name} daemon is running.");

        try
        {
            await ExecuteDaemonAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when the daemon is stopping
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{Name} daemon encountered an unexpected error.");
        }
        finally
        {
            logger.LogInformation($"{Name} daemon execution loop has ended.");
        }
    }
}
