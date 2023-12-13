// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostInfoMonitorService(IHostConfigurationService hostConfigurationService, IHostInfoService hostInfoService, IHostServiceManager hostServiceManager) : IHostedService
{
    private readonly string _hostInfoFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "HostInfo.json");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        HostConfiguration configuration;

        try
        {
            var configPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, hostConfigurationService.ConfigurationFileName);
            configuration = await hostConfigurationService.LoadConfigurationAsync(configPath);
        }
        catch (Exception ex) when (ex is FileNotFoundException || ex is InvalidDataException)
        {
            Log.Error(ex, "Error loading configuration.");

            return;
        }

        var currentHostInfo = await ReadHostInfo();

        var newIPAddress = hostInfoService.GetIPv4Address();
        var newHostName = hostInfoService.GetHostName();

        if (currentHostInfo.IPAddress != newIPAddress || currentHostInfo.HostName != newHostName)
        {
            var macAddress = hostInfoService.GetMacAddress();

            await hostServiceManager.UpdateHostInformation(configuration, newHostName, newIPAddress, macAddress);

            try
            {
                var hostInfo = new HostInfo
                {
                    IPAddress = newIPAddress,
                    HostName = newHostName
                };

                var json = JsonSerializer.Serialize(hostInfo);
                await File.WriteAllTextAsync(_hostInfoFilePath, json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving host information.");
            }
        }
        else
        {
            Log.Information("Current IP and hostname match the saved values. No action required.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<HostInfo> ReadHostInfo()
    {
        try
        {
            if (File.Exists(_hostInfoFilePath))
            {
                var json = await File.ReadAllTextAsync(_hostInfoFilePath);

                return JsonSerializer.Deserialize<HostInfo>(json) ?? new HostInfo();
            }
        }
        catch (Exception ex)
        {
            Log.Information("Error reading the saved host information: {Message}", ex.Message);
        }

        return new HostInfo();
    }
}
