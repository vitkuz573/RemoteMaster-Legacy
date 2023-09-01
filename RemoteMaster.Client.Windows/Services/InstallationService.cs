// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Services;

public class InstallationService : IInstallationService
{
    public async Task<bool> InstallClientAsync(ConfigurationModel config, string hostName, string ipAddress, string group)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            Console.WriteLine("Installing...");
            var result = await connection.InvokeAsync<bool>("RegisterClient", hostName, ipAddress, group);

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Installation failed: {ex.Message}");
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
