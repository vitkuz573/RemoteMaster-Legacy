// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;

namespace RemoteMaster.Client.Services;

public class RemoteConnectionManager : IRemoteConnectionManager
{
    private readonly ConcurrentDictionary<string, HubConnection> _connections = new();
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

        try
        {
            var connectionBuilder = new HubConnectionBuilder().WithUrl(url);

            if (useMessagePack)
            {
                connectionBuilder.AddMessagePackProtocol();
            }

            var connection = connectionBuilder.Build();

            if (!_connections.TryAdd(connectionType.Name, connection))
            {
                _logger.LogWarning("Connection of type {ConnectionType} already exists.", connectionType.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating connection of type {ConnectionType} to {URL}.", connectionType.Name, url);
            throw;
        }
    }

    public async Task RemoveConnectionAsync(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        try
        {
            if (_connections.TryRemove(connectionType.Name, out var connection))
            {
                await connection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection of type {ConnectionType}.", connectionType.Name);
            throw;
        }
    }

    public HubConnection? GetConnection(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        _connections.TryGetValue(connectionType.Name, out var connection);

        return connection;
    }

    public async Task StartConnectionAsync(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        try
        {
            if (_connections.TryGetValue(connectionType.Name, out var connection))
            {
                await connection.StartAsync();
                _logger.LogInformation("Started connection of type {ConnectionType}.", connectionType.Name);
            }
            else
            {
                _logger.LogWarning("No connection of type {ConnectionType} found.", connectionType.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting connection of type {ConnectionType}.", connectionType.Name);
            throw;
        }
    }

    public async Task StopConnectionAsync(IConnectionType connectionType)
    {
        if (connectionType == null)
        {
            throw new ArgumentNullException(nameof(connectionType));
        }

        try
        {
            if (_connections.TryGetValue(connectionType.Name, out var connection))
            {
                await connection.StopAsync();
                _logger.LogInformation("Stopped connection of type {ConnectionType}.", connectionType.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping connection of type {ConnectionType}.", connectionType.Name);
            throw;
        }
    }
}