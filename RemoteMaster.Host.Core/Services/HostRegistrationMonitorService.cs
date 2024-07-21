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
    private readonly IHostInformationUpdaterService _hostInformationMonitorService;
    private readonly IUserInstanceService _userInstanceService;

    private readonly Timer _timer;

    public HostRegistrationMonitorService(IHostLifecycleService hostLifecycleService, IHostConfigurationService hostConfigurationService, IHostInformationUpdaterService hostInformationUpdaterService, IUserInstanceService userInstanceService)
    {
        _hostLifecycleService = hostLifecycleService;
        _hostConfigurationService = hostConfigurationService;
        _hostInformationMonitorService = hostInformationUpdaterService;
        _userInstanceService = userInstanceService;

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
            var isHostRegistered = await _hostLifecycleService.IsHostRegisteredAsync();

            if (configurationChanged)
            {
                if (isHostRegistered)
                {
                    Log.Information("Updating host information and renewing certificate due to configuration change.");
                    await _hostLifecycleService.UpdateHostInformationAsync();

                    Log.Information("Host information updated and certificate renewed.");
                }
                else
                {
                    Log.Warning("Host is not registered and configuration has changed. Registering and renewing certificate...");
                    await _hostLifecycleService.RegisterAsync();
                }

                await _hostLifecycleService.IssueCertificateAsync(hostConfiguration);
                await RestartUserInstance();
            }
            else if (!isHostRegistered)
            {
                Log.Warning("Host is not registered and configuration has not changed. Registering and issuing a new certificate...");

                await _hostLifecycleService.RegisterAsync();
                await _hostLifecycleService.IssueCertificateAsync(hostConfiguration);
                await RestartUserInstance();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during host registration check.");
        }
    }

    private async Task RestartUserInstance()
    {
        Log.Information("Stopping user instance...");
        _userInstanceService.Stop();

        while (_userInstanceService.IsRunning)
        {
            Log.Information("Waiting for user instance to stop...");
            await Task.Delay(50);
        }

        Log.Information("User instance stopped. Starting a new instance...");
        _userInstanceService.Start();
        Log.Information("New user instance started.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();

        return Task.CompletedTask;
    }
}