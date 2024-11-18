// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Formatters;
using RemoteMaster.Shared.Models;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Windows.Services;

public class HostUpdater : IHostUpdater
{
    private readonly string _baseFolderPath;
    private readonly string _updateFolderPath;

    private readonly TaskCompletionSource<bool> _clientConnectedTcs = new();
    private bool _emergencyRecoveryApplied;
    private HubConnection? _updaterHubClient;

    private readonly IFileService _fileService;
    private readonly IFileSystem _fileSystem;
    private readonly INetworkDriveService _networkDriveService;
    private readonly IUserInstanceService _userInstanceService;
    private readonly IChatInstanceService _chatInstanceService;
    private readonly IServiceFactory _serviceFactory;
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IHubContext<UpdaterHub, IUpdaterClient> _hubContext;
    private readonly ILogger<HostUpdater> _logger;

    public HostUpdater(IFileService fileService, IFileSystem fileSystem, INetworkDriveService networkDriveService, IUserInstanceService userInstanceService, IChatInstanceService chatInstanceService, IServiceFactory serviceFactory, IHostConfigurationService hostConfigurationService, IHubContext<UpdaterHub, IUpdaterClient> hubContext, ILogger<HostUpdater> logger)
    {
        _fileService = fileService;
        _fileSystem = fileSystem;
        _networkDriveService = networkDriveService;
        _userInstanceService = userInstanceService;
        _chatInstanceService = chatInstanceService;
        _serviceFactory = serviceFactory;
        _hostConfigurationService = hostConfigurationService;
        _hubContext = hubContext;
        _logger = logger;

        _baseFolderPath = _fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");
        _updateFolderPath = _fileSystem.Path.Combine(_baseFolderPath, "Update");

        Task.Run(async () => await InitializeHubClient());
    }

    private async Task InitializeHubClient()
    {
        var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync();

        _updaterHubClient = new HubConnectionBuilder()
            .WithUrl($"https://{hostConfiguration.Host.IpAddress}:5001/hubs/updater", options =>
            {
                options.Headers.Add("X-Service-Flag", "true");
            })
            .AddMessagePackProtocol(options =>
            {
                var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            })
            .Build();

        await _updaterHubClient.StartAsync();

        await _updaterHubClient.InvokeAsync("NotifyPortReady", 6001);
    }

    public async Task UpdateAsync(string folderPath, string? username, string? password, bool force = false, bool allowDowngrade = false)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        try
        {
            _logger.LogInformation("Waiting for client to connect to UpdaterHub...");
            await _clientConnectedTcs.Task;
            _logger.LogInformation("Client connected. Proceeding with update.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for client connection.");

            return;
        }

        try
        {
            await NotifyFlags(force, allowDowngrade);
            await NotifyProcessId();

            var sourceFolderPath = _fileSystem.Path.Combine(folderPath, "Host");
            var isNetworkPath = folderPath.StartsWith(@"\\");

            if (isNetworkPath)
            {
                if (!await MapNetworkDriveAsync(folderPath, username, password))
                {
                    await Notify("Update aborted.", MessageSeverity.Error);

                    return;
                }
            }

            if (!_fileSystem.Directory.Exists(_updateFolderPath))
            {
                _fileSystem.Directory.CreateDirectory(_updateFolderPath);
            }

            var isDownloaded = await CopyDirectoryAsync(sourceFolderPath, _updateFolderPath, true);

            if (isNetworkPath)
            {
                await UnmapNetworkDriveAsync(folderPath);
            }

            if (!isDownloaded)
            {
                await Notify("Download or copy failed. Update aborted.", MessageSeverity.Error);

                return;
            }

            if (!await CheckForUpdateVersion(allowDowngrade, force))
            {
                await Notify("Update aborted due to version check.", MessageSeverity.Error);

                return;
            }

            if (!await NeedUpdate() && !force)
            {
                await NotifyNoUpdateNeeded();

                return;
            }

            await PerformUpdate();
        }
        catch (Exception ex)
        {
            await Notify($"Error while updating host: {ex.Message}", MessageSeverity.Error);
            await AttemptEmergencyRecovery();
        }
    }

    public void NotifyClientConnected()
    {
        if (!_clientConnectedTcs.Task.IsCompleted)
        {
            _clientConnectedTcs.SetResult(true); 

            _logger.LogInformation("Client successfully connected to UpdaterHub.");
        }
        else
        {
            _logger.LogWarning("NotifyClientConnected called but TCS is already completed.");
        }
    }

    private async Task PerformUpdate()
    {
        var hostService = _serviceFactory.GetService("RCHost");

        hostService.Stop();
        _chatInstanceService.Stop();
        _userInstanceService.Stop();

        await WaitForFileRelease(_baseFolderPath);
        await CopyDirectoryAsync(_updateFolderPath, _baseFolderPath, true);

        hostService.Start();
        await EnsureServicesRunning([hostService, _userInstanceService], 5, 5);

        if (!_emergencyRecoveryApplied)
        {
            await Notify("Update completed successfully.", MessageSeverity.Information);
        }
        else
        {
            await Notify("Emergency recovery was applied. Please check the system's integrity.", MessageSeverity.Warning);
        }

        await CleanupUpdateFolder();
    }

    private async Task NotifyFlags(bool force, bool allowDowngrade)
    {
        await Notify($"Force update flag: {force}", MessageSeverity.Information);
        await Notify($"Allow downgrade flag: {allowDowngrade}", MessageSeverity.Information);
    }

    private async Task NotifyProcessId()
    {
        await _hubContext.Clients.All.ReceiveMessage(new Message(Environment.ProcessId.ToString(), MessageSeverity.Service)
        {
            Meta = "pid"
        });
    }

    private async Task<bool> MapNetworkDriveAsync(string folderPath, string? username, string? password)
    {
        await Notify($"Attempting to map network drive with remote path: {folderPath}", MessageSeverity.Information);

        var isMapped = _networkDriveService.MapNetworkDrive(folderPath, username, password);

        if (!isMapped)
        {
            await Notify($"Failed to map network drive with remote path {folderPath}. Details can be found in the log files.", MessageSeverity.Error);
            await Notify("Unable to map network drive with the provided credentials.", MessageSeverity.Error);
        }
        else
        {
            await Notify($"Successfully mapped network drive with remote path: {folderPath}", MessageSeverity.Information);
        }

        return isMapped;
    }

    private async Task UnmapNetworkDriveAsync(string folderPath)
    {
        await Notify($"Attempting to unmap network drive with remote path: {folderPath}", MessageSeverity.Information);

        var isCancelled = _networkDriveService.CancelNetworkDrive(folderPath);

        if (!isCancelled)
        {
            await Notify($"Failed to unmap network drive with remote path {folderPath}. Details can be found in the log files.", MessageSeverity.Error);
        }
        else
        {
            await Notify($"Successfully unmapped network drive with remote path: {folderPath}", MessageSeverity.Information);
        }
    }

    private async Task CleanupUpdateFolder()
    {
        await Notify("Starting cleanup...", MessageSeverity.Information);

        var updateDirectory = _fileSystem.Path.Combine(_baseFolderPath, "Update");

        try
        {
            _fileService.DeleteDirectory(updateDirectory);

            await Notify($"Successfully deleted directory: {updateDirectory}", MessageSeverity.Information);
        }
        catch (Exception ex)
        {
            await Notify($"Failed to delete directory {updateDirectory}: {ex.Message}", MessageSeverity.Error);
        }

        await Notify("Cleanup completed.", MessageSeverity.Information);
    }

    private async Task NotifyNoUpdateNeeded()
    {
        await Notify("No update required. Files are identical.", MessageSeverity.Information);
        await Notify("If you wish to force an update regardless, you can use --force to override this check.", MessageSeverity.Information);
    }

    private async Task<bool> CopyDirectoryAsync(string sourceDir, string destDir, bool overwrite = false, int maxAttempts = 5)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _fileService.CopyDirectory(sourceDir, destDir, overwrite);

                foreach (var sourceFile in _fileSystem.Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = sourceFile[(sourceDir.Length + 1)..];
                    var destFile = _fileSystem.Path.Combine(destDir, relativePath);

                    if (!_fileSystem.File.Exists(destFile))
                    {
                        await Notify($"File {sourceFile} was not copied to {destFile}.", MessageSeverity.Error);
                        
                        throw new IOException($"File {sourceFile} was not copied successfully.");
                    }

                    if (await VerifyChecksum(sourceFile, destFile, expectDifference: false))
                    {
                        continue;
                    }

                    await Notify($"File {sourceFile} copied with errors. Checksum does not match.", MessageSeverity.Error);
                    
                    throw new IOException($"Checksum mismatch for file {sourceFile}.");
                }

                await Notify($"Successfully copied directory {sourceDir} to {destDir} on attempt {attempt}.", MessageSeverity.Information);
                
                return true;
            }
            catch (Exception ex)
            {
                await Notify($"Attempt {attempt} to copy directory {sourceDir} to {destDir} failed: {ex.Message}", MessageSeverity.Warning);

                if (attempt == maxAttempts)
                {
                    await Notify($"All {maxAttempts} attempts to copy directory {sourceDir} to {destDir} failed.", MessageSeverity.Error);
                    
                    return false;
                }

                await Task.Delay(1000);
            }
        }

        return false;
    }

    private async Task<bool> VerifyChecksum(string sourceFilePath, string destFilePath, bool expectDifference)
    {
        var sourceChecksum = _fileService.CalculateChecksum(sourceFilePath);
        var destChecksum = _fileService.CalculateChecksum(destFilePath);

        var checksumsMatch = sourceChecksum == destChecksum;

        await Notify($"Verifying checksum: {sourceFilePath} [Checksum: {sourceChecksum}] vs {destFilePath} [Checksum: {destChecksum}].", MessageSeverity.Information);

        if (expectDifference)
        {
            if (!checksumsMatch)
            {
                await Notify("Checksums differ as expected. Proceeding.", MessageSeverity.Information);

                return true;
            }

            await Notify("Checksums match unexpectedly. No update needed.", MessageSeverity.Warning);

            return false;
        }

        if (checksumsMatch)
        {
            await Notify("Checksums match as expected.", MessageSeverity.Information);

            return true;
        }

        await Notify("Checksums differ unexpectedly. Possible file corruption.", MessageSeverity.Error);

        return false;
    }

    private async Task WaitForFileRelease(string directory)
    {
        var locked = true;
        var excludedFolders = new HashSet<string> { "Updater", "Update" };

        while (locked)
        {
            locked = false;

            foreach (var file in _fileSystem.DirectoryInfo.New(directory).GetFiles("*", SearchOption.AllDirectories))
            {
                var directoryName = file.DirectoryName;

                if (directoryName == null || excludedFolders.Any(directoryName.Contains))
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

    private async Task EnsureServicesRunning(List<IRunnable> services, int delayInSeconds, int attempts)
    {
        var allServicesRunning = false;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            await Notify($"Attempt {attempt}: Checking if services are running...", MessageSeverity.Information);

            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));

            var nonRunningServices = services.Where(service => !service.IsRunning).ToList();

            allServicesRunning = nonRunningServices.Count == 0;

            if (allServicesRunning)
            {
                await Notify("All services have been successfully started.", MessageSeverity.Information);
                break;
            }

            var nonRunningServicesList = string.Join(", ", nonRunningServices.Select(service => service.ToString()));

            await Notify($"Not all services are running. The following services are not active: {nonRunningServicesList}. Waiting and retrying...", MessageSeverity.Warning);
        }

        if (!allServicesRunning)
        {
            await Notify($"Failed to start all services after {attempts} attempts. Initiating emergency recovery...", MessageSeverity.Information);
            await AttemptEmergencyRecovery();
        }
    }

    private async Task AttemptEmergencyRecovery()
    {
        _emergencyRecoveryApplied = true;

        try
        {
            var hostService = _serviceFactory.GetService("RCHost");

            StopServiceWithRetry(hostService, "Host");

            if (_userInstanceService.IsRunning)
            {
                _userInstanceService.Stop();
            }

            await WaitForFileRelease(_baseFolderPath);

            var sourceExePath = _fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");
            var destinationExePath = _fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");

            if (!_fileSystem.File.Exists(sourceExePath))
            {
                await Notify("Source executable for recovery not found. Unable to proceed with recovery.", MessageSeverity.Error);
                
                return;
            }

            CopyFileWithRetry(sourceExePath, destinationExePath, true);
            
            await Notify("Emergency recovery completed successfully. Attempting to restart services...", MessageSeverity.Information);

            StartServiceWithRetry(hostService, "Host");
        }
        catch (Exception ex)
        {
            await Notify($"Emergency recovery failed: {ex.Message}", MessageSeverity.Error);
        }
    }

    private async Task CopyFileWithRetry(string sourceFile, string destinationFile, bool overwrite, int maxAttempts = 5)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _fileService.CopyFile(sourceFile, destinationFile, overwrite);

                if (await VerifyChecksum(sourceFile, destinationFile, expectDifference: false))
                {
                    await Notify($"Successfully copied file {sourceFile} to {destinationFile} on attempt {attempt}.", MessageSeverity.Information);
                    
                    return;
                }

                await Notify($"Checksum verification failed for file {sourceFile}. Retrying...", MessageSeverity.Warning);
            }
            catch (IOException ex) when (attempt < maxAttempts)
            {
                await Notify($"Attempt {attempt} to copy file {sourceFile} failed: {ex.Message}. Retrying in 1 second...", MessageSeverity.Warning);
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                await Notify($"Failed to copy file {sourceFile} to {destinationFile}: {ex.Message}.", MessageSeverity.Error);
                throw;
            }
        }

        throw new IOException($"Failed to copy file {sourceFile} to {destinationFile} after {maxAttempts} attempts.");
    }

    private static void StopServiceWithRetry(IRunnable service, string serviceName, int maxAttempts = 3)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                service.Stop();

                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    throw new Exception($"Failed to stop {serviceName} service after {maxAttempts} attempts: {ex.Message}");
                }

                Thread.Sleep(1000);
            }
        }
    }

    private static void StartServiceWithRetry(IRunnable service, string serviceName, int maxAttempts = 3)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                service.Start();

                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    throw new Exception($"Failed to start {serviceName} service after {maxAttempts} attempts: {ex.Message}");
                }

                Thread.Sleep(1000);
            }
        }
    }

    private async Task<bool> NeedUpdate()
    {
        var updateFiles = _fileSystem.Directory.GetFiles(_updateFolderPath, "*", SearchOption.AllDirectories)
            .Select(_fileSystem.Path.GetFullPath);

        foreach (var file in updateFiles)
        {
            var targetFile = file.Replace(_updateFolderPath, _baseFolderPath);

            if (!_fileSystem.File.Exists(targetFile))
            {
                return true;
            }

            if (await VerifyChecksum(file, targetFile, expectDifference: true))
            {
                return true;
            }
        }

        return false;
    }

    private Version GetCurrentVersion()
    {
        var currentExecutablePath = _fileSystem.Path.Combine(_baseFolderPath, "RemoteMaster.Host.exe");

        return GetVersionFromExecutable(currentExecutablePath);
    }

    private Version GetUpdateVersion()
    {
        var updateExecutablePath = _fileSystem.Path.Combine(_updateFolderPath, "RemoteMaster.Host.exe");

        return GetVersionFromExecutable(updateExecutablePath);
    }

    private async Task<bool> CheckForUpdateVersion(bool allowDowngrade, bool force)
    {
        var currentVersion = GetCurrentVersion();
        var updateVersion = GetUpdateVersion();

        await Notify($"Current version: {currentVersion}", MessageSeverity.Information);
        await Notify($"Update version: {updateVersion}", MessageSeverity.Information);

        var currentExecutablePath = Path.Combine(_baseFolderPath, "RemoteMaster.Host.exe");
        var updateExecutablePath = Path.Combine(_updateFolderPath, "RemoteMaster.Host.exe");

        if (updateVersion > currentVersion)
        {
            await Notify("Update version is newer than current version; proceeding with update.", MessageSeverity.Information);
            return true;
        }
        else if (updateVersion == currentVersion)
        {
            var checksumsDiffer = await VerifyChecksum(updateExecutablePath, currentExecutablePath, expectDifference: true);

            if (checksumsDiffer)
            {
                await Notify("Checksums differ; proceeding with update.", MessageSeverity.Information);
                return true;
            }
            else if (force)
            {
                await Notify("Force flag is set; proceeding with update even though files are identical.", MessageSeverity.Warning);
                return true;
            }
            else
            {
                await Notify("No update needed; files are identical.", MessageSeverity.Information);
                return false;
            }
        }
        else
        {
            if (allowDowngrade)
            {
                await Notify("Allowing downgrade as per --allow-downgrade flag; proceeding with update.", MessageSeverity.Warning);
               
                return true;
            }
            else
            {
                await Notify($"Update version {updateVersion} is older than current version {currentVersion}. Use --allow-downgrade to proceed.", MessageSeverity.Warning);
                
                return false;
            }
        }
    }

    private Version GetVersionFromExecutable(string filePath)
    {
        if (!_fileSystem.File.Exists(filePath))
        {
            throw new FileNotFoundException("Executable file not found.", filePath);
        }

        var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
        var fileVersion = versionInfo.FileVersion;

        if (string.IsNullOrEmpty(fileVersion))
        {
            throw new InvalidOperationException("The file version information is missing or invalid.");
        }

        try
        {
            return new Version(fileVersion);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Invalid file version format: {fileVersion}", ex);
        }
    }

    private async Task Notify(string message, MessageSeverity messageType)
    {
        switch (messageType)
        {
            case MessageSeverity.Information:
                _logger.LogInformation("{Message}", message);
                break;
            case MessageSeverity.Warning:
                _logger.LogWarning("{Message}", message);
                break;
            case MessageSeverity.Error:
                _logger.LogError("{Message}", message);
                break;
        }

        var streamReader = new StringReader(message);

        while (await streamReader.ReadLineAsync() is { } line)
        {
            await _hubContext.Clients.All.ReceiveMessage(new Message(line, messageType));
        }
    }
}
