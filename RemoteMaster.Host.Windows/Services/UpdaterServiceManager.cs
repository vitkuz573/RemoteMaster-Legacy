// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ServiceProcess;
using RemoteMaster.Host.Abstractions;
using RemoteMaster.Host.Helpers;

namespace RemoteMaster.Host.Services;

public class UpdaterServiceManager : IUpdaterServiceManager
{
    private readonly IServiceManager _serviceManager;

    private readonly IServiceConfig _updaterConfig;
    private readonly ILogger<UpdaterServiceManager> _logger;

    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Updater";

    public UpdaterServiceManager(IServiceManager serviceManager, IDictionary<string, IServiceConfig> configs, ILogger<UpdaterServiceManager> logger)
    {
        if (configs == null)
        {
            throw new ArgumentNullException(nameof(configs));
        }

        _serviceManager = serviceManager;
        _updaterConfig = configs["updater"];
        _logger = logger;
    }

    public void InstallOrUpdate()
    {
        try
        {
            var directoryPath = GetDirectoryPath();

            if (_serviceManager.IsServiceInstalled(_updaterConfig.Name))
            {
                using var serviceController = new ServiceController(_updaterConfig.Name);

                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    _serviceManager.StopService(_updaterConfig.Name);
                }

                CopyToTargetPath(directoryPath);
            }
            else
            {
                CopyToTargetPath(directoryPath);
                var updaterPath = Path.Combine(directoryPath, $"{MainAppName}.{SubAppName}.exe");
                _serviceManager.InstallService(_updaterConfig, updaterPath);
            }

            _serviceManager.StartService(_updaterConfig.Name);

            _logger.LogInformation("{ServiceName} Service installed and started successfully.", _updaterConfig.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ServiceName} Service installation failed: {Message}", _updaterConfig.Name, ex.Message);
        }
        finally
        {
            NetworkDriveHelper.CancelNetworkDrive(@"\\SERVER-DC02\Win\RemoteMaster");
        }
    }

    public void Uninstall()
    {
        try
        {
            if (_serviceManager.IsServiceInstalled(_updaterConfig.Name))
            {
                _serviceManager.StopService(_updaterConfig.Name);
                _serviceManager.UninstallService(_updaterConfig.Name);

                DeleteUpdaterFiles();

                _logger.LogInformation("{ServiceName} Service uninstalled successfully.", _updaterConfig.Name);
            }
            else
            {
                _logger.LogInformation("{ServiceName} Service is not installed.", _updaterConfig.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ServiceName} Service uninstallation failed: {Message}", _updaterConfig.Name, ex.Message);
        }
    }

    private static string GetDirectoryPath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return Path.Combine(programFilesPath, MainAppName, SubAppName);
    }

    private static void CopyToTargetPath(string targetDirectoryPath)
    {
        if (!Directory.Exists(targetDirectoryPath))
        {
            Directory.CreateDirectory(targetDirectoryPath);
        }

        var targetExecutablePath = Path.Combine(targetDirectoryPath, $"{MainAppName}.{SubAppName}.exe");

        try
        {
            NetworkDriveHelper.MapNetworkDrive(@"\\SERVER-DC02\Win\RemoteMaster", "support@it-ktk.local", "bonesgamer123!!");
            var sourcePath = Path.Combine(@"\\SERVER-DC02\Win\RemoteMaster", SubAppName);
            NetworkDriveHelper.DirectoryCopy(sourcePath, targetDirectoryPath, true, true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy the executable to {targetExecutablePath}. Details: {ex.Message}", ex);
        }
    }

    private void DeleteUpdaterFiles()
    {
        var directoryPath = GetDirectoryPath();

        if (directoryPath != null && Directory.Exists(directoryPath))
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
