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
        Parameters.Add("modules-path", new LaunchParameter("Specifies the folder path where the program's modules will be installed.", false));
        Parameters.Add("username", new LaunchParameter("Specifies the username for authentication.", false));
        Parameters.Add("password", new LaunchParameter("Specifies the password for authentication.", false));
    }
}
