// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Services;

public class InstallationService : IInstallationService
{
    public async Task<bool> InstallClientAsync(ConfigurationModel config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub(config.Server);

            Console.WriteLine("Installing...");
            var result = await connection.InvokeAsync<bool>("RegisterClient");

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
            .WithUrl($"{serverUrl}/yourHubName") // Замените на имя вашего хаба
            .Build();

        await hubConnection.StartAsync();

        return hubConnection;
    }
}
