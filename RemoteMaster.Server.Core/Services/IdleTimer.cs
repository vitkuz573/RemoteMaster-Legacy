// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;

namespace RemoteMaster.Server.Core.Services;

public class IdleTimer : IIdleTimer
{
    private readonly IAppState _appState;
    private readonly IShutdownService _shutdownService;
    private readonly ILogger<IdleTimer> _logger;
    private Timer _timer;
    private readonly object _lock = new();

    public DateTime LastSeen { get; private set; }

    public IdleTimer(IAppState appState, IShutdownService shutdownService, ILogger<IdleTimer> logger)
    {
        _appState = appState;
        _shutdownService = shutdownService;
        _logger = logger;
        LastSeen = DateTime.UtcNow;
    }

    public void StartMonitoring()
    {
        LastSeen = DateTime.UtcNow;
        _timer = new Timer(CheckViewers, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public void StopMonitoring()
    {
        var timer = Interlocked.Exchange(ref _timer, null);
        timer?.Dispose();
    }

    private void CheckViewers(object state)
    {
        try
        {
            if (!_appState.Viewers.Any())
            {
                var now = DateTime.UtcNow;

                if (Monitor.TryEnter(_lock, TimeSpan.FromSeconds(2)))
                {
                    try
                    {
                        if ((now - LastSeen).TotalSeconds > 30)
                        {
                            StopMonitoring();
                            _shutdownService.InitiateShutdown();
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_lock);
                    }
                }
            }
            else
            {
                if (Monitor.TryEnter(_lock, TimeSpan.FromSeconds(2)))
                {
                    try
                    {
                        LastSeen = DateTime.UtcNow;
                    }
                    finally
                    {
                        Monitor.Exit(_lock);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking viewers.");
        }
    }
}