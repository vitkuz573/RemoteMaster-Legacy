// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;

namespace RemoteMaster.Host.Core.Services;

public class ViewerFactory(IAppState appState, IScreenCapturerService screenCapturerService, IHubContext<ControlHub, IControlClient> hubContext) : IViewerFactory
{
    public IViewer Create(string connectionId)
    {
        return new Viewer(appState, screenCapturerService, hubContext, connectionId);
    }
}