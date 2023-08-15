// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Models;

namespace RemoteMaster.Client.Services;

public class ConnectionContext : IConnectionContext
{
    public HubConnection Connection { get; private set; }

    private DateTime _connectionStartTime;
    private string _protocolUsed = "Default";

    public IConnectionContext Configure(string url, bool useMessagePack = false)
    {
        var builder = new HubConnectionBuilder().WithUrl(url);

        if (useMessagePack)
        {
            builder.AddMessagePackProtocol();
            _protocolUsed = "MessagePack";
        }

        Connection = builder.Build();

        return this;
    }

    public ConnectionDiagnostics GetConnectionDiagnostics()
    {
        return new ConnectionDiagnostics
        {
            ConnectionState = Connection.State.ToString(),
            ConnectionDuration = DateTime.UtcNow - _connectionStartTime,
            ConnectionId = Connection.ConnectionId,
            ProtocolUsed = _protocolUsed
        };
    }

    public IConnectionContext On<T>(string methodName, Action<T> handler)
    {
        Connection.On(methodName, handler);
        return this;
    }

    public IConnectionContext On<T>(string methodName, Func<T, Task> handler)
    {
        Connection.On(methodName, handler);

        return this;
    }

    public async Task<IConnectionContext> StartAsync()
    {
        await Connection.StartAsync();
        _connectionStartTime = DateTime.UtcNow;

        return this;
    }

    public async Task<IConnectionContext> StopAsync()
    {
        await Connection.StopAsync();

        return this;
    }
}
