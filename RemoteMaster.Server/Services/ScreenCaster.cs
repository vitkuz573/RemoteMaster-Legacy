using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dtos;

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
        var viewer = _viewerFactory.CreateViewer(connectionId);
        _viewerStore.AddViewer(viewer);

        await viewer.StartStreaming(cancellationToken);
    }

    public void SetSelectedScreen(string connectionId, SelectScreenDto dto)
    {
        var viewer = _viewerStore.GetViewer(connectionId);

        if (viewer != null)
        {
            viewer.SetSelectedScreen(dto);
        }
        else
        {
            _logger.LogError("Failed to find a viewer for connection ID {connectionId}", connectionId);
        }
    }

    // You can also implement methods to stop streaming and remove viewers when a client disconnects
}
