// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class IPAddressMonitorService : IHostedService
{
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IHostInfoService _hostInfoService;
    private readonly IHostServiceManager _hostServiceManager;

    private readonly string _ipAddressFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "IPAddress.txt");

    public IPAddressMonitorService(IHostConfigurationService hostConfigurationService, IHostInfoService hostInfoService, IHostServiceManager hostServiceManager)
    {
        _hostConfigurationService = hostConfigurationService;
        _hostInfoService = hostInfoService;
        _hostServiceManager = hostServiceManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        HostConfiguration configuration;

        try
        {
            var configPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, _hostConfigurationService.ConfigurationFileName);
            configuration = await _hostConfigurationService.LoadConfigurationAsync(configPath);
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException)
        {
            Log.Error(ex, "Error loading configuration.");

            return;
        }

        var savedIPAddress = await ReadSavedIPAddress();
        var currentIPAddress = _hostInfoService.GetIPv4Address();

        if (string.IsNullOrEmpty(savedIPAddress) || savedIPAddress != currentIPAddress)
        {
            var hostname = _hostInfoService.GetHostName();

            await _hostServiceManager.UpdateHostInformation(configuration, hostname, currentIPAddress);

            try
            {
                await File.WriteAllTextAsync(_ipAddressFilePath, currentIPAddress);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving the current IP address.");
            }
        }
        else
        {
            Log.Information("Current IP matches the saved IP. No action required.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<string> ReadSavedIPAddress()
    {
        try
        {
            if (File.Exists(_ipAddressFilePath))
            {
                var ipAddress = await File.ReadAllTextAsync(_ipAddressFilePath);

                return ipAddress.Trim();
            }
        }
        catch (Exception ex)
        {
            Log.Information("Error reading the saved IP address: {Message}", ex.Message);
        }

        return string.Empty;
    }
}
