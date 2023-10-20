// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostLifecycleService : IHostLifecycleService
{
    private readonly ILogger<HostLifecycleService> _logger;

    public HostLifecycleService(ILogger<HostLifecycleService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(HostConfiguration config, string hostName, string ipAddress, string macAddress)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            _logger.LogInformation("Registering host...");
            var result = await connection.InvokeAsync<bool>("RegisterHostAsync", hostName, ipAddress, macAddress, config.Group);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Registering host failed: {Message}", ex.Message);

            return false;
        }
    }

    public async Task<bool> UnregisterAsync(HostConfiguration config, string hostName)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            _logger.LogInformation("Unregistering host...");
            var result = await connection.InvokeAsync<bool>("UnregisterHostAsync", hostName, config.Group);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Unregistering host failed: {Message}", ex.Message);

            return false;
        }
    }

    private static async Task<HubConnection> ConnectToServerHub(string serverUrl)
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/hubs/management")
            .Build();

        await hubConnection.StartAsync();

        return hubConnection;
    }
}
