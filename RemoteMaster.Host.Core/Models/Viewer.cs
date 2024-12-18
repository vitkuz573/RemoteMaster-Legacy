// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class Viewer(ICapturingContext capturingContext, HubCallerContext context, string group, string connectionId, string userName, string role, IPAddress ipAddress, string authenticationType) : IViewer
{
    public ICapturingContext CapturingContext { get; } = capturingContext;

    public HubCallerContext Context { get; } = context;

    public string Group { get; } = group;

    public string ConnectionId { get; } = connectionId;

    public string UserName { get; } = userName;

    public string Role { get; } = role;

    public DateTime ConnectedTime { get; } = DateTime.UtcNow;

    public IPAddress IpAddress { get; } = ipAddress;

    public string AuthenticationType { get; } = authenticationType;

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CapturingContext.Dispose();

        _disposed = true;
    }
}