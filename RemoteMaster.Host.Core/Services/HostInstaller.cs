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

public class HostInstaller(ICertificateService certificateService, IHostInformationService hostInformationService, IHostConfigurationService hostConfigurationService, IServiceFactory serviceFactory, IHostLifecycleService hostLifecycleService, IFileSystem fileSystem, IFileService fileService, IProcessService processService, ILogger<HostInstaller> logger) : IHostInstaller
{
    private readonly string _applicationDirectory = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");

    public async Task InstallAsync(HostInstallRequest installRequest)
    {
        ArgumentNullException.ThrowIfNull(installRequest);

        try
        {
            var hostInformation = hostInformationService.GetHostInformation();

            logger.LogInformation("Starting installation...");
            logger.LogInformation("Server: {Server}", installRequest.Server);
            logger.LogInformation("Host Name: {HostName}, IP Address: {IPAddress}, MAC Address: {MacAddress}", hostInformation.Name, hostInformation.IpAddress, hostInformation.MacAddress);
            logger.LogInformation("Distinguished Name: CN={CommonName}, O={Organization}, OU={OrganizationalUnit}", hostInformation.Name, installRequest.Organization, installRequest.OrganizationalUnit);

            var hostService = serviceFactory.GetService("RCHost");

            if (hostService.IsInstalled)
            {
                hostService.Stop();
                CopyToTargetPath();
            }
            else
            {
                CopyToTargetPath();
                hostService.Create();
            }

            var subject = new SubjectDto(installRequest.Organization, [installRequest.OrganizationalUnit]);

            var hostConfiguration = new HostConfiguration(installRequest.Server, subject, hostInformation);

            await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

            logger.LogInformation("{ServiceName} installed and started successfully.", hostService.Name);

            await hostLifecycleService.RegisterAsync();
            await certificateService.GetCaCertificateAsync();

            var organizationAddress = await hostLifecycleService.GetOrganizationAddressAsync(installRequest.Organization);

            await certificateService.IssueCertificateAsync(hostConfiguration, organizationAddress);

            hostService.Start();
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred: {Message}", ex.Message);
        }
    }

    private void CopyToTargetPath()
    {
        try
        {
            var sourceExecutablePath = processService.GetProcessPath();
            var targetExecutablePath = Path.Combine(_applicationDirectory, Path.GetFileName(sourceExecutablePath));

            fileService.CopyFile(sourceExecutablePath, targetExecutablePath, true);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to copy files to {TargetPath}. Details: {Error}", _applicationDirectory, ex.Message);
        }
    }
}
