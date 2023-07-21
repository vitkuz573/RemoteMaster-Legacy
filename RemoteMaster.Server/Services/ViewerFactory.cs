using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;

namespace RemoteMaster.Server.Services;

public class ViewerFactory : IViewerFactory
{
    private readonly IScreenCapturer _screenCapturer;
    private readonly IAudioCapturer _audioCapturer;
    private readonly IHubContext<ControlHub> _hubContext;
    private readonly ILogger<Viewer> _logger;

    public ViewerFactory(IScreenCapturer screenCapturer, IAudioCapturer audioCapturer, ILogger<Viewer> logger, IHubContext<ControlHub> hubContext)
    {
        _screenCapturer = screenCapturer;
        _audioCapturer = audioCapturer;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Viewer Create(string connectionId)
    {
        return new Viewer(_screenCapturer, _audioCapturer, _logger, _hubContext, connectionId);
    }
}