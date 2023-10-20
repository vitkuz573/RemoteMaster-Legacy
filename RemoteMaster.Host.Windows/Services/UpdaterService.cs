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

    private readonly string _baseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host");
    private readonly string _scriptPath;
    private readonly string _updateFolderPath;

    private readonly HostServiceConfig _config;
    private readonly ILogger<UpdaterService> _logger;

    public UpdaterService(HostServiceConfig config, ILogger<UpdaterService> logger)
    {
        _scriptPath = Path.Combine(_baseFolderPath, "update.bat");
        _updateFolderPath = Path.Combine(_baseFolderPath, "Update");

        _config = config;
        _logger = logger;
    }

    public void Download()
    {
        var sourceFolder = Path.Combine(SHARED_FOLDER, "Host");

        NetworkDriveHelper.MapNetworkDrive(SHARED_FOLDER, LOGIN, PASSWORD);
        NetworkDriveHelper.DirectoryCopy(sourceFolder, _updateFolderPath, true, true);
        NetworkDriveHelper.CancelNetworkDrive(SHARED_FOLDER);
    }

    public void Execute()
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine($"sc stop {_config.Name}");
        contentBuilder.AppendLine(@"xcopy /y /s "".\Update\*.*"" "".\*.*""");
        contentBuilder.AppendLine($"sc start {_config.Name}");

        File.WriteAllText(_scriptPath, contentBuilder.ToString());

        var options = new ProcessStartOptions(_scriptPath, -1)
        {
            ForceConsoleSession = true,
            DesktopName = "default",
            HiddenWindow = true,
            UseCurrentUserToken = false
        };

        using var _ = new NativeProcess(options);
    }

    public void Clean()
    {
        if (File.Exists(_scriptPath))
        {
            File.Delete(_scriptPath);

            _logger.LogInformation("Updater script successfully deleted.");
        }
        else
        {
            _logger.LogInformation("Updater script doesn't exists");
        }

        if (Directory.Exists(_updateFolderPath))
        {
            Directory.Delete(_updateFolderPath, true);

            _logger.LogInformation("Update folder successfully deleted.");
        }
        else
        {
            _logger.LogInformation("Update folder doesn't exists");
        }
    }
}
