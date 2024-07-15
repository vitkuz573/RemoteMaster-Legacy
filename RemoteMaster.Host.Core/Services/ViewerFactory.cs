// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Services;

public class ViewerFactory(IScreenCapturingService screenCapturingService) : IViewerFactory
{
    public IViewer Create(string connectionId, string group, string userName, string role, string ipAddress, string authenticationType)
    {
        return new Viewer(screenCapturingService, connectionId, group, userName, role, ipAddress, authenticationType);
    }
}