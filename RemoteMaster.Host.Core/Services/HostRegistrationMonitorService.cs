// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
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

        _timer = new Timer(CheckHostRegistration, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configurationChanged = await _hostInformationMonitorService.UpdateHostConfigurationAsync();

        var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync(false);

        if (configurationChanged)
        {
            await HandleHostRegistration(hostConfiguration);
        }

        if (hostConfiguration != null)
        {
            if (_hostInformationMonitorService.CheckCertificateExpiration())
            {
                await _hostLifecycleService.RenewCertificateAsync(hostConfiguration);
            }
        }

        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private async void CheckHostRegistration(object? state)
    {
        try
        {
            var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync(false);
            await HandleHostRegistration(hostConfiguration);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during host registration check.");
        }
    }

    private async Task HandleHostRegistration(HostConfiguration hostConfiguration)
    {
        var isHostRegistered = await _hostLifecycleService.IsHostRegisteredAsync(hostConfiguration);

        if (!isHostRegistered)
        {
            Log.Warning("Host is not registered. Performing necessary actions...");
            await _hostLifecycleService.RegisterAsync(hostConfiguration);
        }
        else
        {
            Log.Information("Updating host information due to configuration change.");
            await _hostLifecycleService.UpdateHostInformationAsync(hostConfiguration);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();

        return Task.CompletedTask;
    }
}
