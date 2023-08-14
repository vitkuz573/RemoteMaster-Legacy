// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class RemoteConnectionManager : IRemoteConnectionManager
{
    private readonly ConcurrentDictionary<IConnectionType, HubConnection> _connections = new();
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

        if (_connections.ContainsKey(connectionType))
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

        if (!_connections.TryAdd(connectionType, connection))
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

        if (_connections.TryRemove(connectionType, out var connection))
        {
            await connection.DisposeAsync();
        }
    }

    public HubConnection? GetConnection(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        _connections.TryGetValue(connectionType, out var connection);

        return connection;
    }

    public async Task StartConnectionAsync(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (_connections.TryGetValue(connectionType, out var connection))
        {
            await connection.StartAsync();
            _logger.LogInformation("Started connection of type {ConnectionType}.", connectionType.Name);
        }
    }

    public async Task StopConnectionAsync(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (_connections.TryGetValue(connectionType, out var connection))
        {
            await connection.StopAsync();
            _logger.LogInformation("Stopped connection of type {ConnectionType}.", connectionType.Name);
        }
    }

    public void RegisterEventHandler<TPayload>(IConnectionType connectionType, string eventName, Action<TPayload> handler)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name cannot be null or whitespace.", nameof(eventName));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (_connections.TryGetValue(connectionType, out var connection))
        {
            connection.On(eventName, handler);
        }
    }

    public void RegisterEventHandler<TPayload>(IConnectionType connectionType, string eventName, Func<TPayload, Task> handler)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name cannot be null or whitespace.", nameof(eventName));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (_connections.TryGetValue(connectionType, out var connection))
        {
            connection.On(eventName, handler);
        }
    }

    public void RemoveEventHandler(IConnectionType connectionType, string eventName)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name cannot be null or whitespace.", nameof(eventName));
        }

        if (_connections.TryGetValue(connectionType, out var connection))
        {
            connection.Remove(eventName);
        }
    }
}
