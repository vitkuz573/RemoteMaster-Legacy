// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostRegistrationMonitorService : IHostedService
{
    private readonly IHostLifecycleService _hostLifecycleService;
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IHostInformationMonitorService _hostInformationMonitorService;

    private readonly Timer _timer;

    public HostRegistrationMonitorService(IHostLifecycleService hostLifecycleService, IHostConfigurationService hostConfigurationService, IHostInformationMonitorService hostInformationMonitorService)
    {
        _hostLifecycleService = hostLifecycleService;
        _hostConfigurationService = hostConfigurationService;
        _hostInformationMonitorService = hostInformationMonitorService;

        _timer = new Timer(CheckHostRegistration, null, Timeout.Infinite, 0);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));

        return Task.CompletedTask;
    }

    private async void CheckHostRegistration(object? state)
    {
        try
        {
            var configurationChanged = await _hostInformationMonitorService.UpdateHostConfigurationAsync();
            var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync(false);
            var isHostRegistered = await _hostLifecycleService.IsHostRegisteredAsync(hostConfiguration);

            switch (isHostRegistered)
            {
                case true when configurationChanged:
                    await _hostLifecycleService.UpdateHostInformationAsync(hostConfiguration);
                    Log.Information("Host information updated due to configuration change.");
                    break;
                case false:
                    Log.Warning("Host is not registered. Performing necessary actions...");
                    await _hostLifecycleService.RegisterAsync(hostConfiguration);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during host registration check.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();

        return Task.CompletedTask;
    }
}