// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostInstaller(IHostInformationService hostInformationService, IHostConfigurationService hostConfigurationService, IServiceFactory serviceFactory, IHostLifecycleService hostLifecycleService, IFileSystem fileSystem) : IHostInstaller
{
    private readonly string _applicationDirectory = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");

    public async Task InstallAsync()
    {
        try
        {
            var hostInformation = hostInformationService.GetHostInformation();
            var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();

            Log.Information("Starting installation...");
            Log.Information("Server: {Server}", hostConfiguration.Server);
            Log.Information("Host Name: {HostName}, IP Address: {IPAddress}, MAC Address: {MacAddress}", hostInformation.Name, hostInformation.IpAddress, hostInformation.MacAddress);

            var organizationalUnits = string.Join(", ", hostConfiguration.Subject.OrganizationalUnit.Select(ou => $"OU={ou}"));
            Log.Information("Distinguished Name: CN={CommonName}, O={Organization}, {OrganizationalUnits}", hostInformation.Name, hostConfiguration.Subject.Organization, organizationalUnits);

            var hostService = serviceFactory.GetService("RCHost");

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

            hostConfiguration.Host = hostInformation;

            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

            Log.Information("{ServiceName} installed and started successfully.", hostService.Name);

            await hostLifecycleService.RegisterAsync();
            await hostLifecycleService.GetCaCertificateAsync();

            var organizationAddress = await hostLifecycleService.GetOrganizationAddressAsync(hostConfiguration);

            await hostLifecycleService.IssueCertificateAsync(hostConfiguration, organizationAddress);

            hostService.Start();
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred: {Message}", ex.Message);
        }
    }

    private void CopyToTargetPath(string targetDirectoryPath)
    {
        if (!fileSystem.Directory.Exists(targetDirectoryPath))
        {
            fileSystem.Directory.CreateDirectory(targetDirectoryPath);
        }

        var targetExecutablePath = fileSystem.Path.Combine(targetDirectoryPath, "RemoteMaster.Host.exe");

        try
        {
            fileSystem.File.Copy(Environment.ProcessPath!, targetExecutablePath, true);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to copy the executable to {targetExecutablePath}. Details: {ex.Message}", ex);
        }

        var sourceDirectoryPath = fileSystem.Path.GetDirectoryName(Environment.ProcessPath)!;

        if (fileSystem.File.Exists(sourceDirectoryPath))
        {
            fileSystem.File.Copy(sourceDirectoryPath, targetDirectoryPath, true);
        }
    }
}
