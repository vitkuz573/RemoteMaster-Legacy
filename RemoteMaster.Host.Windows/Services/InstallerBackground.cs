// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Services;

public class InstallerBackground(IConfiguration configuration, IHostApplicationLifetime hostApplicationLifetime, IHostInstaller hostInstaller) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        hostApplicationLifetime.ApplicationStarted.Register(Callback);

        return Task.CompletedTask;

        async void Callback()
        {
            var server = configuration["server"];

            if (server == null)
            {
                throw new ArgumentNullException(server, "Configuration 'server' cannot be null or empty");
            }

            var organization = configuration["organization"];

            if (organization == null)
            {
                throw new ArgumentNullException(organization, "Configuration 'organization' cannot be null or empty");
            }

            var organizationalUnit = configuration["organizational-unit"];

            if (organizationalUnit == null)
            {
                throw new ArgumentNullException(organizationalUnit, "Configuration 'organizational-unit' cannot be null or empty");
            }

            var modulesPath = configuration["modules-path"];
            var username = configuration["username"];
            var password = configuration["password"];

            await Task.Delay(2000, cancellationToken);

            var installRequest = new HostInstallRequest(server, organization, organizationalUnit)
            {
                ModulesPath = modulesPath,
            };

            if (username != null && password != null)
            {
                installRequest.UserCredentials = new Credentials(username, password);
            }

            await hostInstaller.InstallAsync(installRequest);

            Environment.Exit(0);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
