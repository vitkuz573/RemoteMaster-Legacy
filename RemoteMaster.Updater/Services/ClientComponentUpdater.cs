// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Services;

public class ClientComponentUpdater : IComponentUpdater
{
    private readonly ILogger<ClientComponentUpdater> _logger;

    protected const string SharedFolder = @"\\SERVER-DC02\Win\RemoteMaster";
    protected const string Login = "support@it-ktk.local";
    protected const string Password = "bonesgamer123!!";

    public string ComponentName => "Client";

    public ClientComponentUpdater(ILogger<ClientComponentUpdater> logger)
    {
        _logger = logger;
    }

    public async Task<UpdateResponse> IsUpdateAvailableAsync()
    {
        var localExeFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Client", "RemoteMaster.Client.exe");
        var sharedExeFilePath = Path.Combine(SharedFolder, ComponentName, "RemoteMaster.Client.exe");

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
            NetworkDriveHelper.MapNetworkDrive(SharedFolder, Login, Password);

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
            NetworkDriveHelper.CancelNetworkDrive(SharedFolder);
        }

        return response;
    }

    public async Task UpdateAsync()
    {
        var destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", ComponentName);
        var backupFolder = Path.Combine(destinationFolder, "Backup");

        try
        {
            var processes = Process.GetProcessesByName("RemoteMaster.Client");

            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }

            var sourceFolder = Path.Combine(SharedFolder, ComponentName);
            
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            foreach (var file in Directory.GetFiles(destinationFolder))
            {
                var backupPath = Path.Combine(backupFolder, Path.GetFileName(file));
                File.Copy(file, backupPath, true);
            }

            NetworkDriveHelper.MapNetworkDrive(SharedFolder, Login, Password);

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
                    NetworkDriveHelper.CancelNetworkDrive(SharedFolder);

                    throw new InvalidOperationException($"Unable to access file {filePath} after {maxRetries} retries.");
                }
            }

            NetworkDriveHelper.DirectoryCopy(sourceFolder, destinationFolder, true, true);
            NetworkDriveHelper.CancelNetworkDrive(SharedFolder);

            if (Directory.Exists(backupFolder))
            {
                Directory.Delete(backupFolder, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating component {ComponentName}", ComponentName);

            RestoreFromBackup(backupFolder, destinationFolder);
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
