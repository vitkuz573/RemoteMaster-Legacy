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

public class HostInfoMonitorService(IHostConfigurationService hostConfigurationService, IHostInfoService hostInfoService, IHostServiceManager hostServiceManager, JsonSerializerOptions jsonOptions) : IHostedService
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

        var newIPAddress = hostInfoService.GetIPv4Address();
        var newHostName = hostInfoService.GetHostName();

        if (hostConfiguration.Host?.IPAddress != newIPAddress || hostConfiguration.Host?.Name != newHostName)
        {
            try
            {
                hostConfiguration.Host.Name = newHostName;
                hostConfiguration.Host.IPAddress = newIPAddress;

                await hostConfigurationService.SaveConfigurationAsync(hostConfiguration, _configPath);

                await hostServiceManager.UpdateHostInformation(hostConfiguration);
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
