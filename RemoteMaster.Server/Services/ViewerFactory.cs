using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;

namespace RemoteMaster.Server.Services;

public class ViewerFactory : IViewerFactory
{
    private readonly IScreenCapturer _screenCapturer;
    private readonly IHubContext<ControlHub> _hubContext;
    private readonly ILogger<Viewer> _logger;

    public ViewerFactory(IScreenCapturer screenCapturer, ILogger<Viewer> logger, IHubContext<ControlHub> hubContext)
    {
        _screenCapturer = screenCapturer;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Viewer CreateViewer(string connectionId)
    {
        return new Viewer(_screenCapturer, _logger, _hubContext, connectionId);
    }
}