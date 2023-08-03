// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Server.Core.Hubs;

namespace RemoteMaster.Server.Core.Services;

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

    public IViewer Create(string connectionId)
    {
        return new Viewer(_screenCapturer, _logger, _hubContext, connectionId);
    }
}