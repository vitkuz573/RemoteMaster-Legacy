// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Windows.Abstractions;

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
            var organization = configuration["organization"];
            var organizationalUnit = configuration["organizational-unit"];

            var modulesPath = configuration["modules-path"];
            var username = configuration["username"];
            var password = configuration["password"];

            await Task.Delay(2000, cancellationToken);

            await hostInstaller.InstallAsync(server, organization, organizationalUnit, modulesPath, username, password);

            Environment.Exit(0);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
