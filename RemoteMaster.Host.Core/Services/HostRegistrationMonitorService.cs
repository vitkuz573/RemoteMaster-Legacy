// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Timers;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostRegistrationMonitorService(IHostLifecycleService hostLifecycleService, IHostConfigurationService hostConfigurationService, IHostInfoService hostInfoService) : IHostedService
{
    private System.Timers.Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new System.Timers.Timer(60000);
        _timer.Elapsed += CheckHostRegistration;
        _timer.Start();

        return Task.CompletedTask;
    }

    private async void CheckHostRegistration(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var configPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, hostConfigurationService.ConfigurationFileName);

            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(configPath);

            if (!await hostLifecycleService.IsHostRegisteredAsync(hostConfiguration))
            {
                Log.Warning("Host is not registered. Performing necessary actions...");

                await hostLifecycleService.RegisterAsync(hostConfiguration);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during host registration check.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        _timer?.Dispose();

        return Task.CompletedTask;
    }
}
