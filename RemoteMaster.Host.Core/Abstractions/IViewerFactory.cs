// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IViewerFactory
{
    IViewer Create(string connectionId, HubCallerContext context, string group, string userName, string role, string ipAddress, string authenticationType);
}