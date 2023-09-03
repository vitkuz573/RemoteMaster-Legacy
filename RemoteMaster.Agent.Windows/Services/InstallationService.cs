// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent.Services;

public class InstallationService : IInstallationService
{
    private readonly ILogger<InstallationService> _logger;

    public InstallationService(ILogger<InstallationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> InstallAsync(ConfigurationModel config, string hostName, string ipAddress, string group)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            _logger.LogInformation("Installing...");
            var result = await connection.InvokeAsync<bool>("RegisterClient", hostName, ipAddress, group);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Installation failed: {ex.Message}");

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
