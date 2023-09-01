// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, IConnectionContext> _contexts = new();
    private readonly IConnectionContextFactory _connectionContextFactory;

    public ConnectionManager(IConnectionContextFactory connectionContextFactory)
    {
        _connectionContextFactory = connectionContextFactory;
    }

    public IConnectionContext Connect(string connectionName, string url, bool useMessagePack = false)
    {
        var context = _connectionContextFactory.Create().Configure(url, useMessagePack);
        _contexts[connectionName] = context;

        return context;
    }

    public IConnectionContext? Get(string connectionName)
    {
        return _contexts.TryGetValue(connectionName, out var context) ? context : null;
    }

    public async Task DisconnectAsync(string connectionName)
    {
        if (_contexts.TryRemove(connectionName, out var context) && context != null)
        {
            await context.StopAsync();
        }
    }

    public void Dispose()
    {
        foreach (var context in _contexts.Values)
        {
            context?.StopAsync().Wait();
        }
    }
}
