// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostServiceManager(IHostLifecycleService hostLifecycleService, IHostInformationService hostInformationService, IHostConfigurationService hostConfigurationService, IUserInstanceService userInstanceService, IServiceManager serviceManager, IHostConfigurationService configurationService, IServiceConfiguration hostServiceConfig) : IHostServiceManager
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Host";

    private readonly string _applicationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), MainAppName, SubAppName);

    public async Task Install()
    {
        try
        {
            var hostInformation = hostInformationService.GetHostInformation();
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();

            Log.Information("Starting installation...");
            Log.Information("{MainAppName} Server: {Server}, Group: {Group}", MainAppName, hostConfiguration.Server, hostConfiguration.Group);
            Log.Information("Host Name: {HostName}, IP Address: {IPAddress}, MAC Address: {MacAddress}", hostInformation.Name, hostInformation.IpAddress, hostInformation.MacAddress);

            if (serviceManager.IsInstalled(hostServiceConfig.Name))
            {
                serviceManager.Stop(hostServiceConfig.Name);
                CopyToTargetPath(_applicationDirectory);
            }
            else
            {
                CopyToTargetPath(_applicationDirectory);
                serviceManager.Create(hostServiceConfig);
            }

            hostConfiguration.Host = hostInformation;

            var configurationFilePath = Path.Combine(_applicationDirectory, configurationService.ConfigurationFileName);
            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration, configurationFilePath);
            
            serviceManager.Start(hostServiceConfig.Name);

            Log.Information("{ServiceName} installed and started successfully.", hostServiceConfig.Name);

            await hostLifecycleService.RegisterAsync(hostConfiguration);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    public async Task Uninstall()
    {
        try
        {
            HostConfiguration hostConfiguration;

            try
            {
                var configurationFilePath = Path.Combine(_applicationDirectory, configurationService.ConfigurationFileName);
                hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(configurationFilePath);
            }
            catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
            {
                Log.Error(ex, "Configuration error.");

                return;
            }

            if (serviceManager.IsInstalled(hostServiceConfig.Name))
            {
                serviceManager.Stop(hostServiceConfig.Name);
                serviceManager.Delete(hostServiceConfig.Name);

                Log.Information("{ServiceName} Service uninstalled successfully.", hostServiceConfig.Name);
            }
            else
            {
                Log.Information("{ServiceName} Service is not installed.", hostServiceConfig.Name);
            }

            if (userInstanceService.IsRunning)
            {
                userInstanceService.Stop();
            }

            DeleteFiles();

            await hostLifecycleService.UnregisterAsync(hostConfiguration);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    private void CopyToTargetPath(string targetDirectoryPath)
    {
        if (!Directory.Exists(targetDirectoryPath))
        {
            Directory.CreateDirectory(targetDirectoryPath);
        }

        var targetExecutablePath = Path.Combine(targetDirectoryPath, $"{MainAppName}.{SubAppName}.exe");

        try
        {
            File.Copy(Environment.ProcessPath!, targetExecutablePath, true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy the executable to {targetExecutablePath}. Details: {ex.Message}", ex);
        }

        var configName = configurationService.ConfigurationFileName;
        var sourceConfigPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, configName);
        var targetConfigPath = Path.Combine(targetDirectoryPath, configName);

        if (File.Exists(sourceConfigPath))
        {
            File.Copy(sourceConfigPath, targetConfigPath, true);
        }
    }

    private void DeleteFiles()
    {
        const int maxRetries = 100;
        var delayOnRetry = 2000;

        if (!Directory.Exists(_applicationDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_applicationDirectory))
        {
            WaitForFile(file, maxRetries, delayOnRetry);
        }

        for (var i = 0; i < maxRetries; ++i)
        {
            try
            {
                Directory.Delete(_applicationDirectory, true);
                Log.Information("{AppName} files deleted successfully.", SubAppName);
                break;
            }
            catch (IOException ex)
            {
                if (i < maxRetries - 1)
                {
                    Log.Warning("Attempt {Attempt}: Deleting {AppName} files failed, retrying in {Delay}ms: {Message}", i + 1, SubAppName, delayOnRetry, ex.Message);
                    Thread.Sleep(delayOnRetry);

                    delayOnRetry *= 2;
                }
                else
                {
                    Log.Error("Final attempt: Deleting {AppName} files failed: {Message}", SubAppName, ex.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected error occurred while deleting {AppName} files: {Message}", SubAppName, ex.Message);
                break;
            }
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