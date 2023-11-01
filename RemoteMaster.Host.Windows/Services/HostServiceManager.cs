// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ServiceProcess;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostServiceManager : IHostServiceManager
{
    private readonly IHostLifecycleService _hostLifecycleService;
    private readonly IHostInstanceService _hostInstanceService;
    private readonly IServiceManager _serviceManager;
    private readonly IHostConfigurationService _configurationService;

    private readonly IServiceConfiguration _hostServiceConfig;

    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Host";

    public HostServiceManager(IHostLifecycleService hostLifecycleService, IHostInstanceService hostInstanceService, IServiceManager serviceManager, IHostConfigurationService configurationService, IServiceConfiguration hostServiceConfig)
    {
        _hostLifecycleService = hostLifecycleService;
        _hostInstanceService = hostInstanceService;
        _serviceManager = serviceManager;
        _configurationService = configurationService;
        _hostServiceConfig = hostServiceConfig;
    }

    public async Task InstallOrUpdate(HostConfiguration configuration, string hostName, string ipv4Address, string macAddress)
    {
        try
        {
            var directoryPath = GetDirectoryPath();

            if (_serviceManager.IsServiceInstalled(_hostServiceConfig.Name))
            {
                using var serviceController = new ServiceController(_hostServiceConfig.Name);

                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    _serviceManager.StopService(_hostServiceConfig.Name);
                }

                CopyToTargetPath(directoryPath, ipv4Address);
            }
            else
            {
                CopyToTargetPath(directoryPath, ipv4Address);
                var hostPath = Path.Combine(directoryPath, $"{MainAppName}.{SubAppName}.exe");
                _serviceManager.InstallService(_hostServiceConfig, $"{hostPath} --service-mode");
            }

            _serviceManager.StartService(_hostServiceConfig.Name);

            Log.Information("{ServiceName} installed and started successfully.", _hostServiceConfig.Name);

            await _hostLifecycleService.RegisterAsync(configuration, hostName, ipv4Address, macAddress);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    public async Task Uninstall(HostConfiguration configuration, string hostName)
    {
        try
        {
            if (_serviceManager.IsServiceInstalled(_hostServiceConfig.Name))
            {
                _serviceManager.StopService(_hostServiceConfig.Name);
                _serviceManager.UninstallService(_hostServiceConfig.Name);

                Log.Information("{ServiceName} Service uninstalled successfully.", _hostServiceConfig.Name);
            }
            else
            {
                Log.Information("{ServiceName} Service is not installed.", _hostServiceConfig.Name);
            }

            if (_hostInstanceService.IsRunning())
            {
                _hostInstanceService.Stop();
            }

            DeleteFiles();

            await _hostLifecycleService.UnregisterAsync(configuration, hostName);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    public async Task UpdateHostInformation(HostConfiguration configuration, string hostname, string ipAddress)
    {
        await _hostLifecycleService.UpdateHostInformationAsync(configuration, hostname, ipAddress);
    }

    private static string GetDirectoryPath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return Path.Combine(programFilesPath, MainAppName, SubAppName);
    }

    private void CopyToTargetPath(string targetDirectoryPath, string ipv4Address)
    {
        if (!Directory.Exists(targetDirectoryPath))
        {
            Directory.CreateDirectory(targetDirectoryPath);
        }

        var targetExecutablePath = Path.Combine(targetDirectoryPath, $"{MainAppName}.{SubAppName}.exe");

        try
        {
            File.Copy(Environment.ProcessPath!, targetExecutablePath, true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy the executable to {targetExecutablePath}. Details: {ex.Message}", ex);
        }

        var configName = _configurationService.ConfigurationFileName;
        var sourceConfigPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, configName);
        var targetConfigPath = Path.Combine(targetDirectoryPath, configName);

        if (File.Exists(sourceConfigPath))
        {
            try
            {
                File.Copy(sourceConfigPath, targetConfigPath, true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to copy the configuration file to {targetConfigPath}.", ex);
            }
        }

        var ipAddressFilePath = Path.Combine(targetDirectoryPath, "IPAddress.txt");
        
        try
        {
            File.WriteAllText(ipAddressFilePath, ipv4Address);
            Log.Information("IP Address file created successfully.");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to create the IP Address file: {Message}", ex.Message);
        }
    }

    private static void DeleteFiles()
    {
        var directoryPath = GetDirectoryPath();

        if (directoryPath != null && Directory.Exists(directoryPath))
        {
            try
            {
                Directory.Delete(directoryPath, true);
                Log.Information("{AppName} files deleted successfully.", SubAppName);
            }
            catch (Exception ex)
            {
                Log.Error("Deleting {AppName} files failed: {Message}", SubAppName, ex.Message);
            }
        }
    }
}