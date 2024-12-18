// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.SignalR;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IViewerFactory
{
    IViewer Create(HubCallerContext context, string group, string connectionId, string userName, string role, IPAddress ipAddress, string authenticationType);
}
