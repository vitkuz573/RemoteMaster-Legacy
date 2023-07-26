using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class ScreenCaster : IScreenCaster
{
    private readonly IViewerFactory _viewerFactory;
    private readonly IViewerStore _viewerStore;
    private readonly ILogger<ScreenCaster> _logger;

    public ScreenCaster(IViewerFactory viewerFactory, IViewerStore viewerStore, ILogger<ScreenCaster> logger)
    {
        _viewerFactory = viewerFactory;
        _viewerStore = viewerStore;
        _logger = logger;
    }

    public async Task StartStreaming(string connectionId, CancellationToken cancellationToken)
    {
        var viewer = _viewerFactory.Create(connectionId);

        if (_viewerStore.TryAddViewer(viewer))
        {
            await viewer.StartStreaming(cancellationToken);
        }
        else
        {
            _logger.LogError("Failed to add viewer with connection ID {connectionId}", connectionId);
        }
    }

    public void SetSelectedScreen(string connectionId, string displayName)
    {
        if (_viewerStore.TryGetViewer(connectionId, out var viewer))
        {
            viewer.SetSelectedScreen(displayName);
        }
        else
        {
            _logger.LogError("Failed to find a viewer for connection ID {connectionId}", connectionId);
        }
    }

    // You can also implement methods to stop streaming and remove viewers when a client disconnects
}
