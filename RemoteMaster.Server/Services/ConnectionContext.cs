// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class ConnectionContext : IConnectionContext
{
    public HubConnection Connection { get; private set; }

    public IConnectionContext Configure(string url, Action<HttpConnectionOptions> configureOptions, bool useMessagePack = false)
    {
        var builder = new HubConnectionBuilder();

        if (configureOptions != null)
        {
            builder.WithUrl(url, configureOptions);
        }
        else
        {
            builder.WithUrl(url);
        }

        if (useMessagePack)
        {
            builder.AddMessagePackProtocol();
        }

        Connection = builder.Build();

        return this;
    }

    public IConnectionContext On<T>(string methodName, Action<T> handler)
    {
        Connection.On(methodName, (T payload) => handler(payload));

        return this;
    }

    public IConnectionContext On<T>(string methodName, Func<T, Task> handler)
    {
        Connection.On(methodName, (T payload) => handler(payload));

        return this;
    }

    public async Task<IConnectionContext> StartAsync()
    {
        await Connection.StartAsync();

        return this;
    }

    public async Task<IConnectionContext> StopAsync()
    {
        await Connection.StopAsync();

        return this;
    }
}
