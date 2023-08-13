// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class RemoteConnectionManager : IRemoteConnectionManager
{
    private readonly ConcurrentDictionary<string, (HubConnection connection, string url)> _connections = new();
    private readonly ILogger<RemoteConnectionManager> _logger;

    public RemoteConnectionManager(ILogger<RemoteConnectionManager> logger)
    {
        _logger = logger;
    }

    public void CreateConnectionAsync(IConnectionType connectionType, string url, bool useMessagePack = false)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (_connections.ContainsKey(connectionType.Name))
        {
            _logger.LogWarning("Connection of type {ConnectionType} already exists.", connectionType.Name);
            return;
        }

        var connectionBuilder = new HubConnectionBuilder().WithUrl(url);

        if (useMessagePack)
        {
            connectionBuilder.AddMessagePackProtocol();
        }

        var connection = connectionBuilder.Build();

        if (!_connections.TryAdd(connectionType.Name, (connection, url)))
        {
            _logger.LogWarning("Failed to add the connection of type {ConnectionType}.", connectionType.Name);
        }
    }

    public async Task RemoveConnectionAsync(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (_connections.TryRemove(connectionType.Name, out var connectionTuple))
        {
            await connectionTuple.connection.DisposeAsync();
        }
    }

    public HubConnection? GetConnection(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        _connections.TryGetValue(connectionType.Name, out var connectionTuple);
        return connectionTuple.connection;
    }

    public async Task StartConnectionAsync(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (_connections.TryGetValue(connectionType.Name, out var connectionTuple))
        {
            await connectionTuple.connection.StartAsync();
            _logger.LogInformation("Started connection of type {ConnectionType} with URL {Url}.", connectionType.Name, connectionTuple.url);
        }
    }

    public async Task StopConnectionAsync(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (_connections.TryGetValue(connectionType.Name, out var connectionTuple))
        {
            await connectionTuple.connection.StopAsync();
            _logger.LogInformation("Stopped connection of type {ConnectionType} with URL {Url}.", connectionType.Name, connectionTuple.url);
        }
    }
}
