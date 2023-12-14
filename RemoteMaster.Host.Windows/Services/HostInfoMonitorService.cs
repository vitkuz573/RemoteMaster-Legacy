// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostInfoMonitorService(IHostConfigurationService hostConfigurationService, IHostInfoService hostInfoService, IHostServiceManager hostServiceManager) : IHostedService
{
    private readonly string _configPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, hostConfigurationService.ConfigurationFileName);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        HostConfiguration configuration;

        try
        {
            configuration = await hostConfigurationService.LoadConfigurationAsync(_configPath);
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException)
        {
            Log.Error(ex, "Error loading configuration.");

            return;
        }

        var newIPAddress = hostInfoService.GetIPv4Address();
        var newHostName = hostInfoService.GetHostName();

        if (configuration.Host?.IPAddress != newIPAddress || configuration.Host?.Name != newHostName)
        {
            var macAddress = hostInfoService.GetMacAddress();

            await hostServiceManager.UpdateHostInformation(configuration, newHostName, newIPAddress, macAddress);

            try
            {
                configuration.Host = new Computer
                {
                    Name = newHostName,
                    IPAddress = newIPAddress,
                    MACAddress = macAddress
                };

                var json = JsonSerializer.Serialize(configuration);
                await File.WriteAllTextAsync(_configPath, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving updated configuration.");
            }
        }
        else
        {
            Log.Information("Current IP and hostname match the saved values. No action required.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
