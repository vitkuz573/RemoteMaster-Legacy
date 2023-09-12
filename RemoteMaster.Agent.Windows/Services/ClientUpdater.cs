// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Updater.Common;

namespace RemoteMaster.Agent.Services;

public class ClientUpdater : UpdaterBase, IClientUpdater
{
    private const string Folder = $"{SharedFolder}/Client";

    public ClientUpdater(ILogger<ClientUpdater> logger) : base(logger)
    {
    }

    public void Install()
    {
        MapNetworkDrive(SharedFolder, Login, Password);
        DirectoryCopy(Folder, $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster/Client");
        CancelNetworkDrive(SharedFolder);
    }

    public void Update()
    {
        var processes = Process.GetProcessesByName("RemoteMaster.Client");

        foreach (var client in processes)
        {
            client.Kill();
        }

        Thread.Sleep(10000);

        MapNetworkDrive(SharedFolder, Login, Password);
        DirectoryCopy(Folder, $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster", true, true);
        CancelNetworkDrive(SharedFolder);
    }
}
