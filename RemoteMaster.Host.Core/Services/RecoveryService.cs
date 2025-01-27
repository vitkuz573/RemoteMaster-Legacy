// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Core.Services;

public class RecoveryService(IChecksumValidator checksumValidator, IApplicationPathProvider applicationPathProvider, IServiceFactory serviceFactory, IFileSystem fileSystem, IFileService fileService, IUserInstanceService userInstanceService, IHostUpdaterNotifier notifier) : IRecoveryService
{
    private readonly string _rootDirectory = applicationPathProvider.RootDirectory;
    private readonly string _updaterDirectory = applicationPathProvider.UpdaterDirectory;

    public async Task ExecuteEmergencyRecoveryAsync()
    {
        try
        {
            await notifier.NotifyAsync("Emergency recovery process has started.", MessageSeverity.Information);

            var hostService = serviceFactory.GetService("RCHost");

            StopServiceWithRetry(hostService);

            if (userInstanceService.IsRunning)
            {
                userInstanceService.Stop();
            }

            await fileService.WaitForFileReleaseAsync(_rootDirectory);

            var fileName = fileSystem.Path.GetFileName(Environment.ProcessPath!);

            var sourceExePath = fileSystem.Path.Combine(_updaterDirectory, fileName);
            var destinationExePath = fileSystem.Path.Combine(_rootDirectory, fileName);

            if (!fileSystem.File.Exists(sourceExePath))
            {
                await notifier.NotifyAsync("Source executable for recovery not found. Unable to proceed with recovery.", MessageSeverity.Error);

                return;
            }

            await CopyFileWithRetry(sourceExePath, destinationExePath, true);

            await notifier.NotifyAsync("Emergency recovery completed successfully. Attempting to restart services...", MessageSeverity.Information);

            await StartServiceWithRetry(hostService);

            await notifier.NotifyAsync("Services have been successfully restarted after emergency recovery.", MessageSeverity.Information);
        }
        catch (Exception ex)
        {
            await notifier.NotifyAsync($"Emergency recovery failed: {ex.Message}", MessageSeverity.Error);
        }
    }

    private async Task CopyFileWithRetry(string sourceFile, string destinationFile, bool overwrite, int maxAttempts = 5)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                fileService.CopyFile(sourceFile, destinationFile, overwrite);

                if (checksumValidator.AreChecksumsEqual(sourceFile, destinationFile))
                {
                    await notifier.NotifyAsync($"Successfully copied file {sourceFile} to {destinationFile} on attempt {attempt}.", MessageSeverity.Information);

                    return;
                }

                await notifier.NotifyAsync($"Checksum verification failed for file {sourceFile}. Retrying...", MessageSeverity.Warning);
            }
            catch (IOException ex) when (attempt < maxAttempts)
            {
                await notifier.NotifyAsync($"Attempt {attempt} to copy file {sourceFile} failed: {ex.Message}. Retrying in 1 second...", MessageSeverity.Warning);
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                await notifier.NotifyAsync($"Failed to copy file {sourceFile} to {destinationFile}: {ex.Message}.", MessageSeverity.Error);
                throw;
            }
        }

        throw new IOException($"Failed to copy file {sourceFile} to {destinationFile} after {maxAttempts} attempts.");
    }

    private static void StopServiceWithRetry(IRunnable runnableService, int maxAttempts = 3)
    {
        if (runnableService is not IService service)
        {
            throw new ArgumentException("The provided service does not implement IService.", nameof(service));
        }

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
                    throw new Exception($"Failed to stop {service.Name} service after {maxAttempts} attempts: {ex.Message}");
                }

                Thread.Sleep(1000);
            }
        }
    }

    private async Task StartServiceWithRetry(IRunnable runnableService, int maxAttempts = 3)
    {
        if (runnableService is not IService service)
        {
            throw new ArgumentException("The provided service does not implement IService.", nameof(service));
        }

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                service.Start();

                await notifier.NotifyAsync($"{service.Name} service started successfully.", MessageSeverity.Information);

                return;
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    await notifier.NotifyAsync($"❌ Failed to start {service.Name} service after {maxAttempts} attempts: {ex.Message}", MessageSeverity.Error);

                    throw new Exception($"Failed to start {service.Name} service after {maxAttempts} attempts: {ex.Message}");
                }

                await notifier.NotifyAsync($"Attempt {attempt} to start {service.Name} service failed: {ex.Message}. Retrying...", MessageSeverity.Warning);

                Thread.Sleep(1000);
            }
        }
    }
}
