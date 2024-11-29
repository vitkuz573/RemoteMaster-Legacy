// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class HostUninstaller(IServiceFactory serviceFactory, ICertificateService certificateService, IUserInstanceService userInstanceService, IHostLifecycleService hostLifecycleService, IFileSystem fileSystem, IFileService fileService, ILogger<HostUninstaller> logger) : IHostUninstaller
{
    private readonly string _applicationDirectory = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");

    public async Task UninstallAsync()
    {
        try
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

            var currentDirectory = fileSystem.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);

            if (!string.Equals(currentDirectory, _applicationDirectory, StringComparison.OrdinalIgnoreCase))
            {
                DeleteFiles(_applicationDirectory);

                try
                {
                    fileService.DeleteDirectory(_applicationDirectory, recursive: true);
                    logger.LogInformation("Directory {DirectoryPath} has been successfully deleted.", _applicationDirectory);
                }
                catch (DirectoryNotFoundException)
                {
                    logger.LogInformation("Directory {DirectoryPath} does not exist, no files to delete.", _applicationDirectory);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to delete directory {DirectoryPath}: {Message}", _applicationDirectory, ex.Message);
                }
            }
            else
            {
                logger.LogInformation("Current process is running from the application directory. Skipping deletion of files and directory.");
            }

            certificateService.RemoveCertificates();

            logger.LogInformation("Uninstallation process completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred: {Message}", ex.Message);
        }
    }

    private void DeleteFiles(string directoryPath)
    {
        if (!fileSystem.Directory.Exists(directoryPath))
        {
            logger.LogInformation("Directory {DirectoryPath} does not exist, no files to delete.", directoryPath);

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
