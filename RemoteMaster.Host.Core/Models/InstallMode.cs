// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class InstallMode : LaunchModeBase
{
    public override string Name => "Install";

    public override string Description => "Installs the necessary components for the program.";

    protected override void InitializeParameters()
    {
        Parameters.Add("server", new LaunchParameter("Specifies the server where the host will be registered.", false, "srv"));
        Parameters.Add("organization", new LaunchParameter("Specifies the name of the organization where the host is registered.", false, "org"));
        Parameters.Add("organizational-unit", new LaunchParameter("Specifies the organizational unit where the host is registered.", false, "ou"));
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider)
    {
        var server = GetParameterValue("server");
        var organization = GetParameterValue("organization");
        var organizationalUnit = GetParameterValue("organizational-unit");

        var hostInstaller = serviceProvider.GetRequiredService<IHostInstaller>();
        var installRequest = new HostInstallRequest(server, organization, organizationalUnit);

        await hostInstaller.InstallAsync(installRequest);
    }
}
