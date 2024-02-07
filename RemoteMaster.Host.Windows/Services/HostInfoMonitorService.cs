// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostInfoMonitorService(IHostConfigurationService hostConfigurationService, IHostInfoService hostInfoService, IHostServiceManager hostServiceManager, IHostLifecycleService hostLifecycleService) : IHostedService
{
    private readonly string _configPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, hostConfigurationService.ConfigurationFileName);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        HostConfiguration hostConfiguration;

        try
        {
            hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(_configPath);
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException)
        {
            Log.Error(ex, "Error loading configuration.");
            return;
        }

        if (hostConfiguration.Host == null)
        {
            Log.Error("Host configuration is missing host details.");
            return;
        }

        var newIPAddress = hostInfoService.GetIPv4Address();
        var newMACAddress = hostInfoService.GetMacAddress();
        var newHostName = hostInfoService.GetHostName();

        if (hostConfiguration.Host.IPAddress != newIPAddress || hostConfiguration.Host.MACAddress != newMACAddress || hostConfiguration.Host.Name != newHostName)
        {
            try
            {
                hostConfiguration.Host.IPAddress = newIPAddress;
                hostConfiguration.Host.MACAddress = newMACAddress;
                hostConfiguration.Host.Name = newHostName;

                await hostConfigurationService.SaveConfigurationAsync(hostConfiguration, _configPath);
 
                await hostServiceManager.UpdateHostInformation(hostConfiguration);

                // TODO: Запросить новый сертификат (исправить возможный костыль)
                await hostLifecycleService.UnregisterAsync(hostConfiguration);
                await hostLifecycleService.RegisterAsync(hostConfiguration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving updated configuration.");
            }
        }
        else
        {
            Log.Information("Current host information matches the saved values. No action required.");
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{hostConfiguration.Server}:5254");

            var newGroup = await connection.InvokeAsync<string>("GetNewGroupIfChangeRequested", hostConfiguration.Host.MACAddress);

            if (!string.IsNullOrEmpty(newGroup))
            {
                hostConfiguration.Group = newGroup;
                await hostConfigurationService.SaveConfigurationAsync(hostConfiguration, _configPath);
                Log.Information("Group for this device was updated based on the group change request.");
                await connection.InvokeAsync("AcknowledgeGroupChange", hostConfiguration.Host.MACAddress);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing group change requests.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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
