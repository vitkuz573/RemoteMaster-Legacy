// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Services;

public class HostInstaller(ICertificateService certificateService, IHostInformationService hostInformationService, IHostConfigurationService hostConfigurationService, IServiceFactory serviceFactory, IHostLifecycleService hostLifecycleService, IFileSystem fileSystem, ILogger<HostInstaller> logger) : IHostInstaller
{
    private readonly string _applicationDirectory = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");

    private const int MaxConnectionAttempts = 5;
    private const int ConnectionRetryDelay = 1000;

    public async Task InstallAsync(HostInstallRequest installRequest)
    {
        ArgumentNullException.ThrowIfNull(installRequest);

        if (!await IsServerAvailableAsync(installRequest.Server))
        {
            logger.LogWarning("The server {Server} is unavailable. Installation will not proceed.", installRequest.Server);

            return;
        }

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
                CopyToTargetPath(_applicationDirectory);
            }
            else
            {
                CopyToTargetPath(_applicationDirectory);
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

    private async Task<bool> IsServerAvailableAsync(string server)
    {
        for (var attempt = 1; attempt <= MaxConnectionAttempts; attempt++)
        {
            try
            {
                logger.LogInformation("Checking server availability, attempt {Attempt} of {MaxAttempts}...", attempt, MaxConnectionAttempts);

                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(server, 5254);

                logger.LogInformation("Server {Server} is available.", server);
                
                return true;
            }
            catch (SocketException)
            {
                logger.LogWarning("Attempt {Attempt} failed. Retrying in {RetryDelay}ms...", attempt, ConnectionRetryDelay);

                await Task.Delay(ConnectionRetryDelay);
            }
        }

        logger.LogError("Server {Server} is unavailable after {MaxAttempts} attempts.", server, MaxConnectionAttempts);

        return false;
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
