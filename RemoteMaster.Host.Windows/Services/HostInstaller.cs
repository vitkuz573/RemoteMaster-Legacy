// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostInstaller(IHostInformationService hostInformationService, IHostConfigurationService hostConfigurationService, IServiceFactory serviceFactory, IHostLifecycleService hostLifecycleService) : IHostInstaller
{
    private readonly string _applicationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");
    private readonly string _updaterDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Updater");

    public async Task InstallAsync()
    {
        try
        {
            var hostInformation = hostInformationService.GetHostInformation();
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();

            Log.Information("Starting installation...");
            Log.Information("Server: {Server}", hostConfiguration.Server);
            Log.Information("Host Name: {HostName}, IP Address: {IPAddress}, MAC Address: {MacAddress}", hostInformation.Name, hostInformation.IpAddress, hostInformation.MacAddress);
            Log.Information("Distinguished Name: CN={CommonName}, O={Organization}, OU={OrganizationalUnit}, L={Locality}, ST={State}, C={Country}", hostInformation.Name, hostConfiguration.Subject.Organization, hostConfiguration.Subject.OrganizationalUnit, hostConfiguration.Subject.Locality, hostConfiguration.Subject.State, hostConfiguration.Subject.Country);

            var hostService = serviceFactory.GetService("RCHost");
            var updaterService = serviceFactory.GetService("RCUpdater");

            if (hostService.IsInstalled)
            {
                hostService.Stop();
                CopyToTargetPath(_applicationDirectory);
            }
            else
            {
                CopyToTargetPath(_applicationDirectory);
                hostService.Create();
            }

            if (updaterService.IsInstalled)
            {
                updaterService.Stop();
                CopyToTargetPath(_updaterDirectory);
            }
            else
            {
                CopyToTargetPath(_updaterDirectory);
                updaterService.Create();
            }

            hostConfiguration.Host = hostInformation;

            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

            hostService.Start();

            Log.Information("{ServiceName} installed and started successfully.", hostService.Name);
            Log.Information("{ServiceName} installed successfully.", updaterService.Name);

            await hostLifecycleService.RegisterAsync(hostConfiguration);
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

        var targetExecutablePath = Path.Combine(targetDirectoryPath, "RemoteMaster.Host.exe");

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
}
