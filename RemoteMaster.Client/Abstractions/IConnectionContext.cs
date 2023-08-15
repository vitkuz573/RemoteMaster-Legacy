// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client.Abstractions;

public interface IConnectionContext
{
    HubConnection Connection { get; }

    IConnectionContext Configure(string url, bool useMessagePack = false);

    IConnectionContext On<T>(string methodName, Action<T> handler);

    IConnectionContext On<T>(string methodName, Func<T, Task> handler);

    Task<IConnectionContext> StartAsync();

    Task<IConnectionContext> StopAsync();
}
