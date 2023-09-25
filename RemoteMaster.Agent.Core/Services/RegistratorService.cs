// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent.Core.Services;

public class RegistratorService : IRegistratorService
{
    private readonly ILogger<RegistratorService> _logger;

    public RegistratorService(ILogger<RegistratorService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(ConfigurationModel config, string hostName, string ipAddress, string macAddress)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            _logger.LogInformation("Installing...");
            var result = await connection.InvokeAsync<bool>("RegisterClient", hostName, ipAddress, macAddress, config.Group);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Installation failed: {Message}", ex.Message);

            return false;
        }
    }

    public async Task<bool> UnregisterAsync(ConfigurationModel config, string hostName)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            _logger.LogInformation("Uninstalling...");
            var result = await connection.InvokeAsync<bool>("UnregisterClient", hostName, config.Group);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("Uninstallation failed: {Message}", ex.Message);

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
