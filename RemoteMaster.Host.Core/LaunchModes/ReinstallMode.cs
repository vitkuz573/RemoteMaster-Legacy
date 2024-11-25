// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.LaunchModes;

public class ReinstallMode : LaunchModeBase
{
    public override string Name => "Reinstall";

    public override string Description => "Reinstalls the program using the current configuration.";

    protected override void InitializeParameters()
    {
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var hostConfigurationService = serviceProvider.GetRequiredService<IHostConfigurationService>();
        var hostUninstaller = serviceProvider.GetRequiredService<IHostUninstaller>();
        var hostInstaller = serviceProvider.GetRequiredService<IHostInstaller>();

        var currentConfig = await hostConfigurationService.LoadConfigurationAsync();

        var server = currentConfig.Server;
        var organization = currentConfig.Subject.Organization;
        var organizationalUnit = currentConfig.Subject.OrganizationalUnit.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(organization) || string.IsNullOrWhiteSpace(organizationalUnit))
        {
            throw new InvalidOperationException("The configuration is incomplete or invalid.");
        }

        await hostUninstaller.UninstallAsync();

        var installRequest = new HostInstallRequest(server, organization, organizationalUnit);

        await hostInstaller.InstallAsync(installRequest);

        Environment.Exit(0);
    }
}
