// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class HostRegistrationMonitorService : IHostedService
{
    private readonly IHostLifecycleService _hostLifecycleService;
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IHostInformationUpdaterService _hostInformationMonitorService;
    private readonly IUserInstanceService _userInstanceService;
    private readonly ILogger<HostRegistrationMonitorService> _logger;

    private readonly Timer _timer;
    private readonly string _syncIndicatorFilePath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "sync_required.ind");


    public HostRegistrationMonitorService(IHostLifecycleService hostLifecycleService, IHostConfigurationService hostConfigurationService, IHostInformationUpdaterService hostInformationUpdaterService, IUserInstanceService userInstanceService, ILogger<HostRegistrationMonitorService> logger)
    {
        _hostLifecycleService = hostLifecycleService;
        _hostConfigurationService = hostConfigurationService;
        _hostInformationMonitorService = hostInformationUpdaterService;
        _userInstanceService = userInstanceService;
        _logger = logger;

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
            var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync();
            var isHostRegistered = await _hostLifecycleService.IsHostRegisteredAsync();

            var isSyncRequired = IsSyncRequired();

            if (configurationChanged || isSyncRequired)
            {
                try
                {
                    if (isHostRegistered)
                    {
                        _logger.LogInformation("Updating host information and renewing certificate due to configuration change.");
                        
                        await _hostLifecycleService.UpdateHostInformationAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Host is not registered. Registering and issuing a new certificate...");

                        await _hostLifecycleService.RegisterAsync();
                    }

                    var organizationAddress = await _hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration.Subject.Organization);

                    await _hostLifecycleService.IssueCertificateAsync(hostConfiguration, organizationAddress);

                    ClearSyncIndicator();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update host information. Sync will be retried.");

                    SetSyncRequired();
                }

                await RestartUserInstance();
            }
            else if (!isHostRegistered)
            {
                _logger.LogWarning("Host is not registered and configuration has not changed. Registering and issuing a new certificate...");

                await _hostLifecycleService.RegisterAsync();

                var organizationAddress = await _hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration.Subject.Organization);

                await _hostLifecycleService.IssueCertificateAsync(hostConfiguration, organizationAddress);
                await RestartUserInstance();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during host registration check.");
        }
    }

    private async Task RestartUserInstance()
    {
        _logger.LogInformation("Stopping user instance...");
        _userInstanceService.Stop();

        while (_userInstanceService.IsRunning)
        {
            _logger.LogInformation("Waiting for user instance to stop...");
            await Task.Delay(50);
        }

        _logger.LogInformation("User instance stopped. Starting a new instance...");
        _userInstanceService.Start();
        _logger.LogInformation("New user instance started.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();

        return Task.CompletedTask;
    }

    private bool IsSyncRequired()
    {
        return File.Exists(_syncIndicatorFilePath);
    }

    private void SetSyncRequired()
    {
        try
        {
            File.WriteAllText(_syncIndicatorFilePath, "Sync required");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sync indicator file.");
        }
    }

    private void ClearSyncIndicator()
    {
        try
        {
            if (File.Exists(_syncIndicatorFilePath))
            {
                File.Delete(_syncIndicatorFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sync indicator file.");
        }
    }
}
