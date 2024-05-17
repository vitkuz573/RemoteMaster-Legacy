// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;

namespace RemoteMaster.Host.Core.Services;

public class ViewerFactory(IHubContext<ControlHub, IControlClient> hubContext, IScreenCapturerService screenCapturerService) : IViewerFactory
{
    public IViewer Create(string connectionId, string userName)
    {
        return new Viewer(hubContext, screenCapturerService, connectionId, userName);
    }
}