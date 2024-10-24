// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class InstallMode : LaunchModeBase
{
    public override string Name => "Install";

    public override string Description => "Installs the necessary components for the program.";

    protected override void InitializeParameters()
    {
        Parameters.Add("server", new LaunchParameter("Specifies the server where the host will be registered.", false));
        Parameters.Add("organization", new LaunchParameter("Specifies the name of the organization where the host is registered.", false));
        Parameters.Add("organizational-unit", new LaunchParameter("Specifies the organizational unit where the host is registered.", false));
        Parameters.Add("username", new LaunchParameter("Specifies the username for authentication.", false));
        Parameters.Add("password", new LaunchParameter("Specifies the password for authentication.", false));
    }
}
