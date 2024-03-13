// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;
using Windows.Win32.Foundation;

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
                var isMapped = networkDriveService.MapNetworkDrive(folderPath, username, password);

                if (!isMapped)
                {
                    Log.Error("Unable to map network drive with the provided credentials. Update aborted.");
                    return;
                }
            }

            if (!Directory.Exists(_updateFolderPath))
            {
                Directory.CreateDirectory(_updateFolderPath);
            }

            var isDownloaded = await CopyDirectoryAsync(sourceFolderPath, _updateFolderPath, true);

            if (!isDownloaded)
            {
                Log.Information("Download or copy failed. Update aborted.");
                return;
            }

            if (!NeedUpdate())
            {
                Log.Information("No update required. Files are identical.");
                return;
            }

            var hostService = serviceFactory.GetService("RCHost");

            hostService.Stop();
            userInstanceService.Stop();

            await WaitForFileRelease(BaseFolderPath);
            await CopyDirectoryAsync(_updateFolderPath, BaseFolderPath, true);

            hostService.Start();
            await EnsureServicesRunning([hostService, userInstanceService], 5, 5);

            Log.Information("Update completed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error("Error while updating host: {Message}", ex.Message);
            AttemptEmergencyRecovery();
        }
    }

    private static async Task<bool> CopyDirectoryAsync(string sourceDir, string destDir, bool overwrite = false)
    {
        try
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}.");
            }

            var dirs = dir.GetDirectories();

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            foreach (var file in dir.GetFiles())
            {
                var tempPath = Path.Combine(destDir, file.Name);
                var copiedSuccessfully = await TryCopyFileAsync(file.FullName, tempPath, overwrite);

                if (!copiedSuccessfully)
                {
                    Log.Error($"File {file.Name} copied with errors. Checksum does not match.");
                    return false;
                }
            }

            foreach (var subdir in dirs)
            {
                var tempPath = Path.Combine(destDir, subdir.Name);

                if (!await CopyDirectoryAsync(subdir.FullName, tempPath, overwrite))
                {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to copy directory {sourceDir} to {destDir}: {ex.Message}");

            return false;
        }
    }

    private static string GenerateChecksum(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static bool VerifyChecksum(string sourceFilePath, string destFilePath, bool expectDifference = false)
    {
        var sourceChecksum = GenerateChecksum(sourceFilePath);
        var destChecksum = GenerateChecksum(destFilePath);

        var checksumMatch = sourceChecksum == destChecksum;

        Log.Information($"Verifying checksum: {sourceFilePath} [Source Checksum: {sourceChecksum}] -> {destFilePath} [Destination Checksum: {destChecksum}].");

        if (expectDifference && !checksumMatch)
        {
            Log.Information("Checksums do not match as expected for an update. An update is needed.");

            return false;
        }
        else if (!expectDifference && !checksumMatch)
        {
            Log.Error("Unexpected checksum mismatch. The files may have been tampered with or corrupted.");

            return false;
        }

        Log.Information("Checksum verification successful. No differences found.");

        return true;
    }

    private static async Task<bool> TryCopyFileAsync(string sourceFile, string destFile, bool overwrite)
    {
        var attempts = 0;

        while (true)
        {
            try
            {
                File.Copy(sourceFile, destFile, overwrite);

                if (VerifyChecksum(sourceFile, destFile))
                {
                    break;
                }
                else
                {
                    Log.Error($"Checksum verification failed for file {sourceFile}.");
                    return false;
                }
            }
            catch (IOException ex) when (ex.HResult == (int)WIN32_ERROR.ERROR_SHARING_VIOLATION)
            {
                if (++attempts == 5)
                {
                    throw;
                }

                await Task.Delay(1000);
            }
        }

        return true;
    }

    private static async Task WaitForFileRelease(string directory)
    {
        var locked = true;
        var excludedFolders = new HashSet<string> { "Updater", "Update" };

        while (locked)
        {
            locked = false;

            foreach (var file in new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories))
            {
                if (excludedFolders.Any(file.DirectoryName.Contains))
                {
                    continue;
                }

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

            if (userInstanceService.IsRunning)
            {
                userInstanceService.Stop();
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

    private bool NeedUpdate()
    {
        var updateFiles = Directory.GetFiles(_updateFolderPath, "*", SearchOption.AllDirectories).Select(Path.GetFullPath);

        foreach (var file in updateFiles)
        {
            var targetFile = file.Replace(_updateFolderPath, BaseFolderPath);

            if (!File.Exists(targetFile))
            {
                return true;
            }

            if (!VerifyChecksum(file, targetFile, true))
            {
                return true;
            }
        }

        return false;
    }
}
