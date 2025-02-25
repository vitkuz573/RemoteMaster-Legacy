// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class HostRegistrationMonitorService : IHostedService
{
    private readonly ICertificateService _certificateService;
    private readonly ISyncIndicatorService _syncIndicatorService;
    private readonly IHostLifecycleService _hostLifecycleService;
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IHostInformationUpdaterService _hostInformationMonitorService;
    private readonly IUserInstanceService _userInstanceService;
    private readonly ILogger<HostRegistrationMonitorService> _logger;

    private readonly Timer _timer;

    public HostRegistrationMonitorService(ICertificateService certificateService, ISyncIndicatorService syncIndicatorService, IHostLifecycleService hostLifecycleService, IHostConfigurationService hostConfigurationService, IHostInformationUpdaterService hostInformationUpdaterService, IUserInstanceService userInstanceService, ILogger<HostRegistrationMonitorService> logger)
    {
        _certificateService = certificateService;
        _syncIndicatorService = syncIndicatorService;
        _hostLifecycleService = hostLifecycleService;
        _hostConfigurationService = hostConfigurationService;
        _hostInformationMonitorService = hostInformationUpdaterService;
        _userInstanceService = userInstanceService;
        _logger = logger;

        _timer = new Timer(CheckHostRegistrationAsync, null, Timeout.Infinite, 0);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));

        return Task.CompletedTask;
    }

    private async void CheckHostRegistrationAsync(object? state)
    {
        try
        {
            var configurationChanged = await _hostInformationMonitorService.UpdateHostConfigurationAsync();
            var hostConfiguration = await _hostConfigurationService.LoadAsync();
            var isHostRegistered = await _hostLifecycleService.IsHostRegisteredAsync();

            var isSyncRequired = _syncIndicatorService.IsSyncRequired();

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

                        await _hostLifecycleService.RegisterAsync(true);
                    }

                    var organizationAddress = await _hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration.Subject.Organization);

                    await _certificateService.IssueCertificateAsync(hostConfiguration, organizationAddress);

                    _syncIndicatorService.ClearSyncIndicator();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update host information. Sync will be retried.");

                    await _syncIndicatorService.SetSyncRequiredAsync();
                }

                await _userInstanceService.RestartAsync();
            }
            else if (!isHostRegistered)
            {
                _logger.LogWarning("Host is not registered and configuration has not changed. Registering and issuing a new certificate...");

                await _hostLifecycleService.RegisterAsync(true);

                var organizationAddress = await _hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration.Subject.Organization);

                await _certificateService.IssueCertificateAsync(hostConfiguration, organizationAddress);
                
                await _userInstanceService.RestartAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during host registration check.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _timer.DisposeAsync();
    }
}
