// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ServiceProcess;
using System.Text.Json;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostServiceManager(IHostLifecycleService hostLifecycleService, IUserInstanceService userInstanceService, IServiceManager serviceManager, IHostConfigurationService configurationService, IServiceConfiguration hostServiceConfig, JsonSerializerOptions jsonOptions) : IHostServiceManager
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Host";

    public async Task InstallOrUpdate(HostConfiguration hostConfiguration, string hostName, string ipAddress, string macAddress)
    {
        try
        {
            var directoryPath = GetDirectoryPath();

            if (serviceManager.IsServiceInstalled(hostServiceConfig.Name))
            {
                using var serviceController = new ServiceController(hostServiceConfig.Name);

                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    serviceManager.StopService(hostServiceConfig.Name);
                }

                CopyToTargetPath(directoryPath, hostName, ipAddress, macAddress);
            }
            else
            {
                CopyToTargetPath(directoryPath, hostName, ipAddress, macAddress);
                var hostPath = Path.Combine(directoryPath, $"{MainAppName}.{SubAppName}.exe");
                serviceManager.InstallService(hostServiceConfig, $"{hostPath} --service-mode");
            }

            var updatedHostConfiguration = await UpdateConfigurationAsync(Path.Combine(directoryPath, configurationService.ConfigurationFileName), hostName, ipAddress, macAddress);

            serviceManager.StartService(hostServiceConfig.Name);

            Log.Information("{ServiceName} installed and started successfully.", hostServiceConfig.Name);

            await hostLifecycleService.RegisterAsync(updatedHostConfiguration);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    public async Task Uninstall(HostConfiguration hostConfiguration)
    {
        try
        {
            if (serviceManager.IsServiceInstalled(hostServiceConfig.Name))
            {
                serviceManager.StopService(hostServiceConfig.Name);
                serviceManager.UninstallService(hostServiceConfig.Name);

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

    public async Task UpdateHostInformation(HostConfiguration hostConfiguration)
    {
        await hostLifecycleService.UpdateHostInformationAsync(hostConfiguration);
    }

    private static string GetDirectoryPath()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return Path.Combine(programFilesPath, MainAppName, SubAppName);
    }

    private void CopyToTargetPath(string targetDirectoryPath, string hostName, string ipAddress, string macAddress)
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
            try
            {
                var hostConfiguration = JsonSerializer.Deserialize<HostConfiguration>(File.ReadAllText(sourceConfigPath));

                if (hostConfiguration != null)
                {
                    hostConfiguration.Host = new Computer
                    {
                        Name = hostName,
                        IPAddress = ipAddress,
                        MACAddress = macAddress
                    };

                    var json = JsonSerializer.Serialize(hostConfiguration, jsonOptions);

                    File.WriteAllText(targetConfigPath, json);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to copy the configuration file to {targetConfigPath}.", ex);
            }
        }
    }

    private static void DeleteFiles()
    {
        var directoryPath = GetDirectoryPath();
        const int maxRetries = 100;
        var delayOnRetry = 2000;

        if (directoryPath != null && Directory.Exists(directoryPath))
        {
            foreach (var file in Directory.EnumerateFiles(directoryPath))
            {
                WaitForFile(file, maxRetries, delayOnRetry);
            }

            for (var i = 0; i < maxRetries; ++i)
            {
                try
                {
                    Directory.Delete(directoryPath, true);
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

    private async Task<HostConfiguration> UpdateConfigurationAsync(string filePath, string hostName, string ipAddress, string macAddress)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var config = JsonSerializer.Deserialize<HostConfiguration>(json, jsonOptions);

        config.Host ??= new Computer
        {
            Name = hostName,
            IPAddress = ipAddress,
            MACAddress = macAddress
        };

        return config;
    }
}