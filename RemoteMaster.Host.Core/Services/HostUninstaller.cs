// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class HostUninstaller(IProcessService processService, IServiceFactory serviceFactory, ICertificateService certificateService, IUserInstanceService userInstanceService, IHostLifecycleService hostLifecycleService, IFileSystem fileSystem, IFileService fileService, IInstanceManagerService instanceManagerService, IApplicationPathProvider applicationPathProvider, ILogger<HostUninstaller> logger) : IHostUninstaller
{
    public async Task UninstallAsync()
    {
        try
        {
            var applicationDirectory = applicationPathProvider.RootDirectory; 
            var currentDirectory = fileSystem.Path.GetDirectoryName(processService.GetCurrentProcess().MainModule?.FileName);

            if (string.Equals(currentDirectory, applicationDirectory, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Current process is running from the application directory.");

                var tempDirectory = fileSystem.Path.GetTempPath();
                var tempExecutablePath = fileSystem.Path.Combine(tempDirectory, fileSystem.Path.GetFileName(Environment.ProcessPath)!);

                if (!fileSystem.Directory.Exists(tempDirectory))
                {
                    fileSystem.Directory.CreateDirectory(tempDirectory);
                }

                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                var tempProcessId = instanceManagerService.StartNewInstance(tempExecutablePath, "uninstall", [], startInfo);
                logger.LogInformation("Temporary uninstaller started with Process ID: {ProcessId}. Exiting current process...", tempProcessId);

                Environment.Exit(0);
            }
            else
            {
                logger.LogInformation("Current process is not running from the application directory.");

                await RemoveServicesAndResources();

                DeleteFiles(applicationDirectory);

                try
                {
                    fileService.DeleteDirectory(applicationDirectory, recursive: true);
                    logger.LogInformation("Directory {DirectoryPath} has been successfully deleted.", applicationDirectory);
                }
                catch (DirectoryNotFoundException)
                {
                    logger.LogInformation("Directory {DirectoryPath} does not exist, no files to delete.", applicationDirectory);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to delete directory {DirectoryPath}: {Message}", applicationDirectory, ex.Message);
                }

                logger.LogInformation("Uninstallation process completed successfully. Exiting...");
            }
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred: {Message}", ex.Message);
        }
    }

    private async Task RemoveServicesAndResources()
    {
        var hostService = serviceFactory.GetService("RCHost");

        if (hostService.IsInstalled)
        {
            hostService.Stop();
            hostService.Delete();
            logger.LogInformation("{ServiceName} Service uninstalled successfully.", hostService.Name);
        }
        else
        {
            logger.LogInformation("{ServiceName} Service is not installed.", hostService.Name);
        }

        if (userInstanceService.IsRunning)
        {
            userInstanceService.Stop();
        }

        await hostLifecycleService.UnregisterAsync();

        certificateService.RemoveCertificates();
        logger.LogInformation("Services and resources have been removed.");
    }

    private void DeleteFiles(string directoryPath)
    {
        if (!fileSystem.Directory.Exists(directoryPath))
        {
            logger.LogInformation("Directory {DirectoryPath} does not exist, skipping deletion.", directoryPath);
            return;
        }

        foreach (var file in fileSystem.Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            WaitForFile(file, 100, 2000);
        }
    }

    private void WaitForFile(string filepath, int maxRetries, int delayOnRetry)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                using var stream = fileSystem.File.Open(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                stream.Close();
                break;
            }
            catch (IOException)
            {
                Thread.Sleep(delayOnRetry);
            }
        }
    }
}
