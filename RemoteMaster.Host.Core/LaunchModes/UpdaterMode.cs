// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.LaunchModes;

public class UpdaterMode : LaunchModeBase
{
    public override string Name => "Updater";

    public override string Description => "Updates the program to the latest version.";

    protected override void InitializeParameters()
    {
        Parameters.Add("folder-path", new LaunchParameter("Specifies the folder path for the update operation.", true, "path", "fp"));
        Parameters.Add("username", new LaunchParameter("Specifies the username for authentication.", false, "user"));
        Parameters.Add("password", new LaunchParameter("Specifies the password for authentication.", false, "pass"));
        Parameters.Add("force", new LaunchParameter("Forces the update operation to proceed, even if no update is needed.", false, "f"));
        Parameters.Add("allow-downgrade", new LaunchParameter("Allows the update operation to proceed with a lower version than the current one.", false, "downgrade", "ad"));
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider)
    {
        var folderPath = GetParameterValue("folder-path");
        var username = GetParameterValue("username");
        var password = GetParameterValue("password");

        var force = bool.TryParse(GetParameterValue("force"), out var forceUpdate) && forceUpdate;
        var allowDowngrade = bool.TryParse(GetParameterValue("allow-downgrade"), out var allow) && allow;

        var hostUpdater = serviceProvider.GetRequiredService<IHostUpdater>();

        await Task.Delay(2000);

        await hostUpdater.UpdateAsync(folderPath, username, password, force, allowDowngrade);

        Environment.Exit(0);
    }
}
