using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dtos;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Services;

public class ScreenCaster : IScreenCaster
{
    private readonly IViewerFactory _viewerFactory;
    private readonly ILogger<ScreenCaster> _logger;
    private readonly ConcurrentDictionary<string, Viewer> _viewers = new();

    public ScreenCaster(IViewerFactory viewerFactory, ILogger<ScreenCaster> logger)
    {
        _viewerFactory = viewerFactory;
        _logger = logger;
    }

    public async Task StartStreaming(string connectionId, CancellationToken cancellationToken)
    {
        var viewer = _viewerFactory.CreateViewer(connectionId);
        _viewers.TryAdd(connectionId, viewer);

        await viewer.StartStreaming(cancellationToken);
    }

    public void SetSelectedScreen(string connectionId, SelectScreenDto dto)
    {
        if (_viewers.TryGetValue(connectionId, out var viewer))
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
