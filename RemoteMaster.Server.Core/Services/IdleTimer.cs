using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;

namespace RemoteMaster.Server.Core.Services;

public class IdleTimer : IIdleTimer
{
    private readonly IViewerStore _viewerStore;
    private readonly IShutdownService _shutdownService;
    private readonly ILogger<IdleTimer> _logger;
    private Timer _timer;
    private readonly object _lock = new();

    public DateTime LastSeen { get; private set; }

    public IdleTimer(IViewerStore viewerStore, IShutdownService shutdownService, ILogger<IdleTimer> logger)
    {
        _viewerStore = viewerStore;
        _shutdownService = shutdownService;
        _logger = logger;
        LastSeen = DateTime.UtcNow;
    }

    public void StartMonitoring()
    {
        _timer = new Timer(CheckViewers, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public void StopMonitoring()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void CheckViewers(object state)
    {
        try
        {
            if (!_viewerStore.Viewers.Any())
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