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

    public override string Description => "Reinstalls the program using the current configuration or specified parameters.";

    protected override void InitializeParameters()
    {
        AddParameter(new LaunchParameter<string>("server", "Specifies the server where the host will be registered. Overrides the current configuration.", false, "srv"));
        AddParameter(new LaunchParameter<string>("organization", "Specifies the name of the organization where the host is registered. Overrides the current configuration.", false, "org"));
        AddParameter(new LaunchParameter<string>("organizational-unit", "Specifies the organizational unit where the host is registered. Overrides the current configuration.", false, "ou"));
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var hostConfigurationService = serviceProvider.GetRequiredService<IHostConfigurationService>();
        var hostUninstaller = serviceProvider.GetRequiredService<IHostUninstaller>();
        var hostInstaller = serviceProvider.GetRequiredService<IHostInstaller>();

        var currentConfig = await hostConfigurationService.LoadConfigurationAsync();

        var server = GetParameter<string>("server").Value ?? currentConfig.Server;
        var organization = GetParameter<string>("organization").Value ?? currentConfig.Subject.Organization;
        var organizationalUnit = GetParameter<string>("organizational-unit").Value ?? currentConfig.Subject.OrganizationalUnit.FirstOrDefault();

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
