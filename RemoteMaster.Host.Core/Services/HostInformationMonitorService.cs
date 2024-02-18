// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using System.Timers;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class HostInformationMonitorService(IServerHubService serverHubService, IHostConfigurationService hostConfigurationService, IHostInformationService hostInformationService, IHostLifecycleService hostLifecycleService) : IHostedService
{
    private System.Timers.Timer? _timer;

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
            hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);
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

        try
        {
            var hostInformation = hostInformationService.GetHostInformation();

            if (!hostConfiguration.Host.Equals(hostInformation))
            {
                try
                {
                    hostConfiguration.Host = hostInformation;

                    await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);
                    
                    await hostLifecycleService.UpdateHostInformationAsync(hostConfiguration);
                    await hostLifecycleService.UnregisterAsync(hostConfiguration);
                    await hostLifecycleService.RegisterAsync(hostConfiguration);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error saving updated configuration.");
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning("{Message}. Unable to update host information at this time.", ex.Message);
            
            return;
        }

        try
        {
            await serverHubService.ConnectAsync(hostConfiguration.Server);

            var newOrganizationalUnits = await serverHubService.GetNewOrganizationalUnitIfChangeRequested(hostConfiguration.Host.MacAddress);

            if (newOrganizationalUnits.Length == 0)
            {
                return;
            }

            hostConfiguration.Subject.OrganizationalUnit = newOrganizationalUnits;
            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);
            Log.Information("Organizational unit for this device was updated based on the organizational unit change request.");
            await serverHubService.AcknowledgeOrganizationalUnitChange(hostConfiguration.Host.MacAddress);

            await hostLifecycleService.UnregisterAsync(hostConfiguration);
            await hostLifecycleService.RegisterAsync(hostConfiguration);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing organizational unit change requests.");
        }

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var certificatePath = Path.Combine(programData, "RemoteMaster", "certificate.pfx");

        if (!CertificateHasExpired(certificatePath))
        {
            return;
        }

        try
        {
            await hostLifecycleService.UnregisterAsync(hostConfiguration);
            await hostLifecycleService.RegisterAsync(hostConfiguration);

            Log.Information("Certificate renewed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error renewing certificate.");
        }
    }

    private static bool CertificateHasExpired(string certificatePath)
    {
        using var certificate = new X509Certificate2(certificatePath);

        return DateTime.Now > certificate.NotAfter;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        _timer?.Dispose();

        return Task.CompletedTask;
    }
}
