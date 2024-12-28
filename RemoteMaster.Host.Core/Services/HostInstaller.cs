// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class HostInstaller(ICertificateService certificateService, IHostInformationService hostInformationService, IHostConfigurationProvider hostConfigurationProvider, IHostConfigurationService hostConfigurationService, IServiceFactory serviceFactory, IHostLifecycleService hostLifecycleService, IFileSystem fileSystem, IFileService fileService, IApplicationPathProvider applicationPathProvider, ILogger<HostInstaller> logger) : IHostInstaller
{
    public async Task InstallAsync(HostInstallRequest installRequest)
    {
        ArgumentNullException.ThrowIfNull(installRequest);

        try
        {
            var applicationDirectory = applicationPathProvider.RootDirectory;
            var hostInformation = hostInformationService.GetHostInformation();

            logger.LogInformation("Starting installation...");
            logger.LogInformation("Server: {Server}", installRequest.Server);
            logger.LogInformation("Host Name: {HostName}, IP Address: {IPAddress}, MAC Address: {MacAddress}", hostInformation.Name, hostInformation.IpAddress, hostInformation.MacAddress);

            var subject = new SubjectDto(installRequest.Organization, installRequest.OrganizationalUnit);
            var hostConfiguration = new HostConfiguration(installRequest.Server, subject, hostInformation);

            hostConfigurationProvider.SetConfiguration(hostConfiguration);

            var organizationAddress = await hostLifecycleService.GetOrganizationAddressAsync(installRequest.Organization);
            var ous = string.Join(", ", installRequest.OrganizationalUnit.Select(ou => $"OU={ou}"));

            logger.LogInformation("Distinguished Name: CN={CommonName}, O={Organization}, {OrganizationalUnit}, L={Locality}, ST={State}, C={Country}", hostInformation.Name, installRequest.Organization, ous, organizationAddress.Locality, organizationAddress.State, organizationAddress.Country);

            var hostService = serviceFactory.GetService("RCHost");

            if (hostService.IsInstalled)
            {
                hostService.Stop();
                CopyToTargetPath(applicationDirectory);
            }
            else
            {
                CopyToTargetPath(applicationDirectory);
                hostService.Create();
            }

            var registrationSucceeded = await hostLifecycleService.RegisterAsync(installRequest.Force);

            if (!registrationSucceeded)
            {
                logger.LogError("Registration failed. Aborting installation workflow.");

                return;
            }

            await certificateService.GetCaCertificateAsync();

            await certificateService.IssueCertificateAsync(hostConfiguration, organizationAddress);

            await hostConfigurationService.SaveAsync(hostConfiguration);

            hostService.Start();

            logger.LogInformation("{ServiceName} installed and started successfully.", hostService.Name);
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred: {Message}", ex.Message);
        }
    }

    private void CopyToTargetPath(string targetDirectoryPath)
    {
        try
        {
            var sourceExecutablePath = Environment.ProcessPath!;
            var targetExecutablePath = fileSystem.Path.Combine(targetDirectoryPath, fileSystem.Path.GetFileName(sourceExecutablePath));

            fileService.CopyFile(sourceExecutablePath, targetExecutablePath, true);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to copy files to {TargetPath}. Details: {Error}", targetDirectoryPath, ex.Message);
        }
    }
}
