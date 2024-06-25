// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Shared.Dtos;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterInstanceService(IArgumentBuilderService argumentBuilderService, IInstanceStarterService instanceStarterService) : IUpdaterInstanceService
{
    private readonly string _sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");
    private readonly string _executablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");

    public void Start(UpdateRequest updateRequest)
    {
        ArgumentNullException.ThrowIfNull(updateRequest);

        var arguments = new Dictionary<string, object>
        {
            { "launch-mode", "updater" },
            { "folder-path", updateRequest.FolderPath },
            { "force", updateRequest.ForceUpdate },
            { "allow-downgrade", updateRequest.AllowDowngrade }
        };

        if (updateRequest.UserCredentials != null)
        {
            arguments["username"] = updateRequest.UserCredentials.Username;
            arguments["password"] = updateRequest.UserCredentials.Password;
        }

        var additionalArguments = argumentBuilderService.BuildArguments(arguments);

        var startInfo = new NativeProcessStartInfo(_executablePath, additionalArguments)
        {
            CreateNoWindow = true
        };

        try
        {
            instanceStarterService.StartNewInstance(_sourcePath, _executablePath, startInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {ExecutablePath}", _executablePath);
        }
    }
}
