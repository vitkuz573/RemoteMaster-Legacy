// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostUpdater(INetworkDriveService networkDriveService, IUserInstanceService userInstanceService, IServiceFactory serviceFactory) : IHostUpdater
{
    private static readonly string BaseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");
    
    private readonly string _updateFolderPath = Path.Combine(BaseFolderPath, "Update");

    public async Task UpdateAsync(string folderPath, string? username, string? password)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        try
        {
            var sourceFolderPath = Path.Combine(folderPath, "Host");
            var isNetworkPath = folderPath.StartsWith(@"\\");

            if (isNetworkPath)
            {
                networkDriveService.MapNetworkDrive(folderPath, username, password);
            }

            var isDownloaded = DirectoryCopy(sourceFolderPath, _updateFolderPath, true, true);

            Log.Information("Copied from {SourceFolder} to {DestinationFolder}", sourceFolderPath, _updateFolderPath);

            if (isNetworkPath)
            {
                networkDriveService.CancelNetworkDrive(folderPath);
            }

            if (!isDownloaded)
            {
                return;
            }

            var hostService = serviceFactory.GetService("RCHost");

            hostService.Stop();

            Console.WriteLine($"{hostService.Name} sucessfully stopped. Starting update...");

            userInstanceService.Stop();

            foreach (var filePath in Directory.GetFiles(_updateFolderPath))
            {
                var fileName = Path.GetFileName(filePath);
                var destFilePath = Path.Combine(BaseFolderPath, fileName);
                File.Copy(filePath, destFilePath, true);
            }

            Log.Information("Update from {_updateFolderPath} to {BaseFolderPath} completed.", _updateFolderPath, BaseFolderPath);

            hostService.Start();
        }
        catch (Exception ex)
        {
            Log.Error("Error while updating host: {Message}", ex.Message);
        }
    }

    private static bool DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true, bool overwriteExisting = false)
    {
        var sourceDir = new DirectoryInfo(sourceDirName);

        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        foreach (var file in sourceDir.GetFiles())
        {
            var destPath = Path.Combine(destDirName, file.Name);

            if (File.Exists(destPath) && !overwriteExisting)
            {
                continue;
            }

            try
            {
                file.CopyTo(destPath, true);
            }
            catch (Exception)
            {
                return false;
            }
        }

        if (!copySubDirs)
        {
            return true;
        }

        foreach (var subdir in sourceDir.GetDirectories())
        {
            var destSubDir = Path.Combine(destDirName, subdir.Name);

            if (!DirectoryCopy(subdir.FullName, destSubDir, true, overwriteExisting))
            {
                return false;
            }
        }

        return true;
    }
}
