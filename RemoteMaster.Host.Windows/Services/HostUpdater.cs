// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;
using Windows.Win32.Foundation;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Windows.Services;

public class HostUpdater(INetworkDriveService networkDriveService, IUserInstanceService userInstanceService, IServiceFactory serviceFactory, IHubContext<UpdaterHub, IUpdaterClient> hubContext) : IHostUpdater
{
    private static readonly string BaseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");

    private readonly string _updateFolderPath = Path.Combine(BaseFolderPath, "Update");

    private bool _emergencyRecoveryApplied;

    public async Task UpdateAsync(string folderPath, string? username, string? password, bool force = false, bool allowDowngrade = false)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        try
        {
            await hubContext.Clients.All.ReceiveMessage(new Message(Environment.ProcessId.ToString(), MessageType.Service)
            {
                Meta = "pid"
            });

            var sourceFolderPath = Path.Combine(folderPath, "Host");
            var isNetworkPath = folderPath.StartsWith(@"\\");

            if (isNetworkPath)
            {
                await Notify($"Attempting to map network drive with remote path: {folderPath}", MessageType.Information);

                var isMapped = networkDriveService.MapNetworkDrive(folderPath, username, password);

                if (!isMapped)
                {
                    await Notify($"Failed to map network drive with remote path {folderPath}. Details can be found in the log files.", MessageType.Error);
                    await Notify("Unable to map network drive with the provided credentials. Update aborted.", MessageType.Error);
    
                    return;
                }
                else
                {
                    await Notify($"Successfully mapped network drive with remote path: {folderPath}", MessageType.Information);
                }
            }

            if (!Directory.Exists(_updateFolderPath))
            {
                Directory.CreateDirectory(_updateFolderPath);
            }

            var isDownloaded = await CopyDirectoryAsync(sourceFolderPath, _updateFolderPath, true);

            await Notify($"Attempting to unmap network drive with remote path: {folderPath}", MessageType.Information);

            var isCancelled = networkDriveService.CancelNetworkDrive(folderPath);

            if (!isCancelled)
            {
                await Notify($"Failed to unmap network drive with remote path {folderPath}. Details can be found in the log files.", MessageType.Error);
            }
            else
            {
                await Notify($"Successfully unmapped network drive with remote path: {folderPath}", MessageType.Information);
            }

            if (!isDownloaded)
            {
                await Notify("Download or copy failed. Update aborted.", MessageType.Error);
  
                return;
            }

            if (!await CheckForUpdateVersion(allowDowngrade))
            {
                await Notify("Update aborted due to version check.", MessageType.Error);
                
                return;
            }

            if (!await NeedUpdate() && !force)
            {
                await Notify("No update required. Files are identical.", MessageType.Information);
                await Notify("If you wish to force an update regardless, you can use --force=true to override this check.", MessageType.Information);
  
                return;
            }

            var hostService = serviceFactory.GetService("RCHost");

            hostService.Stop();
            userInstanceService.Stop();

            await WaitForFileRelease(BaseFolderPath);
            await CopyDirectoryAsync(_updateFolderPath, BaseFolderPath, true);

            hostService.Start();
            await EnsureServicesRunning([hostService, userInstanceService], 5, 5);

            if (!_emergencyRecoveryApplied)
            {
                await Notify("Update completed successfully.", MessageType.Information);
            }
            else
            {
                await Notify("Emergency recovery was applied. Please check the system's integrity.", MessageType.Warning);
            }

            await Notify("Starting cleanup...", MessageType.Information);
            await DeleteDirectoriesAsync(Path.Combine(BaseFolderPath, "Update"));
            await Notify("Cleanup completed.", MessageType.Information);
        }
        catch (Exception ex)
        {
            await Notify($"Error while updating host: {ex.Message}", MessageType.Error);
            await AttemptEmergencyRecovery();
        }
    }

    private async Task<bool> CopyDirectoryAsync(string sourceDir, string destDir, bool overwrite = false)
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
                if (file.Name.Equals("RemoteMaster.Host.json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var tempPath = Path.Combine(destDir, file.Name);
                var copiedSuccessfully = await TryCopyFileAsync(file.FullName, tempPath, overwrite);

                if (copiedSuccessfully)
                {
                    continue;
                }

                await Notify($"File {file.Name} copied with errors. Checksum does not match.", MessageType.Error);

                return false;
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
            await Notify($"Failed to copy directory {sourceDir} to {destDir}: {ex.Message}", MessageType.Error);
    
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

    private async Task<bool> VerifyChecksum(string sourceFilePath, string destFilePath, bool expectDifference = false)
    {
        var sourceChecksum = GenerateChecksum(sourceFilePath);
        var destChecksum = GenerateChecksum(destFilePath);

        var checksumMatch = sourceChecksum == destChecksum;

        await Notify($"Verifying checksum: {sourceFilePath} [Source Checksum: {sourceChecksum}] -> {destFilePath} [Destination Checksum: {destChecksum}].", MessageType.Information);

        switch (expectDifference)
        {
            case true when !checksumMatch:
                await Notify("Checksums do not match as expected for an update. An update is needed.", MessageType.Information);
                return false;
            case false when !checksumMatch:
                await Notify("Unexpected checksum mismatch. The files may have been tampered with or corrupted.", MessageType.Error);
                return false;
            default:
                await Notify("Checksum verification successful. No differences found.", MessageType.Information);
                return true;
        }
    }

    private async Task<bool> TryCopyFileAsync(string sourceFile, string destFile, bool overwrite)
    {
        var attempts = 0;

        while (true)
        {
            try
            {
                File.Copy(sourceFile, destFile, overwrite);

                if (await VerifyChecksum(sourceFile, destFile))
                {
                    break;
                }
                else
                {
                    await Notify($"Checksum verification failed for file {sourceFile}.", MessageType.Error);
    
                    return false;
                }
            }
            catch (IOException ex) when (ex.HResult == (int)WIN32_ERROR.ERROR_SHARING_VIOLATION)
            {
                if (++attempts == 5)
                {
                    throw;
                }

                await Notify($"File {sourceFile} is currently in use. Retrying in 1 second...", MessageType.Warning);
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
                    await using var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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
            await Notify($"Attempt {attempt}: Checking if services are running...", MessageType.Information);

            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));

            var nonRunningServices = services.Where(service => !service.IsRunning).ToList();

            allServicesRunning = nonRunningServices.Count == 0;

            if (allServicesRunning)
            {
                await Notify("All services have been successfully started.", MessageType.Information);
                break;
            }

            var nonRunningServicesList = string.Join(", ", nonRunningServices.Select(service => service.ToString()));

            await Notify($"Not all services are running. The following services are not active: {nonRunningServicesList}. Waiting and retrying...", MessageType.Warning);
        }

        if (!allServicesRunning)
        {
            await Notify($"Failed to start all services after {attempts} attempts. Initiating emergency recovery...", MessageType.Information);
   
            await AttemptEmergencyRecovery();
        }
    }

    private async Task AttemptEmergencyRecovery()
    {
        _emergencyRecoveryApplied = true;

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

            await WaitForFileRelease(BaseFolderPath);

            var sourceExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");
            var destinationExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");

            File.Copy(sourceExePath, destinationExePath, true);

            await Notify("Emergency recovery completed successfully. Attempting to restart services...", MessageType.Information);

            hostService.Start();
        }
        catch (Exception ex)
        {
            await Notify($"Emergency recovery failed: {ex.Message}", MessageType.Error);
        }
    }

    private async Task<bool> NeedUpdate()
    {
        var updateFiles = Directory.GetFiles(_updateFolderPath, "*", SearchOption.AllDirectories).Select(Path.GetFullPath);

        foreach (var file in updateFiles)
        {
            var targetFile = file.Replace(_updateFolderPath, BaseFolderPath);

            if (!File.Exists(targetFile))
            {
                return true;
            }

            if (!await VerifyChecksum(file, targetFile, true))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> CheckForUpdateVersion(bool allowDowngrade)
    {
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        var updateVersion = GetVersionFromExecutable(Path.Combine(_updateFolderPath, "RemoteMaster.Host.exe"));

        await Notify($"Current version: {currentVersion}", MessageType.Information);
        await Notify($"Update version: {updateVersion}", MessageType.Information);

        if (updateVersion > currentVersion || allowDowngrade)
        {
            return true;
        }

        await Notify($"Current version {currentVersion} is up to date or newer than update version {updateVersion}. To allow downgrades, use the --allow-downgrade=true option. If you wish to force an update regardless, you can use --force=true.", MessageType.Information);
  
        return false;

    }

    private static Version GetVersionFromExecutable(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Update executable file not found.", filePath);
        }

        var versionInfo = FileVersionInfo.GetVersionInfo(filePath);

        return new Version(versionInfo.FileVersion);
    }

    private async Task Notify(string message, MessageType messageType)
    {
        switch (messageType)
        {
            case MessageType.Information:
                Log.Information(message);
                break;
            case MessageType.Warning:
                Log.Warning(message);
                break;
            case MessageType.Error:
                Log.Error(message);
                break;
        }

        var streamReader = new StringReader(message);

        while (await streamReader.ReadLineAsync() is { } line)
        {
            await hubContext.Clients.All.ReceiveMessage(new Message(line, messageType));
        }
    }

    private async Task DeleteDirectoriesAsync(params string[] directories)
    {
        foreach (var directory in directories)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                    await Notify($"Successfully deleted directory: {directory}", MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                await Notify($"Failed to delete directory {directory}: {ex.Message}", MessageType.Error);
            }
        }
    }
}
