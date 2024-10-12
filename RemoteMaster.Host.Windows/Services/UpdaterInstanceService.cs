// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterInstanceService(IArgumentBuilderService argumentBuilderService, IInstanceManagerService instanceManagerService, ILogger<UpdaterInstanceService> logger) : IUpdaterInstanceService
{
    private readonly string _executablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");

    public void Start(UpdateRequest updateRequest)
    {
        ArgumentNullException.ThrowIfNull(updateRequest);

        var arguments = new Dictionary<string, object>
        {
            { "launch-mode", "updater" },
            { "folder-path", updateRequest.FolderPath },
            { "force", updateRequest.ForceUpdate.ToString().ToLower() },
            { "allow-downgrade", updateRequest.AllowDowngrade.ToString().ToLower() }
        };

        if (updateRequest.UserCredentials != null)
        {
            arguments["username"] = updateRequest.UserCredentials.UserName;
            arguments["password"] = updateRequest.UserCredentials.Password;
        }

        var additionalArguments = argumentBuilderService.BuildArguments(arguments);

        var startInfo = new NativeProcessStartInfo
        {
            Arguments = additionalArguments,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            instanceManagerService.StartNewInstance(_executablePath, startInfo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting new instance of the host. Executable path: {ExecutablePath}", _executablePath);
        }
    }
}
