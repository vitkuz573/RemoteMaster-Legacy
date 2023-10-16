// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.ServiceProcess;
using RemoteMaster.Host.Abstractions;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Services;

public class HostServiceManager : IHostServiceManager
{
    private readonly IRegistratorService _registratorService;
    private readonly IServiceManager _serviceManager;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<HostServiceManager> _logger;

    private readonly IServiceConfig _hostConfig;

    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Host";

    public HostServiceManager(IRegistratorService registratorService, IServiceManager serviceManager, IConfigurationService configurationService, IDictionary<string, IServiceConfig> configs, ILogger<HostServiceManager> logger)
    {
        if (configs == null)
        {
            throw new ArgumentNullException(nameof(configs));
        }

        _registratorService = registratorService;
        _serviceManager = serviceManager;
        _configurationService = configurationService;
        _hostConfig = configs["host"];
        _logger = logger;
    }

    public async Task InstallOrUpdate(ConfigurationModel configuration, string hostName, string ipv4Address, string macAddress)
    {
        try
        {
            var directoryPath = GetDirectoryPath();

            if (_serviceManager.IsServiceInstalled(_hostConfig.Name))
            {
                using var serviceController = new ServiceController(_hostConfig.Name);

                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    _serviceManager.StopService(_hostConfig.Name);
                }

                CopyToTargetPath(directoryPath);
            }
            else
            {
                CopyToTargetPath(directoryPath);
                var hostPath = Path.Combine(directoryPath, $"{MainAppName}.{SubAppName}.exe");
                _serviceManager.InstallService(_hostConfig, $"{hostPath} --service-mode");
            }

            _serviceManager.StartService(_hostConfig.Name);

            _logger.LogInformation("{ServiceName} installed and started successfully.", _hostConfig.Name);

            var registerResult = await _registratorService.RegisterAsync(configuration, hostName, ipv4Address, macAddress);

            if (!registerResult)
            {
                _logger.LogError("Computer registration failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred: {Message}", ex.Message);
        }
    }

    public async Task Uninstall(ConfigurationModel configuration, string hostName)
    {
        try
        {
            if (_serviceManager.IsServiceInstalled(_hostConfig.Name))
            {
                _serviceManager.StopService(_hostConfig.Name);
                _serviceManager.UninstallService(_hostConfig.Name);

                foreach (var process in Process.GetProcessesByName($"{MainAppName}.{SubAppName}"))
                {
                    process.Kill();
                }

                DeleteFiles();

                _logger.LogInformation("{ServiceName} Service uninstalled successfully.", _hostConfig.Name);
            }
            else
            {
                _logger.LogInformation("{ServiceName} Service is not installed.", _hostConfig.Name);
            }

            if (!await _registratorService.UnregisterAsync(configuration, hostName))
            {
                _logger.LogError("Computer unregistration failed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred: {Message}", ex.Message);
        }
    }

    private static string GetDirectoryPath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return Path.Combine(programFilesPath, MainAppName, SubAppName);
    }

    private void CopyToTargetPath(string targetDirectoryPath)
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

        var configName = _configurationService.GetConfigurationFileName();
        var sourceConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, configName);
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
    }

    private void DeleteFiles()
    {
        var mainDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), MainAppName);

        var directoryPath = Path.Combine(mainDirectoryPath, SubAppName);

        if (Directory.Exists(directoryPath))
        {
            try
            {
                Directory.Delete(directoryPath, true);
                _logger.LogInformation("{AppName} files deleted successfully.", SubAppName);
            }
            catch (Exception ex)
            {
                _logger.LogError("Deleting {AppName} files failed: {Message}", SubAppName, ex.Message);
            }
        }
    }
}