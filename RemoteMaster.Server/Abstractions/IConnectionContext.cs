// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IConnectionContext
{
    HubConnection Connection { get; }

    IConnectionContext Configure(string url, Action<HttpConnectionOptions> configureOptions, bool useMessagePack = false);

    IConnectionContext On<T>(string methodName, Action<T> handler);

    IConnectionContext On<T>(string methodName, Func<T, Task> handler);

    Task<IConnectionContext> StartAsync();

    Task<IConnectionContext> StopAsync();
}
