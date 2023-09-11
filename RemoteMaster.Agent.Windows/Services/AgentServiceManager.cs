// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Agent.Services;

public class AgentServiceManager : IAgentServiceManager
{
    public event Action<string, MessageType> MessageReceived;

    private readonly IClientService _clientService;
    private readonly IServiceManager _serviceManager;
    private readonly IConfigurationService _configurationService;

    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Agent";

    public AgentServiceManager(IClientService clientService, IServiceManager serviceManager, IConfigurationService configurationService)
    {
        _clientService = clientService;
        _serviceManager = serviceManager;
        _configurationService = configurationService;
    }

    public async Task<bool> InstallOrUpdateService(ConfigurationModel configuration, string hostName, string ipv4Address)
    {
        var newExecutablePath = GetNewExecutablePath();

        if (!File.Exists(newExecutablePath))
        {
            CopyExecutableToNewPath(newExecutablePath);
        }

        if (!_serviceManager.IsServiceInstalled())
        {
            _serviceManager.InstallService(newExecutablePath);
        }

        _serviceManager.StartService();

        var registerResult = await _clientService.RegisterAsync(configuration, hostName, ipv4Address);

        if (!registerResult)
        {
            MessageReceived?.Invoke("Client registration failed.", MessageType.Error);
            
            return false;
        }

        MessageReceived?.Invoke("Service installed and started successfully.", MessageType.Information);
        
        return true;
    }

    public async Task<bool> UninstallService(ConfigurationModel configuration, string hostName)
    {
        if (_serviceManager.IsServiceInstalled())
        {
            var unregisterResult = await _clientService.UnregisterAsync(configuration, hostName);

            if (!unregisterResult)
            {
                MessageReceived?.Invoke("Client unregistration failed.", MessageType.Error);
                
                return false;
            }

            _serviceManager.StopService();
            _serviceManager.UninstallService();
            RemoveServiceFiles();
            MessageReceived?.Invoke("Service uninstalled successfully.", MessageType.Information);
            
            return true;
        }

        MessageReceived?.Invoke("Service is not installed.", MessageType.Information);
        
        return false;
    }

    private static string GetNewExecutablePath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        
        return Path.Combine(programFilesPath, MainAppName, SubAppName, $"{MainAppName}.{SubAppName}.exe");
    }

    private void CopyExecutableToNewPath(string newExecutablePath)
    {
        var newDirectoryPath = Path.GetDirectoryName(newExecutablePath);

        if (newDirectoryPath != null && !Directory.Exists(newDirectoryPath))
        {
            Directory.CreateDirectory(newDirectoryPath);
        }

        var currentExecutablePath = Environment.ProcessPath;
        File.Copy(currentExecutablePath, newExecutablePath, true);

        var configName = _configurationService.GetConfigurationFileName();
        var newConfigPath = Path.Combine(newDirectoryPath, configName);

        if (File.Exists(configName))
        {
            File.Copy(configName, newConfigPath, true);
        }
    }

    private static void RemoveServiceFiles()
    {
        var newExecutablePath = GetNewExecutablePath();
        
        if (File.Exists(newExecutablePath))
        {
            File.Delete(newExecutablePath);
        }

        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var fullPath = Path.Combine(programFilesPath, MainAppName);

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }
}