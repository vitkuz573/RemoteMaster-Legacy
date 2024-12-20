// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Services;

public class ViewerFactory : IViewerFactory
{
    public IViewer Create(HubCallerContext context, string group, string connectionId, string userName, string role, IPAddress ipAddress, string authenticationType)
    {
        return new Viewer(context, group, connectionId, userName, role, ipAddress, authenticationType);
    }
}
