// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Helpers;
using RemoteMaster.Host.Models;

namespace RemoteMaster.Host.Services;

public class UpdaterService : IUpdaterService
{
    private const string SHARED_FOLDER = @"\\SERVER-DC02\Win\RemoteMaster";
    private const string LOGIN = "support@it-ktk.local";
    private const string PASSWORD = "bonesgamer123!!";

    private readonly HostServiceConfig _config;

    public string ScriptPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "update.bat");

    public UpdaterService(HostServiceConfig config)
    {
        _config = config;
    }

    public void Download()
    {
        var sourceFolder = Path.Combine(SHARED_FOLDER, "Host");
        var destinationFolder = Path.Combine(Environment.SpecialFolder.ProgramFiles.ToString(), "RemoteMaster", "Host", "Update");

        NetworkDriveHelper.MapNetworkDrive(SHARED_FOLDER, LOGIN, PASSWORD);
        NetworkDriveHelper.DirectoryCopy(sourceFolder, destinationFolder, true, true);
        NetworkDriveHelper.CancelNetworkDrive(SHARED_FOLDER);
    }

    public void CreateScript()
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine($"sc stop {_config.Name}");
        contentBuilder.AppendLine(@"xcopy /y /s "".\Update\*.*"" "".\*.*""");
        contentBuilder.AppendLine($"sc start {_config.Name}");

        File.WriteAllText(ScriptPath, contentBuilder.ToString());
    }
}
