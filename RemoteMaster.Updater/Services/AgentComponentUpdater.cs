// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Services;

public class AgentComponentUpdater : IComponentUpdater
{
    private readonly IServiceManager _serviceManager;

    protected const string SharedFolder = @"\\SERVER-DC02\Win\RemoteMaster";
    protected const string Login = "support@it-ktk.local";
    protected const string Password = "bonesgamer123!!";

    public string ComponentName => "Agent";

    public AgentComponentUpdater(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    public async Task<UpdateResponse> IsUpdateAvailableAsync()
    {
        var localExeFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Agent", "RemoteMaster.Agent.exe");
        var sharedExeFilePath = Path.Combine(SharedFolder, ComponentName, "RemoteMaster.Agent.exe");

        if (!File.Exists(localExeFilePath))
        {
            throw new FileNotFoundException($"Local file {localExeFilePath} does not exist");
        }

        var localVersionInfo = FileVersionInfo.GetVersionInfo(localExeFilePath);
        var localVersion = new Version(localVersionInfo.FileMajorPart, localVersionInfo.FileMinorPart, localVersionInfo.FileBuildPart, localVersionInfo.FilePrivatePart);

        var response = new UpdateResponse
        {
            ComponentName = ComponentName,
            CurrentVersion = localVersion,
            AvailableVersion = localVersion,
            IsUpdateAvailable = false
        };

        try
        {
            NetworkDriveHelper.MapNetworkDrive(SharedFolder, Login, Password);

            if (!File.Exists(sharedExeFilePath))
            {
                return response;
            }

            var remoteVersionInfo = FileVersionInfo.GetVersionInfo(sharedExeFilePath);
            var remoteVersion = new Version(remoteVersionInfo.FileMajorPart, remoteVersionInfo.FileMinorPart, remoteVersionInfo.FileBuildPart, remoteVersionInfo.FilePrivatePart);

            response.AvailableVersion = remoteVersion;
            response.IsUpdateAvailable = localVersion < remoteVersion;
        }
        finally
        {
            NetworkDriveHelper.CancelNetworkDrive(SharedFolder);
        }

        return response;
    }

    public async Task UpdateAsync()
    {
        _serviceManager.StopService();

        Thread.Sleep(30000);

        var sourceFolder = Path.Combine(SharedFolder, ComponentName);
        var destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", ComponentName);

        NetworkDriveHelper.MapNetworkDrive(SharedFolder, Login, Password);
        NetworkDriveHelper.DirectoryCopy(sourceFolder, destinationFolder, true, true);
        NetworkDriveHelper.CancelNetworkDrive(SharedFolder);

        _serviceManager.StartService();

        await Task.CompletedTask;
    }
}
