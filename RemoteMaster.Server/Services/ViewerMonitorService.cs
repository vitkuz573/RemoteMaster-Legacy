using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class ViewerMonitorService : IViewerMonitorService
{
    private readonly IViewerStore _viewerStore;
    private readonly IShutdownService _shutdownService;
    private Timer _timer;
    private readonly object _lock = new();

    public DateTime LastSeen { get; private set; }

    public ViewerMonitorService(IViewerStore viewerStore, IShutdownService shutdownService)
    {
        _viewerStore = viewerStore;
        _shutdownService = shutdownService;
        LastSeen = DateTime.UtcNow;
    }

    public void StartMonitoring()
    {
        _timer = new Timer(CheckViewers, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private void CheckViewers(object state)
    {
        try
        {
            if (!_viewerStore.Viewers.Any())
            {
                var now = DateTime.UtcNow;

                lock (_lock)
                {
                    if ((now - LastSeen).TotalSeconds > 30)
                    {
                        _shutdownService.InitiateShutdown();
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    LastSeen = DateTime.UtcNow;
                }
            }
        }
        catch (Exception ex)
        {
            // log exception and continue
        }
    }
}
