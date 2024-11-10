// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.LaunchModes;

public class InstallMode : LaunchModeBase
{
    public override string Name => "Install";

    public override string Description => "Installs the necessary components for the program.";

    protected override void InitializeParameters()
    {
        AddParameter(new LaunchParameter<string>("server", "Specifies the server where the host will be registered.", true, "srv"));
        AddParameter(new LaunchParameter<string>("organization" ,"Specifies the name of the organization where the host is registered.", true, "org"));
        AddParameter(new LaunchParameter<string>("organizational-unit", "Specifies the organizational unit where the host is registered.", true, "ou"));
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var server = GetParameter<string>("server").Value;
        var organization = GetParameter<string>("organization").Value;
        var organizationalUnit = GetParameter<string>("organizational-unit").Value;

        var hostInstaller = serviceProvider.GetRequiredService<IHostInstaller>();
        var installRequest = new HostInstallRequest(server, organization, organizationalUnit);

        await hostInstaller.InstallAsync(installRequest);

        Environment.Exit(0);
    }
}
