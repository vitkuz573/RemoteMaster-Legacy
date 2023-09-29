// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent.Services;

public class AgentServiceManager : IAgentServiceManager
{
    public event Action<string, MessageType> MessageReceived;

    private readonly IRegistratorService _registratorService;
    private readonly IServiceManager _serviceManager;
    private readonly IConfigurationService _configurationService;

    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Agent";

    public AgentServiceManager(IRegistratorService registratorService, IServiceManager serviceManager, IConfigurationService configurationService)
    {
        _registratorService = registratorService;
        _serviceManager = serviceManager;
        _configurationService = configurationService;
    }

    public async Task<bool> InstallOrUpdate(ConfigurationModel configuration, string hostName, string ipv4Address, string macAddress)
    {
        try
        {
            var agentPath = GetAgentExecutablePath();

            if (_serviceManager.IsServiceInstalled(AgentServiceConfig.ServiceName))
            {
                using var serviceController = new ServiceController(AgentServiceConfig.ServiceName);

                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    _serviceManager.StopService(AgentServiceConfig.ServiceName);
                }

                CopyExecutableToTargetPath(agentPath);
                _serviceManager.StartService(AgentServiceConfig.ServiceName);
            }
            else
            {
                CopyExecutableToTargetPath(agentPath);
                _serviceManager.InstallService(AgentServiceConfig.ServiceName, AgentServiceConfig.ServiceDisplayName, agentPath, AgentServiceConfig.ServiceStartType, AgentServiceConfig.ServiceDependencies);
                _serviceManager.StartService(AgentServiceConfig.ServiceName);
            }

            var registerResult = await _registratorService.RegisterAsync(configuration, hostName, ipv4Address, macAddress);

            if (!registerResult)
            {
                MessageReceived?.Invoke("Computer registration failed.", MessageType.Error);

                return false;
            }

            MessageReceived?.Invoke($"{AgentServiceConfig.ServiceName} installed and started successfully.", MessageType.Information);

            return true;
        }
        catch (Exception ex)
        {
            MessageReceived?.Invoke($"An error occurred: {ex.Message}", MessageType.Error);

            return false;
        }
    }

    public async Task<bool> Uninstall(ConfigurationModel configuration, string hostName)
    {
        try
        {
            var processes = Process.GetProcessesByName($"{MainAppName}.Client");
           
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }

            if (_serviceManager.IsServiceInstalled(UpdaterServiceConfig.ServiceName))
            {
                _serviceManager.StopService(UpdaterServiceConfig.ServiceName);
                _serviceManager.UninstallService(UpdaterServiceConfig.ServiceName);
                MessageReceived?.Invoke($"{UpdaterServiceConfig.ServiceName} service uninstalled successfully.", MessageType.Information);
            }

            if (_serviceManager.IsServiceInstalled(AgentServiceConfig.ServiceName))
            {
                _serviceManager.StopService(AgentServiceConfig.ServiceName);
                _serviceManager.UninstallService(AgentServiceConfig.ServiceName);
                RemoveApplication();
                MessageReceived?.Invoke($"{AgentServiceConfig.ServiceName} uninstalled successfully.", MessageType.Information);
            }

            var unregisterResult = await _registratorService.UnregisterAsync(configuration, hostName);
            
            if (!unregisterResult)
            {
                MessageReceived?.Invoke("Computer unregistration failed.", MessageType.Error);
               
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageReceived?.Invoke($"An error occurred: {ex.Message}", MessageType.Error);

            return false;
        }
    }

    private static string GetAgentExecutablePath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return Path.Combine(programFilesPath, MainAppName, SubAppName, $"{MainAppName}.{SubAppName}.exe");
    }

    private void CopyExecutableToTargetPath(string targetPath)
    {
        var newDirectoryPath = Path.GetDirectoryName(targetPath);

        if (string.IsNullOrEmpty(newDirectoryPath))
        {
            throw new InvalidOperationException("The directory path is invalid.");
        }

        if (!Directory.Exists(newDirectoryPath))
        {
            Directory.CreateDirectory(newDirectoryPath);
        }

        var currentExecutablePath = Assembly.GetExecutingAssembly().Location;
        
        try
        {
            File.Copy(currentExecutablePath, targetPath, true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy the executable to {targetPath}.", ex);
        }

        var configName = _configurationService.GetConfigurationFileName();
        var newConfigPath = Path.Combine(newDirectoryPath, configName);

        if (File.Exists(configName))
        {
            try
            {
                File.Copy(configName, newConfigPath, true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to copy the configuration file to {newConfigPath}.", ex);
            }
        }
    }

    private static void RemoveApplication()
    {
        var agentPath = GetAgentExecutablePath();

        if (File.Exists(agentPath))
        {
            File.Delete(agentPath);
        }

        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var fullPath = Path.Combine(programFilesPath, MainAppName);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }
}