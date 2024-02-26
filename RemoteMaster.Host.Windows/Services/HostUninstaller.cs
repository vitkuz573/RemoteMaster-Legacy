// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostUninstaller(IHostConfigurationService hostConfigurationService, IServiceFactory serviceFactory, IUserInstanceService userInstanceService, IHostLifecycleService hostLifecycleService) : IHostUninstaller
{
    private readonly string _applicationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");

    public async Task UninstallAsync()
    {
        try
        {
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

            var hostService = serviceFactory.GetService("RCHost");

            if (hostService.IsInstalled)
            {
                hostService.Stop();
                hostService.Delete();

                Log.Information("{ServiceName} Service uninstalled successfully.", hostService.Name);
            }
            else
            {
                Log.Information("{ServiceName} Service is not installed.", hostService.Name);
            }

            if (userInstanceService.IsRunning)
            {
                userInstanceService.Stop();
            }

            DeleteFiles(_applicationDirectory);

            await hostLifecycleService.UnregisterAsync(hostConfiguration);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    private static void DeleteFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Log.Information("Directory {DirectoryPath} does not exist, no files to delete.", directoryPath);
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath))
        {
            WaitForFile(file, 100, 2000);
        }

        try
        {
            Directory.Delete(directoryPath, recursive: true);
            Log.Information("{DirectoryPath} has been successfully deleted.", directoryPath);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to delete directory {DirectoryPath}: {Message}", directoryPath, ex.Message);
        }
    }

    private static void WaitForFile(string filepath, int maxRetries, int delayOnRetry)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                using var stream = File.Open(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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
