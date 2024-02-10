// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Timers;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostInfoMonitorService(IServerHubService serverHubService, IHostConfigurationService hostConfigurationService, IHostInformationService hostInformationService, IHostLifecycleService hostLifecycleService) : IHostedService
{
    private System.Timers.Timer? _timer;

    private readonly string _configDirectoryPath = Path.GetDirectoryName(Environment.ProcessPath)!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new System.Timers.Timer(60000);
        _timer.Elapsed += CheckHostInformation;
        _timer.Start();

        return Task.CompletedTask;
    }

    private async void CheckHostInformation(object? sender, ElapsedEventArgs e)
    {
        HostConfiguration hostConfiguration;

        try
        {
            hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(_configDirectoryPath);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
        {
            Log.Error(ex, "Error loading configuration.");
            return;
        }

        if (hostConfiguration.Host == null)
        {
            Log.Error("Host configuration is missing host details.");
            return;
        }

        var hostInformation = hostInformationService.GetHostInformation();

        if (!hostConfiguration.Host.Equals(hostInformation))
        {
            try
            {
                hostConfiguration.Host = hostInformation;

                await hostConfigurationService.SaveConfigurationAsync(hostConfiguration, _configDirectoryPath);

                await hostLifecycleService.UpdateHostInformationAsync(hostConfiguration);
                await hostLifecycleService.UnregisterAsync(hostConfiguration);
                await hostLifecycleService.RegisterAsync(hostConfiguration);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving updated configuration.");
            }
        }

        try
        {
            await serverHubService.ConnectAsync(hostConfiguration.Server);

            var newGroup = await serverHubService.GetNewGroupIfChangeRequested(hostConfiguration.Host.MacAddress);

            if (string.IsNullOrEmpty(newGroup))
            {
                return;
            }

            hostConfiguration.Group = newGroup;
            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration, _configDirectoryPath);
            Log.Information("Group for this device was updated based on the group change request.");
            await serverHubService.AcknowledgeGroupChange(hostConfiguration.Host.MacAddress);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing group change requests.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        _timer?.Dispose();

        return Task.CompletedTask;
    }
}
