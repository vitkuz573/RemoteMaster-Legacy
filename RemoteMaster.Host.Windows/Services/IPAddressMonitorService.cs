// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Services;

public class IPAddressMonitorService : IHostedService
{
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IHostInfoService _hostInfoService;
    private readonly IHostServiceManager _hostServiceManager;
    private readonly ILogger<IPAddressMonitorService> _logger;

    public IPAddressMonitorService(IHostConfigurationService hostConfigurationService, IHostInfoService hostInfoService, IHostServiceManager hostServiceManager, ILogger<IPAddressMonitorService> logger)
    {
        _hostConfigurationService = hostConfigurationService;
        _hostInfoService = hostInfoService;
        _hostServiceManager = hostServiceManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        HostConfiguration configuration;

        try
        {
            configuration = await _hostConfigurationService.LoadConfigurationAsync();
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Configuration file not found.");

            return;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex, "Invalid configuration data.");

            return;
        }

        var savedIPAddress = ReadSavedIPAddress();
        var currentIPAddress = _hostInfoService.GetIPv4Address();

        if (!string.IsNullOrEmpty(savedIPAddress) && savedIPAddress == currentIPAddress)
        {
            _logger.LogInformation("Current IP matches the saved IP. No action required.");

            return;
        }
        else
        {
            await _hostServiceManager.InstallOrUpdate(configuration, _hostInfoService.GetHostName(), currentIPAddress, _hostInfoService.GetMacAddress());

            try
            {
                File.WriteAllText(@"C:\Program Files\RemoteMaster\Host\IPAddress.txt", currentIPAddress);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error saving the current IP address: {ex.Message}");
            }
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private string? ReadSavedIPAddress()
    {
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "IPAddress.txt");

        try
        {
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath).Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Error reading the saved IP address: {Message}", ex.Message);
        }

        return null;
    }
}
