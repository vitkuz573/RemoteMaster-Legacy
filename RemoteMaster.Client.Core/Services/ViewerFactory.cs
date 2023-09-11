// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Hubs;

namespace RemoteMaster.Client.Core.Services;

public class ViewerFactory : IViewerFactory
{
    private readonly IConfigurationProvider _configurationService;
    private readonly IScreenCapturerService _screenCapturer;
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private readonly ILogger<Viewer> _logger;

    public ViewerFactory(IConfigurationProvider configurationService, IScreenCapturerService screenCapturer, ILogger<Viewer> logger, IHubContext<ControlHub, IControlClient> hubContext)
    {
        _configurationService = configurationService;
        _screenCapturer = screenCapturer;
        _hubContext = hubContext;
        _logger = logger;
    }

    public IViewer Create(string connectionId)
    {
        return new Viewer(_screenCapturer, _configurationService, _logger, _hubContext, connectionId);
    }
}