// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.ServiceProcess;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Services;

public class AgentComponentUpdater : IComponentUpdater
{
    private readonly IServiceManager _serviceManager;
    private readonly ILogger<AgentComponentUpdater> _logger;

    public string ComponentName => "Agent";

    public AgentComponentUpdater(IServiceManager serviceManager, ILogger<AgentComponentUpdater> logger)
    {
        _serviceManager = serviceManager;
        _logger = logger;
    }

    public async Task<UpdateResponse> IsUpdateAvailableAsync(string sharedFolder, string login, string password)
    {
        var localExeFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Agent", "RemoteMaster.Agent.exe");
        var sharedExeFilePath = Path.Combine(sharedFolder, ComponentName, "RemoteMaster.Agent.exe");

        if (!File.Exists(localExeFilePath))
        {
            throw new FileNotFoundException($"Local file {localExeFilePath} does not exist");
        }

        var localVersionInfo = FileVersionInfo.GetVersionInfo(localExeFilePath);
        var localVersion = new Version(localVersionInfo.FileMajorPart, localVersionInfo.FileMinorPart, localVersionInfo.FileBuildPart, localVersionInfo.FilePrivatePart);

        var response = new UpdateResponse
        {
            ComponentName = ComponentName,
            CurrentVersion = localVersion,
            AvailableVersion = localVersion,
            IsUpdateAvailable = false
        };

        try
        {
            NetworkDriveHelper.MapNetworkDrive(sharedFolder, login, password);

            if (!File.Exists(sharedExeFilePath))
            {
                return response;
            }

            var remoteVersionInfo = FileVersionInfo.GetVersionInfo(sharedExeFilePath);
            var remoteVersion = new Version(remoteVersionInfo.FileMajorPart, remoteVersionInfo.FileMinorPart, remoteVersionInfo.FileBuildPart, remoteVersionInfo.FilePrivatePart);

            response.AvailableVersion = remoteVersion;
            response.IsUpdateAvailable = localVersion < remoteVersion;
        }
        finally
        {
            NetworkDriveHelper.CancelNetworkDrive(sharedFolder);
        }

        return response;
    }

    public async Task UpdateAsync(string sharedFolder, string login, string password)
    {
        var destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", ComponentName);
        var backupFolder = Path.Combine(destinationFolder, "Backup");

        try
        {
            _serviceManager.StopService();
            await WaitForServiceToStop();

            var sourceFolder = Path.Combine(sharedFolder, ComponentName);

            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            foreach (var file in Directory.GetFiles(destinationFolder))
            {
                var backupPath = Path.Combine(backupFolder, Path.GetFileName(file));
                File.Copy(file, backupPath, true);
            }

            NetworkDriveHelper.MapNetworkDrive(sharedFolder, login, password);

            var maxRetries = 5;
            var retryDelay = 2000;

            foreach (var filePath in Directory.GetFiles(destinationFolder))
            {
                var retryCount = 0;

                while (IsFileLocked(filePath) && retryCount < maxRetries)
                {
                    await Task.Delay(retryDelay);
                    retryCount++;
                }

                if (retryCount == maxRetries)
                {
                    RestoreFromBackup(backupFolder, destinationFolder);
                    NetworkDriveHelper.CancelNetworkDrive(sharedFolder);

                    throw new InvalidOperationException($"Unable to access file {filePath} after {maxRetries} retries.");
                }
            }

            NetworkDriveHelper.DirectoryCopy(sourceFolder, destinationFolder, true, true);
            NetworkDriveHelper.CancelNetworkDrive(sharedFolder);

            if (Directory.Exists(backupFolder))
            {
                Directory.Delete(backupFolder, true);
            }

            _serviceManager.StartService();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating component {ComponentName}", ComponentName);
            RestoreFromBackup(backupFolder, destinationFolder);
            _serviceManager.StartService();
        }
    }

    private static async Task WaitForServiceToStop()
    {
        using var serviceController = new ServiceController("RCService");
        var timeout = TimeSpan.FromSeconds(30);
        var sw = Stopwatch.StartNew();

        while (serviceController.Status != ServiceControllerStatus.Stopped)
        {
            if (sw.Elapsed > timeout)
            {
                throw new System.TimeoutException("Timeout waiting for service to stop.");
            }

            await Task.Delay(1000);
            serviceController.Refresh();
        }
    }

    private static void RestoreFromBackup(string backupFolder, string destinationFolder)
    {
        foreach (var file in Directory.GetFiles(backupFolder))
        {
            var restorePath = Path.Combine(destinationFolder, Path.GetFileName(file));
            File.Copy(file, restorePath, true);
        }
    }

    private static bool IsFileLocked(string filePath)
    {
        FileStream stream = null;

        try
        {
            stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return true;
        }
        finally
        {
            stream?.Close();
        }

        return false;
    }
}
