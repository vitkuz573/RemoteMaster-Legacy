// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class UpdaterMode : LaunchModeBase
{
    public override string Name => "Updater";

    public override string Description => "Updates the program to the latest version.";

    protected override void InitializeParameters()
    {
        Parameters.Add("folder-path", new LaunchParameter("Specifies the folder path for the update operation.", true));
        Parameters.Add("username", new LaunchParameter("Specifies the username for authentication.", false));
        Parameters.Add("password", new LaunchParameter("Specifies the password for authentication.", false));
        Parameters.Add("force", new LaunchParameter("Forces the update operation to proceed, even if no update is needed.", false));
        Parameters.Add("allow-downgrade", new LaunchParameter("Allows the update operation to proceed with a lower version than the current one.", false));
    }
}
