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
        AddParameter(new LaunchParameter<string>("folder-path", "Specifies the folder path for the update operation.", true, "path", "fp"));
        AddParameter(new LaunchParameter<string>("username", "Specifies the username for authentication.", false, "user"));
        AddParameter(new LaunchParameter<string>("password", "Specifies the password for authentication.", false, "pass"));
        AddParameter(new LaunchParameter<bool>("force", "Forces the update operation to proceed, even if no update is needed.", false, "f"));
        AddParameter(new LaunchParameter<bool>("allow-downgrade", "Allows the update operation to proceed with a lower version than the current one.", false, "downgrade", "ad"));
    }

    public async override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var folderPath = GetParameter<string>("folder-path").Value;
        var username = GetParameter<string>("username").Value;
        var password = GetParameter<string>("password").Value;

        var force = GetParameter<bool>("force").Value;
        var allowDowngrade = GetParameter<bool>("allow-downgrade").Value;

        var hostUpdater = serviceProvider.GetRequiredService<IHostUpdater>();

        await Task.Delay(2000);

        await hostUpdater.UpdateAsync(folderPath, username, password, force, allowDowngrade);

        Environment.Exit(0);
    }
}
