// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Services;

public class ViewerFactory(IScreenCapturerService screenCapturerService) : IViewerFactory
{
    public IViewer Create(string connectionId, string group, string userName, string role)
    {
        return new Viewer(screenCapturerService, connectionId, group, userName, role);
    }
}