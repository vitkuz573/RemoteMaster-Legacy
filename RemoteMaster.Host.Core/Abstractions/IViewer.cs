// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.SignalR;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IViewer : IDisposable
{
    ICapturingContext CapturingContext { get; }

    HubCallerContext Context { get; }

    string Group { get; }

    string ConnectionId { get; }

    string UserName { get; }

    string Role { get; }

    DateTime ConnectedTime { get; }

    public IPAddress IpAddress { get; }

    public string AuthenticationType { get; }
}
