// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Text;
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

            if (!Directory.Exists(_updateFolderPath))
            {
                Directory.CreateDirectory(_updateFolderPath);
            }

            var isDownloaded = await CopyDirectoryAsync(sourceFolderPath, _updateFolderPath, true);

            if (isNetworkPath)
            {
                networkDriveService.CancelNetworkDrive(folderPath);
            }

            if (!isDownloaded)
            {
                Log.Information("Download or copy failed. Update aborted.");
                return;
            }

            var hostService = serviceFactory.GetService("RCHost");
            hostService.Stop();
            userInstanceService.Stop();

            await WaitForFileRelease(_updateFolderPath);

            await CopyDirectoryAsync(_updateFolderPath, BaseFolderPath, true);

            hostService.Start();
            userInstanceService.Start();

            await EnsureServicesRunning(new IRunnable[] { hostService, userInstanceService }, 5, 5);

            Log.Information("Update completed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error("Error while updating host: {Message}", ex.Message);
        }
    }

    private static async Task<bool> CopyDirectoryAsync(string sourceDir, string destDir, bool overwrite = false)
    {
        try
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");
            }

            var dirs = dir.GetDirectories();

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in dir.GetFiles())
            {
                var tempPath = Path.Combine(destDir, file.Name);
                await TryCopyFileAsync(file.FullName, tempPath, overwrite);
            }

            foreach (var subdir in dirs)
            {
                var tempPath = Path.Combine(destDir, subdir.Name);
                await CopyDirectoryAsync(subdir.FullName, tempPath, overwrite);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to copy directory {sourceDir} to {destDir}: {ex.Message}");
            return false;
        }
    }

    private static async Task TryCopyFileAsync(string sourceFile, string destFile, bool overwrite)
    {
        var attempts = 0;

        while (true)
        {
            try
            {
                File.Copy(sourceFile, destFile, overwrite);
                break;
            }
            catch (IOException ex) when (ex.HResult == -2147024864)
            {
                if (++attempts == 5)
                {
                    throw;
                }

                await Task.Delay(1000);
            }
        }
    }

    private static async Task WaitForFileRelease(string directory)
    {
        var locked = true;

        while (locked)
        {
            locked = false;

            foreach (var file in new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    using var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    stream.Close();
                }
                catch
                {
                    locked = true;
                    await Task.Delay(2000);
                    break;
                }
            }
        }
    }

    private async Task EnsureServicesRunning(IEnumerable<IRunnable> services, int delayInSeconds, int attempts)
    {
        var allServicesRunning = false;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            Log.Information($"Attempt {attempt}: Checking if services are running...");
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));

            allServicesRunning = services.All(service => service.IsRunning);

            if (!allServicesRunning)
            {
                Log.Warning("Not all services are running. Waiting and retrying...");
            }
            else
            {
                Log.Information("All services have been successfully started.");
                break;
            }
        }

        if (!allServicesRunning)
        {
            Log.Error("Failed to start all services after {Attempts} attempts. Initiating emergency recovery...", attempts);

            AttemptEmergencyRecovery();
        }
    }

    private void AttemptEmergencyRecovery()
    {
        try
        {
            var hostService = serviceFactory.GetService("RCHost");

            if (hostService.IsRunning)
            {
                hostService.Stop();
            }

            var sourceExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");
            var destinationExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");

            File.Copy(sourceExePath, destinationExePath, true);

            Log.Information("Emergency recovery completed successfully. Attempting to restart services...");

            hostService.Start();
        }
        catch (Exception ex)
        {
            Log.Error($"Emergency recovery failed: {ex.Message}");
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
