// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Agent.Services;

public class UpdaterServiceManager : IUpdaterServiceManager
{
    public event Action<string, MessageType> MessageReceived;

    private readonly IServiceManager _serviceManager;
    private readonly UpdaterServiceConfigProvider _updaterServiceConfig;

    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Updater";

    public UpdaterServiceManager(IServiceManager serviceManager, UpdaterServiceConfigProvider updaterServiceConfig)
    {
        _serviceManager = serviceManager;
        _updaterServiceConfig = updaterServiceConfig;
    }

    public async Task<bool> InstallOrUpdate()
    {
        try
        {
            var newExecutablePath = GetNewExecutablePath();
            var newExecutableDirectoryPath = Path.GetDirectoryName(newExecutablePath);

            if (newExecutableDirectoryPath != null && !Directory.Exists(newExecutableDirectoryPath))
            {
                Directory.CreateDirectory(newExecutableDirectoryPath);
            }

            NetworkDriveHelper.MapNetworkDrive(@"\\SERVER-DC02\Win\RemoteMaster", "support@it-ktk.local", "bonesgamer123!!");
            var sourcePath = Path.Combine(@"\\SERVER-DC02\Win\RemoteMaster", SubAppName);
            NetworkDriveHelper.DirectoryCopy(sourcePath, newExecutableDirectoryPath, true, true);

            if (!_serviceManager.IsServiceInstalled(_updaterServiceConfig.ServiceName))
            {
                _serviceManager.InstallService(_updaterServiceConfig.ServiceName, _updaterServiceConfig.ServiceDisplayName, newExecutablePath, _updaterServiceConfig.ServiceStartType, _updaterServiceConfig.ServiceDependencies);
            }

            _serviceManager.StartService(_updaterServiceConfig.ServiceName);
            MessageReceived?.Invoke("Updater Service installed and started successfully.", MessageType.Information);

            return true;
        }
        catch (Exception ex)
        {
            MessageReceived?.Invoke($"Updater Service installation failed: {ex.Message}", MessageType.Error);
            return false;
        }
        finally
        {
            NetworkDriveHelper.CancelNetworkDrive(@"\\SERVER-DC02\Win\RemoteMaster");
        }
    }

    public async Task<bool> Uninstall()
    {
        try
        {
            if (_serviceManager.IsServiceInstalled(_updaterServiceConfig.ServiceName))
            {
                _serviceManager.StopService(_updaterServiceConfig.ServiceName);
                _serviceManager.UninstallService(_updaterServiceConfig.ServiceName);
                RemoveServiceFiles();
                MessageReceived?.Invoke("Updater Service uninstalled successfully.", MessageType.Information);

                return true;
            }
            else
            {
                MessageReceived?.Invoke("Updater Service is not installed.", MessageType.Information);

                return false;
            }
        }
        catch (Exception ex)
        {
            MessageReceived?.Invoke($"Updater Service uninstallation failed: {ex.Message}", MessageType.Error);

            return false;
        }
    }

    private static string GetNewExecutablePath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return Path.Combine(programFilesPath, MainAppName, SubAppName, $"{MainAppName}.{SubAppName}.exe");
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
