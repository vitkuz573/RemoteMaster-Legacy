// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Pipes;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Shared.Models;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Core.Services;

public class HostUpdater(IRecoveryService recoveryService, IApplicationPathProvider applicationPathProvider, IHostUpdaterNotifier notifier, IChecksumValidator checksumValidator, IFileService fileService, IFileSystem fileSystem, IApplicationVersionProvider applicationVersionProvider, INetworkDriveService networkDriveService, IUserInstanceService userInstanceService, IChatInstanceService chatInstanceService, IServiceFactory serviceFactory, IHostConfigurationService hostConfigurationService) : IHostUpdater
{
    private readonly string _rootDirectory = applicationPathProvider.RootDirectory;
    private readonly string _updateDirectory = applicationPathProvider.UpdateDirectory;

    private readonly TaskCompletionSource<bool> _clientConnectedTcs = new();

    public async Task UpdateAsync(string folderPath, string? username, string? password, bool force, bool allowDowngrade, int waitForClientConnectionTimeout)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        try
        {
            await NotifyReadiness();

            if (waitForClientConnectionTimeout != 0)
            {
                var completedTask = await Task.WhenAny(_clientConnectedTcs.Task, Task.Delay(waitForClientConnectionTimeout));

                if (completedTask != _clientConnectedTcs.Task)
                {
                    await notifier.NotifyAsync("Timeout reached while waiting for client connection. Update aborted.", MessageSeverity.Warning);

                    return;
                }
            }
            else
            {
                await _clientConnectedTcs.Task;
            }
        }
        catch (Exception ex)
        {
            await notifier.NotifyAsync($"Error initializing hub client: {ex.Message}", MessageSeverity.Error);

            return;
        }

        try
        {
            await notifier.NotifyAsync($"Force update flag: {force}", MessageSeverity.Information);
            await notifier.NotifyAsync($"Allow downgrade flag: {allowDowngrade}", MessageSeverity.Information);
            await notifier.NotifyAsync(Environment.ProcessId.ToString(), MessageSeverity.Information, MessageMeta.ProcessIdInformation);

            var originalFolderPath = folderPath;

            var isNetworkPath = folderPath.StartsWith(@"\\");

            if (isNetworkPath)
            {
                if (!await MapNetworkDriveAsync(folderPath, username, password))
                {
                    await notifier.NotifyAsync("Update aborted.", MessageSeverity.Error);

                    return;
                }

                folderPath = networkDriveService.GetEffectivePath(folderPath);
            }

            var sourceFolderPath = fileSystem.Path.Combine(folderPath, "Host");

            try
            {
                fileService.CreateDirectory(_updateDirectory);
            }
            catch (IOException ex) when (ex.Message.Contains("Directory already exists"))
            {
                await notifier.NotifyAsync($"Update directory already exists: {_updateDirectory}. Proceeding with existing directory.", MessageSeverity.Warning);
            }
            catch (Exception ex)
            {
                await notifier.NotifyAsync($"Failed to create update directory: {ex.Message}", MessageSeverity.Error);
                throw;
            }

            var isDownloaded = await CopyDirectoryAsync(sourceFolderPath, _updateDirectory, true);

            if (isNetworkPath)
            {
                await UnmapNetworkDriveAsync(originalFolderPath);
            }

            if (!isDownloaded)
            {
                await notifier.NotifyAsync("Download or copy failed. Update aborted.", MessageSeverity.Error);

                return;
            }

            if (!await CheckForUpdateVersion(allowDowngrade, force))
            {
                await notifier.NotifyAsync("Update aborted due to version check.", MessageSeverity.Error);

                return;
            }

            if (!await IsUpdateNeeded(force))
            {
                await notifier.NotifyAsync("No update required. Files are identical.", MessageSeverity.Information);
                await notifier.NotifyAsync("If you wish to force an update regardless, you can use --force to override this check.", MessageSeverity.Information);

                return;
            }

            await PerformUpdateAsync();
        }
        catch (Exception ex)
        {
            await notifier.NotifyAsync($"Error while updating host: {ex.Message}", MessageSeverity.Error);
            await recoveryService.ExecuteEmergencyRecoveryAsync();
        }
    }

    public void NotifyClientConnected()
    {
        if (!_clientConnectedTcs.Task.IsCompleted)
        {
            _clientConnectedTcs.SetResult(true);
        }
    }

    private static async Task NotifyReadiness()
    {
        await using var client = new NamedPipeClientStream(".", PipeNames.UpdaterReadyPipe, PipeDirection.Out, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000);

        await using var writer = new StreamWriter(client);
        writer.AutoFlush = true;

        await writer.WriteLineAsync("Updater is ready at port 6001");
    }

    private async Task PerformUpdateAsync()
    {
        var hostService = serviceFactory.GetService("RCHost");

        hostService.Stop();

        chatInstanceService.Stop();
        userInstanceService.Stop();

        await fileService.WaitForFileReleaseAsync(_rootDirectory);

        var copySuccess = await CopyDirectoryAsync(_updateDirectory, _rootDirectory, true);

        if (!copySuccess)
        {
            await notifier.NotifyAsync("Failed to copy updates. Update aborted.", MessageSeverity.Error);
            throw new InvalidOperationException("Failed to copy updates.");
        }

        hostService.Start();
        await EnsureServicesRunning([hostService, userInstanceService], TimeSpan.FromSeconds(5), 5);

        await CleanupUpdateFolder();

        await notifier.NotifyAsync("Update completed successfully.", MessageSeverity.Information);
    }

    private async Task<bool> MapNetworkDriveAsync(string folderPath, string? username, string? password)
    {
        await notifier.NotifyAsync($"Attempting to map network drive with remote path: {folderPath}", MessageSeverity.Information);

        var isMapped = networkDriveService.MapNetworkDrive(folderPath, username, password);

        if (!isMapped)
        {
            await notifier.NotifyAsync($"Failed to map network drive with remote path {folderPath}. Details can be found in the log files.", MessageSeverity.Error);
        }
        else
        {
            await notifier.NotifyAsync($"Successfully mapped network drive with remote path: {folderPath}", MessageSeverity.Information);
        }

        return isMapped;
    }

    private async Task UnmapNetworkDriveAsync(string folderPath)
    {
        await notifier.NotifyAsync($"Attempting to unmap network drive with remote path: {folderPath}", MessageSeverity.Information);

        var isCancelled = networkDriveService.CancelNetworkDrive(folderPath);

        if (!isCancelled)
        {
            await notifier.NotifyAsync($"Failed to unmap network drive with remote path {folderPath}. Details can be found in the log files.", MessageSeverity.Error);
        }
        else
        {
            await notifier.NotifyAsync($"Successfully unmapped network drive with remote path: {folderPath}", MessageSeverity.Information);
        }
    }

    private async Task CleanupUpdateFolder()
    {
        await notifier.NotifyAsync("Starting cleanup...", MessageSeverity.Information);

        try
        {
            fileService.DeleteDirectory(_updateDirectory);

            await notifier.NotifyAsync($"Successfully deleted directory: {_updateDirectory}", MessageSeverity.Information);
        }
        catch (Exception ex)
        {
            await notifier.NotifyAsync($"Failed to delete directory {_updateDirectory}: {ex.Message}", MessageSeverity.Error);
        }

        await notifier.NotifyAsync("Cleanup completed.", MessageSeverity.Information);
    }

    private async Task<bool> CopyDirectoryAsync(string sourceDir, string destDir, bool overwrite = false, int maxAttempts = 5)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                fileService.CopyDirectory(sourceDir, destDir, overwrite);

                foreach (var sourceFile in fileSystem.Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = fileSystem.Path.GetRelativePath(sourceDir, sourceFile);
                    var destFile = fileSystem.Path.Combine(destDir, relativePath);

                    if (!fileSystem.File.Exists(destFile))
                    {
                        await notifier.NotifyAsync($"File {sourceFile} was not copied to {destFile}.", MessageSeverity.Error);

                        throw new IOException($"File {sourceFile} was not copied successfully.");
                    }

                    if (checksumValidator.AreChecksumsEqual(sourceFile, destFile))
                    {
                        continue;
                    }

                    await notifier.NotifyAsync($"File {sourceFile} copied with errors. Checksum does not match.", MessageSeverity.Error);

                    throw new IOException($"Checksum mismatch for file {sourceFile}.");
                }

                await notifier.NotifyAsync($"Successfully copied directory {sourceDir} to {destDir} on attempt {attempt}.", MessageSeverity.Information);

                return true;
            }
            catch (Exception ex)
            {
                await notifier.NotifyAsync($"Attempt {attempt} to copy directory {sourceDir} to {destDir} failed: {ex.Message}", MessageSeverity.Warning);

                if (attempt == maxAttempts)
                {
                    await notifier.NotifyAsync($"All {maxAttempts} attempts to copy directory {sourceDir} to {destDir} failed.", MessageSeverity.Error);

                    return false;
                }

                await Task.Delay(1000);
            }
        }

        return false;
    }

    private async Task EnsureServicesRunning(IEnumerable<IRunnable> services, TimeSpan delay, int attempts)
    {
        var allServicesRunning = false;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            await notifier.NotifyAsync($"Attempt {attempt}: Checking if services are running...", MessageSeverity.Information);

            await Task.Delay(delay);

            var nonRunningServices = services.Where(service => !service.IsRunning).ToList();

            allServicesRunning = nonRunningServices.Count == 0;

            if (allServicesRunning)
            {
                await notifier.NotifyAsync("All services have been successfully started.", MessageSeverity.Information);
                break;
            }

            var nonRunningServicesList = string.Join(", ", nonRunningServices.Select(service => service.ToString()));

            await notifier.NotifyAsync($"Not all services are running. The following services are not active: {nonRunningServicesList}. Waiting and retrying...", MessageSeverity.Warning);
        }

        if (!allServicesRunning)
        {
            await notifier.NotifyAsync($"Failed to start all services after {attempts} attempts.", MessageSeverity.Error);
            throw new Exception("Failed to start all services after multiple attempts.");
        }
    }

    private async Task<bool> IsUpdateNeeded(bool force)
    {
        if (force)
        {
            return true;
        }

        var updateFiles = fileSystem.Directory.GetFiles(_updateDirectory, "*", SearchOption.AllDirectories)
            .Select(fileSystem.Path.GetFullPath);

        foreach (var file in updateFiles)
        {
            var relativePath = file[(_updateDirectory.Length + 1)..];
            var targetFile = fileSystem.Path.Combine(_rootDirectory, relativePath);

            if (!fileSystem.File.Exists(targetFile))
            {
                await notifier.NotifyAsync($"File missing in target: {targetFile}. Update needed.", MessageSeverity.Information);

                return true;
            }

            if (checksumValidator.AreChecksumsEqual(file, targetFile))
            {
                continue;
            }

            await notifier.NotifyAsync($"Checksum mismatch for file: {file}. Update needed.", MessageSeverity.Warning);

            return true;
        }

        await notifier.NotifyAsync("All files are identical. No update required.", MessageSeverity.Information);

        return false;
    }

    private async Task<bool> CheckForUpdateVersion(bool allowDowngrade, bool force)
    {
        var updateExecutablePath = fileSystem.Path.Combine(_updateDirectory, fileSystem.Path.GetFileName(Environment.ProcessPath!));

        var currentVersion = applicationVersionProvider.GetVersionFromAssembly();
        var updateVersion = applicationVersionProvider.GetVersionFromExecutable(updateExecutablePath);

        await notifier.NotifyAsync($"Current version: {currentVersion}", MessageSeverity.Information);
        await notifier.NotifyAsync($"Update version: {updateVersion}", MessageSeverity.Information);

        if (updateVersion > currentVersion)
        {
            await notifier.NotifyAsync("Update version is newer than current version; proceeding with update.", MessageSeverity.Information);

            return true;
        }

        if (updateVersion == currentVersion)
        {
            if (!checksumValidator.AreChecksumsEqual(updateExecutablePath, Environment.ProcessPath!))
            {
                await notifier.NotifyAsync("Checksums differ; proceeding with update.", MessageSeverity.Information);

                return true;
            }

            if (force)
            {
                await notifier.NotifyAsync("Force flag is set; proceeding with update even though files are identical.", MessageSeverity.Warning);

                return true;
            }

            await notifier.NotifyAsync("No update needed; files are identical.", MessageSeverity.Information);

            return false;
        }

        if (allowDowngrade)
        {
            await notifier.NotifyAsync("Allowing downgrade as per --allow-downgrade flag; proceeding with update.", MessageSeverity.Warning);

            return true;
        }

        await notifier.NotifyAsync($"Update version {updateVersion} is older than current version {currentVersion}. Use --allow-downgrade to proceed.", MessageSeverity.Warning);

        return false;
    }
}
