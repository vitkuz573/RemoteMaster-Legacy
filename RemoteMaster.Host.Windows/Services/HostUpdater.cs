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
using static RemoteMaster.Shared.Models.ScriptResult;

namespace RemoteMaster.Host.Windows.Services;

public class HostUpdater(INetworkDriveService networkDriveService, IUserInstanceService userInstanceService, IServiceFactory serviceFactory, IHubContext<UpdaterHub, IUpdaterClient> hubContext) : IHostUpdater
{
    private static readonly string BaseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");
    
    private readonly string _updateFolderPath = Path.Combine(BaseFolderPath, "Update");

    public async Task UpdateAsync(string folderPath, string? username, string? password, bool force = false, bool allowDowngrade = false)
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

                    await ReadStreamAsync(new StringReader("Unable to map network drive with the provided credentials. Update aborted."), MessageType.Error);
                    
                    return;
                }
            }

            if (!Directory.Exists(_updateFolderPath))
            {
                Directory.CreateDirectory(_updateFolderPath);
            }

            var isDownloaded = await CopyDirectoryAsync(sourceFolderPath, _updateFolderPath, true);

            networkDriveService.CancelNetworkDrive(folderPath);

            if (!isDownloaded)
            {
                Log.Error("Download or copy failed. Update aborted.");

                await ReadStreamAsync(new StringReader("Download or copy failed. Update aborted."), MessageType.Error);
                
                return;
            }

            if (!await CheckForUpdateVersion(allowDowngrade))
            {
                Log.Error("Update aborted due to version check.");

                await ReadStreamAsync(new StringReader("Update aborted due to version check."), MessageType.Error);
                
                return;
            }

            if (!await NeedUpdate() && !force)
            {
                Log.Information("No update required. Files are identical.");

                await ReadStreamAsync(new StringReader("No update required. Files are identical."), MessageType.Output);

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

            await ReadStreamAsync(new StringReader("Update completed successfully."), MessageType.Output);
        }
        catch (Exception ex)
        {
            Log.Error("Error while updating host: {Message}", ex.Message);

            await ReadStreamAsync(new StringReader($"Error while updating host: {ex.Message}"), MessageType.Error);
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
                var tempPath = Path.Combine(destDir, file.Name);
                var copiedSuccessfully = await TryCopyFileAsync(file.FullName, tempPath, overwrite);

                if (!copiedSuccessfully)
                {
                    Log.Error($"File {file.Name} copied with errors. Checksum does not match.");

                    await ReadStreamAsync(new StringReader($"File {file.Name} copied with errors. Checksum does not match."), MessageType.Error);

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

            await ReadStreamAsync(new StringReader($"Failed to copy directory {sourceDir} to {destDir}: {ex.Message}"), MessageType.Error);

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

        Log.Information($"Verifying checksum: {sourceFilePath} [Source Checksum: {sourceChecksum}] -> {destFilePath} [Destination Checksum: {destChecksum}].");

        await ReadStreamAsync(new StringReader($"Verifying checksum: {sourceFilePath} [Source Checksum: {sourceChecksum}] -> {destFilePath} [Destination Checksum: {destChecksum}]."), MessageType.Output);

        if (expectDifference && !checksumMatch)
        {
            Log.Information("Checksums do not match as expected for an update. An update is needed.");

            await ReadStreamAsync(new StringReader("Checksums do not match as expected for an update. An update is needed."), MessageType.Output);

            return false;
        }
        else if (!expectDifference && !checksumMatch)
        {
            Log.Error("Unexpected checksum mismatch. The files may have been tampered with or corrupted.");

            await ReadStreamAsync(new StringReader("Unexpected checksum mismatch. The files may have been tampered with or corrupted."), MessageType.Error);

            return false;
        }

        Log.Information("Checksum verification successful. No differences found.");

        await ReadStreamAsync(new StringReader("Checksum verification successful. No differences found."), MessageType.Output);

        return true;
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
                    Log.Error($"Checksum verification failed for file {sourceFile}.");

                    await ReadStreamAsync(new StringReader($"Checksum verification failed for file {sourceFile}."), MessageType.Error);
                    
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

            await ReadStreamAsync(new StringReader($"Attempt {attempt}: Checking if services are running..."), MessageType.Output);
            
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));

            allServicesRunning = services.All(service => service.IsRunning);

            if (!allServicesRunning)
            {
                Log.Warning("Not all services are running. Waiting and retrying...");

                await ReadStreamAsync(new StringReader("Not all services are running. Waiting and retrying..."), MessageType.Output);
            }
            else
            {
                Log.Information("All services have been successfully started.");

                await ReadStreamAsync(new StringReader("All services have been successfully started."), MessageType.Output);
                break;
            }
        }

        if (!allServicesRunning)
        {
            Log.Error($"Failed to start all services after {attempts} attempts. Initiating emergency recovery...");

            await ReadStreamAsync(new StringReader($"Failed to start all services after {attempts} attempts. Initiating emergency recovery..."), MessageType.Output);

            await AttemptEmergencyRecovery();
        }
    }

    private async Task AttemptEmergencyRecovery()
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

            await WaitForFileRelease(BaseFolderPath);

            var sourceExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");
            var destinationExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");

            File.Copy(sourceExePath, destinationExePath, true);

            Log.Information("Emergency recovery completed successfully. Attempting to restart services...");

            await ReadStreamAsync(new StringReader("Emergency recovery completed successfully. Attempting to restart services..."), MessageType.Output);

            hostService.Start();
        }
        catch (Exception ex)
        {
            Log.Error("Emergency recovery failed: {Message}", ex.Message);

            await ReadStreamAsync(new StringReader($"Emergency recovery failed: {ex.Message}"), MessageType.Error);
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

        if (updateVersion <= currentVersion && !allowDowngrade)
        {
            Log.Information($"Current version {currentVersion} is up to date or newer than update version {updateVersion}. To allow downgrades, use the --allow-downgrade=true option.");

            await ReadStreamAsync(new StringReader($"Current version {currentVersion} is up to date or newer than update version {updateVersion}. To allow downgrades, use the --allow-downgrade=true option."), MessageType.Output);
            
            return false;
        }

        return true;
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

    private async Task ReadStreamAsync(TextReader streamReader, MessageType messageType)
    {
        while (await streamReader.ReadLineAsync() is { } line)
        {
            await hubContext.Clients.All.ReceiveScriptResult(new ScriptResult
            {
                Message = line,
                Type = messageType
            });
        }
    }
}
