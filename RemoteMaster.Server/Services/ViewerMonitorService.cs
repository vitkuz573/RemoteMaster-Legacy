using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class ViewerMonitorService : IViewerMonitorService
{
    private readonly IViewerStore _viewerStore;
    private Timer _timer;

    public DateTime LastSeen { get; set; }

    public ViewerMonitorService(IViewerStore viewerStore)
    {
        _viewerStore = viewerStore;
        LastSeen = DateTime.UtcNow;
    }

    public void StartMonitoring()
    {
        _timer = new Timer(CheckViewers, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private void CheckViewers(object state)
    {
        if (!_viewerStore.Viewers.Any())
        {
            var now = DateTime.UtcNow;

            if ((now - LastSeen).TotalSeconds > 30)
            {
                Environment.Exit(0);
            }
        }
        else
        {
            LastSeen = DateTime.UtcNow;
        }
    }
}
