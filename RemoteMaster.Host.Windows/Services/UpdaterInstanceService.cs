// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterInstanceService(IInstanceManagerService instanceManagerService, IFileSystem fileSystem, ILogger<UpdaterInstanceService> logger) : IUpdaterInstanceService
{
    private readonly string _executablePath = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");

    public void Start(UpdateRequest updateRequest)
    {
        ArgumentNullException.ThrowIfNull(updateRequest);

        var argumentsBuilder = new StringBuilder();

        argumentsBuilder.Append($"--launch-mode=updater --folder-path={updateRequest.FolderPath}");

        if (updateRequest.UserCredentials != null)
        {
            argumentsBuilder.Append($" --username={updateRequest.UserCredentials.UserName} --password={updateRequest.UserCredentials.Password}");
        }

        if (updateRequest.ForceUpdate)
        {
            argumentsBuilder.Append(" --force");
        }

        if (updateRequest.AllowDowngrade)
        {
            argumentsBuilder.Append(" --allow-downgrade");
        }

        var startInfo = new NativeProcessStartInfo
        {
            Arguments = argumentsBuilder.ToString(),
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
