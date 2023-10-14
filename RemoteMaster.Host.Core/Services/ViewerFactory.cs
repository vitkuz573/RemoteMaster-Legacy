// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;

namespace RemoteMaster.Host.Core.Services;

public class ViewerFactory : IViewerFactory
{
    private readonly IScreenCapturerService _screenCapturerService;
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private readonly ILogger<Viewer> _logger;

    public ViewerFactory(IScreenCapturerService screenCapturerService, ILogger<Viewer> logger, IHubContext<ControlHub, IControlClient> hubContext)
    {
        _screenCapturerService = screenCapturerService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public IViewer Create(string connectionId)
    {
        return new Viewer(_screenCapturerService, _logger, _hubContext, connectionId);
    }
}