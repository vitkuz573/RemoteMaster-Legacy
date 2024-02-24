// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostServiceManager(IHostLifecycleService hostLifecycleService, IHostInformationService hostInformationService, IHostConfigurationService hostConfigurationService, IUserInstanceService userInstanceService, IServiceManager serviceManager, IServiceConfigurationFactory serviceConfigurationFactory) : IHostServiceManager
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
            Log.Information("{MainAppName} Server: {Server}", MainAppName, hostConfiguration.Server);
            Log.Information("Host Name: {HostName}, IP Address: {IPAddress}, MAC Address: {MacAddress}", hostInformation.Name, hostInformation.IpAddress, hostInformation.MacAddress);
            Log.Information("Distinguished Name: CN={CommonName}, O={Organization}, OU={OrganizationalUnit}, L={Locality}, ST={State}, C={Country}", hostInformation.Name, hostConfiguration.Subject.Organization, hostConfiguration.Subject.OrganizationalUnit, hostConfiguration.Subject.Locality, hostConfiguration.Subject.State, hostConfiguration.Subject.Country);

            var hostServiceConfiguration = serviceConfigurationFactory.GetServiceConfiguration("RCHost");

            if (serviceManager.IsInstalled(hostServiceConfiguration.Name))
            {
                serviceManager.Stop(hostServiceConfiguration.Name);
                CopyToTargetPath(_applicationDirectory);
            }
            else
            {
                CopyToTargetPath(_applicationDirectory);
                serviceManager.Create(hostServiceConfiguration);
            }

            hostConfiguration.Host = hostInformation;

            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);
            
            serviceManager.Start(hostServiceConfiguration.Name);

            Log.Information("{ServiceName} installed and started successfully.", hostServiceConfiguration.Name);

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
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

            var hostServiceConfiguration = serviceConfigurationFactory.GetServiceConfiguration("RCHost");

            if (serviceManager.IsInstalled(hostServiceConfiguration.Name))
            {
                serviceManager.Stop(hostServiceConfiguration.Name);
                serviceManager.Delete(hostServiceConfiguration.Name);

                Log.Information("{ServiceName} Service uninstalled successfully.", hostServiceConfiguration.Name);
            }
            else
            {
                Log.Information("{ServiceName} Service is not installed.", hostServiceConfiguration.Name);
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

    private static void CopyToTargetPath(string targetDirectoryPath)
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

        var sourceDirectoryPath = Path.GetDirectoryName(Environment.ProcessPath)!;

        if (File.Exists(sourceDirectoryPath))
        {
            File.Copy(sourceDirectoryPath, targetDirectoryPath, true);
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